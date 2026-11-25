# 9. Keycloak for authentication, members linked by verified email

- **Status:** Accepted
- **Date:** 2026-11-25

## Context

The API needs sign-in for admins and members, but should not store passwords or implement login flows. Members are registered by an admin before they have an account.

## Decision

- Keycloak (realm `padel`) is the identity provider. The API validates JWT bearer tokens. Admins have the realm role `admin`.
- An account is linked to a member the first time it signs in with that member's **verified** email address. From then on, the account (`sub`) identifies the member, even if the email changes (`MemberIdentityResolver`).
- The API adds the `member_id` claim itself (`MemberClaimsTransformation`). A `member_id` in the token is ignored, so the identity provider cannot assign members.
- Keycloak is not part of the readiness check. The API validates tokens with cached signing keys, so a short Keycloak outage does not take it out of service.

## Consequences

- No password handling in the API, and standard OpenID Connect for clients (Scalar signs in with the authorization code flow and PKCE).
- Without a verified email, an account cannot claim a member. That is intended: it prevents taking over a member by email.
- Local development and the integration tests run Keycloak in a container, with the same realm file.
