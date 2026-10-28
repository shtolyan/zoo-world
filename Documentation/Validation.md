# Validation

Verified locally on **28 October 2026**, using **Unity 6000.4.6f1** on an Apple M1 Mac.

Code revision used for the final automated test/build pass: `d6a1c5769593c653b9b39bf41518866a19654f6a`.

| Check | Result |
| --- | --- |
| EditMode tests | **36 passed**, 0 failed, 0 skipped |
| PlayMode integration tests | **12 passed**, 0 failed, 0 skipped |
| Desktop build | **Succeeded**, StandaloneOSX, clean Git source |
| Executable architectures | Universal **arm64 + x86_64** |
| Native player startup | Launched with Metal / PhysX; no application exceptions in its log |
| Follow-up visual inspection | **1440×900**, **1024×768**, plus a **6-second motion recording** |
| Continuous Editor simulation | Over **2 minutes**, no game-code exceptions; articulated bodies, arrivals and predation continued |
| Audio assets | **4 mono WAVs**, 44.1 kHz, decoded and peak/RMS checked |

The PlayMode suite exercises real physics contacts for prey/prey, predator/prey and predator/predator, pooled-life isolation, uGUI counters and labels, pause/resume, spawn/reset/speed buttons, scheduled arrivals and inward movement after leaving the habitat. New cases verify 12 dynamic connected body parts bending behind the head, contact with the tail producing one prey death and eating cue, exact pose/velocity reset on snake reuse, and sound mute/cooldown behavior. The EditMode suite also covers the first frog jump, distance-based corner following, bounded path history, stationary jitter, fresh-life path reset and full-body spawn bounds.

The pool-reuse regression initially exposed stale native poses on inactive rigid bodies. Activation now resynchronizes every native/rendered pose and interpolation state before the first physics tick. The focused regression and the full final suite both passed after the fix.

The UI inspection checked counter/button placement, full habitat framing, separate death counters, floating “Tasty!” labels, the sound button and the absence of clipped text or black viewport gutters. [The committed screenshot](Screenshot.png) and [motion preview](Motion.gif) are actual running scene captures. The native player was also launched after the final build and its log checked for application exceptions.

Local artifacts, deliberately excluded from Git:

- `TestResults/motion-editmode.xml` and `TestResults/motion-playmode.xml`, plus JUnit equivalents.
- `TestResults/pool-reset.xml` — the focused pooling regression.
- `Builds/macOS/ZooWorld.app` — ready-to-run macOS player.
- `Builds/macOS/ZooWorld.app/unity-build.provenance.json` — editor/package versions and source revision.
- `Logs/build-macos.log`, `Logs/player-smoke.log` and `Artifacts/preview-*.png`.

The verified target is macOS. Other platform modules being installed does not imply that other targets were built or tested.
