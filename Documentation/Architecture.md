# Architecture and extension guide

Zoo World separates species data, engine-independent rules, Unity physics and presentation. It uses explicit dependency injection and ordinary GameObjects; there is no service locator, singleton, ECS, Jobs or Burst.

## Responsibilities

| Type | Owns | Does not own |
| --- | --- | --- |
| `ZooSimulation` | Scene wiring, input, pause/speed, arrival clock, HUD/audio subscription | Animal creation internals, food-chain decisions, animation |
| `AnimalSpawner` | Catalog snapshot, weighted selection, initial placement, seeding | UI, death accounting, pool internals |
| `AnimalPopulation` | Spawn preparation, encounters, consumed-animal returns | Keyboard, camera framing, HUD, sound playback |
| `AnimalPool` | Inactive creation, rentals, bounded per-species reuse, disposal | Life IDs, diet, death counters |
| `AnimalAgent` | Serialized bindings, Unity callbacks, dependency injection | Steering calculations, animation curves, action timers |
| `AnimalMotor` | Planar wandering, boundary steering, Rigidbody velocity, collision recovery | Visual transforms, food-chain rules |
| `AnimalView` | Appearance, lift, shadow, jaw/lunge, disappearance | Physics velocity, counters, life state |
| `AnimalActivity` | Bite cooldown and consumption timing in simulation seconds | Unity and rendering |
| `TrailBody` | Jointed body following a distance-sampled physical trail | Species selection and predation rules |
| `PopulationModel` / `FoodChain` | Life identity, ownership, encounters and accounting | Unity objects and presentation |

The motor and view are ordinary per-instance C# collaborators. Their serialized references stay on `AnimalAgent`, so existing scene and prefab bindings remain valid without a migration or runtime component searches. They are constructed once per pooled instance and reset for each new life.

## Composition and events

```text
SimulationSettings -> AnimalDefinition -> prefab + diet + MovementDefinition

ZooSimulation
  -> SpawnClock -> AnimalSpawner -> AnimalPopulation.Spawn
  -> AnimalPopulation.Model.Changed -> ZooHud.Present
  -> AnimalPopulation.Ate -> TastyPresenter + HabitatAudio

AnimalPopulation.Spawn
  -> MovementDefinition.Create
  -> PopulationModel.Register(prepare)
       -> AnimalPool.Rent (inactive)
       -> AnimalAgent.Initialize
            -> AnimalMotor.Reset
            -> AnimalActivity.Reset
            -> AnimalView.Reset
       -> AnimalAgent.ActivateAt
       -> publish the initialized life and population snapshot

AnimalAgent contact -> AnimalPopulation.Collide
  -> PopulationModel.TryResolve -> winner head contact + bite readiness
  -> PopulationModel.TryEncounter -> one committed death
  -> eater bite + victim consumption -> Ate event
  -> LateUpdate -> release finished victim to pool
```

`AnimalPopulation` can run without a HUD or audio service; tests use this boundary for custom species and scale probes. The scene adapter exposes a few convenience methods for controls and integration tests, while physical contacts go directly to the population service.

## Species extension

1. Make a prefab with `AnimalAgent`, a dynamic root `Rigidbody` and `SphereCollider`. Assign separate child transforms for the visual and ground shadow; optionally assign a jaw and `TrailBody`. Visual-only geometry has no colliders.
2. Reuse a `MovementDefinition` or derive one and return a fresh `ILocomotion` from `Create`. Override `IsValid` to reject invalid serialized settings before spawning.
3. Create an `AnimalDefinition` with prefab, movement, diet, mass, turn speed, spawn weight and initial population.
4. Add the asset to `SimulationSettings.Species` and restart Play mode. The spawner snapshots catalog membership and weights at startup.

The catalog is validated once on construction, and an explicit spawn validates the selected definition again. A cumulative weight distribution uses double precision and binary search: selecting a species does not scan 1,000 definitions or overflow a float sum. A test addresses every bucket in a 1,000-entry catalog. Another test configures a new gliding predator using the frog prefab and existing movement assets, and verifies a real physical meal without changes to the simulation.

Movement and diet are independent dimensions. New species need no frog/snake conditionals. The two manual buttons and keyboard shortcuts are presentation for this demo, not the species registry.

### Movement scope

The supplied motor operates on XZ and uses a root sphere. Hop elevation is visual, keeping collisions meaningful throughout a jump. `ILocomotion` describes distance and visual height per step; it is not a complete flight or swimming interface. True volumetric motion would require another motor contract with physical vertical displacement and appropriate habitat constraints. The current separation isolates that future work from population, food-chain and view code; it does not claim flight is already implemented.

## Creation and pooling

Prefabs are instantiated beneath an inactive parent. `OnEnable` therefore cannot run before the life, definition, motor and view have been initialized. On activation, native and rendered Rigidbody poses, velocities and interpolation caches are reset before the next physics tick. Long bodies reset every segment and their trail, and reapply ignored self-collisions.

A population life is published only after successful preparation. If preparation throws, the provisional life ends, the rented instance is discarded, and no counter or population notification is emitted. Pool ownership records the rental definition independently of `AnimalAgent.Definition`, so even partially initialized instances can be removed safely. Factories returning no movement strategy are rejected before rental.

IDs remain monotonic across reset and failed preparation. Labels remember the target's life ID, so a reused object does not inherit an old label. The inactive pool retains at most 64 objects per species; excess returns are destroyed. Population disposal invalidates lives and destroys both active and retained objects. HUD delegates are detached even if Unity destroyed the HUD before the scene adapter.

## Encounters and actions

Prey/prey contacts only separate through physics. A predator eats prey. Between two predators the older life wins, independent of callback order. The runtime additionally requires the chosen winner's head to be in contact and its bite cooldown to have elapsed. A younger head touching an older tail cannot make that tail eat. Both enter and stay callbacks carry contact-part identity; staying in contact can retry after cooldown.

The victim's life and death counters change once, before notifications. All victim bodies immediately stop colliding. Its meshes shrink for 0.24 seconds before pool return. The eater's 0.32-second bite animation ends before its 0.48-second cooldown. `AnimalActivity` owns these clocks; the view reads progress but cannot authorize a meal. Pause supplies zero simulation time and freezes both actions. Reset clears pending returns and action state.

`AnimalMotor` yields to a physical impulse for 0.18 seconds after contact. Otherwise it applies the locomotion strategy's planar speed. Hop travel is analytical, preserving total distance across irregular ticks. Bounds steering uses the actual camera viewport and redirects toward the visible ground area without teleporting or adding walls.

## Articulation and presentation

Each snake has a root body plus 12 dynamic segments joined by `ConfigurableJoint`. `DistanceTrail` stores a bounded ring buffer of positions sampled by distance, skips stationary solver jitter and uses binary search for trail samples. Soft force drives follow those samples; contacts can deflect the links. A species' seeded head and full tail must fit inside its habitat.

The view preserves authored visual/shadow scales and jaw rest rotation when resetting a pooled life. It applies appearance, lift, bite and consumption separately from the motor. uGUI population counters update on events. Time labels are reformatted only when their displayed value changes. Tasty labels cache their Canvas and use its UI camera, rather than searching the hierarchy each frame.

`HabitatAudio` receives injected hop feedback and scene-level arrival/meal/button requests. Eight reusable voices and per-cue cooldowns bound overlapping playback. Its pitch RNG is independent of simulation RNG; mute works during pause. Committed WAV files require no external service to play.

## Time and reproducibility

Arrivals are uniform in [1, 2] simulation seconds using a dedicated RNG. Species selection and animal movement use separate seeded streams. Pause stops the clock, physics and animation; 2x speed doubles simulation time. Restart clears lives, effects and counters, resets speed and the arrival clock, and recreates the initial population. Restart does not rewind the random streams; a fresh session with a fixed seed reproduces their initial sequence.

All domain time consumers reject negative, NaN and infinite steps before changing their state. Settings validate finite dimensions, valid movement parameters, diet and non-negative initial populations, and positive finite mass, weight and turn speed.

## Verification and limits

Run `bash Scripts/validate.sh` for EditMode and PlayMode checks without building a player. It writes NUnit/JUnit reports and logs, including an Editor physics probe for 25, 100 and 200 animals. The probe measures `Physics.Simulate` separately from script updates and rendering; it is not a player FPS claim. See [Validation](Validation.md) for measured results.

No active-population cap is imposed. A long session or a dense crowd still needs profiling on its intended device. A large catalog of species is distinct from a large simultaneous population. This demo has no pathfinding, persistence or networking, and animal behavior remains random wandering as requested.
