# Task checklist

Source: the user-provided two-page Zoo World specification. The original PDF is kept outside the public repository.

| Requirement | Implementation |
| --- | --- |
| 3D, top-down view | Orthographic camera above an XZ habitat; adaptive framing |
| Primitive graphics acceptable | Original frog/snake prefabs made from Unity primitives |
| One animal every random 1–2 seconds | `SpawnClock`, independent interval RNG, weighted species catalog |
| Random movement | Per-life random heading and direction changes |
| Physical contact between animals | Dynamic `Rigidbody` + solid `SphereCollider`, bouncy contact material |
| Off-screen animals turn back | `HabitatBounds.Redirect`, no blocking walls or teleports |
| Prey/prey survive and separate | No predation result; physical solver separates the bodies |
| Predator/prey eats prey | Domain rule; immediate death accounting and collision disable, then a 0.24-second disappearance |
| Predator/predator leaves one survivor | Older life wins when its head contacts the other animal and bite cooldown has elapsed; one predator death count |
| “Tasty!” under the eater each time | Pooled uGUI overlay anchored below the living predator; repeated meals refresh it |
| Frog is prey; fixed-distance timed jumps | Frog data asset + `HopLocomotion` |
| Snake is predator; linear constant speed | Snake data asset + `LinearLocomotion` |
| Dead prey and predator counts at top right | Separate event-driven uGUI counters |
| Architecture supports many species | Data-driven composition, movement strategies, explicit DI, no species branches in core |
| No ECS | GameObjects, MonoBehaviours and plain C# objects |
| Publish a Git repository | Public GitHub repository requested by the user |

Extra controls are for inspection: pause/resume, 1×/2×, restart and manual species spawning. They do not replace scheduled automatic arrivals.

Follow-up improvements: frogs are 20% larger, snakes have a larger head and a 12-part physical body following the head's path, and four quiet ElevenLabs sound effects cover appearance, hops, eating and buttons. Sound can be muted with M or the uGUI button. The user subsequently approved head-only bites and bite animation. This narrows the original specification's any-contact predation: body contacts bounce and bites have a 0.48-second cooldown. Snake movement also includes weave and brief bite pauses rather than strictly straight constant travel. The domain food-chain rules and the 1–2 second spawn schedule are unchanged.
