# 4. Expected failures as result unions, not exceptions

- **Status:** Accepted
- **Date:** 2026-11-25

## Context

Many requests fail in expected ways: invalid input, a broken business rule, a missing resource, a conflict. Throwing exceptions for these hides the failure paths from method signatures, costs performance, and pushes the mapping to HTTP status codes into a catch-all handler.

## Decision

- Operations that can fail return `Result` or `Result<T>`, which are C# 15 union types: `union Result<T>(T, Error)`.
- `Error` has a fixed set of kinds: `ValidationError`, `NotFoundError`, `ConflictError`, `ForbiddenError`, `BusinessRuleError` and `PreconditionFailedError`. Each kind maps to one HTTP status code in `ResultExtensions.ToProblem`. The compiler checks that the `switch` covers every kind.
- Callers pattern-match: `if (court.SetMaintenance(true) is Error error) return error;`.
- Every error has a stable `Code` (for example `Member.EmailInUse`). It is returned as `errorCode` in the RFC 9457 problem details, so clients can rely on it.
- Exceptions remain for bugs and infrastructure failures. `ApiExceptionHandler` turns them into a 500 and logs them.

## Consequences

- Failure paths are visible in signatures and tested like any other return value (`ShouldFail().Is<ConflictError>()`).
- A new error kind needs a status code in `ToProblem`; the build fails until it has one.
- Union types are new in C# 15; contributors used to exceptions need a short introduction.
