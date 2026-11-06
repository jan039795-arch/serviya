---
paths:
  - "**/*.cs"
  - "**/*.csx"
---
# C# Patterns

> This file extends [common/patterns.md](../common/patterns.md) with C#-specific content.

## API Error Pattern

Return successful payloads directly and errors as RFC 9457 `ProblemDetails` (`Results.Problem`, `Results.ValidationProblem`, `AddProblemDetails()`). Do not wrap responses in a custom `ApiResponse<T>` envelope.

## Data Access

Use the module's `DbContext` directly from handlers or services; EF Core already implements unit of work and repository. Add a specific query service only when a query is reused or complex. Do not add a generic `IRepository<T>`.

## Options Pattern

Use strongly typed options for config instead of reading raw strings throughout the codebase.

```csharp
public sealed class PaymentsOptions
{
    public const string SectionName = "Payments";
    public required string BaseUrl { get; init; }
    public required string ApiKeySecretName { get; init; }
}
```

## Dependency Injection

- Depend on interfaces at service boundaries
- Keep constructors focused; if a service needs too many dependencies, split responsibilities
- Register lifetimes intentionally: singleton for stateless/shared services, scoped for request data, transient for lightweight pure workers
