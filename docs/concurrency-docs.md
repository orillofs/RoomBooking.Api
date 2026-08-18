# Concurrency in RoomBooking.Api

Two users editing the same booking at the same time can silently overwrite each other. Without protection, the second write wins and the first user's changes disappear with no error or warning. A room booking system has a second risk: two bookings for the same room can overlap in time if both are created at nearly the same moment.

RoomBooking.Api handles these with three mechanisms. Each one covers a specific gap.

## xmin for optimistic locking

PostgreSQL tracks a hidden system column called `xmin` on every row. It records the transaction ID that last wrote the row. Every update changes the xmin value.

EF Core uses xmin as a concurrency token through the `IsRowVersion()` mapping. When `Booking.Version` is configured as a row version, EF Core includes `WHERE xmin = @version` in every UPDATE and DELETE statement. If another transaction modified the row between the read and the write, the WHERE clause matches zero rows. EF Core sees that zero rows were affected and throws `DbUpdateConcurrencyException`.

The controller catches that exception and returns a 409 Conflict with a problem-details body telling the client to refresh.

```csharp
// Data/AppDbContext.cs
builder.Entity<Booking>()
    .Property(b => b.Version)
    .IsRowVersion();
```

The Version property on the entity is `uint` with a private setter. Application code never writes to it. PostgreSQL manages the value.

## GiST exclusion constraint for overlapping bookings

The xmin check protects against stale edits on the same row. It does nothing when two separate INSERT operations try to book the same room for overlapping dates. Neither row exists yet, so there is no xmin to compare.

A GiST exclusion constraint at the database level rejects any new row whose room ID and date range overlap with an existing row.

```sql
CREATE EXTENSION IF NOT EXISTS btree_gist;

ALTER TABLE "Bookings"
ADD CONSTRAINT "EX_Bookings_RoomId_DateRange"
EXCLUDE USING gist (
    "RoomId" WITH =,
    (tstzrange("StartDate", "EndDate", '[)')) WITH &&
);
```

The constraint checks two conditions together: same room (equality) and overlapping time range (the `&&` operator on `tstzrange`). The range uses `[)` notation (inclusive start, exclusive end), so two bookings can be back-to-back, one ending at 10:00 and the next starting at 10:00, without conflicting.

When a second INSERT violates this constraint, PostgreSQL returns error code `23P01` (ExclusionViolation). The controller checks for a `PostgresException` with that SqlState and the constraint name, then returns 409 with a "room is no longer available" message.

## ETag and If-Match for the HTTP gap

The xmin check works when EF Core tracks the same entity instance from read to write. In a typical HTTP flow, the client reads a booking, closes the connection, makes changes on their screen, and sends a PUT request seconds or minutes later. The controller's `FindAsync` loads the entity fresh from the database, reading the latest xmin. If another request updated the booking between the client's read and write, the client's changes silently overwrite the prior update because the xmin the controller sees is already the new one.

ETag and If-Match close this gap.

1. **GET** returns the booking with an `ETag` response header containing the current version (the xmin value, quoted).
2. **PUT** requires an `If-Match` header with the ETag the client received. The controller parses it, passes the expected version to the repository, and the repository sets the entity's original Version value before calling `SaveChangesAsync`. EF Core then generates `WHERE xmin = @clientVersion`. If the row has changed since the client read it, zero rows match and `DbUpdateConcurrencyException` fires.
3. **DELETE** optionally accepts If-Match for the same protection.
4. **POST** returns an ETag on the created booking so the client can start tracking from the first response.

Without an If-Match header on PUT, the server returns 412 Precondition Failed. The endpoint refuses to process a blind update.

```
Client reads booking (GET /api/booking/5)
  ← 200 OK, ETag: "42"

Someone else updates booking 5
  ← xmin changes from 42 to 43

Client tries to update (PUT /api/booking/5, If-Match: "42")
  ← 409 Conflict — "This booking was changed or removed by another request."

Client refreshes (GET /api/booking/5)
  ← 200 OK, ETag: "43"

Client retries (PUT /api/booking/5, If-Match: "43")
  ← 204 No Content
```

## What the tests prove and what they don't

### Two-context EF Core test

The `BookingConcurrencyTests` class creates two separate `AppDbContext` instances pointing at the same database row. One context saves first, the second tries to save with a stale xmin. The test asserts that `DbUpdateConcurrencyException` fires on the second save.

**Proves:** The xmin concurrency token is configured correctly. EF Core generates the `WHERE xmin = @version` clause, and PostgreSQL enforces it. Two tracked entity instances writing to the same row cannot both succeed.

**Does not prove:** The HTTP endpoint is safe. The EF Core test exercises the ORM directly, bypassing the controller and service layers. The ETag/If-Match flow is tested separately.

### HTTP endpoint tests

The `BookingEndpointConcurrencyTests` class uses `WebApplicationFactory` to run the full application stack against a real PostgreSQL test database. It creates a booking via POST, captures the ETag, updates the booking to increment xmin, then retries the update with the stale ETag. The test asserts 409 Conflict with the correct problem-details body.

**Proves:** The full HTTP pipeline works: controller parsing If-Match, repository setting OriginalValue, EF Core detecting the mismatch, and the controller returning the right error response.

**Does not prove:** Concurrent HTTP requests will always be caught. The tests are sequential (create, update, retry). They verify that the server detects stale versions, not that two simultaneous requests will conflict. True concurrent testing would require parallel request threads and timing coordination, which is possible but fragile. The database-level guarantees (xmin + GiST) are what protect against genuine simultaneous writes.

### GiST exclusion constraint test

The overlap test creates two bookings with overlapping time ranges in two separate contexts. The first saves successfully. The second triggers a PostgresException with SqlState `23P01`.

**Proves:** The constraint exists in the database, it is checked on INSERT, and the controller's error mapping produces the correct 409 response with the room-unavailable message.

**Does not prove:** The constraint catches every edge case. The adjacent-boundary test (one booking ends at 10:00, the next starts at 10:00) confirms that back-to-back bookings are allowed, which validates the `[)` range semantics.
