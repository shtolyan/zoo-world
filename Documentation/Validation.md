# Validation

## Head-contact bites — 28 October 2026

Unity **6000.4.6f1**: **36 EditMode + 15 PlayMode tests passed**, no failures or skipped cases. Reports: `TestResults/bite-editmode.xml` and `TestResults/bite-playmode.xml` (plus JUnit equivalents).

The physical-contact suite verifies that prey survives tail contact, a younger head touching an older tail causes no death, an older head can eat a younger body, and head/head encounters retain the older-survivor rule. It also checks exactly one death/eating cue, the bite cooldown, delayed pool return, collision restoration on reuse and reset during consumption. Existing movement, UI, pause, spawn, sound and pooling checks continue to pass.

[Bite.gif](Bite.gif) is a 40-frame capture of the running simulation from a temporary close-up camera. For this presentation the frog's position was held still while the snake approached; actual PhysX contact triggered the bite. The capture showed both the jaw/lunge animation and the prey's shrinking disappearance. The temporary camera and staging are excluded from the saved scene.

No desktop player was rebuilt for this change, as requested. The local macOS player documented below remains the previous movement/audio revision; use the Editor to run the current bite behavior.

## Previous movement/audio validation

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
