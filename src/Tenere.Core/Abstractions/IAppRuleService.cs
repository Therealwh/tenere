using Tenere.Core.Abstractions;

namespace Tenere.Core.Abstractions;

public sealed record AppRule(string ProcessName, IReadOnlyList<TransformSpec> Chain, bool Enabled);

public interface IAppRuleService
{
    AppRule? Find(string processName);
}