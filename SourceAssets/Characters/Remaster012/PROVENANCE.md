# Character source provenance

The anatomical head and neck adapt MakeHuman's bundled base mesh and male
age/nose morph target **asset data**, released as CC0. No MakeHuman program code
is incorporated. Upstream commit: `a8bc2d54ff0ac92e78ff71431b1023eda42bf482`.

- [Upstream asset-license statement](https://github.com/makehumancommunity/makehuman/blob/a8bc2d54ff0ac92e78ff71431b1023eda42bf482/LICENSE.md)
- [Base mesh](https://github.com/makehumancommunity/makehuman/blob/a8bc2d54ff0ac92e78ff71431b1023eda42bf482/makehuman/data/3dobjs/base.obj)
- [Age targets](https://github.com/makehumancommunity/makehuman/tree/a8bc2d54ff0ac92e78ff71431b1023eda42bf482/makehuman/data/targets/macrodetails)
- [Nose targets](https://github.com/makehumancommunity/makehuman/tree/a8bc2d54ff0ac92e78ff71431b1023eda42bf482/makehuman/data/targets/nose)

The exact imported data and license statements are retained in `MakeHuman/`.
The head is cropped from that topology, morphed, rescaled, shaped at the neck,
weighted, UV preserved, colored, and augmented with eyes, hair and stubble.
Clothing, hands, shoes and all equipment are authored in the project Blender
script. Distance sources retain their decimated editable topology and weights.

The visual reference is the previously supplied
`Docs/Validation/Alpha-v0.0.10/UserReferences/doctor-turnaround.png`.
The reference supplies proportions, outfit/hair guidance and the frontal skin
albedo, mapped through the editable HouseReferenceUV layer onto the anatomical
mesh. ReferenceSkin fades the projection toward side/neck vertex colors. The
runtime shader reduces baked illumination before applying dynamic world light.
The original image is retained unchanged in Resources/Characters/DoctorReference.
It is not pasted over the game view or substituted for the actual character mesh.
