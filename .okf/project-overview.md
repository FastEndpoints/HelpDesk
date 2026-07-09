---
type: Reference
title: Project Overview
description: Project purpose, scope, capabilities, status, and glossary.
tags: [overview, product]
---

# Project Overview

## Purpose

HelpDesk is a brokerless, event-driven microservice mesh built with .NET and FastEndpoints. Services communicate through public contract events, not direct service references, synchronous service-to-service REST/RPC, or a central broker.

## Current scope

The implemented product scope is the user onboarding path:

1. External client registers an identity.
2. `UserIdentity` stores a deactivated identity and publishes `UserIdentityRegisteredEvent` plus `UserIdentityVerificationIssuedEvent`.
3. `UserProfile` creates a deactivated profile from `UserIdentityRegisteredEvent` and publishes `UserProfileRegisteredEvent`.
4. `Notifications` queues/sends a welcome verification email from `UserIdentityVerificationIssuedEvent`.
5. External client verifies the identity.
6. `UserIdentity` activates the identity and publishes `UserIdentityVerifiedEvent`.
7. `UserProfile` activates the matching profile and marks email verified.

## Users and consumers

- External clients consume the `UserIdentity` HTTP API.
- Other services consume only contract event records from `Contracts/*`.
- Developers/agents maintain independently deployable service nodes.

## Major capabilities

- Identity registration, login, verification, password hashing, and JWT issuance.
- Profile creation/activation from identity events.
- Notification email job queuing and SMTP delivery when enabled; null sender otherwise.
- MongoDB-backed durable FastEndpoints remote event storage.
- Service-local tests for endpoints, subscriptions, and event publication.

## Current service status

| Service | Public API | Publishes | Subscribes |
| --- | --- | --- | --- |
| `UserIdentity` | `POST /identities/register`, `POST /identities/login`, `GET /identities/verify/{VerificationCode}` | `UserIdentityRegisteredEvent`, `UserIdentityVerificationIssuedEvent`, `UserIdentityVerifiedEvent` | none |
| `UserProfile` | `GET /profiles/me` | `UserProfileRegisteredEvent` | `UserIdentityRegisteredEvent`, `UserIdentityVerifiedEvent` |
| `Notifications` | no public business API | none | `UserIdentityVerificationIssuedEvent` |

## Non-goals and constraints

- No RabbitMQ/Kafka/Azure Service Bus broker in the current architecture.
- No cross-service business workflow via REST/RPC.
- No shared domain models across service implementations.
- No service implementation project references from other services.

## Glossary

- **Contract project**: public cross-service language for a service: service name, event records, known-subscriber ID arrays (`EventSubscribers`), and simple DTOs if needed.
- **Service implementation**: deployable FastEndpoints app under `Services/<ServiceName>/` with private endpoints, handlers, persistence, and tests.
- **IPC mesh**: local FastEndpoints remote messaging topology using `ListenInterProcess(...)` and `MapRemote(...)`.
- **Event hub**: publisher-side remote messaging hub registered with `RegisterEventHub<TEvent>(EventSubscribers.SomeEvent)`.
- **EventSubscribers**: data-only publisher-contract arrays of subscriber service name string literals for durable startup/offline delivery.

## Sources

- `README.md`
- `Contracts/UserIdentity/Events.cs`
- `Contracts/UserProfile/Events.cs`
- `Services/UserIdentity/Program.cs`
- `Services/UserProfile/Program.cs`
- `Services/Notifications/Program.cs`