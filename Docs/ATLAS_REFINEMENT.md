# PAUSED BY USER - 2026-10-27

See the newest section of Docs/AEVORIA_RESUME.md for authoritative build, test
and source differences. Atlas5 was intentionally interrupted. Work must remain
paused until the user resumes. The implementation below is a development pass;
full visual acceptance and release are still open.

# Approved atlas refinement — 2026-10-27

User approved implementation with “ok start working on it”. The supplied image
is preserved at `Docs/Validation/Alpha-v0.0.14/References/Aevoria_Fantasy_Atlas_Overhead.png`.
It is art direction for procedural worlds, not a request to bake in an image.

## Approved scope

- Connected snowy northern ranges, separate central/southern ranges, broad
  intervening plains and valleys, branching foothills and readable exposed rock.
- One or two substantial Mediterranean-like seas with natural bays, straits,
  peninsulas and islands. Greatly reduce scattered ponds; retain justified lakes.
- Terrain-led connected rivers, smooth transitions, coherent tributaries,
  widths/banks/confluences/mouths and desert floodplains; no random decorative bends.
- Tundra, alpine meadow/scree, permanent snow/glacier, boreal and mixed forest,
  grassland/flower meadow, Mediterranean woodland/scrub, riparian corridors,
  wetlands, rocky/sandy coasts, steppe, rocky/sand desert, salt flats and oases.
  Distinct biological features and vegetation appropriate to each habitat.
- Geographic blending and a navy/turquoise ocean, green central region, muted
  Mediterranean transition, warm southern desert and pale northern snow palette.
- F3 names the current biome and cardinal facing direction. Explorer uses the
  player's heading; World mode uses the camera's heading.
- Map and minimap always show the entire playable world, fixed north-up,
  independent of main-view movement, rotation or zoom. Main camera stays usable.
- Player marker: small white circle. Village marker: small brown house.

## Preservation and implementation

Before edits, 581 source files were copied and hash verified in
`Archives/Alpha-v0.0.14-Development-BeforeAtlas`; manifest:
`Docs/Validation/Alpha-v0.0.14/pre-atlas-source-manifest.json`.
Original user saves, accepted character work, Chronicles and official archives
are outside this change. No staging or commits. Latest official release is v12;
this remains v14 development.

New menu-created worlds select persisted `GeographyProfile.Atlas = 1`.
Historical records missing that field retain `Legacy = 0`. Legacy branches and
the legacy fingerprint byte stream are preserved. Profile changes never migrate
an existing save. Atlas iteration remains development work, not a published save
compatibility promise until acceptance.

First implementation: continental sea masks, two mountain placement provinces,
broader lowland divides, stricter lake retention, removal of per-segment random
river wiggle, ten appended biome values, habitat vegetation/ecology, blended
habitat color field, fixed map panels, procedural circle/house icons, F3 heading.

## Evidence and open work

- `atlas-generation1.log`: normal and 85% coverage for three seeds passed; sea
  interiors remain below sea datum. Full Miniature default seed generated 8 lakes
  (earlier legacy fixture had 735). New biomes were observed in sampled output.
- Full generation1 predates the subsequent northern cold-desert→tundra adjustment
  and habitat ecology/rendering changes; do not call its fingerprint final.
- `atlas1-build.log`: first Unity candidate, validation in progress.
- GenerationPipelineChecks reached the existing legacy ordinary-slope test and
  failed (p95=.13495, required <.05). Preserved-source comparison still required.
- Native visual acceptance, save reload compatibility, map shape/corner fit,
  heading checks, biome distribution across sizes/seeds, smooth river beds and
  actual settlement icons remain to verify. No claim of final visual match.
- Salt-flat occurrence/physical dunes, more varied island coasts and blend quality
  require actual image review. New habitat trees are prototypes, not final assets.
- Earlier Genesis lighting/faceting investigation is still open; see the prior
  material checkpoint. Full material-family implementations remain unfinished.
