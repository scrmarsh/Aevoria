# Editable character sources — v0.0.12

The active Doctor source is `Remaster012/Doctor012.blend`, with distance meshes
`Doctor012_LOD1.blend` and `Doctor012_LOD2.blend`. The source contains an anatomical
head and neck, shaped clothing, articulated hands, 37 bones, UVs, vertex Color
and Metallic attributes. The cane is a separate equipment asset mounted to the
right wrist at runtime. Normal Doctor tools attach to the left wrist.

`Tools/ExportCharacterRemaster012.py` exports **edits in these Blender files** to
the actual runtime JSON. Run Blender in background mode:

```powershell
& 'C:/Program Files/Blender Foundation/Blender 5.2/blender.exe' --background --python Tools/ExportCharacterRemaster012.py -- M:/Aevoria SourceAssets/Characters/Remaster012/Doctor012.blend
```

Run `Tools/BuildCharacterLods012.py` after changing the near mesh to regenerate
and export the distance meshes. `BuildCharacterRemaster012.py` recreates the
initial authored geometry and overwrites the Remaster012 sources; do not run it
over manual artist edits. Keep a source backup before explicit regeneration.

Editable Unity base clips are in `Assets/Resources/Characters/Motion012`.
They are sampled by the actual runtime and followed by live ground/cane contact
and first-person arm IK. Builds preserve artist edits to existing clips. The
Unity menu `Aevoria/Characters/Rebake Doctor 012 animation clips` explicitly
replaces them with seeds sampled from the rig's motion authoring code.

The head uses adapted MakeHuman CC0 mesh/morph data, with licenses and the pinned
source commit in `Remaster012/MakeHuman`. See `Remaster012/PROVENANCE.md`.
Observed likeness and visual acceptance are recorded separately from test passes.

## Preserved legacy assets

`Explorer.blend`, `Roman.blend` and `Doctor.blend` preserve the older procedural
exports. The old Doctor and its left-wrist cane are **not the v0.0.12 runtime
asset**. The companion `*-skin.json` files retain their historical export data.

Runtime geometry and animation parameters are authored in `Assets/Scripts/Presentation/HumanoidPresentation.cs`. `RomanSoldier.cs` retains the old public component as a Roman-only compatibility adapter. Blender edits are editable source work, and are not automatically imported back into the runtime procedural generator.

The supplied reference at `Docs/Validation/Alpha-v0.0.10/UserReferences/doctor-turnaround.png` was inspected for the doctor's clothing, face/hair proportions and cane. This is an authored procedural approximation, not a photographic facial reconstruction. Native screenshots and the current development report determine observed likeness and animation limitations; mesh complexity alone does not establish reference fidelity.

Reproduce the export with `AEVORIA_CHARACTER_SOURCE` pointing to this folder and `Tools/ValidatePhase1.ps1 -Action characters` using the isolated D: validation project. Then run Blender in background mode with `--python Tools/CreateEditableCharacters.py -- <this-folder>`. Keep Unity inactive in the validation cache while copying source into it.
