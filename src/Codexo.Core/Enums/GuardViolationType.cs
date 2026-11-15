namespace Codexo.Core.Enums;

public enum GuardViolationType
{
    None = 0,
    EmptyContent = 1,
    ContainsCodeBlock = 2,
    ContainsCompilableCode = 3,
    ContainsFullSolution = 4,
    SpoilerAtEarlyLevel = 5,
    MalformedJson = 6
}

