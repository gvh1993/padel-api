# 15. Log every use case: who, what went in, what came out

- **Status:** Accepted
- **Date:** 2026-11-25

## Context

When something goes wrong, or when someone asks who changed a booking or a member, we need to know which use case ran, for whom, with which input and with which outcome. Before this decision, only failures were logged, with just the use case name and the error code; successes were logged at Debug, which is off by default.

## Decision

- A pipeline behavior, `LoggingBehavior`, logs every command and query. It runs between `TelemetryBehavior` and `ValidationBehavior`, so invalid input is logged too, and every entry carries the span's trace id.
- Each entry has:
  - the use case;
  - the caller: the account (`sub`), the linked member, and whether the caller is an admin (`anonymous` without a token);
  - the request as JSON;
  - the result as JSON, or the error code and description;
  - the duration.
- A page of results is summarised (count, ids, whether there is a next page) instead of logging up to 100 items each time.
- Levels: success and expected failures at Information, exceptions at Error with the request.
- The entries use their own category, `Padel.UseCases`, so they can be filtered, routed or kept separately as an audit trail. Logs written while the use case runs (EF Core, the handler) get the use case and the user as scope.
- JSON longer than 4 KB is truncated.
- **Personal data is logged as is.** We considered redacting it (with `[PersonalData]` attributes, or by masking every string by default) and decided against it: readable logs are worth more for this API than the extra protection.

## Consequences

- Every change and every read can be traced to an account, with its input and outcome.
- The logs contain personal data (names, email addresses, phone numbers, free-text reasons). Under the GDPR, the log store therefore needs the same care as the database: limited access, a retention period, and a processor agreement with any external log service. A member's data cannot be removed from the logs on request, which is one more reason to keep the retention short.
- If this changes, redaction can be added in `UseCaseLogSerializer` alone, because every entry is serialised there.
