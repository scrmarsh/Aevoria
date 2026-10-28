# Alpha v0.0.12 character comparisons

The Windows images below are actual player backbuffer captures. The source
turnarounds are explicitly labeled Blender diagnostics. They establish mesh
changes; they do not replace the separate Windows review.

## Actual Windows before and after

| Preserved v0.0.11 | Packaged v0.0.12 |
|---|---|
| ![Original native Doctor](Validation/Alpha-v0.0.12/Comparisons/before-v11-front.png) | ![Remastered native Doctor](Validation/Alpha-v0.0.12/Native-Final-Package/doctor-idle-front.png) |
| ![Original native walking](Validation/Alpha-v0.0.12/Comparisons/before-v11-walking.png) | ![Remastered walking sample](Validation/Alpha-v0.0.12/Native-Final-Package/doctor-walk-0012.png) |

The historical Windows captures have different camera framing. They remain
unmodified. No original native v11 face close-up/profile was captured; the
preserved editable sources provide the matching studio comparisons below.

## Editable source comparison — Blender diagnostic

Both source versions use the same renderer, lights and portrait scale. Camera
height follows each source's actual head height. Neither source file is altered
by rendering.

| Legacy source | Remastered source |
|---|---|
| ![Legacy face](Validation/Alpha-v0.0.12/Blender-Legacy-Before/face-front.png) | ![Remastered face](Validation/Alpha-v0.0.12/Blender-Neck11-After/face-front.png) |
| ![Legacy profile](Validation/Alpha-v0.0.12/Blender-Legacy-Before/face-profile.png) | ![Remastered profile](Validation/Alpha-v0.0.12/Blender-Neck11-After/face-profile.png) |

## Final Windows details

| Face and attached neck | Crouch and cane support |
|---|---|
| ![Native face](Validation/Alpha-v0.0.12/Native-Final-Package/doctor-face-front.png) | ![Native crouch](Validation/Alpha-v0.0.12/Native-Final-Package/doctor-crouch-side.png) |
| ![Native profile](Validation/Alpha-v0.0.12/Native-Final-Package/doctor-face-profile.png) | ![Back and clothing](Validation/Alpha-v0.0.12/Native-Final-Package/doctor-idle-back.png) |

[Left tool grip](Validation/Alpha-v0.0.12/Native-Final-Package/doctor-left-tool-grip.png),
[right cane grip](Validation/Alpha-v0.0.12/Native-Final-Package/doctor-right-cane-grip.png),
[first person](Validation/Alpha-v0.0.12/Native-Final-Package/doctor-first-person.png),
[third person](Validation/Alpha-v0.0.12/Native-Final-Package/doctor-third-person.png),
[reverse third person](Validation/Alpha-v0.0.12/Native-Final-Package/doctor-reverse-third-person.png).
The audit temporarily selects Hands for clear face portraits; the cane remains.

The final directory includes every shovel/pickaxe/axe/stick/crate strike and
first-person view for Doctor, Roman and Explorer, plus jump/flight evidence and
save/reload results. Forced-distance-mesh crouch views verify the
[first](Validation/Alpha-v0.0.12/Native-Final-Package/doctor-crouch-lod1.png) and
[second](Validation/Alpha-v0.0.12/Native-Final-Package/doctor-crouch-lod2.png) LODs;
these are intentionally shown closer than their normal display distance.

## Actual animation samples

- [Doctor pickaxe cycle, repeated](Validation/Alpha-v0.0.12/Native-Final-Package/Replays/doctor-Pickaxe-strike-loop.mp4)
- [Roman right-handed pickaxe cycle, repeated](Validation/Alpha-v0.0.12/Native-Final-Package/Replays/roman-Pickaxe-strike-loop.mp4)
- [Doctor walking](Validation/Alpha-v0.0.12/Native-Final-Package/Replays/doctor-walk.mp4)
- [Crouch walking](Validation/Alpha-v0.0.12/Native-Final-Package/Replays/doctor-crouch.mp4)
- [Faster movement](Validation/Alpha-v0.0.12/Native-Final-Package/Replays/doctor-run.mp4)
- [Uphill](Validation/Alpha-v0.0.12/Native-Final-Package/Replays/doctor-uphill-walk.mp4) and [downhill](Validation/Alpha-v0.0.12/Native-Final-Package/Replays/doctor-downhill-walk.mp4)

Strike frames were captured every second simulated frame at60fps and replay at
30fps. Walking videos are8fps **sampled replays**, not continuous real-time video.
Original PNG sequences and action/contact CSVs remain beside them.

![Walking sequence](Validation/Alpha-v0.0.12/Native-Final-Package/Replays/doctor-walk-sequence.png)

![Crouch sequence](Validation/Alpha-v0.0.12/Native-Final-Package/Replays/doctor-crouch-sequence.png)
