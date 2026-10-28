# Architecture and extension guide

## Data flow

```text
SimulationSettings ── species catalog ── AnimalDefinition
                                           ├── prefab / presentation
                                           ├── diet / mass / spawn weight
                                           └── MovementDefinition → new ILocomotion per life

ZooSimulation (composition root)
  ├── SpawnClock + independent seeded arrival RNG → AnimalPool.Rent
  ├── PopulationModel.Register → fresh AnimalLife → AnimalAgent.Initialize
  ├── HabitatBounds → random spawn points + inward steering
  ├── AnimalAgent.OnCollisionEnter → PopulationModel.TryEncounter
  │                                  ├── FoodChain → eater + victim
  │                                  └── commit one death → Changed → ZooHud
  └── head-contact gate → bite + shrink victim → pooled release + Tasty!
```

## Why composition rather than one class per species

Movement and diet are independent dimensions. A hopping predator or a gliding prey is another asset configuration, not a fork of the frog/snake code. `AnimalAgent` only handles the Unity body and view. New locomotion is supplied through `ILocomotion` and a `MovementDefinition` factory; every life receives fresh mutable movement state. A catalog of 1,000 species adds data and prefabs instead of 1,000 branches in a central script.

The pure C# domain assembly is deliberately small and does not depend on the engine. Dependency injection is explicit at `ZooSimulation.Start` and `AnimalAgent.Initialize`; a container would add package/version overhead without improving this dependency graph. If project size later warrants a container, the same constructor and method boundaries can be bound there.

This is regular object-oriented GameObject code. It does not use ECS, Jobs, Burst, hidden global state or per-frame scene searches. A thousand **species definitions** does not imply a claim that thousands of dynamic physics bodies have been benchmarked.

## Encounters and lifecycle

The food-chain rule is symmetric in callback order. Two prey survive. A predator eats prey. Between two predators, the lower monotonic life ID wins. `PopulationModel` validates that both lives belong to the current population, ends the victim and removes it before raising `Changed`. Reversed callbacks and later contacts with that dead life are therefore harmless.

The runtime first checks that the chosen eater contacted the other animal with its head and its 0.48-second bite cooldown has elapsed. Body and tail contacts only bounce; the younger head touching an older tail does not let the older snake eat through its tail. Both enter and stay callbacks carry each contact part, allowing a continuing head contact to become a meal after cooldown. The survivor rule remains unchanged.

Death accounting happens once at contact. The eater pauses its motor and plays a 0.32-second jaw/lunge animation. All victim rigid bodies immediately stop colliding; its meshes shrink over 0.24 seconds before the GameObject returns to its species pool. This cosmetic delay cannot cause another death. Pause freezes both animations and reset clears pending returns. A new rental receives a fresh `AnimalLife` and locomotion object. IDs are not reset on restart, preventing a stale callback from matching a subsequent life. Labels also remember the target's life ID, so a reused body cannot inherit an old label. There is no active-population cap; the pool retains at most 64 inactive instances per species and destroys excess returns.

## Movement and physics

Snakes request constant planar speed with smooth heading changes and a small distance-driven weave. Frogs use an analytical jump curve: total distance is computed from elapsed jump phase, so crossing a jump boundary on a physics tick does not lose distance. Each frog waits between jumps, and starts at a random point in its resting phase to avoid synchronized motion without shortening its first jump. Ordinary steering is held while a frog is airborne.

The dynamic root collider stays on the XZ plane; the visual root rises through the jump arc. This keeps frog/predator collisions meaningful throughout a jump and avoids aerial near misses. Gravity and body rotation are constrained. Physics applies the actual contact response; the motor yields for 0.18 seconds after a collision so its next velocity update does not erase the impulse. Collision displacement can consequently change an animal's net travel, as expected.

Random wandering changes direction between hops. Bounds steering points escaped animals back toward the habitat without teleporting them or blocking them with walls. It also checks the current camera footprint. The orthographic camera fits the habitat as the window aspect changes. Decorative ponds, reeds, grid and rim have no colliders.

### Articulated snake body

The neighboring `snakes` project's distance-resampled head trail inspired the following approach. `DistanceTrail` records the head's actual physical path in a bounded ring buffer, skips stationary solver jitter and samples positions by arc length using binary search. It does not rely on a fixed rendering frame rate. Each subsequent segment follows an older position along that path, so a turn travels from head to tail instead of rotating a rigid model.

`TrailBody` drives 12 **dynamic** segment rigid bodies toward those samples with clamped acceleration. `ConfigurableJoint` distance limits keep adjacent links together; no segment is teleported to its target during ordinary movement. Contacts can deflect the body. The head and all segment bodies stay in the XZ plane, and the 3D meshes and shadows orient to the physical links. Self-collisions are ignored per animal and reapplied after each pooled activation; collisions with other animals remain enabled.

Each body part's `AnimalContactRelay` reports its owning `AnimalAgent` and identifies itself as a non-head contact. Only the selected eater's head can start predation; every segment remains a physical obstacle. An eligible head can bite another animal's head or body. Pool rental injects the new life before activation. Immediately after activation, `ActivateAt` resets native and rendered poses, velocities, interpolation state and head history before the first physics tick. This order matters because inactive Unity rigid bodies do not reliably accept native pose updates. An initial spawn considers the full straight tail, avoiding a long snake appearing partly outside the habitat. `TrailBody` is optional and reusable by another long-bodied species, without species checks in collision or population code.

The frog prefab is 20% larger than the initial demo. The snake has a larger head and a longer, tapered body so its movement reads clearly from the top-down camera.

### Audio feedback

`HabitatAudio` implements the injected `IAnimalFeedback` hop callback. The composition root also requests appearance, predation and UI cues. Four ElevenLabs clips are mastered as short mono WAVs, with generation prompts and measured levels committed in `Audio.json`. Eight reusable audio sources and per-cue cooldowns avoid uncontrolled layering. A separate audio RNG varies pitch without changing the simulation's random sequence. Mute uses the HUD button or M and remains available while paused.

## Time and randomness

Arrival intervals are uniform in [1, 2] simulation seconds, independent of movement/species RNG calls. Pause freezes physics, scheduling and labels; uGUI remains responsive. 2× speed intentionally doubles simulation time. The clock carries over fractional overshoot and catches up after a long frame. Restart clears all lives, labels and counters, returns to 1×, resets the clock and seeds the initial population.

`Simulation.asset` can use a fixed random seed for reproduction. Normal demo runs randomize the seed. The configured 3:1 frog/snake spawn weights help maintain a readable mixed population; the initial 12:4 population makes the scene immediately observable. These are editable demonstration choices, not special food-chain rules.

## Test boundaries

EditMode tests exercise food-chain permutations, duplicate callbacks, dead/foreign/stale lives, event ordering, hop timing, frame independence, spawn timing and inward steering. PlayMode tests load the committed scene and exercise actual physics callbacks, object reuse, uGUI counters/labels, buttons, pause, automatic spawning and an escaped animal returning. Render inspection and a desktop player build supplement these tests.

## Deliberate limits

The demo uses simple primitive meshes and circular planar contact volumes. It is not a pathfinding or predator-AI simulation: all animals wander randomly as requested. It has no persistence or networking. The two manual spawn buttons and legend belong to the two-species demo; extend that presentation when adding more demo shortcuts. The automatic simulation reads every catalog entry without needing those buttons.
