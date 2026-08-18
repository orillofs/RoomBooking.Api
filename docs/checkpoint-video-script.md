# Five-Minute Checkpoint Video Script

Estimated duration: 4-5 minutes at a natural speaking pace.

---

**[Opening, ~15 seconds]**

Hi, this is Bernard Jay Orillo. This is my week one async communications checkpoint. I'll cover three questions about async and concurrency based on what I built this week.

**[Prompt 1: What is async actually doing?, ~1 minute 15 seconds]**

One sentence first: async does not mean your code runs on multiple threads. What it actually does is let a single thread give up control while it waits for something external, like a database query or an HTTP call, and pick back up when that thing finishes.

Before this week I thought `async` meant "runs in the background." That is not what happens. When you write `await` on a database call, the compiler generates a state machine. The thread that was running your method gets released back to the thread pool. It is free to handle other requests. When the database responds, a thread picks up from where the method left off. It might be the same thread or a different one.

The practical effect is throughput. In a web API, if every request blocks a thread while waiting for the database, you run out of threads under load. With async, the threads are not sitting idle. They go do other work. You handle more concurrent requests with the same thread pool size.

I verified this in my project on Day 3 by checking for anti-patterns. `.Result` and `.Wait()` block the thread, which defeats the point. `async void` swallows exceptions. `Thread.Sleep` blocks when `Task.Delay` would not. None of these were in my code, but I now understand why they are problems instead of just knowing they are on a "do not use" list.

**[Prompt 2: Why does firing two HTTP calls not prove concurrency handling?, ~1 minute 30 seconds]**

Sending two requests at the same time and checking that one fails is not a real concurrency test. The reason is timing.

When two HTTP requests arrive at your API at almost the same time, the web server processes them one at a time on separate threads. But each request goes through its own database connection. PostgreSQL serializes writes at the database level using row locks and transaction isolation. So by the time both requests reach the database, one will get a lock and the other will wait. The second request then sees the committed state and succeeds with the new values.

Both requests complete without conflict. That looks like success, but it actually means nothing was tested. The database serialized the writes cleanly. Your code never hit a concurrency conflict because the requests were not truly simultaneous at the database level. They were sequential from the database's perspective, just close together in wall clock time.

To actually prove concurrency handling, you need to control the timing. You need one context to read the data, a second context to read the same data, the first context to save, and then the second context to try to save with a version that is no longer current. That is what my test does.

**[Prompt 3: What does the two-context test prove, and what does it not prove?, ~1 minute 30 seconds]**

My `BookingConcurrencyTests` class opens two `AppDbContext` instances pointing at the same row. The first context loads a booking, the second context loads the same booking. The first context changes the end date and saves. The second context also changes the end date and tries to save. Because the first save changed the xmin value, the second save's `WHERE xmin = @originalVersion` matches zero rows. EF Core throws `DbUpdateConcurrencyException`.

That proves the xmin concurrency token is wired up correctly. It proves EF Core generates the right WHERE clause and that PostgreSQL enforces it. Two tracked entities writing to the same row cannot both succeed.

What it does not prove is that my HTTP endpoint is safe. The test exercises EF Core directly. It does not go through the controller, the service, or the repository. In the real HTTP flow, there is a gap. The client reads the booking, makes changes in their browser, and sends a PUT request later. The controller calls `FindAsync`, which loads the entity fresh from the database with the latest xmin. If someone else updated the booking between the client's read and write, the controller would not know. The ETag and If-Match headers close that gap. The GET response includes an ETag, the PUT request must include it in the If-Match header, and the controller checks that the client's version matches before saving. I wrote HTTP integration tests for that flow separately.

**[Closing, ~15 seconds]**

That covers the three prompts. The main thing I learned this week is that proving something works is different from assuming it works. Each layer, async, error handling, and concurrency, had gaps that I only found when I tried to write tests or documentation for it.
