---
type: Reference
title: Project Overview
description: Product scope, current services, onboarding flow, and glossary for HelpDesk.
tags: [overview, scope, services]
---

# Project Overview

## Name and purpose

HelpDesk is a .NET 10 brokerless, event-driven microservice mesh built with FastEndpoints. Services communicate through public contract events over `FastEndpoints.Messaging.Remote`; the current repo intentionally avoids direct service references, synchronous service-to-service REST/RPC, and central brokers.

## Current scope

The implemented product scope is the user onboarding path:

```text
register identity
  -> UserIdentityRegisteredEvent
  -> create deactivated user profile
  -> UserProfileRegisteredEvent
  -> queue/send welcome verification email
  -> verify identity
  -> UserIdentityVerifiedEvent
  -> activate matching user profile
```

## Services and capabilities

| Service | Responsibility | Public API/events |
| --- | --- | --- |
| `Services/UserIdentity` | Owns identity data, registration, login, password hashing, JWT issuance, verification codes, and identity status. | REST: `POST /identities/register`, `POST /identities/login`, `GET /identities/verify/{VerificationCode}`. Publishes `UserIdentityRegisteredEvent`, `UserIdentityVerifiedEvent`. |
| `Services/UserProfile` | Owns profile data derived from identity events; activates profiles when identities verify. | Subscribes to identity events. Publishes `UserProfileRegisteredEvent`. No public business API currently. |
| `Services/Notifications` | Owns notification delivery, durable email jobs, SMTP/null sender selection, and welcome email content. | Subscribes to `UserProfileRegisteredEvent`. No public business API currently. |

## Main consumers

- External clients call UserIdentity REST endpoints for registration, login, and verification.
- Services consume each other only through contract events.

## Current maturity/status

The repo contains service implementations, colocated tests, appsettings files, central package management, and a `.slnx` solution. There are no Docker, CI, deployment, or migration files in the repository at OKF initialization time.

## Explicit non-goals / constraints from docs

- No RabbitMQ, Kafka, Azure Service Bus, or other central broker in the current architecture.
- No direct service implementation references across services.
- No synchronous inter-service REST/RPC for internal business workflows.
- No shared domain models across services.

## Glossary

- **Contracts**: Public cross-service language: stable service names, event records, and simple DTOs if needed.
- **Common**: Reusable infrastructure/helpers only; not a place for domain behavior.
- **Service**: Independently deployable FastEndpoints app under `Services/<Name>`.
- **Mesh**: Remote event queues between service nodes using FastEndpoints remote messaging.
- **IPC**: Current local transport via `ListenInterProcess(Service.Name)` and `MapRemote(...)`.
- **Event hub**: Publisher-side registration for event types a service emits.
