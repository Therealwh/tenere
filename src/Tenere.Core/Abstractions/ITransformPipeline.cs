namespace Tenere.Core.Abstractions;

public sealed record TransformSpec(string Kind, string? Parameters = null);

public interface ITransformPipeline
{
    string Apply(string input, IEnumerable<TransformSpec> steps);
}