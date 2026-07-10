---
type: Reference
title: Project Overview
description: HelpDesk is a .NET 10 brokerless event-driven microservice mesh for user onboarding (identity, profile, notifications).
tags: [overview]
resource: README.md
---

# Project Overview

## Purpose

HelpDesk implements a brokerless, event-driven microservice mesh with .NET and FastEndpoints. Independently deployable service nodes communicate through public contract events—no central message broker and no cross-service RPC for business workflows.

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
| UserIdentity | Public identity REST API, credentials, JWT issuance, identity lifecycle events |
| UserProfile | Authenticated profile API; reacts to identity events |
| Notifications | Email jobs from verification-issued events; no public business API |
| Common | MongoDB-backed remote event storage; lookup string helpers |
| Contracts | Stable service names, events, subscriber ID arrays |

## Status

Active development. Targets .NET 10. Local mesh uses IPC (`ListenInterProcess` / `MapRemote`); remote topology is deployment config, not business-code change.

## Non-goals

- Central broker (RabbitMQ/Kafka/Service Bus)
- Service-to-service REST for internal workflows
- Shared domain models across services
- Cross-service project references between `Services/*`
- Exhaustive multi-domain helpdesk product surface yet—onboarding mesh only

## Glossary

| Term | Meaning |
| --- | --- |
| Contract | Public cross-service language: service name + events (+ DTOs if needed) |
| Event hub | Publisher-side registration of an event type and known subscriber IDs |
| IPC | Local inter-process FastEndpoints remote transport |
| Mesh | Set of service nodes linked by remote event subscriptions |
| SubscriberID | Stable service name used as remote subscriber identity |

## Sources

- `README.md`
- `HelpDesk.slnx`
- `Directory.Packages.props`
