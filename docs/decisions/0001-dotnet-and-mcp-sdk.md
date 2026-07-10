# ADR 0001: .NET and MCP SDK baseline

- Status: Accepted
- Date: 2026-07-10
- Related issue: #2

## Decision

Use **.NET 10 LTS** as the initial target framework and use the official **Model Context Protocol C# SDK**, beginning with package version **1.4.1**.

For the local stdio prototype, use the `ModelContextProtocol` package. Add `ModelContextProtocol.AspNetCore` only when the hosted HTTP gateway is implemented. Consider `ModelContextProtocol.Extensions.Apps` later for the interactive ChatGPT app experience.

## Rationale

- .NET 10 is the current active LTS release and is supported until November 14, 2028.
- .NET 8 reaches end of support in November 2026, making it a poor baseline for a new long-lived project.
- The `modelcontextprotocol/csharp-sdk` repository is the official C# SDK and is maintained in collaboration with Microsoft.
- The main `ModelContextProtocol` package includes hosting and dependency-injection support and is the recommended package for most non-HTTP MCP servers.
- Version 1.4.1 is the current stable release as of this decision date.

## Initial package plan

```xml
<TargetFramework>net10.0</TargetFramework>
<PackageReference Include="ModelContextProtocol" Version="1.4.1" />
```

Test framework and supporting package versions should be chosen during Issue #3 and pinned centrally.

## Constraints

- Do not use prerelease SDK packages without a documented reason.
- Keep package versions centrally managed.
- Apply current .NET patch updates in CI and release builds.
- Revisit this decision before the first public release and whenever the MCP SDK introduces a breaking major version.

## Sources checked

- Microsoft .NET support policy, updated June 9, 2026.
- Official Model Context Protocol C# SDK repository.
- NuGet package listing for `ModelContextProtocol` 1.4.1.