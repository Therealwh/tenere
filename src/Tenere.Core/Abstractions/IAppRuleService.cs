namespace Tenere.Core.Abstractions;

public sealed record AppRule(string ProcessName, IReadOnlyList<TransformSpec> Chain, bool Enabled);

public interface IAppRuleService
{
    Task<AppRule?> FindAsync(string processName, CancellationToken ct);
}