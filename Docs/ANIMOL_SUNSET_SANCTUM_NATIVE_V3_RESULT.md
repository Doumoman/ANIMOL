# Sunset Sanctum native character application

Applied on 2026-10-19 to the existing third theme. The saved theme resource ID remains `letter_observatory`; its visible Korean name is `낙일 성소`, with `Sunset Sanctum` in the package metadata. Theme scenery, puzzle movement, colliders, camera logic, and rewards were not changed.

## Applied assets

- Moved the existing Art folder to `Assets/ANIMOL/Art/SunsetSanctumCharacters` through Unity `AssetDatabase.MoveAsset`.
- Moved all 360 prior frame assets according to `Export/legacy_frame_path_map.json` before replacing their PNG bytes. Retained all 360 frame GUIDs, all 60 existing AnimationClip GUIDs, all four prior illustration GUIDs, and the Art folder GUID. The fifth character, `doyun`, is a new independent entry; `sein`, `daon`, `lumi`, and `soyul` remain.
- Imported 450 source size frames into 75 six-frame clips, using each manifest frame's width, height, pixels per unit, and pivot. Import settings are Sprite Single, Point, FullRect, uncompressed, no mipmaps, NPOT None, and no platform size/compression override.
- Connected five full-body illustrations to the puzzle character catalog and the third lobby theme, retaining the lobby `observatory` ID and the puzzle `letter_observatory` ID. The lobby scenery and other themes remain as they were.
- Kept the existing `StarwayCharacterVisual` SpriteRenderer and state playback. Left uses side frames with `flipX`. No pet renderer was added for Lumi's ferret.

The pre-apply Art assets and catalogs are backed up at `Logs/sunset_sanctum_preapply_20261019.zip` (local, Git ignored). The supplied `Preserved`, `Source/OriginalAtlases`, and `Sheets` package folders were not imported as runtime animation. The former Art folder's existing Sheets remain as review material under the moved Art root.

## Validation

- Unity 6000.3.8f1 compiled without errors.
- `SunsetSanctumCharacterArtTests`: 2/2 passed, checking all 450 Sprite dimensions, PPU, pivot and import settings, all 75 clips, and five catalog entries.
- `LetterObservatoryCharacterPlayTests`: 2/2 passed, checking all five characters across front, back, side, left flip, and idle/move/sliding/jump/victory.
- `MainLobbyPlayTests`: 5/5 passed, including all 18 appearances and the Doyun lobby preview.
- `MainLobbyArtTests`: 3/4 passed. The existing unrelated `choa.png` has Point filtering while the test expects Bilinear. This asset was not changed.
- Captured 15 portrait character screenshots in `Docs/Validation/SunsetSanctumNative/Portrait`; checked Sein's glasses, Doyun without glasses or animal ears, Lumi with one attached ferret, and Soyul as a separate character.
- Android device execution was not performed.
