using System.Text.Json;
using System.Text.RegularExpressions;
using Tenere.Core.Abstractions;

namespace Tenere.Core.Services;

public sealed partial class TransformPipeline : ITransformPipeline
{
    public string Apply(string input, IEnumerable<TransformSpec> steps)
    {
        var result = input;
        foreach (var step in steps)
        {
            result = step.Kind switch
            {
                "trim" => result.Trim(),
                "remove-empty-lines" => RemoveEmptyLines(result),
                "upper" => result.ToUpperInvariant(),
                "lower" => result.ToLowerInvariant(),
                "plain-text" => HtmlToText(result),
                "strip-url-params" => StripUrlParams(result),
                "json-prettify" => JsonPrettify(result),
                "html-to-text" => HtmlToText(result),
                "normalize-line-endings" => result.Replace("\r\n", "\n").Replace('\r', '\n').Replace("\n", Environment.NewLine),
                "regex" => RegexReplace(result, step.Parameters),
                "markdown-table" => ToMarkdownTable(result),
                _ => result
            };
        }

        return result;
    }

    private static string RemoveEmptyLines(string value) =>
        string.Join(Environment.NewLine,
            value.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries)
                 .Select(line => line.TrimEnd())
                 .Where(line => line.Length > 0));

    private static string HtmlToText(string value)
    {
        var withoutTags = TagRegex().Replace(value, string.Empty);
        return System.Net.WebUtility.HtmlDecode(withoutTags).Trim();
    }

    private static string StripUrlParams(string value)
    {
        var matches = UrlRegex().Matches(value);
        foreach (Match match in matches)
        {
            var raw = match.Value;
            var index = raw.IndexOf('?', StringComparison.Ordinal);
            if (index < 0) continue;
            var clean = raw[..index];
            var fragmentIndex = raw.IndexOf('#', index);
            if (fragmentIndex >= 0) clean += raw[fragmentIndex..];
            value = value.Replace(raw, clean, StringComparison.Ordinal);
        }
        return value;
    }

    private static string JsonPrettify(string value)
    {
        try
        {
            using var document = JsonDocument.Parse(value);
            return JsonSerializer.Serialize(document, new JsonSerializerOptions { WriteIndented = true });
        }
        catch (JsonException)
        {
            return value;
        }
    }

    private static string RegexReplace(string value, string? parameters)
    {
        if (string.IsNullOrWhiteSpace(parameters)) return value;
        try
        {
            var parts = parameters.Split('\u001F', 2);
            return parts.Length == 2
                ? Regex.Replace(value, parts[0], parts[1])
                : value;
        }
        catch (ArgumentException)
        {
            return value;
        }
    }

    private static string ToMarkdownTable(string value)
    {
        var lines = value.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries)
                         .Select(line => line.Trim())
                         .Where(line => line.Contains('\t') || line.Contains(';'))
                         .ToList();
        if (lines.Count == 0) return value;

        var separator = lines[0].Contains('\t') ? '\t' : ';';
        var rows = lines.Select(line => line.Split(separator).Select(cell => cell.Trim()).ToArray()).ToList();
        var width = rows.Max(row => row.Length);
        var sb = new System.Text.StringBuilder();
        sb.AppendLine("| " + string.Join(" | ", PadRow(rows[0], width)) + " |");
        sb.AppendLine("|" + string.Join("|", Enumerable.Repeat(" --- ", width)) + "|");
        foreach (var row in rows.Skip(1))
            sb.AppendLine("| " + string.Join(" | ", PadRow(row, width)) + " |");
        return sb.ToString().TrimEnd();
    }

    private static IEnumerable<string> PadRow(string[] row, int width)
    {
        for (var i = 0; i < width; i++)
            yield return i < row.Length ? row[i] : string.Empty;
    }

    [GeneratedRegex(@"<[^>]+>")]
    private static partial Regex TagRegex();

    [GeneratedRegex(@"https?://[^\s<>""]+")]
    private static partial Regex UrlRegex();
}