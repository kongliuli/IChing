using System.Text.Json;
using System.Text.Json.Serialization;
using IChing.Tarot.App.Models;

namespace IChing.Tarot.App.Services;

public static class PersonalityQuizCatalog
{
    public static readonly IReadOnlyList<PersonalityQuizListItem> All =
    [
        new("mbti-16", "十六型人格", "28 题 · IPIP 风格四维计分", 28),
        new("enneagram-9", "九型人格", "27 题 · 核心动机倾向", 27),
        new("holland-riasec", "霍兰德职业兴趣", "30 题 · RIASEC 六维", 30)
    ];

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip
    };

    public static Task<PersonalityQuizDefinition> LoadAsync(string id, CancellationToken ct = default)
    {
        if (PersonalityQuizBank.TryGet(id, out var builtIn))
        {
            return Task.FromResult(builtIn);
        }

        return LoadFromAssetAsync(id, ct);
    }

    private static async Task<PersonalityQuizDefinition> LoadFromAssetAsync(string id, CancellationToken ct)
    {
        var path = $"quizzes/{id}.json";
        await using var stream = await FileSystem.OpenAppPackageFileAsync(path);
        var quiz = await JsonSerializer.DeserializeAsync<PersonalityQuizDefinition>(stream, JsonOptions, ct)
                   ?? throw new InvalidOperationException($"无法解析测评：{id}");
        if (quiz.Questions.Count == 0)
        {
            throw new InvalidOperationException($"测评为空：{id}");
        }

        return quiz;
    }
}
