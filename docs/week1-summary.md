# Week 1 Summary

## Day 1 — Baselines and project setup (Monday)

Started with Pluralsight Skill IQ assessments before touching any learning material. The scores gave me a starting point to measure against later: Git at 118 (29th percentile), ASP.NET Core Web API at 190 (75th percentile), and API Security at 101 (20th percentile). I recorded a one-minute communication baseline video for the same reason.

After the baselines, I scaffolded the ASP.NET Core Room Booking API project, connected it to PostgreSQL on Neon, ran the initial migrations, and seeded the database with users, rooms, and bookings. The Git repository was initialized and pushed that day.

## Day 2 — Git workflow and conflict resolution (Tuesday)

Practiced the feature branch workflow end to end. I created a feature branch, implemented booking validation, committed, and opened PR #1. After simulating a code review cycle with additional pushes, the PR was merged into main.

Then I created a stale branch deliberately, modified the same line on main, and rebased. Git produced a merge conflict. I resolved it by comparing both sides and keeping the clearer message. The exercise also produced a written note on when to merge and when to rebase.

## Day 3 — Async and .NET depth (Wednesday)

Completed the Pluralsight async programming course and used the Microsoft Learn async guide as a reference alongside it. After the course, I reviewed the booking API code for async anti-patterns: `.Result`, `.Wait()`, `.GetAwaiter()`, `async void`, `Thread.Sleep`, and `Task.Run`. None were present.

I wrote the async explainer (`docs/async-explainer.md`) and merged it through PR #4. Writing it forced me to articulate what `await` actually does rather than just using it.

## Day 4 — API error design (Thursday)

Built the RFC 7807 problem-details error contract. Registered the problem-details middleware in `Program.cs` and created the `ErrorHandler` class that maps 400, 403, 404, 409, and 500 to problem-details responses with consistent structure.

Added DataAnnotations to `BookingRequest` and a cross-field date check in the controller. Wrote the validation placement ADR (`docs/validation-adr.md`) explaining why validation lives at the controller level and not in the service. Merged through PR #6.

Smoke tested the running API: POST with `StartDate >= EndDate` returns 400 with `errors.EndDate`, GET for a nonexistent booking returns 404, and both responses follow the problem-details shape.

## Day 5 — Concurrency (Friday)

Added three layers of concurrency protection to the booking API.

The first layer is `xmin`, PostgreSQL's system column that tracks the last writing transaction. EF Core uses it as a concurrency token through `IsRowVersion()`, adding `WHERE xmin = @version` to every UPDATE. If the row changed since the client read it, zero rows match and EF Core throws `DbUpdateConcurrencyException`.

The second layer is a GiST exclusion constraint. Since `xmin` only protects against stale edits on an existing row, it cannot prevent two INSERTs from booking the same room at overlapping times. The constraint rejects any new row whose room ID and date range overlap with an existing one, returning error code `23P01`.

The third layer is ETag and If-Match at the HTTP level. Without this, a PUT endpoint that reloads the entity via `FindAsync` always sees the latest `xmin`, so a stale client write can still overwrite a concurrent change sequentially. The API now returns an ETag on GET and POST responses, requires If-Match on PUT (returning 412 without it), and passes the expected version through to the repository which sets the original Version value before saving.

Created a test project with xUnit, wrote EF Core integration tests (two-context concurrency, overlap, and adjacent boundary), HTTP endpoint integration tests using `WebApplicationFactory` (stale PUT, stale DELETE, missing If-Match), and a unit test with a mocked service. Wrote the concurrency documentation in `deliverables/concurrency-docs.md`.

## What I'm taking away

The biggest shift this week was in how I think about correctness. On Day 3, writing the async explainer showed me that `await` is not "running in the background." It is a compiler-generated state machine that yields control at awaitable boundaries and resumes when the task completes. Before that, I was using `async`/`await` correctly in syntax but could not have explained what happens between the lines.

On Day 5, the concurrency work drove home a related point: a concurrency token that works at the ORM level is not enough for an HTTP API. The EF Core two-context test proves that `xmin` catches stale tracked entities. But the HTTP flow has a gap between the client reading the data and sending the update, and during that gap the server reloads the entity fresh. ETag and If-Match close that gap by making the client prove it has the current version.

The pattern across the week is that each day revealed something I assumed was working but had not actually verified. Async was correct but not understood. Errors were returned but not consistent. Concurrency was partially handled but not end to end. The tests and documentation are partly about proving the code works, but they also forced me to think about what "works" actually means in each case.

## Areas to keep working on

- Async mental model: I can explain `await` now, but more complex patterns like `Task.WhenAll`, cancellation tokens, and async streams need practice.
- Error design: The error contract is in place, but the 403 and 500 paths have no reachable code yet. Authorization in week 2 will exercise 403.
- Testing: This was my first time writing xUnit tests. The mocking pattern (injecting a fake service) works for unit tests, but I need more practice with integration test setup and cleanup.
- Frontend: No Vue.js work this week. That starts in week 2.
