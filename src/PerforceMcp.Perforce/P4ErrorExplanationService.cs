using System.Text;
using System.Text.RegularExpressions;

namespace PerforceMcp.Perforce;

public static partial class P4ErrorExplanationService
{
    public const int MaximumErrorTextCharacters = 4096;
    public const int MaximumErrorCodeCharacters = 64;
    public const int MaximumItemsPerSection = 5;

    private static readonly Dictionary<string, ErrorCategory> KnownCodes =
        new Dictionary<string, ErrorCategory>(StringComparer.Ordinal)
        {
            ["AUTHREQUIRED"] = ErrorCategory.Authentication,
            ["MISSINGLOGIN"] = ErrorCategory.Authentication,
            ["SERVERUNREACHABLE"] = ErrorCategory.Connection,
            ["UNREACHABLESERVER"] = ErrorCategory.Connection,
            ["CLIENTNOTCONFIGURED"] = ErrorCategory.Workspace,
            ["MISSINGCLIENT"] = ErrorCategory.Workspace,
            ["PERMISSIONDENIED"] = ErrorCategory.Permission,
            ["RESOLVEREQUIRED"] = ErrorCategory.ResolveRequired,
            ["EXCLUSIVELOCK"] = ErrorCategory.ExclusiveLock,
            ["STORAGEEXHAUSTED"] = ErrorCategory.Storage,
            ["SERVERUNAVAILABLE"] = ErrorCategory.ServerAvailability,
            ["TIMEOUT"] = ErrorCategory.ServerAvailability,
            ["TIMEDOUT"] = ErrorCategory.ServerAvailability,
            ["COMMANDFAILED"] = ErrorCategory.Unknown,
            ["NONZEROEXIT"] = ErrorCategory.Unknown,
            ["MALFORMEDOUTPUT"] = ErrorCategory.Unknown,
            ["INVALIDREQUEST"] = ErrorCategory.Unknown,
            ["MISSINGFILE"] = ErrorCategory.Unknown,
            ["EXECUTABLEUNAVAILABLE"] = ErrorCategory.Unknown,
            ["P4NOTFOUND"] = ErrorCategory.Unknown,
            ["STARTFAILED"] = ErrorCategory.Unknown,
        };

    public static P4ErrorExplanationResult Explain(string? errorText = null, string? errorCode = null)
    {
        bool hasText = !string.IsNullOrWhiteSpace(errorText);
        bool hasCode = !string.IsNullOrWhiteSpace(errorCode);

        if (!hasText && !hasCode)
        {
            return P4ErrorExplanationResult.Failure(
                P4ErrorExplanationRequestErrorCode.InvalidRequest,
                "Provide errorText, errorCode, or both.");
        }

        if (errorText?.Length > MaximumErrorTextCharacters || errorCode?.Length > MaximumErrorCodeCharacters)
        {
            return P4ErrorExplanationResult.Failure(
                P4ErrorExplanationRequestErrorCode.InputTooLarge,
                $"errorText is limited to {MaximumErrorTextCharacters} characters and errorCode to {MaximumErrorCodeCharacters} characters.");
        }

        var categories = new HashSet<ErrorCategory>();
        var textCategories = new HashSet<ErrorCategory>();
        ErrorCategory? structuredCategory = null;
        if (hasCode)
        {
            string normalizedSourceCode = NormalizeSourceCode(errorCode!);
            if (!KnownCodes.TryGetValue(normalizedSourceCode, out ErrorCategory knownCategory))
            {
                return P4ErrorExplanationResult.Failure(
                    P4ErrorExplanationRequestErrorCode.UnknownErrorCode,
                    "errorCode is not a recognized Perforce Companion error code.");
            }

            structuredCategory = knownCategory;
            if (knownCategory != ErrorCategory.Unknown)
            {
                categories.Add(knownCategory);
            }
        }

        if (hasText)
        {
            AddTextCategories(errorText!, textCategories);
            categories.UnionWith(textCategories);
        }

        bool instructionLikeContentIgnored = hasText && InstructionLikeContentRegex().IsMatch(errorText!);
        List<string> redactionIndicators = hasText ? DetectSensitiveContent(errorText!) : [];
        bool redactionOccurred = redactionIndicators.Count > 0;
        bool isAmbiguous = categories.Count > 1;
        ErrorCategory category = categories.Count == 0 || isAmbiguous ? ErrorCategory.Unknown : categories.Single();

        if (isAmbiguous)
        {
            category = ErrorCategory.Ambiguous;
        }

        Classification classification = GetClassification(category);
        var observedFacts = new List<string>(MaximumItemsPerSection);

        if (hasCode)
        {
            observedFacts.Add(structuredCategory == ErrorCategory.Unknown
                ? "A recognized generic structured error code was supplied."
                : "A recognized structured error code identified a specific category.");
        }

        if (hasText)
        {
            observedFacts.Add(textCategories.Count switch
            {
                0 => "The supplied text did not match a specific supported error category.",
                1 => "The supplied text contained indicators for one supported error category.",
                _ => "The supplied information contained indicators for multiple supported error categories.",
            });
        }

        if (redactionOccurred)
        {
            observedFacts.Add("Sensitive-looking content was detected and omitted from the response.");
        }

        if (instructionLikeContentIgnored)
        {
            observedFacts.Add("Instruction-like content was treated only as untrusted error data and ignored.");
        }

        P4ErrorExplanationConfidence confidence = isAmbiguous || category == ErrorCategory.Unknown
            ? P4ErrorExplanationConfidence.Low
            : structuredCategory is not null and not ErrorCategory.Unknown
                ? P4ErrorExplanationConfidence.High
                : P4ErrorExplanationConfidence.Medium;

        return new P4ErrorExplanationResult(
            classification.Category,
            classification.Code,
            classification.Summary,
            observedFacts.Take(MaximumItemsPerSection).ToArray(),
            classification.PossibleCauses.Take(MaximumItemsPerSection).ToArray(),
            classification.SafeNextSteps.Take(MaximumItemsPerSection).ToArray(),
            confidence,
            isAmbiguous,
            redactionOccurred,
            redactionIndicators.Take(MaximumItemsPerSection).ToArray(),
            instructionLikeContentIgnored,
            null);
    }

    private static void AddTextCategories(string text, HashSet<ErrorCategory> categories)
    {
        AddWhenMatched(text, categories, ErrorCategory.Authentication,
            "password invalid", "p4passwd invalid", "p4passwd unset", "please login", "login required", "session has expired",
            "ticket expired", "not logged in", "authentication failed");
        if (text.Contains("p4passwd", StringComparison.OrdinalIgnoreCase) &&
            ContainsAny(text, "invalid", "unset", "expired"))
        {
            categories.Add(ErrorCategory.Authentication);
        }

        AddWhenMatched(text, categories, ErrorCategory.Connection,
            "connect to server failed", "tcp connect", "connection refused", "host not found",
            "name resolution", "network is unreachable", "could not connect");
        AddWhenMatched(text, categories, ErrorCategory.Workspace,
            "client unknown", "unknown client", "no client name", "client not found",
            "workspace unknown", "not under client's root", "not in client view");
        if (UnknownClientRegex().IsMatch(text))
        {
            categories.Add(ErrorCategory.Workspace);
        }

        AddWhenMatched(text, categories, ErrorCategory.Permission,
            "no permission", "permission denied", "protections table", "not permitted", "access denied",
            "don't have permission", "do not have permission");
        AddWhenMatched(text, categories, ErrorCategory.ResolveRequired,
            "must resolve", "resolve required", "unresolved file", "needs resolve", "outstanding resolves");
        AddWhenMatched(text, categories, ErrorCategory.ExclusiveLock,
            "exclusive file already opened", "can't edit exclusive", "already opened for edit by",
            "locked by", "exclusive lock");
        AddWhenMatched(text, categories, ErrorCategory.Storage,
            "no space left", "disk full", "insufficient disk space", "out of disk space",
            "not enough space", "storage quota");
        AddWhenMatched(text, categories, ErrorCategory.ServerAvailability,
            "server too busy", "server is too busy", "server unavailable", "service unavailable", "overloaded",
            "try again later", "maximum users");
    }

    private static void AddWhenMatched(
        string text,
        HashSet<ErrorCategory> categories,
        ErrorCategory category,
        params string[] indicators)
    {
        if (indicators.Any(indicator => text.Contains(indicator, StringComparison.OrdinalIgnoreCase)))
        {
            categories.Add(category);
        }
    }

    private static bool ContainsAny(string text, params string[] indicators) =>
        indicators.Any(indicator => text.Contains(indicator, StringComparison.OrdinalIgnoreCase));

    private static List<string> DetectSensitiveContent(string text)
    {
        var indicators = new List<string>(MaximumItemsPerSection);
        AddIndicatorWhenMatched(CredentialRegex(), text, indicators,
            "A credential-like value was present and was redacted.");
        AddIndicatorWhenMatched(EmbeddedCredentialRegex(), text, indicators,
            "A connection string or embedded credential was present and was redacted.");
        AddIndicatorWhenMatched(SensitiveArgumentRegex(), text, indicators,
            "Sensitive command arguments were present and were redacted.");
        AddIndicatorWhenMatched(UserIdentifierRegex(), text, indicators,
            "A username-like value was present and was redacted.");
        AddIndicatorWhenMatched(LocalPathRegex(), text, indicators,
            "A local path was present and was redacted.");
        return indicators;
    }

    private static void AddIndicatorWhenMatched(
        Regex regex,
        string text,
        List<string> indicators,
        string indicator)
    {
        if (indicators.Count < MaximumItemsPerSection && regex.IsMatch(text))
        {
            indicators.Add(indicator);
        }
    }

    private static string NormalizeSourceCode(string errorCode)
    {
        var normalized = new StringBuilder(errorCode.Length);
        foreach (char character in errorCode)
        {
            if (char.IsAsciiLetterOrDigit(character))
            {
                normalized.Append(char.ToUpperInvariant(character));
            }
        }

        return normalized.ToString();
    }

    private static Classification GetClassification(ErrorCategory category) => category switch
    {
        ErrorCategory.Authentication => new(
            "authentication",
            "AUTH_REQUIRED",
            "Perforce authentication is required or may have expired.",
            ["The login session may be missing or expired.", "The supplied credential may no longer be valid."],
            ["Verify login status using an approved read-only connection check.", "Sign in through the normal trusted Perforce client if authentication is required."]),
        ErrorCategory.Connection => new(
            "connection",
            "SERVER_UNREACHABLE",
            "The configured Perforce server could not be reached.",
            ["The server address may be unavailable.", "A network, VPN, DNS, or firewall path may be interrupted."],
            ["Verify the configured server address without sharing credentials.", "Check network or VPN connectivity, then retry a read-only connection check."]),
        ErrorCategory.Workspace => new(
            "workspace",
            "CLIENT_NOT_CONFIGURED",
            "The Perforce client/workspace is missing, unknown, or does not map the requested path.",
            ["The selected workspace may not exist for this connection.", "The requested path may be outside the workspace view."],
            ["Verify the active client/workspace name.", "Inspect the workspace mapping in a trusted Perforce client without changing it."]),
        ErrorCategory.Permission => new(
            "permission",
            "PERMISSION_DENIED",
            "Perforce denied access to the requested operation or data.",
            ["The current user may not have the required protection entry.", "The requested depot path may be outside the user's authorized scope."],
            ["Confirm which read operation and depot scope were denied.", "Ask a Perforce administrator to verify protections without sending credentials or tickets."]),
        ErrorCategory.ResolveRequired => new(
            "resolve",
            "RESOLVE_REQUIRED",
            "One or more files require a Perforce resolve before the intended workflow can continue.",
            ["The workspace may contain outstanding content or move conflicts.", "A prior sync, merge, or unshelve may have introduced unresolved files."],
            ["Inspect the pending resolves using an approved read-only status view.", "Review each conflict in P4V or another trusted client before choosing a resolve action."]),
        ErrorCategory.ExclusiveLock => new(
            "exclusive_lock",
            "EXCLUSIVE_LOCK",
            "A file may already be exclusively opened or locked by another workspace.",
            ["The file type may require exclusive open.", "Another user or workspace may currently hold the exclusive open state."],
            ["Check the file's open status and reported owner using a read-only tool.", "Coordinate with the reported owner; do not force-unlock or revert as a first step."]),
        ErrorCategory.Storage => new(
            "storage",
            "STORAGE_EXHAUSTED",
            "A local or server storage limit may have been reached.",
            ["The local workspace volume may be full.", "The Perforce server or a configured quota may lack available storage."],
            ["Check available storage through the normal operating-system or administrator interface.", "Determine whether the message refers to local or server storage before retrying."]),
        ErrorCategory.ServerAvailability => new(
            "server_availability",
            "SERVER_UNAVAILABLE",
            "The Perforce service may be temporarily unavailable, overloaded, or slow.",
            ["The service may be under load or maintenance.", "A transient server-side resource limit may have been reached."],
            ["Wait briefly and retry an approved read-only connection check.", "If the condition persists, ask the server operator to check service health using the opaque error time and code."]),
        ErrorCategory.Ambiguous => new(
            "ambiguous",
            "AMBIGUOUS_ERROR",
            "The supplied information matches more than one supported error category.",
            ["Multiple independent failures may be present.", "Generic wording may overlap several Perforce failure modes."],
            ["Verify connection, login, and workspace status separately with approved read-only checks.", "Use the most specific structured error code available before taking corrective action."]),
        _ => new(
            "unknown",
            "UNKNOWN_ERROR",
            "The error could not be classified safely from the supplied information.",
            ["The message may be incomplete or use an unsupported server-specific wording.", "A generic structured error may need more bounded context."],
            ["Retry the relevant read-only status tool and use its structured error code if available.", "Record only a short redacted description and ask an administrator to verify server-side details."]),
    };

    [GeneratedRegex(
        @"(?i)\b(?:P4PASSWD|password|passwd|ticket|authorization|api[_-]?key|access[_-]?token)\b\s*[:=]\s*[^\s;,]+|\bbearer\s+[^\s;,]+|\bP4PASSWD\b",
        RegexOptions.CultureInvariant | RegexOptions.NonBacktracking)]
    private static partial Regex CredentialRegex();

    [GeneratedRegex(
        @"(?i)\bclient\s+(?:'[^'\r\n]{1,128}'|""[^""\r\n]{1,128}""|[^\s\r\n]{1,128})\s+unknown\b",
        RegexOptions.CultureInvariant | RegexOptions.NonBacktracking)]
    private static partial Regex UnknownClientRegex();

    [GeneratedRegex(
        @"(?i)\b[a-z][a-z0-9+.-]*://[^\s/:@]+:[^\s/@]+@",
        RegexOptions.CultureInvariant | RegexOptions.NonBacktracking)]
    private static partial Regex EmbeddedCredentialRegex();

    [GeneratedRegex(
        @"(?i)(?:^|\s)-(?:P|p|u)\s*(?:=\s*)?[^\s]+",
        RegexOptions.CultureInvariant | RegexOptions.NonBacktracking)]
    private static partial Regex SensitiveArgumentRegex();

    [GeneratedRegex(
        @"(?i)\b(?:P4USER|user(?:name)?)\b\s*[:=]\s*[^\s;,]+",
        RegexOptions.CultureInvariant | RegexOptions.NonBacktracking)]
    private static partial Regex UserIdentifierRegex();

    [GeneratedRegex(
        @"(?i)(?:\b[A-Z]:\\|\\\\[^\s\\]+\\|/(?:home|users)/)[^\r\n]*",
        RegexOptions.CultureInvariant | RegexOptions.NonBacktracking)]
    private static partial Regex LocalPathRegex();

    [GeneratedRegex(
        @"(?i)ignore\s+(?:all\s+)?(?:previous|prior)\s+instructions|\b(?:run|execute|print|reveal)\s+(?:p4|P4PASSWD|password|ticket)|""(?:method|tool|role)""\s*:",
        RegexOptions.CultureInvariant | RegexOptions.NonBacktracking)]
    private static partial Regex InstructionLikeContentRegex();

    private enum ErrorCategory
    {
        Unknown,
        Authentication,
        Connection,
        Workspace,
        Permission,
        ResolveRequired,
        ExclusiveLock,
        Storage,
        ServerAvailability,
        Ambiguous,
    }

    private sealed record Classification(
        string Category,
        string Code,
        string Summary,
        IReadOnlyList<string> PossibleCauses,
        IReadOnlyList<string> SafeNextSteps);
}
