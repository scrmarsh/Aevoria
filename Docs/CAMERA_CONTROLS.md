# Current controls - Alpha v0.0.11

Escape opens the menu and releases the cursor. Choose **Character appearance**, select **Doctor**, then **Return to world**. No phrase or unlock is required. Keyboard and mouse bindings are editable under **Controls**; conflicting inputs are rejected and Escape cancels capture. Save bindings to apply them. Controls below are defaults.

| Input | Action |
| --- | --- |
| T | Leave Explore, or enter it at the current map focus |
| F5 | Cycle first person, third person and reverse third person |
| WASD | Move in the current mode |
| Left Ctrl | Sprint on foot; boost in flight |
| Left Shift | Crouch on foot; descend in flight |
| Space | Jump on foot; ascend in flight |
| Double Space | Enter flight or release propulsion into freefall |
| Mouse wheel | Select tools on foot; scale speed in flight |
| 1-6 | Select hands, shovel, pickaxe, axe, stick or crate |
| Left mouse | Use tool, harvest, excavate or strike a targeted resident |
| Right mouse | Place an equipped crate using three wood |
| E / M / Tab | Inventory / map / release cursor |
| Slash | Open the focused command console |
| Escape / F3 / F4 / F11 | Menu / diagnostics / ecosystems / fullscreen |

Walking is4.5m/s and sprinting9m/s. Flight speed is adjustable in Controls and with the flying mouse wheel. Disabling flight keeps pose and momentum for gravity. Explicit saved-pose restoration is separate from entering at the map focus. Typing in the console blocks gameplay shortcuts; Escape closes text entry first. The console exposes registration and dispatch but ships no gameplay commands yet.

Measurement preferences change display units only; internal world dimensions remain SI. New worlds use schema8 / generator10, ecology3 and civilization2. Open older worlds in their matching preserved players. Excavations, depleted boulders, traveler poses and resident health/deaths persist in compatible saves.
## Historical control records

The sections below document earlier milestones and their earlier limitations. The current controls above take precedence.

# Camera controls - Alpha v0.0.3

Default navigation is a free orbit camera. Horizontal movement preserves the focus height and camera elevation; it does not follow hills or push the camera above mountains. Terrain following and collision are separate optional HUD toggles, both off by default. Passing through ground is supported for inspection. Resetting/regenerating a world or deliberately focusing a location sets a new focus height.

| Input | Action |
| --- | --- |
| WASD / arrows | Camera-relative horizontal movement |
| Shift + WASD | Faster travel |
| Right drag | Yaw and tilt, limited to 20-85 degrees |
| Middle drag | Pan on the fixed focus-height plane |
| Wheel | Fast proportional zoom, with HUD sensitivity control |
| R | Reset orientation |
| F | Iteratively focus the terrain beneath the pointer |
| Q / E | Additional yaw controls |
| Page Up / Down | Legacy distance adjustment |

Orbit distance ranges from 2 m to 32 km; the near clip is 0.08 m. Movement speed scales with distance. Keyboard acceleration and exponential focus/orbit interpolation provide smoothing; wheel zoom changes the target distance proportionally and uses a critically damped 0.10 second response for smooth acceleration/deceleration. Input System 1.20 defaults to normalized +/-1 wheel units. The old code assumed +/-120 and therefore produced very slow zoom with the default setting. The controller now handles normalized units and legacy Windows units explicitly; a default notch changes the 900 m target to 504 m, verified by synthetic tests in both modes. Physical mouse acceptance is still pending. Cursor-directed zoom shifts the horizontal focus along its focus plane. This is an approximation, not a terrain raycast or exact object selection.

The camera can cross authoritative bounds. The HUD labels outside-world scenery; a 200 km coordinate guard limits floating-point degradation. No wall or forced terrain-height constraint obstructs travel. Below-ground views are a debug/free-navigation behavior, not a complete underground environment.

Mouse navigation is blocked over the HUD and seed text entry. Window-focus changes cancel dragging and wait for button release. Drag deltas are bounded to prevent window-edge jumps. Existing simulation controls remain independent.

Synthetic tests verify yaw/tilt limits, wheel bounds and sensitivity, pan, HUD blocking, keyboard-relative motion/Shift/reset, constant altitude across terrain, crossing bounds, and optional collision. These tests do not establish physical device acceptance. Manually verify wheel, right/middle drag, text input, alt-tab and window-edge behavior in Editor and player.

Use [ALPHA_v0.0.3_MANUAL_ACCEPTANCE.md](ALPHA_v0.0.3_MANUAL_ACCEPTANCE.md) for the physical acceptance record. It remains unaccepted until a human performs the checks. The Alpha HUD keeps existing panel bounds and the seed-focus guard while using cached strings and fixed rectangles to reduce allocations.

Motion blur is an optional, default-off URP camera-only Volume override with low quality, intensity 0.13 and clamp 0.015. HUD text is drawn afterward and stays sharp. It is not used to conceal streaming failures. The benchmark includes comparable enabled/disabled camera paths.

Alpha v0.0.4: normal startup opens the menu. Escape opens/closes the paused world menu; the compact world bar supplies pause/resume, save and menu controls. F3 toggles the retained developer HUD. Menus block world camera input; hidden HUD rectangles do not. The creation and developer terrain controls share the 3,200 m relief ceiling. Display changes use a 15-second keep/revert timer. Physical input acceptance remains separate from scripted tests.

## Ecology inspection in Alpha v0.0.5

F4 toggles ecology inspection. Camera focus selects its region; six small regional maps show actual ecological data. Hovering its control panel blocks camera input. F3 retains the developer HUD. Neither panel enables terrain following automatically. Physical-input acceptance remains open in ALPHA_v0.0.5_MANUAL_ACCEPTANCE.md.

## Alpha v0.0.6 controls

Independent WASD/arrows, middle drag and edge scrolling options are in Controls. Combined movement is clamped; UI, typing, lost focus and out-of-window positions gate movement. The god camera can look upward and fly through terrain with optional follow/collision off. Recenter only resets the view. Six visible speed buttons change the simulation clock from 0.25× through 8×.

M opens the full map. In Explore, WASD walks, Ctrl sprints, Shift crouches, Space jumps, V changes first/third person, E opens inventory, Tab releases cursor, 1–6 choose prototype tools, LMB uses a tool and RMB places a funded test crate. F11 toggles borderless fullscreen and remembered window size. See [exact bounds](LIVING_WORLD.md) and [manual acceptance](ALPHA_v0.0.6_MANUAL_ACCEPTANCE.md).


## Alpha v0.0.8 playtesting expansion

Current controls supersede the historical distance and safety descriptions above. Wheel input is normalized from either one-notch or legacy 120-unit deltas; a default notch multiplies the orbit target by exp(-0.242), approximately 0.785. Page Up/Down uses the same multiplicative model. Targets interpolate smoothly and remain finite. Panning scales with actual ground clearance, with a configurable 60 m/s minimum and Shift boost. The theoretical relief control is 16,000 m.

God-mode safety always stays at least 3 m above authoritative ground/water and installed stitched/morphed terrain. Additional Slope Clearance enables wider slope probes; it cannot turn basic safety off. Publication notifications request a before-render correction, and safety lifts release smoothly downward. Near clipping follows clearance with a world-coordinate precision guard, approximately 0.32 m at 800 km coordinates, capped below God-mode terrain clearance. Ground-view far clipping stays at 180 km and expands with the current overview range. Explore applies the same precision guard with a 180 km far plane. High/low zoom traces and rapid alternating input are retained in the expanded validation evidence. These scripted checks remain distinct from physical mouse/keyboard acceptance.

In Explore mode, double Space toggles flight within the configurable double-tap interval; single Space retains walking jump. In flight: WASD move, Space rise, Shift descend, Ctrl accelerate, V toggles first/third person. Flight speed is configurable. Turning flight off seeks safe supported dry ground. If that is unavailable nearby, flight remains active and reports the reason. The optional airborne flag persists with traveler state. See [expanded implementation](ALPHA_v0.0.8_ADDENDUM.md).
