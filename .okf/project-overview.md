---
type: Reference
title: Project Overview
description: HelpDesk is a .NET 10 brokerless event-driven microservice mesh with an Aspire-orchestrated local stack.
tags: [overview]
resource: README.md
---

# Project Overview

## Purpose

HelpDesk implements a brokerless, event-driven microservice mesh with .NET and FastEndpoints. Independently deployable service nodes communicate through public contract events—no central message broker and no cross-service RPC for business workflows. A SvelteKit BFF is the external-client boundary.

## Scope

Current system covers the **user onboarding path**:

1. Register identity → `UserIdentityRegisteredEvent` + `UserIdentityVerificationIssuedEvent`
2. Create deactivated profile → `UserProfileRegisteredEvent`
3. Send welcome/verification email
4. Verify identity → `UserIdentityVerifiedEvent`
5. Activate matching profile

## Capabilities

| Area | Responsibility |
| --- | --- |
| AppHost | Aspire 13.4.6 local orchestration of MongoDB, three backend services, and Vite |
| UserIdentity | Public identity REST API, credentials, JWT issuance, identity lifecycle events |
| UserProfile | Authenticated profile API; reacts to identity events |
| Notifications | Email jobs from verification-issued events; no public business API |
| Frontend | SvelteKit BFF, generated API types, and server-only client/session helpers |
| Common | MongoDB-backed remote event storage; lookup string helpers |
| Contracts | Stable service names, events, subscriber ID arrays |

## Status

Active monorepo development. `backend/` targets .NET 10; `frontend/` requires Node 26 or newer and pnpm 11 or newer. `backend/AppHost/Program.cs` is the sole supported local full-stack orchestrator and is run by `pnpm stack:dev`. The frontend provides a shared shell, landing page, registration/login/verify, and profile view/edit (BFF to Identity/Profile). UI theming targets the FastEndpoints docs dark navy/cyan look (see [Frontend UI](frontend-ui.md)).

Deployment decisions for verification-link routing and profile-picture serving/public URLs remain unresolved and block shipping those corresponding UI flows.

## Non-goals

- Central broker (RabbitMQ/Kafka/Service Bus)
- Service-to-service REST for internal workflows
- Shared domain models across services
- Cross-service project references between `backend/Services/*`
- Compose-based local orchestration
- Exhaustive multi-domain helpdesk product surface yet—onboarding mesh only

## Glossary

| Term | Meaning |
| --- | --- |
| AppHost | Aspire executable that declares and runs the supported local resource graph |
| Contract | Public cross-service language: service name + events (+ DTOs if needed) |
| Event hub | Publisher-side registration of an event type and known subscriber IDs |
| IPC | Local inter-process FastEndpoints remote transport |
| Mesh | Set of service nodes linked by remote event subscriptions |
| SubscriberID | Stable service name used as remote subscriber identity |

## Sources

- `README.md`
- `backend/AppHost/Program.cs`
- `backend/AppHost/HelpDesk.AppHost.csproj`
- `HelpDesk.slnx`
- `backend/Directory.Packages.props`
