# Editable NPC interaction sources

The colleague and module-lead packages below are native source and export candidates for the final Unity integration pass. Their rigs, skin, poses and prop curves are saved and editable in Blender. The original standing sources remain unchanged. Animation does not supply review authority, learner evidence or a domain receipt.

The colleague package is `SourceArt/Demo02/Characters/Colleague/Colleague_Interactions_v001.blend`, with its FBX and receipt under `SourceArt/Demo02/Exports/NPCInteractions_v001`. It retains the existing four-second idle/listen and three-second speak clips, and adds a two-second open-palm SourcePoint gesture. The actual permitted source context must determine its orientation in Unity. Four native views were inspected. A 61-sample FBX reimport preserved the 24 bones and matched the wrist within 0.588 micrometres.

The module-lead package is `SourceArt/Demo02/Characters/ModuleLead/ModuleLead_Interactions_v019.blend`. Its matching FBX, packet-tracks JSON, receipt and validation summary are in the same export folder. The seated rig has 54 bones, including native finger controls. Its source retains a single editable folio, cover hinge, review desk and chair. Only the actor is in the FBX; Unity must bind the separate packet tracks to the existing authored packet. Intermediate pickup/contact sources v008 and v012 are preserved as native authoring inputs.

| Take | Duration | Packet state |
| --- | ---: | --- |
| SeatedIdle | 4 s loop | Closed on desk |
| SeatedListen | 4 s loop | Closed on desk |
| SeatedSpeak | 3 s | Closed on desk |
| PickupPacket | 1.1 s | Desk through frame18, held from frame19 |
| ReadPacket | 1.2 s | Held, opening and reading |
| PlaceOpen | 0.4 s | Placed open, desk owns at frame13 |
| PointCurrentPair | 0.5 s | Desk owns, actual live pair is the target |
| RecoverPoint | 0.5 s | Desk owns, hands prepare to close |
| ReturnPacket | 0.5 s | Lift, close and centre, desk owns at frame16 |
| RecoverToReady | 0.6 s | Closed on desk, hands return to idle |

All clips use 30 fps and in-place actor motion. The continuous interaction order is pickup, read, place open, point, optional hold at the final pointing pose, recover point, return packet and recover to ready. The learner's separate packet-presentation gesture introduces the desk-owned packet; it is not a cross-character handoff. Preserve exact attempt, request, receipt and current-pair identity in the runtime presentation controller.

Native coordinates are metres, Z up, actor facing negative Y. The observed Unity basis is (-x,z,-y), subject to actual import validation. The source chair is at (0,.07,0), desk at (0,-.78,0), desk top at .8125 m, and closed packet root at (.115,-.40,.815). The current-pair pointing target is (-.10,-.32,.8305). Packet quaternion arrays are XYZW. Use actual scene overrides, camera framing, shoe grounding and single ownership when integrating these values.

The final source was reopened and all 484 saved samples checked using evaluated hand vertices against closed cover/page volumes and whole-body vertices against the tabletop. The method uses local bounding-box rejection and three-ray parity, rather than a nearest-face-normal sign that can misclassify points beyond thin edges. It passed 48 analytic thin-box controls and a known native thumb collision. The final source has zero sampled penetrations or ray disagreements at a 0.1 mm tolerance, zero foot drift and exact packet tracks. Eight clip joins differ by less than 9.54e-7 in world-matrix elements.

Independent FBX reimport checks all 54 bone heads across all 484 frames, with maximum position error 1.887 micrometres. Evaluated hand skin at 30 representative frames differs by at most 2.070 micrometres from the native surface. Front, back, both-side and key transition renders were inspected. These checks are not continuous triangle-collision proof or a normal-speed gameplay result. The brief outward hand preparation, actual contacts, current-pair label, camera composition, interruption, reduced motion, reset and post-Play persistence still require Unity acceptance.

Native validation images and full diagnostic records are local under `LocalOnly/Validation/Production/NPCInteractions_v001`. Superseded root-owned studies and their recipes remain recoverable under `LocalOnly/ProductionStudies/NPCInteractions_20261025`, with original paths and hashes in its move ledger. The final authoring recipe uses the retained v008/v012 sources and the local recorded pose-fit controls. The final native source can be edited directly without regenerating it. No paid asset job, player build, hosted build or push was required for this source batch.
