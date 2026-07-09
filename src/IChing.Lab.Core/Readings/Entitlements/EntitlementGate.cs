namespace IChing.Lab.Core.Readings.Entitlements;

/// <summary>
/// 娱乐 AI 远期门槛（会员/每日次数）；现阶段测评无 AI，此桩供后续接入。
/// </summary>
public static class EntitlementGate
{
    public static EntitlementDecision CheckEntertainmentAi(string? userId, string featureId)
    {
        if (string.IsNullOrWhiteSpace(userId))
        {
            return EntitlementDecision.Deny("login_required");
        }

        // ponytail: 未接 Accounts 会员体系前默认放行本地娱乐 AI 实验
        return EntitlementDecision.Allow();
    }
}

public readonly record struct EntitlementDecision(bool Allowed, string? Reason)
{
    public static EntitlementDecision Allow() => new(true, null);
    public static EntitlementDecision Deny(string reason) => new(false, reason);
}
