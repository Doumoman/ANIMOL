import pathlib,json,collections,subprocess
root=pathlib.Path.cwd();m=json.loads((root/'Assets/ANIMOL/Data/device_art_import_manifest.json').read_text());audit=json.loads((root/'Docs/Validation/DeviceArt/audit.json').read_text())
registry={'01':'M01_StarSocket','02':'M02_VineBridge','07':'M07_Conveyor','09':'M09_OneWayGate','18':'M18_BounceCorner','19':'M19_BrakePlate','20':'M20_WindLane','24':'M24_FollowerChick'}
table='\n'.join('| '+d+' | '+registry[d]+' | '+('ranch' if d in ['01','02','20','24'] else 'common')+' | '+str(sum(a['deviceId']==d for a in m['assets']))+' | '+str(sum(a['deviceId']==d for a in m['animations']))+' | '+str(sum(len(a['frames']) for a in m['animations'] if a['deviceId']==d))+' |' for d in sorted(registry))
text=f'''# 장치 8종 적용 결과

기준일: 2026-10-13. Unity 6000.3.8f1, 실제 연결 Editor(port 7800)에서 임포트·프리팹/씬 생성·EditMode/PlayMode 검수를 수행했다. 기준 커밋: `{audit['baselineHead']}`.

**아트 적용과 실행 가능한 QA는 완료했다. 활성 저장소에는 탑뷰 퍼즐 로직과 플레이 씬이 없으므로 실제 이동·장치 판정과의 연결은 미완료다.** 패키지 지시문의 미구현 로직 처리 규정에 따라 새 규칙을 만들거나 레거시 플랫포머를 복원하지 않았다. 현재 씬은 실제 게임이 아닌 표현 검수 씬이다.

## 입력과 정본

`MCP_INBOX/ANIMOL_DEVICE8_V01.zip`을 외부 임시 디렉터리에 해제했다. UNITY_APPLY_DEVICE8_V01.txt 전체, README_KO.md, manifest.json, unity_import_manifest.json, DEVICE_EVENT_BINDINGS.csv, source/ADDED_DECISIONS_KO.md를 읽었다. 원문은 [Docs/Inbox/DeviceArt](Docs/Inbox/DeviceArt)에 보존했다. 제공 QA_REPORT는 제작 측 검사이며 Unity 실행 결과와 구분한다.

기존 정본 규칙이 패키지 기본 경로보다 우선한다. `starway_pasture`는 프로젝트 테마 ID `ranch`로 매핑했다. V01/V04 같은 폴더는 활성 Assets에 만들지 않았다. PNG·개별 프레임만 임포트했고 시트·preview·reference·GIF·source 그림은 연결하지 않았다.

| ID | 정본 폴더: Sprites/Obstacles 아래 | 테마 | 정적 PNG | 클립 | 프레임 PNG |
|---|---|---|---:|---:|---:|
{table}

총 **정적 PNG 52개 + 프레임 PNG 402개 = 454개**, Sequence 59개, AnimationClip 59개, Prefab 119개(장치 전체 상태 8개 + 클립별 59개 + 정적 역할별 52개)를 추가했다. 기존 177 PNG·15클립과 합쳐 활성 정본은 631 PNG·74클립이다. 정적/클립별 프리팹은 QA 및 역할별 배치용이며 같은 셀에 중복 배치하지 않는다.

- PNG 예: `Assets/ANIMOL/Sprites/Obstacles/M07_Conveyor/Frames/spr_obstacle_common_m07_conveyor_run_e_f00.png`
- 클립 예: `Assets/ANIMOL/Sprites/Obstacles/M02_VineBridge/Animations/anim_obstacle_ranch_m02_vine_bridge_bridge_extend_n.anim`
- 모든 원본→정본 경로와 SHA-256: [source_path_map.json](Docs/Validation/DeviceArt/source_path_map.json).
- 프리팹: `Assets/ANIMOL/Prefabs/Devices/`, Sequence: `Assets/ANIMOL/Data/AnimationSequences/Devices/`.
- 정규화한 임포트 정본: `Assets/ANIMOL/Data/device_art_import_manifest.json`. 원본 JSON은 Docs에 별도로 유지했다.

## 임포트와 보존

기존 아트 QA의 셀 간격 1 world unit / 80 pixels를 유지해 새 자산 PPU는 80으로 설정했다. 새로운 Grid/Collider/물리 크기는 만들거나 변경하지 않았다. 일반 96×96, 다리 128×128, 병아리 64×64를 원본 그대로 사용한다. 병아리 피벗은 원본 좌상단 (32,54)을 Unity 정규화 (0.5,0.15625)로 변환했다. 다른 픽셀 피벗도 폭/높이 및 Y 반전을 적용했다.

Sprite Single/FullRect, Point, Clamp, alpha input/transparency, mipmap 없음, NPOT 없음, 압축 없음, max 8192, Android/iPhone/Standalone/WebGL 축소 override 없음. 기존 SpriteImportPolicy가 장치 매니페스트를 우선 조회하므로 미등록 자산 기본값 32 PPU가 덮어쓰지 않는다. 최초 구 임포터로 처리된 32 PPU 자산은 등록 후 ForceUpdate 재임포트하고 검증했다.

454 PNG의 SHA-256은 ZIP 원본과 모두 일치한다. 불투명 RGB 15색, 알파 0/255, 프레임 가장자리 불투명 픽셀 0. 변환·축소·재색상 처리를 하지 않았다. 기존 Assets/ProjectSettings의 전체 바이트 비교에서 바뀐 기존 파일은 임포터를 추가한 `ArtAssetBuilder.cs` 한 개뿐이다. 기존 V04 PNG·클립·프리팹·씬·데이터·폰트·.meta와 사용자 미커밋 설정은 보존됐다.

## 실제 코드 연결점

`DeviceVisualView`는 `SpriteFrameSequence`를 재사용하는 표시 컴포넌트다. 각 상태에 Sprite/Sequence/AnimationClip/terminal Sprite 및 CSV의 사건 문자열을 프리팹으로 직렬화했다. 프레임별 ms와 시작 시각을 보존한다. `.anim`도 같은 프레임을 참조하지만 **렌더러를 실제로 갱신하는 것은 Sequence 플레이어 하나**다. Animator를 동시에 돌려 중복 표시하지 않는다. 클립은 1000 sampleRate, 마지막 중복 키를 totalMilliseconds−1ms에 넣어 Unity의 마지막 1-sample 길이를 보정했다. AnimationEvent는 0개다.

권위 있는 게임 로직은 판정 완료 후 `Notify(committedEvent,state)`를 호출해야 한다. 잘못된 사건/상태 조합은 표시를 바꾸지 않는다. 정적 상태/반복 상태는 `Present(state)`로 갱신하며 같은 상태의 반복 호출은 시간을 재시작하지 않는다. `paused`/`ResetVisual()`은 표시만 제어하며 Time.timeScale, Root 위치, 코스트, 턴, 수집, RNG, 입력 차단을 변경하지 않는다. BlocksInput=false. Renderer에만 프레임을 적용한다. 콜라이더·사운드·햅틱·카메라 흔들림을 추가하지 않았다.

| 장치 | 표시 연결 / 유지한 계약 | 실제 게임 사건 연결 |
|---|---|---|
| 07 | run_e/s/w/n 반복, n=(0,1), e=(1,0), s=(0,-1), w=(-1,0) | conveyor_visual_running 호출자 없음 |
| 09 | allowed_passage_begin→open, clear→close, rejected contact→blocked; 열림은 종료 뒤 정적 open을 유지 | 통과/점유 판정 없음 |
| 18 | impact_nw/ne/se/sw 종료 후 해당 idle. nw/ne/se/sw는 막힌 두 변의 표시 방향 | 튕김 판정 없음; NW N→E/W→S, NE E→S/N→W, SE E→N/S→W, SW W→N/S→E는 지시문 계약만 기록 |
| 19 | horizontal/vertical brake 종료 idle; 시각 방향만 구분 | forced_motion_stopped 호출자 없음 |
| 01 | stone_push 4방향, socket_insert 종료 socket_filled 유지. 돌/소켓은 별도 역할 프리팹 | 별 매칭/밀기 확정 호출자 없음; 삽입 후 실제 돌 제거는 미연결 |
| 02 | plate_press/release 종료 상태 유지, bridge_extend/retract 4방향 및 H/V idle; 풀 프레임을 1 unit 간격으로 연결 | 점유 수·활성화 판정 없음; 0→양수/양수→0의 사건 계약만 제공 |
| 20 | 완성 blower_run 한 Renderer만 사용. wind_flow/wind_push 별도 역할; 종료 transparent는 Renderer를 숨기고 reset 시 복원 | 실제 바람 범위·밀기·금속 면역 없음; housing을 run에 겹치지 않음 |
| 24 | idle/walk 4방향만 표시. 발 피벗 유지, 물리 Root 고정, Collider 없음 | 기록 경로 추종 구현 없음; 이동·텔레포트 로직을 만들지 않음 |

게이트 열림을 시간만으로 닫지 않는다. 압력판의 다중 점유 수나 병아리 제외를 표시 컴포넌트가 계산하지 않는다. 바람 영향 셀이나 병아리 경로를 그림 크기로 추정하지 않는다. 미정의 매칭 실패, 점유 중 다리 철회, 병아리 포털/점프/배/둥지 규칙은 구현하지 않았다.

## 씬과 실행

새 씬: `Assets/ANIMOL/Scenes/ANIMOL_Device8_Art_QA.unity`. 353 DeviceVisualView: 세 배경에서 모든 59클립/52정적 역할(333), 다리 H/V 1/2/3칸(12), 장치 전체 상태 마스터(8). 기존 토끼/분홍 방울/출구 프리팹을 문맥 표시로 직접 참조한다. 기존 `ANIMOL_Starway_Art_QA` 씬은 수정하지 않았다.

Editor에서 새 씬을 열고 1080×1920 Game View와 Play 시작 씬으로 설정했다. Play의 QA 버튼으로 Pause/Resume, Restart, Reset, Next panel을 사용할 수 있다. 마스터 장치/상태 선택 후 Preview event는 CSV 사건을 시각 컴포넌트에만 전달한다. 비용·성공 등을 가짜로 시뮬레이션하지 않는다. 기존 빌드 씬 설정은 보존했다.

기존 UI/HUD·배경·토끼·분홍 방울·280ms 수집 FX의 회귀 테스트는 통과했다. HUD는 기존 미연결 표시를 유지하며 참고 숫자를 게임 상태로 고정하지 않았다. **적용할 퍼즐 플레이 씬은 없어 실제 플레이 씬 연결은 수행하지 못했다.**

## 실행한 검수

| 검사 | 결과 / 근거 |
|---|---|
| 장치 EditMode | 4/4 통과: 임포트/클립, 게이트 열린 상태 유지/명시적 닫힘/정지, 방향/미확정 사건 거부/별 terminal, 다리 hold/반복 상태 idempotence. [로그](Docs/Validation/DeviceArt/device_edit.json) |
| 장치 PlayMode | 1/1 통과: 실제 새 씬 로드, 353 Root 불변, pause/reset/restart, 무콜라이더, 1-unit 다리 간격, timeScale 불변, 실제 게이트 open→hold→clear→closed. [로그](Docs/Validation/DeviceArt/device_play.json) |
| 기존 EditMode | ArtAssetTests 4/4 통과. [로그](Docs/Validation/DeviceArt/base_edit.json) |
| 기존 PlayMode | ArtQaPlayTests 1/1 통과: 수풀 idle/touch/루트 고정, 수집 FX 280ms 종료/재사용, HUD 입력 표시, pause 버튼 상태, 1080×1920 및 360×640 SafeArea. [로그](Docs/Validation/DeviceArt/base_play.json) |
| 재임포트·컴파일 | 컴파일 오류 0, compiling=false, 재임포트 후 장치 454개 및 클립 59개 검사 통과 |
| GUID/자산 | Assets 중복 GUID 0, 직렬화 GUID 누락 0, 원본 hash 일치, 기존 .meta 변경 0. [감사](Docs/Validation/DeviceArt/audit.json) |
| 새 씬 | 누락 스크립트 0, 지원하지 않는/누락 Material 0, DeviceVisualView 353. [씬 검사](Docs/Validation/DeviceArt/scene_validation.json) |
| 화면 | 실제 Camera.Render 42장: 1080×1920 시작/중간/종료 포즈 12장, 정수 1x/2x/4x 상세 12장, 다리 세 배경×H/V×1/2/3칸 4x 상세 18장. 별도 실제 Play Mode Game View 1080×1920 캡처 1장 |

육안 확인: 밝은 전체 중간 포즈, 어두운/풀 표시, 가로 3칸 풀 배경과 세로 3칸 어두운 배경 확대에서 잎이 잘리지 않고 연결된다. 코너의 빈 내부/열린 변은 원본 의도이며 누락 PNG로 간주하지 않는다. SpriteAtlas/Tilemap/Mask는 현재 장치 씬에 사용하지 않으므로 Atlas packing/culling 검수는 해당 없음. 모든 PNG의 불투명 경계가 프레임 내부이며 FullRect를 유지한다. 모든 프레임에 대한 수동 재생/육안 전수 승인까지 수행했다고 주장하지 않는다.

대표 캡처: [실제 Play Mode](Screenshots/DeviceArt/qa_playmode.png), [밝은 전체 중간](Screenshots/DeviceArt/panel_0_pose_1.png), [풀 배경](Screenshots/DeviceArt/panel_2_pose_1.png), [가로 3칸](Screenshots/DeviceArt/bridge_bg_2_axis_0_length_3_4x.png), [세로 3칸](Screenshots/DeviceArt/bridge_bg_1_axis_1_length_3_4x.png). 전체는 [Screenshots/DeviceArt](Screenshots/DeviceArt)다.

작업 중 최초 EditMode 실패는 EditMode Instantiate에서 OnEnable이 실행되지 않는 테스트 초기화 문제였다. 테스트가 ResetVisual을 명시 호출하도록 고쳤다. 최초 PlayMode 실패는 재생성 전 씬의 객체 수 345와 최신 353 기대값 차이였고 씬 재생성 후 통과했다. 일부 대량 eval은 Pipeline의 5초 main-thread 응답 제한을 넘겼지만 작업은 종료됐으며, 최종 씬/자산 검사 및 테스트로 완료를 확인했다. 이 초기 시도는 최종 통과 수에 포함하지 않는다.

## 미실행 / 남은 문제

- 유효 이동/밀기 cost1, 무효 입력 cost0·turn0, 자동 연쇄 추가 cost0, 효과 on/off 게임 결과 동일: 활성 퍼즐 코드가 없어 미실행.
- 방울 중복 접촉, 실제 3개 수집/출구/승리 1회, 실제 게임 리셋·일시정지, 다중 점유/병아리 제외, 장치 판정 및 SFX/haptic 연동: 호출자/서비스가 없어 미연결·미실행. 기존 수집 FX/HUD 표현 회귀 통과와 구분한다.
- 모바일 빌드/실기기 검수: 미실행. 기존 PlayMode UI 화면 크기 검사만 수행했다.
- CommercialPolishPolicyTests/과거 로비 테스트: 활성 Assets에서 제거된 레거시 테스트이므로 미실행이며 통과 수에 포함하지 않았다.
- 기존 단일 수풀 3×3 원본 알파 틈은 이번 장치 팩과 별개로 남아 있으며 원본을 고치지 않았다.
- 새 퍼즐/장치 판정, 나머지 16장치, 새 테마는 추가하지 않았다. 현재 마스터 initialState를 실제 방향/권위 상태에 맞게 설정하고 위 사건 API를 게임 호출자에 연결하는 작업이 다음 구현 범위다.

## 변경 파일과 커밋 범위

기존 수정: `Assets/ANIMOL/Scripts/Editor/ArtAssetBuilder.cs`의 등록 임포트 조회만 확장했다. 새 코드: DeviceVisualView.cs, DeviceArtQaController.cs, DeviceArtBuilder.cs, DeviceArtTests.cs, DeviceArtPlayTests.cs. 그 외 8 장치 Sprites 폴더, Data/device_art_import_manifest.json, Data/AnimationSequences/Devices, Prefabs/Devices, 새 QA 씬, Tools/DeviceArt, Docs/Inbox/DeviceArt, Docs/Validation/DeviceArt, Screenshots/DeviceArt, 현재 구조 문서와 본 보고서를 추가/갱신했다.

정확한 산출물 목록은 [changed_files.txt](Docs/Validation/DeviceArt/changed_files.txt)에 기록한다. 입력 ZIP과 기존 사용자 변경(폰트/설정/레거시)은 커밋에 포함하지 않는다.
'''
(root/'ANIMOL_DEVICE8_V01_APPLY_RESULT.md').write_text(text,encoding='utf-8')
print('Wrote result with explicit unconnected gameplay scope')
