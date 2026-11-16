namespace Codexo.Api.IntegrationTests.AI;

using Codexo.Core.AI;
using Codexo.Core.Enums;
using Codexo.Infrastructure.AI;
using Xunit;

public class TolerantJsonParserTests
{
    [Fact]
    public void Parse_RawCleanJson_DeserializesSuccessfully()
    {
        var rawJson = """
        {
          "level": 1,
          "hint": "Understand the problem constraints and target.",
          "guidingQuestions": ["Are numbers positive?"],
          "concepts": [],
          "steps": [],
          "pitfalls": []
        }
        """;

        var result = TolerantJsonParser.Parse(rawJson, HintLevel.L1_Understand);

        Assert.Equal(HintLevel.L1_Understand, result.Level);
        Assert.Equal("Understand the problem constraints and target.", result.Hint);
        Assert.Single(result.GuidingQuestions);
        Assert.Empty(result.Concepts);
    }

    [Fact]
    public void Parse_MarkdownCodeFences_StripsFencesAndParses()
    {
        var rawOutput = """
        ```json
        {
          "level": 2,
          "hint": "Consider tracking visited values.",
          "guidingQuestions": ["How to store?"],
          "concepts": [],
          "steps": [],
          "pitfalls": []
        }
        ```
        """;

        var result = TolerantJsonParser.Parse(rawOutput, HintLevel.L2_Direction);

        Assert.Equal(HintLevel.L2_Direction, result.Level);
        Assert.Equal("Consider tracking visited values.", result.Hint);
    }

    [Fact]
    public void Parse_PreambleAndPostambleText_ExtractsFirstValidJsonObject()
    {
        var rawOutput = """
        Here is your requested hint:
        {
          "level": 3,
          "hint": "Use a Hash Map.",
          "guidingQuestions": [],
          "concepts": ["Hash Map"],
          "steps": [],
          "pitfalls": []
        }
        Hope this helps you learn!
        """;

        var result = TolerantJsonParser.Parse(rawOutput, HintLevel.L3_Concepts);

        Assert.Equal(HintLevel.L3_Concepts, result.Level);
        Assert.Equal("Use a Hash Map.", result.Hint);
        Assert.Single(result.Concepts);
    }

    [Fact]
    public void Parse_EmptyOrNoJson_ThrowsAiClientException()
    {
        Assert.Throws<AiClientException>(() => TolerantJsonParser.Parse("", HintLevel.L1_Understand));
        Assert.Throws<AiClientException>(() => TolerantJsonParser.Parse("I cannot give hints.", HintLevel.L1_Understand));
    }
}
