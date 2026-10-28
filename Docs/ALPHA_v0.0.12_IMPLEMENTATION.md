# Alpha v0.0.12 — The Character Remaster Hotfix

This Alpha Development package replaces the Doctor's legacy procedural mesh
with an editable anatomical mesh and repairs crouch, grip, handedness and tool
presentation. The final Windows captures and executed checks are linked in the
[validation report](ALPHA_v0.0.12_TEST_RESULTS.md) and
[comparison gallery](ALPHA_v0.0.12_NATIVE_GALLERY.md).

## Character and equipment

The Doctor source has 70,508 vertices, 110,074 triangles, 37 bones and two UV
layers. It includes an anatomical head/neck, tapered limbs, modeled fingers,
blazer/lapels/pockets, open shirt collar and shaped shoes. The supplied reference
provides skin albedo on the actual mesh. MakeHuman CC0 anatomical asset data,
exact provenance and licenses are retained with the source. No reference image
is drawn over the gameplay view.

The neck flares into the upper chest and extends beneath the shirt collar.
Lower skin weights blend from the torso into the neck, then into the head;
animation no longer pulls the lower neck away from the torso. The accepted head
shape was retained when making this attachment correction.

Doctor uses the right hand for his detailed cane and the left for ordinary
tools. Explorer and Roman retain their right tool hand. The pickaxe has curved
tapered points, a socket and wedge; the shovel has a concave blade, foot rests,
socket, rivets and D grip; the axe has a cutting wedge, poll and socket. The stick,
plank crate and cane also have detailed editable sources. All use UVs, normals,
vertex albedo and metallic data with wood/metal surface shading.

Tools carry upright. A wrist roll puts the fingers across the handle; the mount
cancels that roll and applies the tool's cutting-plane alignment. Windup, strike
and recovery follow the actual action timer. Crouched carry compensates for the
torso/arm bend so the pickaxe clears the head. Doctor first person uses his real
body and left arm, with a masked head and eye-relative hand IK.

## Motion and runtime ownership

Doctor walking is 1.65m/s, faster movement 3.3m/s and crouching .8m/s. Explorer and
Roman retain 4.5/9/2m/s. Doctor's right stride and knee flexion are shorter, with
unequal right/left support timing, hip shift and cane support. Foot and cane IK
use actual ground contact without moving the physics root. Crouch blends bent
hips/knees/ankles, lowered/backward pelvis and counterbalanced arms.

Nine editable Unity clips supply base poses, followed by live contact, hand and
first-person overrides. Ordinary builds preserve clip edits; explicit rebaking
is available from the character menu in Unity. Distance meshes contain 44,009
and 13,188 triangles. Bone weights are remapped by name from Blender hierarchy
order to the runtime bind order. All three skins share the skeleton/material;
their meshes and equipment resources are released with the character.

## Editing and preservation

- [Character source instructions](../SourceAssets/Characters/README.md)
- [Equipment sources](../SourceAssets/Equipment/Remaster012/README.md)
- [Asset provenance](../SourceAssets/Characters/Remaster012/PROVENANCE.md)
- [Preserved source checkpoint](../Archives/Alpha-v0.0.12-Source)

`BuildCharacterRemaster012.py` recreates the initial mesh; it overwrites its
Remaster012 source files. `ExportCharacterRemaster012.py` instead exports actual
artist edits from existing Blender sources. `BuildCharacterLods012.py` rebuilds
the distance sources after a near-mesh edit. Legacy Blender sources and official
v0.0.11 runtime/source files remain intact.

World sources, World.dll, generator10/schema8 identity and the dialogue source
remain unchanged. Existing Chronicles are preserved; no new literary chapter is
added. Save compatibility was exercised against a copied v0.0.11 world in an
isolated save root.

## Remaining character-quality limits

- This is a semi-realistic alpha character, with stylized hair strands, cloth
  folds and fingers visible at extreme close range.
- The supplied reference limits albedo resolution. Some lighting is present in
  that image; the shader reduces its contrast before applying world lighting.
- The cynical facial expression is static; speech/facial animation is not added.
- Cloth follows skin weights rather than a physical cloth simulation.
- Locomotion uses authored base curves and contact IK, not motion capture or a
  full physical gait simulation. The wider NPC art remains unchanged.
