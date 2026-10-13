from pathlib import Path
import json,hashlib,zipfile
root=Path.cwd();out=root/'Docs/Validation/TwilightSea'
edit=json.loads((out/'edit_tests.json').read_text())['data']['result'];play=json.loads((out/'play_tests.json').read_text())['data']['result']
assert edit['status']==play['status']=='completed' and edit['summary']['failed']==play['summary']['failed']==0
package=root/'MCP_INBOX/ANIMOL_TWILIGHT_SEA_V01.zip';z=zipfile.ZipFile(package)
provenance={'input':package.relative_to(root).as_posix(),'archiveSha256':hashlib.sha256(package.read_bytes()).hexdigest(),'archiveEntries':len(z.infolist()),'crcError':z.testzip(),'productionScripts':{n:hashlib.sha256(z.read(n)).hexdigest() for n in z.namelist() if '/source/' in n},'recipe':['npm.cmd install --ignore-scripts (sharp 0.35.4)','node source/build_twilight_sea_v01.cjs','python -X utf8 source/document_twilight_sea_v01.py','python -X utf8 source/qa_twilight_sea_v01.py (Temp-only POSIX path normalization)'],'engineVersion':'6000.3.8f1','baselineHead':json.loads((out/'baseline.json').read_text())['head']}
(out/'source_provenance.json').write_text(json.dumps(provenance,indent=2),encoding='utf8')
result='''# ANIMOL 황혼의 바다 적용 결과

기준일: 2026-10-14. **아트 임포트·테마/프리팹·실행 가능한 QA 적용 완료. 실제 퍼즐 플레이 연결은 미완료**다. 활성 저장소에 퍼즐 입력/이동/점유/코스트/장치 턴/승리 엔진과 플레이 씬이 없다. 없는 규칙을 새로 구현하거나 레거시 플랫포머를 복구하지 않았다.

## 실측 프로젝트와 보존

- 프로젝트: `C:/Users/user/Documents/GitHub/ANIMOL`, Unity 6000.3.8f1, live Editor 7800. AGENTS.md와 패키지/asmdef/활성 스크립트를 확인했다. 런타임은 ANIMOL.Presentation, 에디터/검수는 분리된 asmdef다. Packages를 바꾸지 않았다.
- 시작 HEAD: `9c9194ae1d522977f0e44a121e798be6991bfd2c`. [작업 전 Git 상태](Docs/Validation/TwilightSea/git_status_before.txt)와 [자산·설정 SHA-256 기준](Docs/Validation/TwilightSea/baseline.json)을 먼저 기록했다. 기존 EditorSettings/ProjectSettings와 Legacy의 사용자 작업을 보존하고 커밋에서 제외했다.
- 기존 씬은 목장/장치8/기억 극장/편지 천문대의 아트 QA 4개다. 시작 에디터 씬은 편지 천문대 QA, 최종 에디터/Play 시작 씬은 `Assets/ANIMOL/Scenes/ANIMOL_TwilightSea_Art_QA.unity`다. 빌드 씬은 기존 Starway QA 그대로다.
- 실제 Grid 컴포넌트/레벨/테마 선택기는 없다. 기존 QA의 한 셀 월드 폭 1을 유지해 PPU 80, 셀 배치 간격 1을 사용했다. Collider/Grid/물리 크기를 추가·변경하지 않았다.
- 활성 호출 흐름은 `DeviceArtQaController.OnGUI → DeviceVisualView.Present/Notify → Advance → Render`다. 이것은 수동 아트 검수 경로다. 유효 입력 확정→연쇄→장치 턴→게임 결과의 실제 호출자는 발견되지 않았다. `PuzzleHudView`도 표시 어댑터이며 게임 상태 공급자가 없다.

## 입력 제작 소스와 임포트

ZIP은 CRC 검사에 통과한 **제작 소스 묶음**이었다(37항목). 최종 manifest/sprites는 없어서 제공된 로컬 Node/Sharp 제작 코드와 Python 문서/QA 코드를 Temp에서 실행했다. 새 원화를 생성하거나 임의 리사이즈/색 변환 규칙을 넣지 않았다. 최종 PNG는 제공된 제작 레시피의 출력이다.

[제작 근거·스크립트 SHA](Docs/Validation/TwilightSea/source_provenance.json), [소스 재생성 기록](Docs/Validation/TwilightSea/regeneration.json), [원본 문서](Docs/Inbox/TwilightSea/README_KO.txt), manifest/import manifest/DEVICE_RULES/MOTION_BINDINGS/event CSV/polish CSV/추가 결정/생성 QA_REPORT를 읽고 보관했다. Windows 경로 비교만 Temp QA에서 `as_posix()`로 수정했다. source/원화/reference/preview/시트는 활성 Assets에 넣지 않았다. 생성물 파일 QA는 Unity 실행 검사와 별개다.

| 실제 산출물 | 수량/경로 |
|---|---|
| 정적 PNG / 개별 프레임 | 125 / 587, 합계 **712** |
| SpriteFrameSequence / AnimationClip | 각 **137**, 프레임별 누적 시간을 그대로 유지 |
| 장치 core·정적·클립 역할 프리팹 | **270** = 8+125+137 |
| 특수 표시 프리팹 | **2**, Prefabs/Presentation의 occupied ferry / resolved cell preview |
| 테마 카탈로그 | Data/Themes/twilight_sea.asset, **8종** |
| QA 씬 | ANIMOL_TwilightSea_Art_QA, **307 DeviceVisualView** 및 별도 동기화/셀 표시 |
| 스크린샷 | **74**, Screenshots/TwilightSea |

새 파일에 버전 이름을 붙이지 않았다. 단일 정본은 `Assets/ANIMOL/Sprites`다. 전체 정본 총량은 PNG **3455**, 클립 **542**다.

| 출처 | 최종 정본 |
|---|---|
| 공통07/09/18/19 | Sprites/Obstacles/common/twilight_sea/M##_EnglishName/{Frames,Animations} |
| 전용05/11/12/15 | Sprites/Obstacles/twilight_sea/{M05_ExitSpikes,M11_Ferry,M12_RollingBarrel,M15_TimedBomb}/{Frames,Animations} |
| 바닥·물·해안 / 벽·금 간 벽 | Sprites/Tiles/{Floor,Wall}/twilight_sea |
| 나무 상자 | Sprites/Props/twilight_sea |
| 배경·난간·등·해초 | Sprites/Background/twilight_sea/{Far,Mid,Near} |

파일별 전후 경로와 SHA는 [source_path_map](Docs/Validation/TwilightSea/source_path_map.json), 전체 변경 목록은 [changed_files](Docs/Validation/TwilightSea/changed_files.txt)다. 예: `spr_obstacle_twilight_sea_m11_pilot_boat_row_rear_e_f00.png`, `anim_obstacle_twilight_sea_m15_three_turn_bomb_explode.anim`. 프레임 f00, 방향 e/s/w/n을 유지했다. 같은 Sprite를 시트에서 이중 생성하지 않았다.

기존 SpriteImportPolicy에 새 manifest 조회를 연결해 일괄 PPU32 덮어쓰기를 막았다. 712개 모두 Point/Uncompressed/Mipmap 없음/Full Rect/Single/NPOT none, 별도 플랫폼 override 없음, JSON 좌상단 피벗을 (x/w,1-y/h)로 변환했다. 셀80/일반96/폭발128/장식64·64×96/배경1024×1792를 유지했다. 기존 PNG·클립·프리팹·씬·meta·설정 바이트는 변경하지 않았다.

## 실제 코드·프리팹 연결과 평가

`TwilightSeaArtBuilder`가 Unity AssetDatabase/PrefabUtility/EditorSceneManager로 자산과 씬을 작성했다. 4개 공통 core는 기존 Device8 프리팹의 실제 Variant다. Animator를 더하지 않고 기존 variable-time sequence만 Renderer를 쓴다. AnimationClip은 동일 프레임을 참조하며 이벤트가 없다(1000 sample rate는 millisecond 키 간격 표현용이며 고정 FPS 재생이 아니다).

| 장치 | 기존 구현/표현 누락 | 이번 표시 연결·효과 | 난이도 / 다음 우선순위 |
|---|---|---|---|
| 07 무빙워크 | 공통 표시 어댑터만, 바다 외형 없음 | 4방향 run; 내부만 반복, 운반 Root 고정 | 낮음 / 실제 운반 사건 P0 |
| 09 단방향 문 | 통과/거부 판정 없음 | open/close/blocked; open 종료는 투명 중심 유지 | 낮음 / 통과 시작·이탈 P0 |
| 18 튕김 모서리 | 반사 판정 없음 | 4코너 impact/terminal; 안쪽 홈 유지 | 낮음 / 실제 반사 확정 P0 |
| 19 제동판 | 강제 정지 판정 없음 | 가로/세로 brake; 반동 이동 없음 | 낮음 / 실제 정지 P0 |
| 05 이탈 가시 | 점유·이탈 판정 없음 | arm→armed, rise→raised, 거부 후 raised 유지 | 낮음 / 점유 사건 P0; 재하강 미정 |
| 11 나룻배 | 조종·승하선·경로 없음 | 20 rear/front 쌍·승객·wake가 하나의 시계와 Root를 사용 | 중간 / 실제 승하선·경로 P0 |
| 12 통 | 유효 밀기/굴림 경로 없음 | 4방향 start/roll/stop/blocked, 외부 최종 셀 표시 | 중간 / 실제 경로·플레이어 도착 P0 |
| 15 폭탄 | 점화/행동 턴/폭발 범위 없음 | arm3/3→2/2→1/330ms explode→cleared 및 외부 범위 표시 | 중간 / 실제 턴·대상 커밋 P0 |

나룻배 `FerryVisualView`는 **DeviceVisualView 한 개의 Elapsed**를 읽어 rear/front와 passengerOffsetPixelsByFrame을 같이 샘플링한다. occupied 프리팹에는 full Renderer가 없고, wake5 < rear10 < 기존 토끼 Sprite15 < front20이다. 기존 토끼 발 피벗 y8px은 알파 아래 경계 height160-y152와 일치한다. 좌석은 (0,-5/80), PNG의 bob y부호를 반대로 적용한다. 4방향 board/row/disembark/blocked/idle의 프레임 20쌍을 검사했다. 빈 배 full 및 분할 단독 프리팹은 별도 QA 역할이며 같은 칸에 겹쳐 그리지 않는다. 실제 탑승/하선 여부와 Root from/to는 게임 엔진이 공급해야 한다.

`ResolvedCellPreview.PresentBlast`는 공급된 셀 집합의 외부 둘레만 1N/2E/4S/8W로 계산한다. 실제 범위에 포함된 공급 cracked-wall 목록만 target으로 표시하고 blast_cell_preview는 동시에 쓰지 않는다. `PresentStop`은 공급 최종 셀만 표시하며 null/Clear/disable/QA Reset으로 정리한다. BeamPathView의 기존 직렬화 풀을 재사용해 씬 재진입 중복을 막았다. QA의 L자 3셀은 **표시 시험 데이터**로 폭발 규칙이 아니다. 해안도 공급 water/outside 목록만 사용한다.

물결1920ms·등1800ms·해초2100ms/접촉300ms를 유지했다. 물 셀의 좌표 기반 initialPhase는 게임 RNG 없이 분산되며 Reset 후에도 유지된다. 장식 Renderer는 게임 오브젝트 아래에 두었다. 일반 피드백에 Camera Shake/Zoom/Hit Stop/새 Particle/입력 차단을 추가하지 않았다. 실제 기존 오디오/햅틱 서비스는 Legacy에 있으므로 새 사운드 연결을 완료했다고 주장하지 않는다.

공통 토끼/분홍 방울/출구/HUD 프리팹과 Press/Release/Disabled/Safe Area 코드를 그대로 쓴다. 상태 공급자가 없어서 COST -- / --/--다. 24/0/3을 하드코딩하지 않았다. 기존 계정·UI·맵 에디터·러너를 복구하지 않았다. 미사용으로 확정할 수 없는 기존 자산을 삭제하지 않았다. 이번에 만든 임시 Atlas와 캡처용 Assets/Screenshots만 검수 뒤 제거했다.

## 실행한 검수와 증거

| 검수 | 최종 결과 |
|---|---|
| Unity 임포트·컴파일 | 오류0, 컴파일 종료, [compile](Docs/Validation/TwilightSea/compile.json) |
| PNG/피벗/PPU/프레임별 시간/clip duration/종료 Sprite | [Validate](Docs/Validation/TwilightSea/validate.json), 712 PNG·137클립 통과 |
| 새 QA 씬 누락 스크립트 / 지원 안 되는 재질 | **0 / 0**, [final_validate](Docs/Validation/TwilightSea/final_validate.json) |
| GUID 중복 / 해석 불가능한 직렬화 GUID | **0 / 0**, [audit](Docs/Validation/TwilightSea/audit.json) |
| 기존 자산/meta/사용자 설정 SHA | 모두 유지; 수정된 기존 활성 파일은 임포터·표시 위상·QA 컨트롤러 3개만 |
| 원본 PNG SHA/알파/색/캔버스 | 새712개 원본 동일, 13색 Sweetie16 subset, alpha0/255·투명RGB0, 예상 밖 가장자리 잘림0 |
| Atlas 실제 패킹 | 대표80개, 1페이지, 새 불투명 RGB0; Point/비압축/회전·tight 없음. [증거](Docs/Validation/TwilightSea/atlas_qa.json) |
| EditMode 회귀 | **23/23 통과**, [edit_tests](Docs/Validation/TwilightSea/edit_tests.json) |
| PlayMode 회귀 | **8/8 통과**, [play_tests](Docs/Validation/TwilightSea/play_tests.json) |
| 나룻배 동기화/Root 불변/상하 레이어 | 20쌍 모든 프레임 offset 및 variable-time 검사 통과; pause/reentry/reset 통과 |
| 폭탄·가시·문 terminal | 100초 표시 Advance에서도 다음 턴/점유를 생성하지 않고 terminal 유지; 중복 action ID 거절 |
| 기존 테마 전환 표시 | 목장→바다→천문대→극장 gate에서 elapsed/pause/소비 action ID 유지; 기존 4QA PlayMode 회귀 통과 |
| 예측/해안 | 공급 셀 외부 경계·대상 필터·stop 위치·풀 재사용·Clear/reentry/QA Reset 통과; 80px 바닥/해안 연속 접합 파일 및 렌더 확인 |
| 실제 Game View | 1080×1920·720×1280 Play 실행·캡처. 작은 화면 QA 도구의 첫 행 가림을 수정 |

첫 EditMode 실행은 비활성 wake Renderer의 Sprite null로 1개 실패했다. [초기 실패](Docs/Validation/TwilightSea/edit_initial_failure.json)를 남기고 정상 투명 wake_clear 참조로 고쳤다. 전체 재실행에서 23/23 통과했다. Atlas는 런타임에 넣지 않았고 검사 후 제거/원본 재임포트해 Editor-only atlas binding 잔류를 방지했다. 이 검사로 모든 프로젝트/향후 배포 Atlas의 품질을 보장했다고 주장하지 않는다.

카메라 검수는 전 페이지 시작/중간/종료 39장·밝은 배경13장·나룻배20개 중간4배 접사다. 별도 **실제 screen-source PlayMode 캡처** 2장은 HUD/QA 버튼을 포함한다. 다음 이미지에서 투명 홈·숫자·레이어·해안/경계와 UI를 실제로 확인했다.

- [1080 PlayMode](Screenshots/TwilightSea/qa_playmode.png), [720 PlayMode](Screenshots/TwilightSea/qa_playmode_720.png)
- [전체 테마 중간 상태](Screenshots/TwilightSea/page_0_pose_1.png)
- [밝은 배경 폭탄/문/장식](Screenshots/TwilightSea/page_6_bright.png)
- [해안 접합과 공급 범위](Screenshots/TwilightSea/page_12_pose_1.png)
- [승객·배 레이어 접사](Screenshots/TwilightSea/ferry_row_e_mid_4x.png)

## 실패와 미실행을 구분한 남은 작업

**실제 유효/무효 입력 비용1/0, 장치 턴0, 자동 연쇄 추가 비용0, 3방울 수집/출구/성공1회는 미실행**이다. 해당 활성 게임 엔진·플레이 레벨이 없다. metadata 계약을 읽거나 화면 숫자를 선택하는 QA를 게임 턴 검사로 계산하지 않았다. 표시 함수를 사건에 매핑할 수 있게 준비했지만 실제 gameplay subscriber는 없다.

가시 점유/이탈/거부 판정, 배4방향 실제 한 칸 이동·막힌 물/육지·승하선, 통 첫 칸/중간 막힘·플레이어 도착 위치, 폭탄 첫 점화3 및 **유효 행동에 의한**2/1/폭발, 금 간 벽 실제 충돌 제거, 연출 on/off 게임 결과 동등성, 실제 Retry/입력 취소/테마 선택기, 기존 사운드·햅틱은 미실행이다. QA에서 root를 고정하고 표시 사건을 직접 공급한 검수와 구분한다.

모바일 세로 **Unity Game View**는 실행했지만 실제 휴대폰/모바일 빌드/노치가 있는 Safe Area는 미실행이다. 현재 화면 SafeArea는 전체720×1280이었다. CommercialPolishPolicyTests는 `Legacy/PreviousProject_20261013`에 있으며 자동 복원하지 않아 미실행이다. source QA_REPORT의 과거 “Unity unavailable”는 원본 팩 검사 범위를 뜻하며 이번 엔진 검수는 위 별도 근거다.

다음 우선순위:

1. 즉시 효과 큰 작업: 별도 퍼즐 엔진/플레이 레벨 작업에서 행동ID·유효성·점유·경로·턴·수집/성공의 확정 이벤트를 제공하고 준비된 표시 어댑터를 구독시킨다. 가시 재하강·폭발 범위/피해 미정은 DEVICE_RULES의 null을 유지한다.
2. 적은 비용 개선: 실제 경로/범위가 생긴 뒤 stop/target 예측과 기존 단발음·햅틱을 단일 사건 경로에 연결하고, 점화3→유효2→유효1→폭발을 PlayMode로 검증한다.
3. 나중 디테일: 실제 휴대폰의 정수 배율/노치/작은 숫자·물결/해초 위상과 장식 타이밍 검수. 현재 PNG를32px로 축소하거나 새 테마/장치를 확대하지 않는다.

검토 가능한 작업 커밋 제목: `Apply twilight sea art with synchronized ferry presentation`. 사용자 변경/입력 ZIP은 스테이징하지 않는다. 실제 커밋 해시는 완료 응답에서 보고한다.
'''
(root/'ANIMOL_TWILIGHT_SEA_V01_APPLY_RESULT.md').write_text(result,encoding='utf8')
for file,text in {
'Docs/ANIMOL_ACTIVE_PROJECT_LAYOUT.md':'\n황혼의 바다 `twilight_sea`를 추가했다. PNG712·클립137로 현재 정본 총량은 PNG3455·클립542다. 공통4종/전용4종 경로는 Obstacles/common/twilight_sea/M## 및 Obstacles/twilight_sea/M##이며 Tiles/Props/Background도 해당 테마에 있다. Prefabs/Themes 270개 + Prefabs/Presentation 2개, 테마8종, QA307 표시 객체다. 나룻배20쌍은 하나의 재생 시각/승객 발 피벗을 공유하며 범위·해안은 공급 셀만 표시한다. 현재 검수 씬은 ANIMOL_TwilightSea_Art_QA이고 실제 게임 엔진은 없다. [결과](../ANIMOL_TWILIGHT_SEA_V01_APPLY_RESULT.md), Tools/TwilightSea 및 Validation/TwilightSea에 EditMode23/23·PlayMode8/8·74캡처 근거가 있다.\n',
'Docs/ANIMOL_SPRITE_PATHS_AND_NAMES.md':'\n황혼의 바다 지시에 따라 `twilight_sea`를 등록했다. 공통4종은 `Sprites/Obstacles/common/twilight_sea/M##_EnglishName/{Frames,Animations}`, 전용4종은 `Sprites/Obstacles/twilight_sea/M##_EnglishName/{Frames,Animations}`다. 바닥/물/해안·벽은 `Tiles/{Floor,Wall}/twilight_sea`, 상자는 `Props/twilight_sea`, 배경/난간/등/해초는 `Background/twilight_sea/{Far,Mid,Near}`다. 이전 테마 경로와 GUID는 유지한다. PNG712·클립137을 더해 현재 정본은 PNG3455·클립542다. 셀80px/PPU80과96/128px 캔버스·좌상단 JSON 피벗·변동 프레임 시간을 유지하며 원화/시트/preview는 활성 Assets에서 제외한다.\n'
}.items():
    p=root/file;s=p.read_text(encoding='utf8');assert '황혼의 바다 `twilight_sea`를 추가했다' not in s and '황혼의 바다 지시에 따라' not in s;p.write_text(s+text,encoding='utf8')
print('Result, provenance and canonical documentation written')
