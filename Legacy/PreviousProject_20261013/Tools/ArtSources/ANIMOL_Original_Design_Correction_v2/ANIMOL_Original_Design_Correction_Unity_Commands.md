# ANIMOL 원래 디자인 유지 보정 · Unity 적용 명령

이번 보정은 새로운 디자인으로 교체하는 작업이 아니다. 기존 건물·다리·난간의 형태와 테마별 구도를 기준으로, 다리 표면의 불규칙한 점 명암을 줄이고 윤곽과 연결된 그림자 면을 정리한다. 캠페인 카드도 원래의 밝은 내부 면과 어두운 글자 조합을 유지한다.

패키지는 `ANIMOL_Original_Design_Correction_v2`이며, **배경 PNG 교체 20개·SC02 PNG 교체 0개**다. SC02는 기존 Batch01 배경·프레임·썸네일을 그대로 사용하고 `ui-config/campaign-style-corrections.json`의 밝은 내부 면과 글자 색 설정을 현재 UI에 연결한다.

아래 명령은 현재 ANIMOL Unity 프로젝트에 연결된 Codex에 전달한다. `<ANIMOL_Original_Design_Correction_v2 절대 경로>`에는 압축을 푼 폴더의 절대 경로를 넣는다. 이 문서를 만든 환경에는 운영 프로젝트와 Unity Editor가 연결되어 있지 않아 실제 적용·컴파일·모바일 검증은 실행하지 않았다.

## 자산 대응과 규격

표의 출처는 이전 패키지 manifest에서 확인한 제작 파일 경로다. 현재 Unity 프로젝트의 실제 Resources 키·GUID·활성 참조 경로는 명령 실행 시 소스에서 확인한다. 새 수입 폴더는 이번 보정만 담는 목적지다.

| 영역 | 이번 v2 적용 대상 | 확인된 원본 출처 | 규격·처리 |
|---|---|---|---|
| 메인 5테마 레이어 | 새 PNG: `runtime/main-background/T01..T05-{far,mid,platform,near}.png` | `ANIMOL_main_ui_v7/runtime/assets/`의 같은 파일명 | 20개 교체, 각각 352×704, PPU 32 |
| 캠페인 표시 설정 | 새 설정: `ui-config/campaign-style-corrections.json` | 첨부된 원래 캠페인 화면의 밝은 면·어두운 글자 조합 | 기존 UI의 배경색·글자색 연결, PNG 교체 없음 |
| 캠페인 배경 | 기존 `BG_UI_Campaign_Common` 참조 유지 | `ANIMOL_ui_production_v1/runtime/backgrounds/BG_UI_Campaign_Common.png` | 현재 Batch01 PNG 유지, 352×704 |
| 5테마 썸네일 | 기존 `Thumb_Theme_T01..T05` 참조 유지 | `ANIMOL_ui_production_v1/runtime/thumbnails/`의 같은 파일명 | 현재 Batch01 PNG 유지, 224×112 |
| 캠페인 카드 프레임 | 기존 `UI_Campaign_ThemeCard_Frame` 참조 유지 | `ANIMOL_ui_production_v1/runtime/ui/UI_Campaign_ThemeCard_Frame.png` | 현재 Batch01 PNG 유지, 288×136, Sliced border L/B/R/T=24/24/24/24 |
| 캠페인 아이콘 | 기존 `UI_Icon_*` 참조 유지 | `ANIMOL_ui_production_v1/runtime/ui/`의 같은 파일명 | 현재 Batch01 PNG 유지, 32×32 |

실제 PNG 수입 대상은 **새 manifest의 배경 20개만**이다. v2에는 `runtime/theme-select/`가 없으며, SC02 12개 PNG와 토끼 시트는 이번에 수입·교체하지 않는다. 기존 SC02 참조와 파일 해시는 설정 파일의 `retainedArt`와 대조해 기록한다. 현장 자산이 이 목록과 다르면 현재 파일을 보존하고 차이를 보고한다. 기존 아트를 과거 버전으로 되돌린 뒤 설정을 적용하지 않는다. `source/`·`previews/`·참고 이미지·폰트는 운영 자산으로 수입하지 않는다.

공통 수입 설정은 Point, Uncompressed, mipmap 없음, Full Rect, 자동 trim 없음, Clamp, 원색 RGBA다. 플랫폼 override가 다시 압축하거나 Bilinear로 바꾸지 않는지 확인한다. 네이티브 352×704의 정확한 4배는 **1408×2816**이다. 1080×1920이나 1080×2400은 기존 화면 레이아웃 검사 크기이며 원본 도트 해상도가 아니다.

허용 색은 기존 Sweetie16의 다음 16색뿐이다. 참고 이미지의 색을 새로 추출해 추가하지 않는다.

```text
#1a1c2c #5d275d #b13e53 #ef7d57 #ffcd75 #a7f070 #38b764 #257179
#29366f #3b5dc9 #41a6f6 #73eff7 #f4f4f4 #94b0c2 #566c86 #333c57
```

## 1. 메인 배경의 원형과 다리 마감 적용

```text
[ANIMOL_ORIGINAL_DESIGN_CORRECTION_01_BACKGROUND]
보정 패키지 경로: <ANIMOL_Original_Design_Correction_v2 절대 경로>

현재 ANIMOL Unity 로비의 5테마 배경에 이 보정 패키지의 최종 아트를 적용하라.
기존 건물·다리·난간·테마 구도와 레이어 역할을 유지하고, 다리 표면의 점 명암을
줄인 최종 PNG를 실제 활성 화면에 연결하라. 별도의 새 디자인을 만들지 마라.

현재 프로젝트 경로와 AGENTS.md, Assets/Packages/ProjectSettings를 확인하라.
git status와 관련 diff를 기록하고 기존 사용자 변경·서버 연동 변경을 보존하라.
새 패키지 manifest.json의 배경 PNG20개 목록·크기·해시를 확인하라.
영역 manifest가 동봉되어 있으면 레이어 계약도 대조하라.
문서의 originalPath는 제작 출처이며 현재 Unity 경로로 간주하지 마라.

운영 로비의 실제 배경 컨트롤러·프리팹·생성기·Texture/Sprite 로딩 경로를 추적하라.
T01~T05의 far/mid/platform/near가 실제 어느 PNG와 GUID/Resources 키에 연결되는지
기록하라. 과거 Batch01이 아직 수입만 된 상태인지, 운영 화면에 적용됐는지도 확인하라.
활성 참조를 확인하지 않고 동명 파일 전체나 예전 생성 프리팹을 일괄 교체하지 마라.

새 manifest에 선언된 T01~T05의 4레이어 PNG20개만
Assets/ANIMOL/UI/OriginalDesignCorrectionV2/runtime/main-background/에 수입하라.
이 경로는 이번 보정용 새 목적지이며 기존 운영 경로라고 가정하지 마라.
해시를 대조하고 Point/Uncompressed/mipmap 없음/Full Rect/trim 없음/PPU32를 적용하라.
기존 Texture 렌더 경로는 Texture로, Sprite 렌더 경로는 기존 slicing/좌표 방식으로 유지하라.
운영 영역의 실제 아트 참조와 관련 생성기의 아트 매핑만 새 PNG에 연결하라.
기존 키 기반 로딩을 사용하는 경우 공개 Resources/Addressables 키와 호출 계약을 보존하라.
GUID를 유지하는 직접 교체가 필요하면 해당 PNG·meta·importer·사용처를 먼저 백업하라.
다른 화면과 공유된 아트는 로비에만 새 참조를 연결해 영향 범위를 제한하라.

각 테마의 주요 원래 건물 위치·지붕·기둥·아치·난간 개구부·재질 구분을 보존하라.
이번 최종 PNG에는 작은 윤곽 정리와 이진 알파 경계 정리가 포함될 수 있다.
원본 알파의 모든 픽셀이 동일하다고 가정하거나 이전 알파를 다시 강제 덮어쓰지 마라.
far/mid/near의 원형 복원 자산을 새로 다른 형태로 그리거나 보정 필터로 변형하지 마라.
platform의 불규칙한 1~2도트 점 명암은 최종 보정 PNG에서 줄인 상태를 유지하라.
그림자는 밝은 면·기본 면·연결된 어두운 면으로 읽히게 두고,
텍스처를 강조하려고 얼룩·디더링·랜덤 점·잦은 색 띠를 다시 추가하지 마라.
기본 윤곽은 네이티브 1도트의 일정한 선으로 읽히게 유지하라.
원본 구조나 manifest가 명시한 두꺼운 프레임은 그 의도된 두께를 일관되게 유지하라.
곡선·대각선의 계단 길이, 튀어나온 픽셀(Jaggies), 모서리의 겹친 외곽선(Doubles)을
원본1x와 Point4x에서 확인하라. 창틀·재질 경계와 무작위 점 명암을 구분하라.
단순 palette/alpha 검사 통과만으로 마감 검토가 완료됐다고 보고하지 마라.

352x704 네이티브 좌표와 기존 화면 비율·Safe Area를 유지하라.
합성 순서 바탕→far→mid→platform→rabbit→near→기존 UI를 유지하라.
platform은 352x704 캔버스의 (0,0) 고정 레이어이며 시차와 카메라 이동은 0이다.
platform에 속한 건물과 다리는 배경 카메라를 따라 움직이지 않게 하라.
floorY=526, rabbit cell topY=436, footY=90을 유지하라.
export/source 이동량은 최종 PNG에 이미 반영된 제작 기록이므로 런타임에 다시 더하지 마라.
T04 원본 플랫폼의 바닥 Y527은 최종 플랫폼 전체를 위로1도트 이동해 Y526으로 맞췄다.
이 조정은 최종 T04-platform.png에 반영되어 있으므로 런타임에 -1을 추가하지 마라.
중앙 x70..282의 y526 보행면 연결과 T03 난간 창 9개/T05 난간 창 6개를 확인하라.

현재 토끼 캐릭터 디자인·세잎클로버 핀·기존 시트와 셀 정렬은 유지하라.
이번 manifest의 PNG20개에는 토끼가 없으므로 기존 좌우 시트를 교체하지 마라.
기존 64x96×8셀, 512x96 좌우 시트, 14fps, 원래 여백과 발선 움직임을 보존하라.
토끼는 플랫폼 위를 수평으로 달리는 현행 이동을 유지하며 배경 카메라 시차는 0이다.
프레임3/7의 발 Y86과 나머지 프레임의 발 Y90을 별도로 평탄화하지 마라.

기존 테마 순서 T01~T05, 각5초 슬롯, 전환0.36초와 전환 구현을 유지하라.
전환 시간을 더해 슬롯을 5.36초로 늘리지 마라.
배경 방향·토끼 방향의 독립 랜덤과 기존 재현 seed를 유지하라.
카메라 이동88x72, layerScale far1.16/mid1.36/platform1/near1.6,
시차계수 far0.30/mid0.62/platform0/rabbit0/near1.60을 유지하라.
배경의 대각선 위→아래 이동과 근경의 빠른 이동을 유지하라.
새 카메라·Canvas·EventSystem을 중복 생성하지 말고 기존 자원·생명주기를 재사용하라.
배경은 raycast를 받지 않게 하고 버튼·모달·입력·기존 콜백을 보존하라.

최종 파일과 원형 비교판을 원본1x/Point4x에서 대조하고 다리 마감 확대 캡처를 남겨라.
5테마×양쪽 카메라 방향×양쪽 토끼 방향에서 잘림·빈틈·근경 가림을 확인하라.
1080x1920/1080x2400/Safe Area 실제 Game View에서 25초 이상 순환을 확인하라.
현재 프로젝트의 관련 좁은 검사와 Editor 컴파일을 실행하고 기존 실패를 구분하라.
Docs/ANIMOL_ORIGINAL_DESIGN_CORRECTION_01_RESULT.md에 실제 참조 변경·GUID·importer·
검사·캡처·수정 파일 목록을 기록하라. Unity 접근 불가는 미실행으로 쓰고 PASS로 쓰지 마라.
```

## 2. 캠페인 카드의 밝은 면과 원래 UI 복원

```text
[ANIMOL_ORIGINAL_DESIGN_CORRECTION_02_CAMPAIGN_UI]
보정 패키지 경로: <ANIMOL_Original_Design_Correction_v2 절대 경로>

현재 SC02 캠페인 테마 선택창에
ui-config/campaign-style-corrections.json의 표시 보정을 실제로 연결하라.
원래 청록·금색 프레임, 밝은 카드 내부, 어두운 글자와 기존 배치를 유지하라.
이전 개정에서 내부가 남색으로 바뀌었다면 해당 아트/배경색 매핑을 원래 조합으로 보정하라.

현재 git diff와 프로젝트 지침을 확인하고 첫 영역의 작업·사용자 변경을 보존하라.
새 manifest.json과 ui-config/campaign-style-corrections.json을 읽으라.
runtimeSpriteReplacements=[]와 replacedAssetCount=0임을 확인하라.
v2에는 runtime/theme-select/가 없으므로 SC02 PNG를 새로 수입하거나 교체하지 마라.
retainedArt의 기존 Batch01 배경·프레임·5썸네일 해시와 현장 파일을 대조해 기록하라.
기존 12개 SC02 PNG·GUID·참조를 유지하고 이전 후보나 source/ 이미지로 보충하지 마라.
ProductionController·현재 SC02 프리팹/생성기·CampaignCatalog·실제 진행도/잠금 바인딩을
추적하고 활성 Image/RawImage/TMP 참조와 로딩 키를 기록하라.
실제 ThemeId와 아트 ID를 대조하라. 배열 순서나 시안의 표시 숫자를 데이터로 쓰지 마라.

현재 SC02에서 쓰는 PNG의 Point/Uncompressed/mipmap 없음/Full Rect/trim 없음/PPU32를 확인하라.
기존 UI_Campaign_ThemeCard_Frame은 288x136, Sliced border L/B/R/T=24/24/24/24를 유지하라.
코너와 아이콘을 stretch 영역으로 늘리지 마라. Simple 그림은 preserveAspect로 표시하라.
현재 공개 Resources/Addressables 키·ThemeId·GUID와 아트 참조를 유지하라.
설정 JSON을 복사하는 것으로 끝내지 말고 uiCorrections의 색 값을 기존 SC02 카드/헤더
배경 Image와 실제 TMP/PixelTextBridge 글자에 연결하라.
현재 UI가 생성기로 구성된다면 그 영역의 색 설정만 갱신해 재진입 후에도 유지되게 하라.

카드 내부 면은 기존 Sweetie16의 밝은 #f4f4f4를 기준으로 원래 디자인을 복원하라.
투명 중앙을 가진 프레임을 쓰는 렌더 경로라면 해당 카드의 기존 내부 Image 배경색을
밝게 복원하라. 프레임 PNG 자체에 이미 밝은 내부가 있다면 중복 배경 면을 만들지 마라.
밝은 내부 위 제목·진행도·상태 글자는 기존 #1a1c2c 또는 원래 지정된 어두운 팔레트 색으로
되돌리되 실제 상태별 강조색과 선택/잠금 판별 로직은 유지하라.
상단 제목 패널과 뒤로 버튼도 기존 밝은 면·어두운 글자 조합을 유지하라.
참고 사진의 밝은 디자인을 따른다는 이유로 새 색·블러·AA·그라데이션을 추가하지 마라.

previewReferenceLayout은 참고 시안의 좌표이며 layoutIsReferenceOnly=true다.
이 값을 운영 RectTransform에 그대로 복사하거나 실제 UI를 통째로 다시 배치하지 마라.
현행 카드 RectTransform·썸네일 영역·간격·스크롤·터치 영역·CanvasScaler·Safe Area를 보존하라.
pixelroborobo/TMP/PixelTextBridge의 실제 폰트·폰트 크기·텍스트 렌더 구조를 그대로 재사용하라.
텍스트가 색 변경 후 보이지 않는 경우 해당 글자 색과 기존 바인딩만 확인하고 폰트를 교체하지 마라.
테마 이름·0/20 등 진행도·잠금·선택·접근 권한은 현재 실제 데이터를 바인딩하라.
PNG에 글자·숫자·진행도를 굽지 말고 시안 예시 값을 운영 데이터로 복사하지 마라.
Button.onClick·뒤로·테마 선택·SC03 진입·잠금 표시·기존 서버 응답 처리를 유지하라.
아트 색을 바꾸면서 해금/완료 플래그·실제 재화·서비스 차단 상태를 바꾸지 마라.

원본1x/Point4x에서 청록·금색 코너, 카드 외곽과 썸네일 마감의 Jaggies/doubles를 확인하라.
밝은 면에 노이즈나 점 명암을 추가하지 마라.
1080x1920/1080x2400/Safe Area에서 5테마 전체 스크롤, 제목/진행도 가독성,
선택/잠금/준비 상태와 기존 클릭 영역을 확인하라.
로비→SC02→선택 테마의 SC03→뒤로→SC02→로비 동선을 확인하라.
현재 프로젝트의 관련 좁은 검사와 Editor 컴파일을 실행하고 기존 실패를 구분하라.
이번 보정과 무관한 15종 초상·동물 강화·상점·설정·멀티·게임플레이·서버 파일을 재생성하지 마라.
Docs/ANIMOL_ORIGINAL_DESIGN_CORRECTION_02_RESULT.md에 실제 참조 변경·색 매핑·
기능 보존·캡처·검사 결과를 기록하라. 아트 미리보기를 Unity 실제 적용 증거로 대신하지 마라.
```

두 영역을 적용한 담당자는 완료·부분·미실행을 구분해 보고한다. 새 아트의 규격·해시 확인과 원본 대비 시각 검토, 실제 Unity 연결·컴파일·기기 검증은 각각 별도의 결과로 남긴다.
