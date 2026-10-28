# Alpha v0.0.12 validation

Unity6000.3.27f1, Windows player, Direct3D11. This is an Alpha Development
package. Automated success was followed by inspection of native views and
moving frame sequences; prototype failures were preserved and corrected.

## Executed checks

| Check | Result | Evidence |
|---|---|---|
| Character/rig/grip/neck/LOD Editor tests | 11/11 pass | [XML](Validation/Alpha-v0.0.12/character-final-eleven.xml) |
| Traveler terrain collision, camera retention, flight/freefall/landing Play Mode | Pass | [XML](Validation/Alpha-v0.0.12/traveler-physics-and-viewmodes.xml) |
| Final packaged executable: all3 characters, all5 tools, 3 perspectives, jump/flight/landing, slopes, actual save/reload | Pass; exit0, errors0 | [Result](Validation/Alpha-v0.0.12/Native-Final-Package/result.txt), [launch and binary hashes](Validation/Alpha-v0.0.12/Native-Final-Package/launch.json) |
| Existing UI/console/settings/normal Doctor selection and real boulder mining | Pass; exit0, errors0 | [Result](Validation/Alpha-v0.0.12/Native-Gameplay-Regression9/result.json), [UI](Validation/Alpha-v0.0.12/Native-Gameplay-Regression9/interface-result.txt), [60stone and saved depletion](Validation/Alpha-v0.0.12/Native-Gameplay-Regression9/boulder-result.txt) |
| Copied actual v0.0.11 save, character changes and actual save/reload | Pass; exit0, errors0 | [Result](Validation/Alpha-v0.0.12/Native-Character-Compatibility8/result.txt), [copied fixture identity](Validation/Alpha-v0.0.12/Native-Character-Compatibility8/launch.json) |
| Final Windows build | Succeeded | [Build log](Validation/Alpha-v0.0.12/final-build12.log) |

The UI/mining and v11 compatibility runs preceded the final neck/LOD and visual
shader corrections. Those later changes did not modify the interaction/save
code. The packaged player was then exercised through the full character audit.

## Visual review

The supplied head reference and both editable source turnarounds were inspected.
The user accepted the head and requested a wider connection into the torso.
The final neck extends beneath the collar, flares into the chest and has
torso-anchored lower weights. Native standing, crouching, mining and walking
views show the connection; the two distance skins retain it during crouch.

Native review covered front/profile/back, right cane/left tools, upright carry,
windup/strike/recovery, all tool types, first person and both third-person
perspectives. Walking, running and crouch sequences were captured along with
uphill/downhill travel on a measured terrain gradient of about0.094. Actor
matrices were checked for finite values throughout. Foot and cane samples are
retained as CSV; the physics root was separately checked in Editor tests.

See the [native gallery and sampled replays](ALPHA_v0.0.12_NATIVE_GALLERY.md).
The remaining stylized hair/cloth/finger detail and static facial expression
are listed in the [implementation report](ALPHA_v0.0.12_IMPLEMENTATION.md).

## Preservation and limits of regression coverage

World.dll is byte-identical to v0.0.11:
`79E35E987FCCEA66DDB0BB2325C0DCBAD6E8DA6AE5FA9B59E29A9362C101928A`.
All World source hashes and the dialogue source match the pre-hotfix baseline.
The generated/reloaded geographic fingerprint remains
`34E3461F1673AB6A4BCEF5655B18DACE61DD68F6ACCB82C36AC12D8200F201E2`.
The complete v11 runtime and1043-file source inventories were rehashed with zero
differences. Git's index and all existing Chronicles retain their baseline hashes.
No user saves were used directly; native tests used isolated fresh/copied roots.

The entire world-generation test suite was not rerun for this presentation-only
hotfix. Byte/source identity and native save/interaction checks protect that
scope; they do not imply new driver-wide, long-session or world-generation soak
acceptance. Earlier prototype failures remain under `Docs/Validation/Alpha-v0.0.12`.
