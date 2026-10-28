# Alpha v0.0.3 physical control acceptance

Status: **Not performed by a human in this automated session.** Synthetic input tests and scripted camera paths do not check mouse feel, physical devices or alt-tab ergonomics. Leave each item unaccepted until exercised in the Editor and the Windows player. Record hardware, date and any failure beside the item.

Use the current build in Builds/Alpha-v0.0.3. Keep terrain following, collision and motion blur off initially. The HUD must identify the application as Aevoria Alpha v0.0.3.

- [ ] Wheel: one notch visibly changes distance; rapid reverse scrolling settles smoothly. Adjust sensitivity and confirm the effect. Try both a physical wheel and trackpad if available.
- [ ] WASD/arrows: cross hills horizontally; camera altitude stays constant. Shift increases travel speed; releasing keys decelerates without drift.
- [ ] Right drag: yaw and tilt remain controllable at both pitch limits; leaving the window and releasing the button does not produce a jump on return.
- [ ] Middle drag: ground-plane panning follows the pointer; releasing stops the gesture.
- [ ] Close/high zoom: inspect near 2 m and 32 km; no forced terrain following, missing world or unusable clipping. Passing through terrain is allowed by default.
- [ ] Optional following/collision: enable each separately and confirm the requested constraint; disable it and recover free navigation.
- [ ] HUD blocking: scroll/drag over sliders, buttons and panel scroll area without navigating the camera. Start/Pause/Resume and every speed remain functional.
- [ ] Seed text: enter letters including WASD/Q/E/R/F; they must edit the seed without moving/resetting the camera. Click outside the panel and recover navigation. Regenerate and confirm the clock/state persist.
- [ ] Focus: hold a drag or movement key, alt-tab away, release outside, return; no stuck input, jump or unintended navigation. Repeat while seed text is focused.
- [ ] Low coast: inspect the shore edge and underwater surface at grazing angles. Record bright lines, gaps, depth artifacts or excessive glare.
- [ ] Streaming: traverse forests/ranges rapidly and repeatedly zoom out/in. Inspect skirts, silhouettes, overview/detail coverage, shadow changes and tree LOD/distance fades. Record visible popping rather than inferring success from FPS.
- [ ] Motion blur: enable/disable while moving; HUD stays readable and blur does not mask missing terrain.

Acceptance record: tester __________; date __________; Editor/player __________; mouse/keyboard __________; display/presentation settings __________; issues __________.
