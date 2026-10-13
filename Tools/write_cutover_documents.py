"""Write the requested source-of-truth rules and measured classification/recovery appendices."""
import json
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
DEVICES = [
    ('StarSocket', 'ranch'), ('VineBridge', 'ranch'), ('CrescentJump', 'observatory'),
    ('AlternatingShutters', 'theater'), ('ExitSpikes', 'sea'), ('ActionStage', 'theater'),
    ('Conveyor', 'common'), ('RotatingBookshelf', 'theater'), ('OneWayGate', 'common'),
    ('LightSensor', 'observatory'), ('Ferry', 'sea'), ('RollingBarrel', 'sea'),
    ('DominoWall', 'theater'), ('Magnet', 'observatory'), ('TimedBomb', 'sea, 일부 맵'),
    ('PairedPortal', 'observatory'), ('ChargeStatue', 'theater'), ('BounceCorner', 'common'),
    ('BrakePlate', 'common'), ('WindLane', 'ranch'), ('GlassWall', 'observatory'),
    ('DiceBlock', 'theater'), ('CowardDoll', 'theater'), ('FollowerChick', 'ranch')]

RULES = '''# ANIMOL 스프라이트 경로·명명 규칙

기준일: 2026-10-13. `Assets/ANIMOL/Sprites/`를 새 스프라이트의 단일 정본으로 사용한다. `Legacy/Platformer_20261013/`는 저장소 루트, Unity `Assets` 바깥에 둔다.

기준 게임은 세로 화면의 2D 탑뷰 스와이프 퍼즐이다. 유효한 이동 1회는 코스트 1이고 방울 3개를 모아 출구로 간다. 이 문서는 자산 정리 규칙이며 새 퍼즐 24종 구현과 기존 화면 전면 재제작은 작업 범위에 포함하지 않는다.

## 정본 경로

```text
Assets/ANIMOL/Sprites/
├── UI/
│   ├── MainMenu/
│   ├── InGame/
│   ├── Settings/
│   ├── StageSelect/
│   ├── Store/
│   └── Common/
├── Player/<character_id>/
│   ├── Frames/
│   └── Animations/
├── Tiles/
│   ├── Floor/<theme_id>/
│   ├── Wall/<theme_id>/
│   └── Decoration/<theme_id>/
├── Obstacles/M##_EnglishName/
│   ├── Frames/
│   └── Animations/
└── Background/<theme_id>/
    ├── Far/
    ├── Mid/
    └── Near/

Legacy/Platformer_20261013/
├── README.md
└── Assets/ANIMOL/...
```

UI/StageSelect와 UI/Store는 실제 화면이 있는 경우 사용한다. 실제 자산이 없는 빈 장치 폴더는 만들 필요가 없다. 캐릭터 ID는 `rabbit`처럼 ASCII 소문자 snake_case로 쓴다.

화면 ID는 `main`, `ingame`, `settings`, `stage_select`, `store`, `common`을 사용한다. 테마 ID는 `common`, `ranch`, `theater`, `observatory`, `sea`로 고정한다. 한 장치의 모든 프레임과 클립은 같은 `M##` 폴더에 둔다. 에디터가 생성한 `.anim`과 원본 PNG의 이름에는 같은 기계 ID·상태·방향을 쓴다.

## 파일 이름

파일은 ASCII 소문자 snake_case를 기본으로 한다. 지정된 폴더 분류명과 장치 등록표의 대소문자는 그대로 유지한다.

- PNG: `spr_<scope>_<theme-or-screen>_<object>_<state>[_<direction>][_fNN].png`
- SpriteSheet: `sheet_<scope>_<theme-or-screen>_<object>_<state>[_<direction>].png`
- AnimationClip: `anim_<scope>_<theme-or-screen>_<object>_<state>[_<direction>].anim`
- Tile asset: `tile_<theme>_<family>_<edge>[_vNN].asset`
- 기계 ID는 `m01`~`m24`, 방향은 `n/e/s/w`, 프레임은 `f00`부터 두 자리로 통일한다.

| 분류 | 예시 |
|---|---|
| UI | `UI/InGame/spr_ui_ingame_pause_idle.png`, `UI/MainMenu/spr_ui_main_play_pressed.png` |
| Player | `Player/rabbit/Frames/spr_player_rabbit_walk_s_f00.png`, `Player/rabbit/Animations/anim_player_rabbit_walk_s.anim` |
| Floor / Wall / Decoration | `Tiles/Floor/ranch/spr_tile_floor_ranch_grass_ne_v01.png`, `Tiles/Wall/ranch/spr_tile_wall_ranch_stone_n.png`, `Tiles/Decoration/ranch/spr_tile_decoration_ranch_clover_v01.png` |
| Obstacle frames | `Obstacles/M16_PairedPortal/Frames/spr_obstacle_observatory_m16_portal_enter_e_f00.png` |
| Obstacle clips | `Obstacles/M16_PairedPortal/Animations/anim_obstacle_observatory_m16_portal_enter_e.anim` |
| Background | `Background/ranch/Far/spr_bg_ranch_far_stars_v01.png` |

타일 경계는 `center,n,s,e,w,ne,nw,se,sw`를 사용한다. 상태는 실제 화면 변화에 맞춰 `idle, preview, active, blocked, reset` 등 의미 있는 단어로 적는다. 방향이나 프레임이 없는 단일 그림에서는 해당 접미사를 생략한다. `final`, `new`, `fix`, 날짜, 반복된 `v2_final`은 파일 이름에 넣지 않는다. 바뀐 결과는 Git 이력에서 관리한다. `vNN`은 명세에 있는 타일·장식 변형 번호다.

서로 다른 기존 UI 도안에는 object의 도안 식별자로 `portrait`, `theme_menu`를 사용한다. 다른 GUID를 합치거나 PNG 내용을 덮어쓰지 않는다. 예: `spr_ui_main_portrait_button_play_idle.png`.

## 24개 장치 폴더 등록표

| ID | 폴더 | 대표 테마 |
|---|---|---|
{devices}

## 크기·임포트·애니메이션

- 새 타일 원본은 최소 32×32 px이며 여러 칸 그림은 정수 타일 크기로 관리한다.
- 새 최종 픽셀 스프라이트는 Point, 압축 없음, mipmap 없음, 32px 타일의 PPU 32를 기본으로 한다.
- P01 고해상도 원본은 32px 최종 스프라이트로 간주하거나 강제로 축소하지 않는다. 표시 크기를 유지할 별도 PPU와 원본/시제품 상태를 기록한다.
- 기존 P01 10fps 클립은 이동 후 sampleRate·실제 프레임 수·스프라이트 곡선을 유지한다. 원본 `.anim`과 `.meta`를 이동한다.
- 현재 공용 UI는 기존 픽셀 크기·PPU·슬라이스·피벗을 보존한다. 실측 75개 모두 Point/Uncompressed/mipmap 없음/PPU 32다. 32px보다 작은 UI 아이콘 11개는 타일이 아니며 크기를 바꾸지 않았다. [실측 임포트 검사](Validation/TopdownCutover/supplement-import-audit.json)에 각각 기록했다.

## 레거시와 정본의 경계

- Legacy는 플랫포머 시절의 회복 가능한 보관본이며 활성 Assets에 임포트하지 않는다. 보관본의 원래 이름은 유지한다.
- `Assets/ANIMOL/Phase1`이 실제로 있으면 탑뷰 시제품 후보로 유지한다. `UI/AnimalUpgradePhase1`과 구분한다.
- P01 적용 문서만으로 아트를 승인하지 않는다. 정확한 16색이 아닌 PNG·고해상도 시제품은 그 상태를 기록한다.
- 공용 계정·로비·경제·서버·방울 기능은 실제 참조를 검사한 뒤 분류한다.
- 구형 맵 제작기는 원본을 Legacy에 보존한다. 남는 Editor 코드에는 `// LEGACY: platformer map editor, 2026-10-13`과 보관 경로를 적고 기본 비활성 `ANIMOL_LEGACY_MAP_EDITOR` define으로 차단한다.
- `MainUiV7`의 기존 20개 배경 레이어·토끼 시트 2개는 저장소의 별도 배경 기준 패키지로 유지한다.

## 이번 실측 적용

ANIMOL 공용 UI PNG 75개의 내용·GUID·`.meta`를 유지하며 화면 ID·상태 규칙으로 이름과 경로를 정리했다. [정본 manifest](../Assets/ANIMOL/Sprites/sprite_path_manifest.json)는 원래 패키지 키, 이전 정본 경로, 최종 경로와 GUID를 연결한다. Editor의 호환 입력도 최종 경로로 변환한다. 런타임 프리팹·씬은 동일 GUID를 사용한다.

실측 저장소에는 `Assets/ANIMOL/Phase1`과 지정 P01 QA 씬이 없다. 10fps 보존·P01 표시 검사·P01 Validate는 대상 없음으로 구분한다. 동물 업그레이드 Phase1 UI는 공용 기능으로 유지한다. 새 퍼즐 장치 24종이나 없는 테마 아트를 생성하지 않는다.
'''

def main():
    rows = '\n'.join(f'| {i:02} | `M{i:02}_{name}` | {theme} |' for i, (name, theme) in enumerate(DEVICES, 1))
    rules = RULES.replace('{devices}', rows)
    (ROOT / 'Docs/ANIMOL_SPRITE_PATHS_AND_NAMES.md').write_text(rules, encoding='utf-8')
    data = json.loads((ROOT / 'Docs/ANIMOL_CUTOVER_CLASSIFICATION.json').read_text(encoding='utf-8'))
    manifest = ROOT / 'Docs/ANIMOL_LEGACY_CUTOVER_MANIFEST.md'
    original = manifest.read_text(encoding='utf-8').split('\n## 추가 지시 기준 실측 분류')[0]
    counts = data['category_counts']
    appendix = '''
## 추가 지시 기준 실측 분류

추가 작업의 기준 커밋은 `87725aaa`이며 Unity는 `6000.3.8f1`, 활성 타깃은 Android다. 루트는 `C:/Users/user/Documents/GitHub/ANIMOL`이다. 추가 작업 시작 Git 상태·인덱스·기존 파일 해시는 별도 작업 저널에 보존했으며 `git reset/clean`을 사용하지 않았다.

현재 시작 씬은 `Assets/ANIMOL/Scenes/Bootstrap.unity`다. 활성 빌드 씬은 Bootstrap/Lobby/Results이며 기존 제작기·실험 씬을 등록하지 않는다. asmdef 전체 내용, 빌드 설정, 분류마다 경로·GUID·참조자·조치 이유는 [현재 분류 정본](ANIMOL_CUTOVER_CLASSIFICATION.json)에 기록했다. 이전 이동 GUID·원래 경로는 [최초 이동 인벤토리](ANIMOL_LEGACY_CUTOVER_INVENTORY.json)에도 보존한다.

| 분류 | 기록 수 | 조치 |
|---|---:|---|
'''
    for category in ['platformer-only', 'shared-or-still-referenced', 'topdown-puzzle', 'unknown']:
        action = {'platformer-only': '기존 620개 이동, 생성기 원본 17개 추가 사본과 활성 차단 파일 17개 포함',
                  'shared-or-still-referenced': 'UI·서비스·GUID/C# 타입·Resources/주소 참조를 따라 유지',
                  'topdown-puzzle': '정확한 Phase1 루트와 P01 QA 씬 없음', 'unknown': '판정 보류, 파일 유지 및 컴파일 검사'}[category]
        appendix += f'| `{category}` | {counts.get(category, 0)} | {action} |\n'
    appendix += '''
기록 수는 활성 파일과 Legacy 파일/사본 기록을 합친 수다. 같은 원본의 활성 차단 파일과 Legacy 사본은 별도 기록이다. `.meta`와 폴더는 GUID 검사에 포함하며 분류 행과 따로 집계한다. C# 참조는 주석·문자열을 제거한 타입 토큰 후보 분석으로 보수적으로 추적하며 모호한 항목을 삭제하지 않는다.

과거 Campaign/Stages는 T01~T05 각각 20개, 총 100개 슬롯이다. 점프·활강·스태미나·동물 전환 관련 `DevPlayerController`, `DevTestSession`, `MobileGameplayControls`, 동물 정의와 MapObjects는 공유 계약·HUD·캠페인 데이터의 참조를 따라 유지한다. 방울 서비스는 신규 게임에도 적용할 수 있는 별도 서비스이며 플랫폼 동작과 같은 파일로 추정하지 않는다. UI/AnimalUpgradePhase1은 실제 계정/동물 업그레이드 화면이고 `Assets/ANIMOL/Phase1`은 존재하지 않는다.

UiBuildPipeline 부분 클래스 17개는 과거 UI/맵/실험 씬을 함께 재생성하는 비활성 bulk generator다. 현재 UI 런타임이나 MainUiV7Builder의 외부 호출자가 없으며 원본을 Legacy의 동일 경로에 보존했다. 현재 UI 렌더러·서비스·MainUiV7Builder는 유지한다. 파일 전체가 Editor 생성 파이프라인인 이 그룹에만 전체 조건부 컴파일을 유지하며, CampaignUiPresenter는 이미 분리한 과거 씬 호출 함수 본문만 조건부 컴파일한다.

CampaignUiPresenter의 과거 호출부를 복원할 수 있는 원본 사본도 `Legacy/Platformer_20261013/Assets/ANIMOL/Scripts/UI/CampaignUiPresenter.cs`에 보존했다. 활성 공용 UI는 그대로 두고 세 함수의 호출 본문과 보관 위치 주석만 유지한다.

### 보관 생성기 원본과 비활성 지점

| 활성 파일 | GUID | 원본 사본·조치 |
|---|---|---|
'''
    for r in data['records']:
        if r['action'] == 'disabled-with-legacy-copy':
            appendix += f"| `{r['path']}` | `{r['guid']}` | `Legacy/Platformer_20261013/{r['path']}`; LEGACY 주석 + 기본 비활성 define |\n"
    appendix += '''
### 공용 유지 증거

| 파일 | GUID | 직접 참조자 예시·이유 |
|---|---|---|
'''
    names = {'BubbleObjectiveService.cs', 'CampaignStageDefinition.cs', 'DevPlayerController.cs', 'DevTestSession.cs',
             'MobileGameplayControls.cs', 'UiNavigationService.cs', 'MainUiV7Builder.cs', 'CampaignUiPresenter.cs'}
    for r in data['records']:
        if Path(r['path']).name in names and r['action'] == 'retain':
            callers = ', '.join('`' + p + '`' for p in r['referrers'][:4]) or r['reason']
            appendix += f"| `{r['path']}` | `{r['guid']}` | {callers} |\n"
    appendix += '\n현재 unknown 전체 경로와 참조자는 분류 정본에서 `category == unknown`으로 조회한다. 해당 파일을 이번 작업에서 옮기거나 승인된 탑뷰 콘텐츠로 표시하지 않는다.\n'
    manifest.write_text(original + appendix, encoding='utf-8')
    readme = '''# Platformer_20261013 복원 가능한 보관본

기준일: 2026-10-13. 이 폴더는 저장소 루트의 Legacy이며 Unity Assets 바깥에 있다.

## 범위와 기준 커밋

- 플랫포머 전용 코드·실험/게임플레이 씬·맵·프리팹·아트·테스트 620개를 원래 상대 경로와 `.meta`로 보관했다.
- 활성 프로젝트에 유지한 UiBuildPipeline Editor 생성 파일 17개의 조건부 컴파일 이전 원본과 `.meta`도 같은 상대 경로에 보존했다.
- 공용 CampaignUiPresenter의 과거 씬 호출부를 풀어 보존한 사본 1개와 `.meta`도 동일 경로에 있다. 활성 파일에서는 공용 UI와 세 호출부의 조건부 컴파일을 분리한다.
- 기존 사용자 빌드 씬 설정과 NamedArt 인덱스는 `ProjectSettings/`의 동일 상대 경로에 있다.
- 플랫포머 정리 전 추적 파일 기준: `be907f08` (`Resize player body and visuals to a 0.8 tile square`).
- 최초 컷오버: `87725aaa` (`Archive platformer assets and canonicalize UI sprite paths`). 이번 추가 검토의 기준 커밋이기도 하다.
- 현재 작업 트리의 수정 내용과 기존 미추적 파일을 보존한다. 최초 이동 파일 중 242개는 기존 추적 파일, 378개는 기존 미추적 파일이다. 커밋만으로 미추적 사용자 파일과 모든 공용 파일의 미커밋 변경이 복원되지는 않는다.

## 원래 경로와 목록

`Legacy/Platformer_20261013/Assets/ANIMOL/...`의 접두사 `Legacy/Platformer_20261013/`를 제거하면 원래 저장소 상대 경로다. 파일과 `.meta`를 한 쌍으로 취급한다. 폴더 `.meta`도 함께 보존한다.

- [최초 620개 이동 경로·GUID 목록](../../Docs/ANIMOL_LEGACY_CUTOVER_INVENTORY.json)의 `moves` 중 destination이 이 폴더인 행.
- [현재 분류·원본 사본 목록](../../Docs/ANIMOL_CUTOVER_CLASSIFICATION.json)의 `archived`와 `copied-original` 행.
- [전체 컷오버 명세](../../Docs/ANIMOL_LEGACY_CUTOVER_MANIFEST.md)와 [결과 보고](../../MCP_INBOX/ANIMOL_TOPDOWN_LEGACY_SPRITES_RESULT.md).

## 복원 절차

1. 현재 작업 트리와 미추적 파일을 별도 보존하고, 복원은 별도의 작업 트리에서 수행한다. 예: `git worktree add ../ANIMOL-platformer-restore be907f08`.
2. 목록에서 필요한 Legacy 파일을 골라 그 작업 트리의 원래 상대 경로에 파일·인접 `.meta`·폴더 `.meta`를 함께 복사한다. 이미 있는 파일은 차이를 먼저 검토한다. 현재 활성 프로젝트로 같은 GUID의 두 사본을 넣지 않는다.
3. 미추적 378개와 공용 코어·UI·서비스에 남은 사용자 작업본이 해당 복원 코드의 선행 의존성일 수 있다. 현재 저장소의 실제 파일과 분류 참조자를 대조해 복원 작업 트리에서 함께 구성한다.
4. 원본 생성기 사본에는 이번 조건부 컴파일 래퍼가 없다. 활성 파일의 define을 켜는 것만으로 Assets 밖의 씬·자료가 복원되지는 않는다. 선택한 소스/씬/참조를 먼저 복원하고 해당 브랜치에서 제작기 설정을 명시적으로 구성한다.
5. Unity 6000.3.8f1로 재임포트·컴파일·중복 GUID·누락 참조를 검사한다. 보관된 EditorBuildSettings는 복원 참고 자료로 사용하고 현재 탑뷰 빌드 설정과 구분해 검토한다.
6. 맵 제작기·실험 씬·관련 EditMode/PlayMode 테스트를 실행한 뒤 복원 브랜치에 별도로 커밋한다. GUID 재생성, `.cs` 확장자 변경, `git reset/clean`은 복원 방법으로 사용하지 않는다.

보관본은 과거 구현의 회복 가능성을 위한 기록이다. 신규 탑뷰 장치나 승인된 32px 최종 아트로 간주하지 않는다.
'''
    (ROOT / 'Legacy/Platformer_20261013/README.md').write_text(readme, encoding='utf-8')

if __name__ == '__main__':
    main()
