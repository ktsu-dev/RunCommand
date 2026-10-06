## v1.9.4 (patch)

Changes since v1.9.3:

- [patch] Run continuations of the cancellation signal asynchronously, so Cancel() returns before the unwind ([@matt-edmondson](https://github.com/matt-edmondson))
- Drop a reused handler's leftover partial line at the start of each run ([@matt-edmondson](https://github.com/matt-edmondson))

