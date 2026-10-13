"""Record actual move tables, screenshot checks and executed/not-executed validation."""
import json
import xml.etree.ElementTree as ET
from pathlib import Path
from PIL import Image

ROOT = Path(__file__).resolve().parents[1]
JOURNAL = Path.home() / 'AppData/Local/Temp/animol-cutover-supplement'

def main():
    plan = json.loads((JOURNAL / 'plan.json').read_text(encoding='utf-8'))
    classification = json.loads((ROOT / 'Docs/ANIMOL_CUTOVER_CLASSIFICATION.json').read_text(encoding='utf-8'))
    checks = []
    for path in sorted((ROOT / 'Screenshots/TopdownCutover_20261013').glob('*.png')):
        with Image.open(path) as im:
            pixels = list(im.convert('RGB').getdata())
            checks.append({'path': path.relative_to(ROOT).as_posix(), 'width': im.width, 'height': im.height,
                           'unique_rgb_colors': len(set(pixels)),
                           'near_magenta_pixels': sum(r > 250 and g < 5 and b > 250 for r, g, b in pixels)})
    (ROOT / 'Screenshots/TopdownCutover_20261013/capture_checks.json').write_text(json.dumps(checks, indent=2) + '\n', encoding='utf-8')
    inventory_path = ROOT / 'Docs/ANIMOL_LEGACY_CUTOVER_INVENTORY.json'
    inventory = json.loads(inventory_path.read_text(encoding='utf-8'))
    renamed = {m['source']: m['destination'] for m in plan['sprites']}
    for move in inventory['moves']:
        if move['destination'] in renamed:
            move.setdefault('previous_destinations', []).append(move['destination'])
            move['destination'] = renamed[move['destination']]
    inventory['supplement_base_commit'] = classification['base_commit']
    inventory['classification'] = 'Docs/ANIMOL_CUTOVER_CLASSIFICATION.json'
    inventory['legacy_original_copies'] = [r for r in classification['records'] if r['action'] in {'copied-original', 'copied-legacy-parts'}]
    inventory_path.write_text(json.dumps(inventory, ensure_ascii=False, indent=2) + '\n', encoding='utf-8')
    validation = ROOT / 'Docs/Validation/TopdownCutover'
    validation.mkdir(parents=True, exist_ok=True)
    for mode in ['editmode', 'playmode']:
        dest = validation / ('supplement-' + mode + '.xml')
        dest.write_bytes((JOURNAL.parent / ('animol-cutover-supplement-' + mode + '.xml')).read_bytes())
    mobile = json.loads((validation / 'mobile-build.json').read_text(encoding='utf-8'))
    apk = JOURNAL / 'android/animol-cutover.apk'
    mobile['apk_bytes'] = apk.stat().st_size if apk.exists() else 0
    mobile['connected_adb_devices'] = []
    mobile['initial_build_stop'] = 'Three obsolete FreeShapeArt container paths in the NamedArt cache; removed after confirming files and GUIDs absent'
    mobile['build_generated_side_effects'] = 'Font line endings and UnityConnectSettings restored to exact verified starting bytes after build'
    (validation / 'mobile-build.json').write_text(json.dumps(mobile, ensure_ascii=False, indent=2) + '\n', encoding='utf-8')
    tests = {mode: ET.parse(validation / ('supplement-' + mode + '.xml')).getroot().attrib for mode in ['editmode', 'playmode']}
    counts = classification['category_counts']
    report = ROOT / 'MCP_INBOX/ANIMOL_TOPDOWN_LEGACY_SPRITES_RESULT.md'
    original = report.read_text(encoding='utf-8').split('\n## 추가 지시 적용 결과')[0]
    appendix = f'''
## 추가 지시 적용 결과

추가 작업 기준 커밋: `87725aaa`. 실측 Unity 6000.3.8f1 / 활성 타깃 Android / 시작 씬 Bootstrap / 활성 빌드 씬 Bootstrap·Lobby·Results. `Assets/ANIMOL/Phase1` 및 지정 P01 QA 씬 3종은 없다. 동물 업그레이드 Phase1 UI는 별도 공용 기능이다.

### 분류·보관·호출부

- 현재 분류 정본: [ANIMOL_CUTOVER_CLASSIFICATION.json](../Docs/ANIMOL_CUTOVER_CLASSIFICATION.json). `platformer-only` {counts['platformer-only']}, `shared-or-still-referenced` {counts['shared-or-still-referenced']}, `topdown-puzzle` 0, `unknown` {counts['unknown']}. 활성 파일·보관 파일·사본은 별도 기록이다.
- 이전 이동 620개 유지. 비활성 Editor bulk generator 17개의 조건부 컴파일 전 원본과 `.meta`를 Legacy에 추가 보존했다. 혼재한 CampaignUiPresenter는 과거 씬 호출 본문만 분리해 차단하고 복구용 사본을 별도 보존했다.
- [Legacy README](../Legacy/Platformer_20261013/README.md)에 범위·원래 경로·기준 커밋·목록·별도 작업 트리 복원 절차를 기록했다.
- 남는 Editor 코드 17개와 혼재 호출부에 정확한 LEGACY 주석과 보관 경로를 적었다. default PlayerSettings/asmdef에 define을 추가하지 않았다. `.cs` 확장자를 바꾸거나 블록 주석으로 컴파일을 숨기지 않았다.
- 5테마×20 슬롯, 총 100개 스테이지는 실측했다. 공유 계약·캠페인 선택 UI·서버·계정·방울 서비스와 연결된 파일은 유지하고 참조자를 분류 정본과 명세에 기록했다. unknown 391개도 유지했다.

### 정본 경로·이름 및 참조

UI PNG 75개를 Unity AssetDatabase.MoveAsset으로 재개명했다. 파일명은 지정 화면 ID(main/ingame/settings/stage_select/store/common)와 실제 상태 접미사를 사용한다. 기존 패키지 키와 이전 정본 경로는 manifest의 호환 입력으로 유지해 최종 경로로 해석한다. NamedArt 경로·텍스처/Sprite 이름 캐시·슬롯 키도 갱신했다. Runtime Resources 카탈로그·프리팹 키와 GUID 기반 씬·프리팹 참조를 유지했고 활성 SpriteAtlas 파일은 실측 검색에서 없었다.

PNG 75개 모두 Point/Uncompressed/mipmap 없음/PPU 32. 기존 UI 아이콘 11개는 한 변이 32px 미만이며 타일로 취급하거나 크기를 변경하지 않았다. 새 타일 최소 32×32, 고해상도 P01 별도 PPU, 10fps 프레임 보존은 정본 규칙에 명시했다. P01 대상이 없어 해당 처리·검증을 수행한 것으로 표시하지 않는다. M01~M24 등록표는 문서에만 확정하고 빈 장치 폴더나 새 게임 구현은 만들지 않았다.

| 이전 정본 경로 | 최종 정본 경로 | 유지 GUID |
|---|---|---|
'''
    for m in plan['sprites']:
        appendix += f"| `{m['source']}` | `{m['destination']}` | `{m['guid']}` |\n"
    appendix += f'''
### 실행 검증

| 검사 | 실제 결과 |
|---|---|
| 추가 변경 후 Unity 재임포트·C# 컴파일 | 성공 |
| YAML 자산 776개 / GUID 참조 19,160개 | 누락 GUID 0 |
| Assets 전체 `.meta` 중복 GUID | 0 |
| 기본 define 및 컴파일된 구형 제작기 대표 타입 6종 | default define 없음 / 타입 0 |
| 구형 제작기 메뉴·InitializeOnLoad 자동 진입 | 0 |
| Bootstrap/Lobby/Results 누락 스크립트·오브젝트 참조 | 0 |
| PNG·`.meta` 내용, Legacy 원본, 비대상 파일 해시 | 불일치 0 |
| CommercialPolishPolicyTests + CanonicalSpritePathTests | EditMode {tests['editmode']['passed']}/{tests['editmode']['total']} 통과 |
| MenuThemeFlowTests + CutoverPortraitUiTests | PlayMode {tests['playmode']['passed']}/{tests['playmode']['total']} 통과 |
| 실제 Game View 해상도 / 화면 캡처 | 1080×1920 검증 / 로비·설정·기존 HUD 3개 |
| Android DEV 빌드 | {mobile['result']}, errors {mobile['errors']}, warnings {mobile['warnings']}; APK {mobile['apk_bytes']:,} bytes |
| Android 실기기 빈 Sprite/핑크 머티리얼/UI 표시 | 미실행 — adb 연결 기기 0 |
| P01 QA 3종·P01 Validate·10fps 보존 | 대상 없음 — 미실행 |

로비에는 기존 HUD가 등록되어 있지 않으므로 저장소의 SC15_GameplayHud 프리팹을 테스트 중에만 임시 인스턴스화했다. 씬·프리팹을 저장하거나 새 게임플레이 장면을 제작하지 않았다. 캡처는 실제 1080×1920 Game View에서 실행한 UI를 동일 크기의 Canvas camera target으로 렌더한 증거다. 저장된 그림 3개의 near-magenta pixels는 모두 0이다.

시각 확인에서 로비의 일부 하단 안내문은 낮은 대비/하단 경계 근처 배치를 유지하고, 기존 HUD는 시간·스태미나·점프·동물 변경 문구를 유지한다. 이는 기존 UI의 현 상태이며 승인된 탑뷰 HUD로 표시하지 않는다. 새 탑뷰 화면 재제작은 이번 범위 밖이다.

첫 Android 빌드는 이미 존재하지 않는 과거 FreeShapeArt/V2/V4 캐시 소유자 3개 때문에 중단됐다. 파일과 GUID가 없는 것을 확인한 뒤 캐시 항목을 정리하고 현재 폰트 소유자 슬롯을 재인덱싱했다. 사전 검사는 Issues 0/ChangedSlots 0이며 빌드를 우회하지 않았다. 빌드가 자동 재직렬화한 폰트와 UnityConnectSettings는 기존 변경이 없던 파일에 한해 작업 전 해시와 일치하는 원본 바이트로 복구했다.

남은 미확정 항목: unknown 391개, P01 부재, 연결 기기 부재, 기존 HUD의 탑뷰 전용 재구성, Android 경고 25개. 경고에는 기존 TerrainEditorScreen의 CS0628과 TMP IL2CPP 큰 메서드 컴파일 알림 등이 포함된다. 기능 승인이나 장치 24종 구현 완료를 의미하지 않는다.

### 화면 및 검사 증거

- [로비 1080×1920](../Screenshots/TopdownCutover_20261013/sc01_lobby_1080x1920.png)
- [설정 1080×1920](../Screenshots/TopdownCutover_20261013/sc13_settings_1080x1920.png)
- [기존 HUD 프리팹 1080×1920](../Screenshots/TopdownCutover_20261013/sc15_gameplayhud_1080x1920.png)
- [캡처 픽셀 검사](../Screenshots/TopdownCutover_20261013/capture_checks.json)
- [임포트·자동 진입점](../Docs/Validation/TopdownCutover/supplement-import-audit.json)
- [활성 자산 GUID·컴파일 타입 검사](../Docs/Validation/TopdownCutover/supplement-reference-audit.json)
- [NamedArt 빌드 사전 검사](../Docs/Validation/TopdownCutover/namedart-build-preflight.json)
- [모바일 빌드](../Docs/Validation/TopdownCutover/mobile-build.json)
- [추가 EditMode](../Docs/Validation/TopdownCutover/supplement-editmode.xml) / [추가 PlayMode](../Docs/Validation/TopdownCutover/supplement-playmode.xml)

APK는 작업용 외부 저널의 `android/animol-cutover.apk`에 생성했으며 캐시·빌드 산출물을 저장소에 커밋하지 않는다. 추가 작업의 실제 수정 파일 목록은 커밋 경로 목록과 위 이동표/원본 사본 표에서 확인할 수 있다. 기존 사용자 수정과 미추적 파일은 계속 보존한다.
'''
    report.write_text(original + appendix, encoding='utf-8')
    print(json.dumps({'screenshots': len(checks), 'near_magenta_pixels': [c['near_magenta_pixels'] for c in checks],
                      'apk_bytes': mobile['apk_bytes'], 'tests': {m: {'total': t['total'], 'passed': t['passed']} for m, t in tests.items()}}))

if __name__ == '__main__':
    main()
