# Godot host and native integration design

## 1 Native service inventory

Godot owns the following capabilities. The host integrates them; it does not build parallel replacements.

| Capability | Native facility | Application-specific responsibility |
| --- | --- | --- |
| Scene construction | PackedScene, ResourceLoader, scene ownership | Resolve approved sceneRole |
| Movement/collision | CharacterBody2D, CollisionShape2D, MoveAndSlide | Desired velocity/profile and observations |
| Path following | NavigationAgent2D and NavigationServer2D | Actor/target mapping, blocked result |
| Perception queries | Area2D, RayCast2D or physics-space queries | Evidence confidence and knowledge policy |
| Scheduled signals | Timer with physics callback | Domain tick and AI pulse admission |
| Input | InputMap and unhandled input | Intent -> typed command |
| Presentation | AnimationPlayer/AnimationTree, AudioStreamPlayer | Event-to-animation/sound mapping |
| UI | Control, Container, theme and focus navigation | Session views and accessible feedback |
| Storage | FileAccess/DirAccess | Slot policy through save owner |
| Trusted assets | ResourceLoader and approved resource packs | Trust and role catalog |
| Editor authoring | EditorPlugin, Inspector, EditorUndoRedoManager | Scenario Kit forms and validators |

2D is a reversible prototype default. If 3D is selected, replace native adapter variants, not generic engine contracts.

## 2 Scene composition

~~~text
GameBootstrap
  ScenarioGameHost.cs
  SimulationTickTimer                 physics callback, 0.05 seconds
  DecisionPulseTimer                  physics callback, 0.25 seconds
  SessionWorld
    ZoneRoot                          current PackedScene
      NavigationRegion2D
      Terrain and collision
      SpawnAnchors                    stable IDs
      ExitAreas                       native Area2D
      InteractionAreas
    ActorRoot
      ActorBody                       CharacterBody2D
        CollisionShape2D
        NavigationAgent2D
        VisualRoot
        AnimationPlayer
        SensorAreas
  Camera2D
  AudioRoot
  CanvasLayer
    MenuRoot
    TacticalHud
    CommandPanel
    RouteMap
    ExplanationPanel
    NotificationPanel
~~~

Only approved authored scene roles are instantiated. Scene nodes have authored stable IDs; NodePath/GetInstanceId values are not persistent entity IDs.

## 3 Host startup

1. Configure ApplicationDiagnostics and ApplicationNotifications before loading content.
2. Create ScenarioContentService and register built-in asset roles and layer implementations.
3. Validate built-in catalog; display a repair screen if built-in content is invalid.
4. Show scenario menu with Ready/Scanning/Rejected status for each source.
5. On launch, pin pack digest and layer version, disable duplicate launch and begin scene load.
6. Resolve PackedScene and bind required anchors to declared zone/actor IDs.
7. Wait for navigation synchronization using the documented map-change/synchronization pattern; do not query a new map in _Ready and assume it is ready.
8. Capture initial native poses and sensing state. Create engine at tick 0.
9. Enable input, movement callbacks and simulation/decision timers.
10. On failure, free incomplete scene, keep menu usable and report the operation reference.

ResourceLoader asynchronous/threaded loading may be used through its native API. Main-thread scene instantiation is awaited explicitly. There is one owned launch operation and cancellation source.

## 4 Clock and timing

SimulationTickTimer uses process_callback = physics and wait_time = 0.05 seconds. Its timeout calls one Step for the next integer rule tick using the last complete native observation frame. It never derives domain time from DateTime.Now.

DecisionPulseTimer emits actor decision pulses every 0.25 seconds. Evidence/arrival signals can also request a decision. Timers stop when the session is inactive and pause with the world. The pure C# simulator provides equivalent tick/pulse input; the engine is not a second native timer service.

Actor _PhysicsProcess runs only while movement/collision behavior is active. It does not periodically rescan unchanged objectives, menus or packs. Use signals for invalidation and Timer for scheduled native work.

Configure/document native process order so movement/sensor capture completes before an observation frame is published. A frame carries a monotonic native-frame number; a Step consumes one immutable frame. A timeout cannot read half-written sensor lists. Test this order in the compatibility spike.

If a slow frame delays a timeout, record a slow-tick diagnostic. Do not skip rule ticks or inject wall-time catch-up. Native gameplay may slow under load; that failure is visible. Prototype speed options are pause, 1x and compatibility-tested 2x using one Engine.time_scale policy shared by timers and movement.

## 5 Native movement adapter

For a move request:

1. Resolve entity and generation; reject stale actor or unloaded zone.
2. Set NavigationAgent2D target only when the order/target changes.
3. While moving, call GetNextPathPosition according to native physics-step guidance.
4. Compute desired velocity from the layer's current movement profile.
5. If avoidance is enabled, use VelocityComputed safe velocity.
6. Move the CharacterBody2D with MoveAndSlide/MoveAndCollide, not direct position assignment.
7. Report accepted body pose, arrival, obstruction and relevant collision facts.
8. Stop movement callbacks on completion, cancellation, incapacitation or world teardown.

NavigationAgent does not physically move the body. Avoidance is not a replacement for collision. Arrival tolerance is configured relative to speed/radius and tested in doorways; there is no collision-bypassing snap.

Blocked result contains requestId, last accepted pose, reason and reachable status. A later retry uses a changed target/evidence/map revision or the layer's bounded retry policy.

## 6 Sensor and WorldFrame contract

Native evidence producers send typed observations into a host buffer: PoseObserved, DestinationReached, PathBlocked, SightSample, NoiseSample, ExitEntered and DecisionPulse. Every observation has a monotonic sequence and a world generation.

SightSample includes observer/target, accepted positions, native line-of-sight result, distanceMm and observed tick. It is not a command to declare the player detected. LostSoldierLayer applies awareness rules and emits the domain detection event.

WorldFrame is captured after native updates and assigned to exactly one rule tick. Normal runtime cannot submit a pose outside the loaded navigation/collision envelope without an explicit spawn/restore operation. Synthetic test observations are clearly marked by the simulator and never shipped as a fallback for a failing sensor.

## 7 Zone transitions

Zone exits are native Area2D sensors. Entering emits evidence; the layer checks route unlock, target, resource cost and actor proximity. The transition sequence is:

- Commit ZoneTransitionRequested with destination, party list, cost reservation and generation N+1.
- Enter Transitioning and freeze rule timers/orders.
- Load destination scene and validate anchors/navigation without unloading the old committed snapshot.
- On success, initialize native actors at declared entry anchors, commit next-generation frame and consume the reserved travel cost once.
- On failure, release reservation, preserve old zone snapshot and show retry/return-to-menu.
- Disconnect/free old scene only after the new world is bound.

The implementation must test load failure after request and duplicate ready callbacks. A transition cannot charge sustainment twice.

## 8 UI and input design

| View | Input/feedback | Empty/error behavior |
| --- | --- | --- |
| Scenario menu | Title, layer, pack status, New/Resume | Missing layer or invalid pack gives repair reason |
| Tactical HUD | Condition, wounds, fatigue, heat, free/reserved supplies | No false zero replacing unavailable state |
| Command panel | Selected actors, enabled actions, estimated costs | Rejected action keeps selection and explains reason |
| Route map | Known exits, duration, cost, risk/confidence | Unknown route labelled unknown |
| Explanation panel | Observed detection/evidence and action outcomes | Does not reveal hidden omniscient AI knowledge |
| Save/load | Slot time, scenario and compatibility | Failed save never overwrites last-good success label |
| Ending view | Mission/return/people consequences and contributing facts | Uncovered ending shows diagnostic, not victory |

Input actions: select, multi_select, move_or_interact, cancel_order, pause, open_map, inspect, next_actor, focus_objective and open_menu. Bindings are remappable through native InputMap. UI consumes input before world commands; clicking a button cannot also fire a weapon.

Pause/menu containers process when paused; world timers and bodies do not. Use native focus traversal, clear focus restoration on modal close, scalable containers and minimum text sizes. Feedback uses text/icon as well as sound/color. Localization uses keys, explicit missing-key diagnostics and EN/AR layout tests before claiming language support.

## 9 Animation and audio binding

Committed Fired/Wounded/ActionCompleted events drive native animation/audio. Rendering never inflicts damage on an animation frame callback. Cancelled actions stop or blend native animation safely. Audio may communicate a hidden cue only if the player could hear it by the same evidence rules; debug logs are not player audio sources.

No final visual style, approved assets or Game Arts profile exists. Asset production must follow the external Game Arts workflow; temporary role bindings cannot be presented as approved production art.

## 10 Compatibility and source evidence

Official sources inspected 24 October 2026; stable URLs may advance. The standalone C# SDK is pinned to 10.0.401 and its runner/checks execute on .NET 10.0.12. Libraries target net8.0 for host consumption. Godot editor, export templates, renderer and its runtime combination remain unpinned/unverified; these standalone results do not establish Godot compatibility.

| Claim | Primary source | Local execution |
| --- | --- | --- |
| C# requires .NET-enabled editor and SDK; target support varies | [Godot C# basics](https://docs.godotengine.org/en/stable/tutorials/scripting/c_sharp/c_sharp_basics.html) | not_run |
| Agents calculate paths/avoidance and need body movement integration | [Navigation agents](https://docs.godotengine.org/en/stable/tutorials/navigation/navigation_using_navigationagents.html) | not_run |
| Character bodies use collision-aware native movement | [CharacterBody guide](https://docs.godotengine.org/en/stable/tutorials/physics/using_character_body_2d.html) | not_run |
| Timer emits scheduled timeout and can use physics callback | [Timer reference](https://docs.godotengine.org/en/stable/classes/class_timer.html) | not_run |
| PCK/ZIP packs can contain executable content | [Packs and mods](https://docs.godotengine.org/en/stable/tutorials/export/exporting_pcks.html) | not_run |
| ZIPReader exposes file/member reading; allocation bounds still need proof | [ZIPReader reference](https://docs.godotengine.org/en/stable/classes/class_zipreader.html) | not_run |

The spike must pin editor, Godot.NET.Sdk, target framework, runtime architecture, renderer and export templates together. It must execute project load, timers, scene synchronization, navigation/collision, native sensor capture, .NET journal replay and packaged Windows startup. Newer online documentation is not upgrade authorization or proof of compatibility.

## 11 Broader game-type integration

[Chapter 09](09_GAME_TYPE_EXTENSIBILITY.md) separates proposed world persistence, request acknowledgements and logical advancement from native services. The [25 October research](../research/GAME_TYPE_ENGINE_CAPABILITY_RESEARCH.md) rechecks ResourceLoader background loading, Timer behavior, navigation optimization, AStarGrid2D, large-world coordinates and high-level multiplayer. These are documented native facilities, not executed compatibility evidence. Preserve native ownership; qualify the selected game/renderer/export/runtime combination before enabling an extension. Co-op remains optional future scope.
