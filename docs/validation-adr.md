# ADR-001: Where validation lives (controller level)

## Status

Accepted.

## Context

The booking API takes a `BookingRequest` from clients on `POST /api/booking` and `PUT /api/booking/{id}`. Before a booking is created or updated, the request needs checks: required fields present, ids positive, and a date range that makes sense (StartDate before EndDate). The open question is which layer owns those checks. Three layers could take them: the controller, the service, or the repository. The repository was never a real candidate, because it maps DTOs straight onto entities and should stay a plain data-access layer. The actual contest is controller vs service.

## Decision

Validation stays at the controller level. The controller checks the request shape and the cross-field rule; the service assumes the request it receives is already valid.

## Why controller level

- The controller is the only layer that sees the whole request before anything runs. The StartDate < EndDate rule needs both fields at once, and the controller is the one place where the request arrives whole.
- One error contract at the API boundary. `ValidationProblem` renders the RFC 7807 problem-details envelope with an `errors` map. If the service re-validated, it would either emit the same contract from a different layer or produce a second error shape. Keeping validation in one place means a client always gets the same 400 shape.
- Testability. Controller validation is exercised through the HTTP layer (WebApplicationFactory / integration tests) exactly the way a client hits it. Service-level validation only gets tested if someone remembers to write a service test, and that still would not prove the HTTP response shape.
- The service stays focused on its job: the unit-of-work boundary and the booking mutations. Putting validation in the service mixes request-shape concerns into business logic and makes the service harder to reuse without dragging validation along.

## What we give up

- A validating service protects against callers that bypass the controller. Right now the API has one surface, the controller, and no other clients, so that risk is theoretical. It stops being theoretical if the service is later reused from another entry point (a worker, a CLI, another API) or ships as a library. When that happens the validation has to move or be duplicated, and this ADR is the record that we saw it coming.
- The controller gets a bit thicker. The extra logic is small and declarative: DataAnnotations on the DTO plus one cross-field rule. None of it is business logic.

## Consequences

- `BookingRequest` carries DataAnnotations for required fields and positive ids.
- The controller owns the cross-field rule (EndDate after StartDate) via `ModelState` and returns `ValidationProblem` when `ModelState` is invalid.
- The service does not validate. It trusts its input the same way it trusts the repository.

## Error contract the API returns

All errors are RFC 7807 problem details. Five core codes, two listed as reference:

| Status | Scenario | Type URI | When it fires |
|---|---|---|---|
| 400 | validation | https://tools.ietf.org/html/rfc9110#section-15.5.1 | request fields fail validation (ModelState) |
| 403 | forbidden | https://tools.ietf.org/html/rfc9110#section-15.5.4 | caller lacks permission (no auth yet, reserved) |
| 404 | missing | https://tools.ietf.org/html/rfc9110#section-15.5.5 | resource id does not exist |
| 409 | conflict | https://tools.ietf.org/html/rfc9110#section-15.5.10 | request conflicts with current state (concurrency, Day 5) |
| 500 | fault | (middleware) | unhandled exception |
| 401 (reference) | unauthorized | https://tools.ietf.org/html/rfc9110#section-15.5.2 | authentication required (future) |
| 422 (reference) | unprocessable | https://tools.ietf.org/html/rfc9110#section-15.5.21 | semantically invalid entity (future) |
