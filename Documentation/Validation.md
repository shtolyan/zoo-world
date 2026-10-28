# Validation

Verified locally on **28 October 2026**, using **Unity 6000.4.6f1** on an Apple M1 Mac.

Code revision used for the final test/build pass: `b871d8f1b4a5259efa252a48570d6afb0a1521a9`.

| Check | Result |
| --- | --- |
| EditMode tests | **30 passed**, 0 failed, 0 skipped |
| PlayMode integration tests | **8 passed**, 0 failed, 0 skipped |
| Desktop build | **Succeeded**, StandaloneOSX, clean Git source |
| Executable architectures | Universal **arm64 + x86_64** |
| Native player startup | Launched with Metal / PhysX; no application exceptions in its log |
| Visual inspection | **1440×900**, **1280×720**, **1024×768** |
| Continuous Editor simulation | Approximately **3 minutes**, no game-code exceptions; automatic arrivals and predation continued |

The PlayMode suite exercises real physics contacts for prey/prey, predator/prey and predator/predator, pooled-life isolation, uGUI counters and labels, pause/resume, spawn/reset/speed buttons, scheduled arrivals and inward movement after leaving the habitat. The EditMode suite also covers the first frog jump being complete after a randomized initial rest.

The UI inspection checked counter/button placement, full habitat framing, separate death counters, floating “Tasty!” labels and the absence of clipped text or black viewport gutters. [The committed screenshot](Screenshot.png) is an actual running scene capture.

Local artifacts, deliberately excluded from Git:

- `TestResults/editmode.xml` and `TestResults/playmode.xml`, plus JUnit equivalents.
- `Builds/macOS/ZooWorld.app` — ready-to-run macOS player.
- `Builds/macOS/ZooWorld.app/unity-build.provenance.json` — editor/package versions and source revision.
- `Logs/build-macos.log`, `Logs/player-smoke.log` and `Artifacts/preview-*.png`.

The verified target is macOS. Other platform modules being installed does not imply that other targets were built or tested.
