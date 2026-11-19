using System.Text.Json;
using Tenere.Core.Abstractions;

namespace Tenere.Data;

/// <summary>Loads per-application paste rules from SQLite.</summary>
public sealed class DatabaseAppRuleService : IAppRuleService
{
    private readonly IClipRepository _repository;

    public DatabaseAppRuleService(IClipRepository repository) => _repository = repository;

    public async Task<AppRule?> FindAsync(string processName, CancellationToken ct)
    {
        var rules = await _repository.GetAppRulesAsync(ct);
        var match = rules.FirstOrDefault(rule =>
            rule.Enabled &&
            processName.Contains(rule.ProcessName, StringComparison.OrdinalIgnoreCase));
        if (match is null) return null;

        string[] steps;
        try
        {
            steps = JsonSerializer.Deserialize<string[]>(match.ChainJson) ?? [];
        }
        catch (JsonException)
        {
            steps = [];
        }

        return new AppRule(
            match.ProcessName,
            steps.Select(step => new TransformSpec(step)).ToArray(),
            match.Enabled);
    }
}