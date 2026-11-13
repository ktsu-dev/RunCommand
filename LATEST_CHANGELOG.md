## v1.9.6 (patch)

Changes since v1.9.5:

- Split RunCommandTests by area so the test project builds again [patch] (#154) (matt-edmondson)
- Never resolve a bare name to a batch file on the overlay PATH [patch] (#140) (matt-edmondson)
- Ask access(X_OK) whether an overlay PATH match may run, on every target [patch] (#141) (matt-edmondson)
- Kill the whole process tree from the netstandard builds where the runtime can [patch] (#142) (matt-edmondson)
- Treat a read whose handler threw OperationCanceledException as failed [patch] (#146) (matt-edmondson)
- Strip a leading byte order mark once, not twice, for an encoding with a preamble [patch] (#147) (matt-edmondson)
- Resolve ".." in a relative executable from where a symlinked working directory points [patch] (#149) (matt-edmondson)
- Deliver a line ending in a bare CR at once instead of on the next write [patch] (#129) (matt-edmondson)
- Look a bare executable name up on the overlay PATH [patch] (#137) (matt-edmondson)
- [patch] End output lines only at CR, LF and CRLF (#136) (matt-edmondson)
- [patch] Keep a surrogate pair in one output chunk (#135) (matt-edmondson)
- Split each run's output into lines in a handler of its own (#132) (matt-edmondson)
- Resolve a relative executable path against WorkingDirectory [patch] (#131) (matt-edmondson)
- Regenerate CompatibilitySuppressions.xml after the Polyfill 11.4.5 bump [patch] (#127) (matt-edmondson)
- [patch] Never run the standard output and standard error delegates at once (Claude)

