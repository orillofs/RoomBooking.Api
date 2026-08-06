# Async Explainer

> Day 3 deliverable (W1-D3)

At first, I thought async meant the app created more threads. I now understand that it is mostly about what happens while the API is waiting for I/O, such as a database response. When the code reaches `await`, it does not need to keep a request thread sitting idle. That thread can return to the pool and work on another request, then the code continues when the database response is ready, possibly on another available thread. Threads still run actual work such as parsing or CPU-heavy work, but waiting for a database is only waiting. I also learned that async has to continue through the call chain: if one method uses `await`, its caller needs to await it too. Using `.Result` or `.Wait()` blocks the thread again and can deadlock the app, while `async void` can let errors fail silently. Keeping the code async lets the server handle more requests because fewer threads are stuck waiting.
