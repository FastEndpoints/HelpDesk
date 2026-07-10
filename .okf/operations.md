---
type: Playbook
title: Operations
description: Local runtime topology, ports, databases, and configuration keys (names only) for HelpDesk services.
tags: [ops]
resource: Services/UserIdentity/appsettings.json
---

# Operations

## Deploy model

Independently deployable service processes. Local default: IPC mesh on one machine. Remote: change listen/target topology only—contracts/handlers unchanged. No Docker/compose in repo today.

## Services and ports

| Service | HTTP | IPC name | Notes |
| --- | --- | --- | --- |
| UserIdentity | `UserIdentity:HttpPort` (5000) | `USER_IDENTITY_SERVICE` | OpenAPI/Scalar non-prod |
| UserProfile | `UserProfile:HttpPort` (5001) | `USER_PROFILE_SERVICE` | JWT auth; OpenAPI/Scalar non-prod |
| Notifications | none in Program | `NOTIFICATIONS_SERVICE` | Job queue + event subscriber |

## Data stores

- MongoDB via `ConnectionStrings:MongoDB` (default `mongodb://localhost:27017`)
- DBs: `HelpDesk_UserIdentity`, `HelpDesk_UserProfile`, `HelpDesk_Notifications` (+ `_TESTING` variants)

## Config and observability

Config keys (names only—never commit real secrets):

| Key area | Examples |
| --- | --- |
| Mongo | `ConnectionStrings:MongoDB`, `*:DatabaseName` |
| JWT issue | `UserIdentity:Jwt:Issuer`, `Audience`, `AccessTokenDays`, `PrivateKeyPem` |
| JWT validate | `UserProfile:Jwt:Issuer`, `Audience`, `PublicKey` (tests may use `PrivateKey`) |
| SMTP | `Smtp:Enabled`, `Host`, `Port`, `UseSsl`, `Username`, `Password`, `SenderName`, `SenderEmail`, `AdminName`, `AdminEmail` |
| Logging | `Logging:LogLevel:*` |

- User secrets IDs present on service csprojs for local/test secrets
- SMTP live only Production **and** `Smtp:Enabled`; otherwise null/log sender
- Standard ASP.NET Core logging; no separate APM package in tree

## Sources

- `Services/*/appsettings.json`
- `Services/*/Program.cs`
- `Services/*/Properties/launchSettings.json`
