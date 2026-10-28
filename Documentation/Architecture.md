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
  └── release victim immediately + TastyPresenter.Show(eater)
```

## Why composition rather than one class per species

Movement and diet are independent dimensions. A hopping predator or a gliding prey is another asset configuration, not a fork of the frog/snake code. `AnimalAgent` only handles the Unity body and view. New locomotion is supplied through `ILocomotion` and a `MovementDefinition` factory; every life receives fresh mutable movement state. A catalog of 1,000 species adds data and prefabs instead of 1,000 branches in a central script.

The pure C# domain assembly is deliberately small and does not depend on the engine. Dependency injection is explicit at `ZooSimulation.Start` and `AnimalAgent.Initialize`; a container would add package/version overhead without improving this dependency graph. If project size later warrants a container, the same constructor and method boundaries can be bound there.

This is regular object-oriented GameObject code. It does not use ECS, Jobs, Burst, hidden global state or per-frame scene searches. A thousand **species definitions** does not imply a claim that thousands of dynamic physics bodies have been benchmarked.

## Encounters and lifecycle

The food-chain rule is symmetric in callback order. Two prey survive. A predator eats prey. Between two predators, the lower monotonic life ID wins. `PopulationModel` validates that both lives belong to the current population, ends the victim and removes it before raising `Changed`. Reversed callbacks and later contacts with that dead life are therefore harmless.

The victim GameObject is disabled immediately and returned to its species pool. A new rental receives a fresh `AnimalLife` and locomotion object. IDs are not reset on restart, preventing a stale callback from matching a subsequent life. Labels also remember the target's life ID, so a reused body cannot inherit an old label. There is no active-population cap; the pool retains at most 64 inactive instances per species and destroys excess returns.

## Movement and physics

Snakes request constant planar speed. Frogs use an analytical jump curve: total distance is computed from elapsed jump phase, so crossing a jump boundary on a physics tick does not lose distance. Each frog waits between jumps, and has a random phase to avoid synchronized motion.

The dynamic root collider stays on the XZ plane; the visual root rises through the jump arc. This keeps frog/predator collisions meaningful throughout a jump and avoids aerial near misses. Gravity and body rotation are constrained. Physics applies the actual contact response; the motor yields for 0.18 seconds after a collision so its next velocity update does not erase the impulse. Collision displacement can consequently change an animal's net travel, as expected.

Random wandering changes direction between hops. Bounds steering points escaped animals back toward the habitat without teleporting them or blocking them with walls. It also checks the current camera footprint. The orthographic camera fits the habitat as the window aspect changes. Decorative ponds, reeds, grid and rim have no colliders.

## Time and randomness

Arrival intervals are uniform in [1, 2] simulation seconds, independent of movement/species RNG calls. Pause freezes physics, scheduling and labels; uGUI remains responsive. 2× speed intentionally doubles simulation time. The clock carries over fractional overshoot and catches up after a long frame. Restart clears all lives, labels and counters, returns to 1×, resets the clock and seeds the initial population.

`Simulation.asset` can use a fixed random seed for reproduction. Normal demo runs randomize the seed. The configured 3:1 frog/snake spawn weights help maintain a readable mixed population; the initial 12:4 population makes the scene immediately observable. These are editable demonstration choices, not special food-chain rules.

## Test boundaries

EditMode tests exercise food-chain permutations, duplicate callbacks, dead/foreign/stale lives, event ordering, hop timing, frame independence, spawn timing and inward steering. PlayMode tests load the committed scene and exercise actual physics callbacks, object reuse, uGUI counters/labels, buttons, pause, automatic spawning and an escaped animal returning. Render inspection and a desktop player build supplement these tests.

## Deliberate limits

The demo uses simple primitive meshes and circular planar contact volumes. It is not a pathfinding or predator-AI simulation: all animals wander randomly as requested. It has no persistence or networking. The two manual spawn buttons and legend belong to the two-species demo; extend that presentation when adding more demo shortcuts. The automatic simulation reads every catalog entry without needing those buttons.
