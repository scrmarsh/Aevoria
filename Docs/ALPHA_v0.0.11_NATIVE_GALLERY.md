# Alpha v0.0.11 native evidence gallery

All images below are captured from actual Windows players. Manifests and logs live beside them. No generated artwork is used as gameplay evidence.

## Controlled before/after comparisons

The following pairs use the same geographic seed, settings and view. Rock and water changes preserve the physical fingerprint; the boreal fix changes ecological biomass rather than terrain.

| Review | Before | After |
| --- | --- | --- |
| Rock albedo | [Woven surface](Validation/Alpha-v0.0.11/Native-Full-FilteredSurface-16000/Miniature-mountain-diagnostic-3.png) | [Triplanar rock](Validation/Alpha-v0.0.11/Native-WaterMaterials-WindowsD3D11/Miniature-mountain-diagnostic-3.png) |
| Cold forest at -5.1C | [Ecology2 suppresses stand](Validation/Alpha-v0.0.11/Native-Boreal-D3D11/boreal-stand.png) | [Ecology3 renders conifers](Validation/Alpha-v0.0.11/Native-Boreal-Ecology3/boreal-stand.png) |
| Lake depth palette | [Bright inland cyan](Validation/Alpha-v0.0.11/Native-Full-WindowsD3D11-16000/Miniature-lake.png) | [Deeper blue column](Validation/Alpha-v0.0.11/Native-WaterMaterials-WindowsD3D11/Miniature-lake.png) |
| River bank scale | [Broad pale bank](Validation/Alpha-v0.0.11/Native-Full-WindowsD3D11-16000/Miniature-major-river-close.png) | [Width-scaled wet bank](Validation/Alpha-v0.0.11/Native-WaterMaterials-WindowsD3D11/Miniature-major-river-close.png) |

## Geographic scale and movement

- [Current overview](Validation/Alpha-v0.0.11/Native-WaterMaterials-WindowsD3D11/Miniature-overview.png), [23.9km mountain](Validation/Alpha-v0.0.11/Native-WaterMaterials-WindowsD3D11/Miniature-mountain-range.png), [ordinary terrain](Validation/Alpha-v0.0.11/Native-WaterMaterials-WindowsD3D11/Miniature-plain.png), [plateau](Validation/Alpha-v0.0.11/Native-WaterMaterials-WindowsD3D11/Miniature-plateau-region.png).
- [Desert](Validation/Alpha-v0.0.11/Native-WaterMaterials-WindowsD3D11/Miniature-Desert.png), [temperate forest](Validation/Alpha-v0.0.11/Native-WaterMaterials-WindowsD3D11/Miniature-Forest.png), [cold snow region](Validation/Alpha-v0.0.11/Native-Climate-HardwareDepth/cold-snow-range.png).
- Same river at [180m](Validation/Alpha-v0.0.11/Native-WaterMaterials-WindowsD3D11/Miniature-river-zoom-180.png), [1.8km](Validation/Alpha-v0.0.11/Native-WaterMaterials-WindowsD3D11/Miniature-river-zoom-1800.png), [14km](Validation/Alpha-v0.0.11/Native-WaterMaterials-WindowsD3D11/Miniature-river-zoom-14000.png), and [48km](Validation/Alpha-v0.0.11/Native-WaterMaterials-WindowsD3D11/Miniature-river-zoom-48000.png). [Moving captures and frame log](Validation/Alpha-v0.0.11/Native-WaterMaterials-WindowsD3D11/water-motion/frames.csv) are recorded separately from [uncaptured timing](Validation/Alpha-v0.0.11/Native-WaterMaterials-WindowsD3D11/water-zoom-without-capture.csv).
- [Preserved v0.0.10 overview](Validation/Alpha-v0.0.11/Baseline-Native-Miniature/Miniature-overview.png) uses3600m relief; the current overview above uses16000m. This historical pair is not a controlled relief comparison.

[Sampled native zoom replay](Validation/Alpha-v0.0.11/Native-WaterMaterials-WindowsD3D11/water-motion/sampled-native-zoom.mp4) plays captured frames at10samples/second; it is a visual replay, not real-time frame-rate evidence. [Current walking cave entry](Validation/Alpha-v0.0.11/Native-Cave-WindowsD3D11/entrance-walk-inside.png), [mining](Validation/Alpha-v0.0.11/Native-Cave-WindowsD3D11/wall-excavated.png), and [reload](Validation/Alpha-v0.0.11/Native-Cave-WindowsD3D11/underground-reloaded.png) have a verified clean process exit.

## Settlement and controls

- [Founding housing](Validation/Alpha-v0.0.11/Native-Settlement-WindowsD3D11/founding-village.png), [paid construction and roads](Validation/Alpha-v0.0.11/Native-Settlement-WindowsD3D11/paid-village-growth.png), [resident movement](Validation/Alpha-v0.0.11/Native-Settlement-WindowsD3D11/resident-motion.csv), [injury/death/reload result](Validation/Alpha-v0.0.11/Native-Settlement-WindowsD3D11/result.txt).
- [Binding conflict](Validation/Alpha-v0.0.11/Native-Inputs-WindowsD3D11/binding-conflict.png), [actual hotbar selection](Validation/Alpha-v0.0.11/Native-Inputs-WindowsD3D11/grounded-hotbar.png), [flight speed](Validation/Alpha-v0.0.11/Native-Inputs-WindowsD3D11/flight-speed.png), [real process restart result](Validation/Alpha-v0.0.11/Native-Inputs-WindowsD3D11/input-restart-result.txt).

Earlier Direct3D12 runs provide specific gameplay and visual observations but do not prove clean process shutdown. Current Windows Direct3D11 reviews explicitly record exit status. Stylized assets, some hard material boundaries and some confluence transparency remain alpha limitations; the gallery does not establish seamless rendering at every location and LOD.
