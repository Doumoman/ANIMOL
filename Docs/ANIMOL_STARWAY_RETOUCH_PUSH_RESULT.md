# Starway Ranch retouch and PUSH application (2026-11-02)

## Installed art and preserved data

- Four source archives were unpacked outside Assets. The original `Assets/ANIMOL/Art/StarwayCharacters` tree, catalogue, and affected scripts are backed up at `C:/Users/user/Documents/GitHub/ANIMOL_RetouchBackup_20261102/Original`. Scene calibration is in the sibling `scene_size_calibration.json`.
- Retouched frames, sheets, illustrations, and four distinct `starway_<id>_retouch_animations.json` files are under `Assets/ANIMOL/Art/StarwayCharacters`. Each of choa, uram, yeoul, and tori has 108 native-size PNG frames and 18 clips; total 432 frames and 72 clips. The original five states and the new three-direction, six-frame PUSH are catalogued. Left uses side sprites with `flipX`.
- Existing PNG and clip `.meta` GUIDs were preserved. All 432 installed frame PNGs are byte-identical to their ZIP originals. The new PUSH clips bind to the existing `Visual` SpriteRenderer on the same GameObject; active gameplay uses `StarwayCharacterVisual` frame playback, with no active Animator or OverrideController for these four characters.
- The `uram` save ID remains unchanged and its displayed name is 이솔. The runtime `StarwayCharacterCatalog` holds each contact asset, calibrated standing height, resting graphics offset, and approach limit.

## Scene calibration

The live Unity 6000.3.8f1 Editor reported the active `ANIMOL_starway_Prototype` scene. Its runtime player factory uses a graphics scale of 0.85 and a one-unit grid step. Baseline per-direction sprite size, PPU, pivot, foot and body reference, and calculated rest offset were recorded before overwriting in the external `scene_size_calibration.json`.

| ID | Old front standing height | Applied PPU factor | Front standing height after import | Front resting local Y |
|---|---:|---:|---:|---:|
| choa | 0.696721u | 1.013391 | 0.696721u | +0.008340u |
| uram | 0.653241u | 0.993976 | 0.653241u | +0.003512u |
| yeoul | 0.674752u | 1.010536 | 0.674752u | +0.005143u |
| tori | 0.651989u | 1.014198 | 0.651989u | -0.001027u |

The per-character factor was applied to every frame PPU in the working manifests and merged runtime manifest. Original source PPU values remain in `sourcePixelsPerUnit`; PNG dimensions were not changed. The player root, grid positions, device physics, and camera scale were not altered.

## Runtime PUSH and rendering

- `StarwayPushSpriteContacts` and `StarwayPushPresentation` are in the normal Runtime assembly, with `StarwayRetouchImporter` in the existing Editor assembly. The old native importer routes to the retouch importer when retouch manifests exist, preventing Point filtering and source PPU from being restored by that path.
- The existing successful `PuzzleDeviceSession` move events drive visual PUSH in `PuzzlePrototypeController`. The presenter lives on the generated `Player` logic root and moves only its collider-free `Visual` child. Object contact points use the visible sprite physics outline, which excludes transparent padding. A short visual preparation precedes object movement; costs, turns, accepted inputs, and authoritative positions stay in the existing session. Character changes and resets clear the offset and select the matching contact asset.
- The four characters' frames use Bilinear, Uncompressed, Mipmap Off, NPOT None, and adequate Max Size. Editor audit of all 432 frame importers found no Android, iPhone, Standalone, or WebGL overrides. No SpriteAtlas or PixelPerfectCamera applies to this rendering path. Existing camera and tile rendering settings were retained.

## Verification and limits

- EditMode `StarwayCharacterArtTests`: 2 passed. PlayMode retouch PUSH, committed starstone/crate, and four-direction sheep tests: 3 passed. These verify size, frame selection, contact references, PUSH entry, graphics recovery, and unchanged session cost and positions. The contact metadata had no `confidence=fallback` frames; all 72 PUSH hand pixels land on nontransparent sprite alpha.
- The live Editor Game view was inspected at 1080 × 1920 for all four idle characters and a starstone PUSH. No Android device run was performed.
- The visible starstone's outline begins about 0.152u behind its rectangular SpriteRenderer bounds; using the alpha outline removed that false contact plane. The calibrated 0.35-tile approach is limited again by the player sprite's visible outline to avoid body penetration. **A stationary player cannot stay palm-attached throughout an object's full one-cell visual move within this safe graphics-only limit.** Initial bracing and recovery work; continued contact after the object travels remains a visual limitation to review in a production play pass.
- A wider PlayMode filter also ran unrelated memory-theater tests: 7 passed and 2 failed (`ExistingMemoryTheaterPrototypeUsesV03WithoutChangingOccupancy`, `AllFourThemedStagesClearThroughSwipeInputWithExistingHudAndArt`). Those failures were outside this Starway implementation and were not modified here.
