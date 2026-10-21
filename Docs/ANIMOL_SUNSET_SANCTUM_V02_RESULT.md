# 낙일 성소 V02 실제 프로젝트 적용 결과 (2026-10-21)

## 적용 범위

- 프로젝트: Unity 6000.3.8f1, built-in Render Pipeline. 실제 테마 key와 저장용 ID는 `letter_observatory` 그대로다.
- 01_CORE, 02_FLOOR, 03_BRIDGES를 같은 `ANIMOL_SUNSET_SANCTUM_CARTOON_V02` 폴더로 합쳤다. 04_SOURCES는 같은 외부 검수 폴더에 더해 full QA에 사용했다. 선택 원본과 Inspection 이미지는 게임 Sprite 후보인 `Assets`에 넣지 않았다.
- 런타임 1024 PNG와 106 clip key를 `Assets/ANIMOL/Art/ANIMOL_SUNSET_SANCTUM_CARTOON_V02`에 새 GUID로 설치했다. V01/ClockDome 경로와 `.meta`를 덮어쓰지 않았다. 현재 저장소의 옛 `ANIMOL_CLOCK_DOME_V01.zip`은 199 PNG/80 PPU로, 이 패키지의 1024개 동일 경로 V01 baseline이 아니다.
- Runtime/Editor asmdef, importer, shader, 카탈로그, 생성 클립과 빔 머티리얼을 임포트했다. 실제 성소용 6개 `PuzzleScenario`의 시각 prefab 35곳을 새 카탈로그 prefab 2개로 옮겼다. 이전 beam prefab 직렬화 참조 6곳과 성소 theme의 ClockDome renderer 참조를 해제했다. 기존 시뮬레이션의 ID, 셀, 방향, 비용, 맵, 스토리 값을 바꾸지 않았다.
- 원본 7개 SO의 변경 전 사본은 `C:\Users\user\AppData\Local\Temp\ANIMOL_V02_20261021_Implementation\PreMigrationBackup`에 `Assets` 밖으로 보관했다.

## 실제 계약과 바인딩

실제 게임 권위는 `PuzzleDeviceSession`의 `Apply`, `Footprint`, `TraceLight`, `ReflectMirror`, `ReflectCorner`, `Free`와 기존 `PuzzlePrototypeController`에 있다. 방향 enum을 수치 캐스팅하지 않고 E=(1,0), N=(0,1), W=(-1,0), S=(0,-1)로 변환한다. 아래 점유는 `Footprint`의 결과이며 PNG 크기에서 정하지 않았다. 전부 현재 실제 맵에서 1칸이다.

| 실제 role / 주요 ID (`letter_observatory_Scenario`) | 실제 방향·상태 소스 | V02 바인딩 | 점유 |
|---|---|---|---|
| CrescentLeap / `0_leap` | `direction`, host 도약 허용·착지 판정 | `moon_jump.{heading}.idle/jump` | 출발 한 칸 |
| Conveyor / `1_belt1`, `1_belt2`, `5_belt`, `6_belt`, `7_belt1`, `7_belt2` | `direction`, 자동 이동 연쇄 | `conveyor.{heading}.idle/move` | 각 한 칸 |
| OneWayGate / `2_gate` | `direction`, `OneCellGateRules`, 출구 여유 | `oneway_gate.{heading}.closed/opening/closing/blocked` | 문 한 칸 |
| Optics / `3_source`, `3_mirror`, `3_sensor`, `3_lightgate` | source 방향, mirror `phase`, 실제 beam trace, sensor/link `active` | emitter/mirror/receiver/light_gate exact clip | 각 한 칸 |
| Magnet / `4_magnet`, `4_switch`, `4_metal` | host latch·금속 상자 결과 | magnet/switch clip, `devices/metal_box/idle.png` static part | 각 한 칸 |
| Portal / `5_portalA`, `5_portalB` | 양쪽 reciprocal `target`, Walking/Sliding 유지 | 양쪽 `paired_portal.none.pair_01_*` | 각 한 칸 |
| Bounce / `6_bounce` | 기존 `orientation`과 `ReflectCorner` | `bounce_corner.{corner}.idle/rebound` | 한 칸 |
| Brake / `6_brake`, `7_brake` | 기존 축·강제 이동 종료 | `brake_pad.{axis}.idle/press` | 각 한 칸 |
| Glass / `3_glass`, `8_glass` | 기존 폭탄/석상 파괴 확정 | `glass_wall.none.intact/break/open` | 각 한 칸 |

실제 source의 mirror `phase==0`은 화면 `/`이며 E→N, N→E, W→S, S→W다. 다른 phase는 `\`이며 E→S, S→E, W→N, N→W다. 플레이어의 mirror bump 입력, 도약 중간 칸과 막힌 착지, 포털 출구 사전 판정, 수신기 연결문, 유리 파괴는 기존 host가 결정한다. V02 clip과 빔은 위치·코스트·충돌·턴을 확정하지 않는다. `PlayForCommittedSequence`는 실제 커밋 순번을 받아 중복을 거르고, 풀/재바인딩 때만 guard를 초기화한다. 시각 컴포넌트 하나가 SpriteRenderer를 소유하며 Animator/Collider가 새 prefab에 없다.

## 생성 자산 및 레거시 조사

- catalog/expected SHA256: `6df7badeac05b62ed58c3f0916f7d9a872c1f6eb8cda11d13e532cbc5b793b56` (`animation_catalog_sha256.txt`와 일치).
- 생성 catalog: `Assets/ANIMOL/Art/ANIMOL_SUNSET_SANCTUM_CARTOON_V02/_Generated/SunsetSanctumV02VisualCatalog.asset`; 같은 `_Generated` 아래 106 `.anim`과 animated/static prefab.
- runtime beam: `Assets/ANIMOL/Art/ANIMOL_SUNSET_SANCTUM_CARTOON_V02/Materials/LightBeam.mat`, shader `ANIMOL/SunsetSanctumV02/LightBeam`.
- 아트 검토 전용: `_Generated/SunsetSanctumV02ArtReview.unity`, private beam material/mesh, `art_review_scene_build_report.json`. 실제 게임 씬이나 Build Settings를 교체하는 생성물이 아니다.
- 성소의 6개 scenario, 활성 prototype 씬의 Unity dependency, V02 prefab/카탈로그를 조사했다. 시각 prefab 35/35가 V02 경로이며 잘못된 prefab 0, 구 beam prefab 0, theme의 ClockDome renderer 0, 씬의 ClockDome dependency path 0, V02 dependency path 1033이다. 다른 테마의 공용/구 아트와 Editor 제작기는 제거하지 않았다. Addressables 및 기기 빌드는 별도로 실행하지 않았다.
- 04_SOURCES의 원화와 패키지 내부 `final_v01_to_v02_revision_qa.json`은 20개 emitter `opticalAnchorPixel`의 허용된 시각 등록 위치 변화를 기록한다. 실제 광선 기준 `logicalRayAnchorPixel=[160,160]`, 경계 출구, PPU/pivot/slot/footprint는 그대로 쓴다. 1024개 V01 원본이 없어 독립적인 V01↔V02 RGBA 비교 스크립트는 실행하지 못했다. 패키지 내부의 1024/1024 주장만으로 외부 baseline 검증을 완료했다고 판정하지 않는다.

## 실제 검증

| 항목 | 결과 | 증거/범위 |
|---|---|---|
| runtime-only 및 04 병합 full 패키지 QA | PASS_OFFLINE | 각각 1024 PNG·106 clips; full은 native source 18, source PNG refs 26. 옛 픽셀 baseline은 NOT_AVAILABLE. [QA JSON](QA/SunsetSanctumV02_20261021/Full_QA.json) |
| Unity Import 및 카탈로그 | PASS | 1024 Sprite Single/256 PPU/Clamp/Bilinear/무압축/mipmap off/NPOT None, GUID/fileID 불일치 0, catalog 106 clip/1024 part. 생성 `.anim`의 비 Sprite 곡선과 이벤트 0. [로그](QA/SunsetSanctumV02_20261021/SunsetSanctumV02_UnityCatalogValidation_live.json) |
| Unity C# / shader | PASS | 컴파일 오류 0; built-in pipeline에서 LightBeam shader 지원, 1 pass, 메시지/오류 0. [컴파일](QA/SunsetSanctumV02_20261021/SunsetSanctumV02_FinalCompile_live.json), [셰이더](QA/SunsetSanctumV02_20261021/SunsetSanctumV02_Shader_live.json) |
| EditMode NUnit | PASS | `PuzzleDeviceLogicTests` 44/44, V02 package logic 7/7, `OneCellGateRulesTests` 4/4. [최종 44건 로그](QA/SunsetSanctumV02_20261021/SunsetSanctumV02_FinalEditTests_live.json) |
| PlayMode NUnit | PASS | 실제 네 테마 스와이프 클리어, 씬 바인딩, 장거리 포털 카메라 등 `PuzzlePrototypePlayTests` 4/4. [로그](QA/SunsetSanctumV02_20261021/SunsetSanctumV02_FinalPlayTests_result.json) |
| 실제 성소 prototype Play Mode | PASS (시험 범위) | V02 animated 21, static 1, beam 1, terrain 1, 누락 0/콘솔 오류 0. 동쪽 도약 `(1,2)→(5,2)` 비용1, 잘못된 서쪽 입력 비용0, 포털 연쇄 `(1,2)→(7,2)` 비용1, 광학 맵 trace 1/6 cells·보이는 빔 1. [캡처](QA/SunsetSanctumV02_20261021/MainPlay.png), [입력 로그](QA/SunsetSanctumV02_20261021/SunsetSanctumV02_PlayLeap_live.json), [광학 로그](QA/SunsetSanctumV02_20261021/SunsetSanctumV02_PlayOptics_live.json) |
| 아트 검토 씬 생성 | PASS (아트 전용) | 156 static Sprite, 12 review motion, 40 background layer와 private beam을 저장. 게임 성공 판정 아님. [로그](QA/SunsetSanctumV02_20261021/SunsetSanctumV02_ArtReview_final.json) |
| 실제 Android/iOS 기기, 좁은 화면·Safe Area·기울기·zoom, pause/undo/pool 장시간 | NOT_RUN | 에디터의 1080×1920 한 화면과 위 자동 경로만 검증. 상단 검은 띠는 기존 `PuzzleFollowCamera`의 `topInset=90`으로 비워 둔 HUD viewport에서 보이며 후속 UI 검토가 필요하다. |
| `actual_gameplay_checks_NOT_RUN.json`의 86개 시나리오 개별 실행 | NOT_RUN | 위 NUnit/수동 경로는 일부 행동을 검증하지만 86개 case ID 각각에 대한 PASS 증거는 만들지 않았다. |
| 외부 V01 원본 1024 PNG와 decoded RGBA 비교 | BLOCKED_PRECONDITION | 같은 상대 경로의 V01 원본 패키지가 현재 제공되지 않았다. 04_SOURCES는 V02 source이며 V01 baseline이 아니다. |
| 완료 후 표시 이름 `천구 날조국` | BLOCKED_PRECONDITION | 현재 활성 코드/데이터에서 승인된 스토리 완료 플래그를 찾지 못했다. `letter_observatory` key와 기존 표시 이름 `낙일 성소`를 보존했다. 임의 저장 key를 만들지 않았다. |

오프라인 육안 검토 자료: [도약 4방향](QA/SunsetSanctumV02_20261021/moon_jump_directions_frames.png), [포털 쌍·방향](QA/SunsetSanctumV02_20261021/portal_pair_channels_and_travel.png), [자석 4방향](QA/SunsetSanctumV02_20261021/magnet_directions_frames.png), [광학·유리 상태](QA/SunsetSanctumV02_20261021/optics_all_frames.png), [일방향 문](QA/SunsetSanctumV02_20261021/oneway_gate_contact_sheet.png), [바닥·다리 연결](QA/SunsetSanctumV02_20261021/terrain_join_map.png), [전체 씬 오프라인 시안](QA/SunsetSanctumV02_20261021/SANCTUM_FULL_SCENE_PREVIEW.png). 16개 각진 원화 체크를 전 Sprite별 게임 크기로 독립 인증한 결과는 아니다.

## 변경 파일군

- 새 패키지 전체: `Assets/ANIMOL/Art/ANIMOL_SUNSET_SANCTUM_CARTOON_V02/` (runtime PNG, manifest/catalog/import 설정, Runtime/Editor/Shader, `.meta`, 생성물).
- 시각 연결: `Assets/ANIMOL/Scripts/Runtime/SunsetSanctumV02PuzzleBinding.cs`, `SunsetSanctumV02MapRenderer.cs`, `PuzzleDeviceView.cs`, `PuzzleDeviceSession.cs`, `PuzzleMapView.cs`, `PuzzlePrototypeController.cs`, `PuzzleTheme.cs`.
- 프로젝트 설치기/어셈블리/테스트: `SunsetSanctumV02ProjectInstaller.cs`, Runtime/Editor/Tests asmdef, `PuzzleDeviceLogicTests.cs`, `SunsetSanctumV02LogicTests.cs`.
- Unity 직렬화 변경: `letter_observatory_Theme.asset` 및 성소 `PuzzleScenario` 6개. 다른 테마의 씬/프리팹/일러스트와 캐릭터 원화는 수정하지 않았다.
