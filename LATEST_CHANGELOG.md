## v1.9.6 (patch)

Changes since v1.9.5:

- Deliver a line ending in a bare CR at once instead of on the next write [patch] (#129) (matt-edmondson)
- Look a bare executable name up on the overlay PATH [patch] (#137) (matt-edmondson)
- [patch] End output lines only at CR, LF and CRLF (#136) (matt-edmondson)
- [patch] Keep a surrogate pair in one output chunk (#135) (matt-edmondson)
- Split each run's output into lines in a handler of its own (#132) (matt-edmondson)
- Resolve a relative executable path against WorkingDirectory [patch] (#131) (matt-edmondson)
- Regenerate CompatibilitySuppressions.xml after the Polyfill 11.4.5 bump [patch] (#127) (matt-edmondson)
- [patch] Never run the standard output and standard error delegates at once (Claude)

