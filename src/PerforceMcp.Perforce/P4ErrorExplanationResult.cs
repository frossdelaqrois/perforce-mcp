namespace PerforceMcp.Perforce;

public enum P4ErrorExplanationConfidence
{
    None,
    Low,
    Medium,
    High,
}

public enum P4ErrorExplanationRequestErrorCode
{
    InvalidRequest,
    InputTooLarge,
    UnknownErrorCode,
}

public sealed record P4ErrorExplanationRequestError(
    P4ErrorExplanationRequestErrorCode Code,
    string Message);

public sealed record P4ErrorExplanationResult(
    string Category,
    string NormalizedCode,
    string Summary,
    IReadOnlyList<string> ObservedFacts,
    IReadOnlyList<string> PossibleCauses,
    IReadOnlyList<string> SafeNextSteps,
    P4ErrorExplanationConfidence Confidence,
    bool IsAmbiguous,
    bool RedactionOccurred,
    IReadOnlyList<string> RedactionIndicators,
    bool InstructionLikeContentIgnored,
    P4ErrorExplanationRequestError? Error)
{
    public bool IsSuccess => Error is null;

    internal static P4ErrorExplanationResult Failure(
        P4ErrorExplanationRequestErrorCode code,
        string message) =>
        new(
            "invalid_request",
            code == P4ErrorExplanationRequestErrorCode.InputTooLarge
                ? "INPUT_TOO_LARGE"
                : code == P4ErrorExplanationRequestErrorCode.UnknownErrorCode
                    ? "UNKNOWN_ERROR_CODE"
                    : "INVALID_REQUEST",
            "The error explanation request is invalid.",
            [],
            [],
            [],
            P4ErrorExplanationConfidence.None,
            false,
            false,
            [],
            false,
            new P4ErrorExplanationRequestError(code, message));
}
