# Active v0.0.12 equipment

Pickaxe: curved tapered forged steel head, socket, wedge, shaped hickory haft.
Shovel: concave blade, foot rests, rivets, metal socket, wooden shaft and D grip.
Axe: convex cutting wedge, cheeks, poll, eye/socket and shaped shaft.
Stick: tapered irregular branch and knots. Crate: separate planks, metal bands,
nails. Cane: curved grip, slender shaft, colored inlays and rubber ferrule.

Each `.blend` is the editable source for the matching runtime JSON in
`Assets/Resources/Equipment`. All include UVs, normals and Color/Metallic data.
Use `Tools/ExportCharacterRemaster012.py` to export actual Blender edits.

Handles are authored along +Y, with grip at the origin. Pickaxe and axe cutting
heads are authored in XY. `EquipmentGrip012.cs` aligns them to the forward swing
plane and compensates for the wrist roll. Shovel keeps its broad blade face
forward. Doctor alone uses the left tool hand; the right hand holds his cane.
