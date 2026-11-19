using Tenere.Core.Abstractions;
using Tenere.Core.Services;
using Xunit;

namespace Tenere.UnitTests;

public class TransformPipelineTests
{
    private readonly ITransformPipeline _pipeline = new TransformPipeline();

    [Fact]
    public void TrimAndUpperCase_ApplyInOrder()
    {
        var result = _pipeline.Apply("  hello  ",
        [
            new TransformSpec("trim"),
            new TransformSpec("upper")
        ]);

        Assert.Equal("HELLO", result);
    }

    [Fact]
    public void StripUrlParams_RemovesTrackingParameters()
    {
        const string input = "See https://example.com/page?utm_source=mail&id=42#section";

        var result = _pipeline.Apply(input, [new TransformSpec("strip-url-params")]);

        Assert.Contains("https://example.com/page#section", result);
        Assert.DoesNotContain("utm_source", result);
    }

    [Fact]
    public void JsonPrettify_FormatsValidJson()
    {
        var result = _pipeline.Apply("{\"a\":1}", [new TransformSpec("json-prettify")]);

        Assert.Contains('\n', result);
        Assert.Contains("\"a\"", result);
    }

    [Fact]
    public void HtmlToText_RemovesTagsAndDecodesEntities()
    {
        var result = _pipeline.Apply("<p>Hello &amp; world</p>", [new TransformSpec("html-to-text")]);

        Assert.Equal("Hello & world", result);
    }

    [Fact]
    public void MarkdownTable_ConvertsTabSeparatedRows()
    {
        var result = _pipeline.Apply("a\tb\n1\t2", [new TransformSpec("markdown-table")]);

        Assert.Contains("| a | b |", result);
        Assert.Contains("| --- | --- |", result);
    }

    [Fact]
    public void UnknownStep_LeavesTextUnchanged()
    {
        const string input = "unchanged";
        Assert.Equal(input, _pipeline.Apply(input, [new TransformSpec("does-not-exist")]));
    }
}