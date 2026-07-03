using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;

namespace IChing.Desktop;

public sealed record DesktopSettings(string BaseUrl, string Model, string? ApiKey)
{
    public static DesktopSettings Default => new("https://api.openai.com/v1", "gpt-4.1-mini", null);
}

public static class DesktopSettingsStore
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true
    };

    public static string SettingsPath =>
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "IChingDesktop", "settings.json");

    public static DesktopSettings Load()
    {
        if (!File.Exists(SettingsPath))
        {
            return DesktopSettings.Default;
        }

        var dto = JsonSerializer.Deserialize<SettingsDto>(File.ReadAllText(SettingsPath), JsonOptions);
        if (dto is null)
        {
            return DesktopSettings.Default;
        }

        return new DesktopSettings(
            string.IsNullOrWhiteSpace(dto.BaseUrl) ? DesktopSettings.Default.BaseUrl : dto.BaseUrl,
            string.IsNullOrWhiteSpace(dto.Model) ? DesktopSettings.Default.Model : dto.Model,
            Unprotect(dto.ApiKeyProtected));
    }

    public static void Save(DesktopSettings settings)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(SettingsPath)!);
        var dto = new SettingsDto(settings.BaseUrl, settings.Model, Protect(settings.ApiKey));
        File.WriteAllText(SettingsPath, JsonSerializer.Serialize(dto, JsonOptions));
    }

    private static string? Protect(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        try
        {
            return Convert.ToBase64String(Dpapi.Protect(Encoding.UTF8.GetBytes(value)));
        }
        catch
        {
            return null;
        }
    }

    private static string? Unprotect(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        try
        {
            return Encoding.UTF8.GetString(Dpapi.Unprotect(Convert.FromBase64String(value)));
        }
        catch
        {
            return null;
        }
    }

    private sealed record SettingsDto(string BaseUrl, string Model, string? ApiKeyProtected);
}

internal static class Dpapi
{
    public static byte[] Protect(byte[] data) => Crypt(data, protect: true);

    public static byte[] Unprotect(byte[] data) => Crypt(data, protect: false);

    private static byte[] Crypt(byte[] data, bool protect)
    {
        var input = DataBlob.FromManaged(data);
        var output = new DataBlob();
        try
        {
            var ok = protect
                ? CryptProtectData(ref input, null, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero, 0, ref output)
                : CryptUnprotectData(ref input, null, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero, 0, ref output);
            if (!ok)
            {
                throw new InvalidOperationException("Windows user encryption failed.");
            }

            var result = new byte[output.Length];
            Marshal.Copy(output.Data, result, 0, result.Length);
            return result;
        }
        finally
        {
            input.FreeManaged();
            output.FreeCrypt();
        }
    }

    [DllImport("crypt32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    private static extern bool CryptProtectData(
        ref DataBlob dataIn,
        string? description,
        IntPtr optionalEntropy,
        IntPtr reserved,
        IntPtr promptStruct,
        int flags,
        ref DataBlob dataOut);

    [DllImport("crypt32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    private static extern bool CryptUnprotectData(
        ref DataBlob dataIn,
        string? description,
        IntPtr optionalEntropy,
        IntPtr reserved,
        IntPtr promptStruct,
        int flags,
        ref DataBlob dataOut);

    [DllImport("kernel32.dll")]
    private static extern IntPtr LocalFree(IntPtr hMem);

    [StructLayout(LayoutKind.Sequential)]
    private struct DataBlob
    {
        public int Length;
        public IntPtr Data;

        public static DataBlob FromManaged(byte[] data)
        {
            var blob = new DataBlob
            {
                Length = data.Length,
                Data = Marshal.AllocHGlobal(data.Length)
            };
            Marshal.Copy(data, 0, blob.Data, data.Length);
            return blob;
        }

        public void FreeManaged()
        {
            if (Data == IntPtr.Zero)
            {
                return;
            }

            Marshal.FreeHGlobal(Data);
            Data = IntPtr.Zero;
            Length = 0;
        }

        public void FreeCrypt()
        {
            if (Data == IntPtr.Zero)
            {
                return;
            }

            _ = LocalFree(Data);
            Data = IntPtr.Zero;
            Length = 0;
        }
    }
}
