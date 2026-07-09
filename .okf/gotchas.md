---
type: Reference
title: Gotchas
description: Practical traps and non-obvious constraints for future agents.
tags: [gotchas, constraints]
---

# Gotchas

- `.okf/` may be referenced by `AGENTS.md`; keep it present and conformant.
- The solution file is `HelpDesk.slnx`, not traditional `.sln`; some tools need individual `.csproj` paths.
- Cross-service business behavior must not use REST/RPC or service implementation references; use contract events only.
- Contract projects must stay small: service name, event records, and simple DTOs only.
- `Common/` is not a shared domain layer; avoid moving service-specific behavior there.
- Events should be published only after local persistence succeeds. The registration/verification endpoints follow this pattern.
- Durable startup/offline remote events still require matching known subscriber IDs on both sides: process-local `c.SubscriberID = SubscriberService.Name` before subscriber `c.Subscribe<...>()` calls, and publisher contract `EventSubscribers` arrays passed to `RegisterEventHub<TEvent>(...)`. Keep the string literals identical to subscriber `Service.Name`.
- Tests live inside service web projects and are excluded from Release builds; run tests in Debug/default configuration.
- Tests require MongoDB at the configured connection string unless a test explicitly replaces storage.
- `UserIdentity` login JWT creation needs a valid `UserIdentity:Jwt:PrivateKeyPem`; the committed default is empty.
- Notifications will not send real email unless environment is Production and `Smtp:Enabled` is true.
- Verification email links derive their base URL from the registration request's scheme/host/path base; account for reverse proxies or public host headers.
- `UserProfile` and `Notifications` have dummy endpoints in `Program.cs`; do not treat them as public business APIs.
- Email lookup normalization is `Trim().ToUpperInvariant()`; preserve this for uniqueness and duplicate checks.
- Do not commit real MongoDB credentials, JWT private keys, SMTP usernames/passwords, or customer data.

## Sources

- `AGENTS.md`
- `README.md`
- `HelpDesk.slnx`
- `Services/*/*.csproj`
- `Services/*/Program.cs`
- `Services/*/appsettings*.json`
- `Services/UserIdentity/Endpoints/Identities/Register/Endpoint.cs`
- `Services/Notifications/Subscriptions/UserIdentity/VerificationIssued/UserIdentityVerificationIssuedEventHandler.cs`
- `Common/Tools/StringExtensions.cs`
