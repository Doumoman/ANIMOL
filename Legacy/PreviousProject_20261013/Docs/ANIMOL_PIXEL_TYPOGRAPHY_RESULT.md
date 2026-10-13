# ANIMOL 픽셀 UI 폰트 적용 결과

`Assets/ANIMOL/Fonts/pixelroborobo.otf`를 월궁 대표 플레이 HUD, 일시정지 버튼, 결과 화면에 적용하고 1080×1920·1080×2400 Game View에서 확인했다. 검토 후 공통 UI 프리팹 인스턴스로 적용을 확장했다. 기존 Safe Area, 버튼·터치 영역, Text 참조와 갱신 코드를 보존하기 위해 프리팹 원본을 일괄 변환하지 않고 TMP 표시 계층을 추가했다.

## 기존 구성과 적용 방식

조사한 UI 프리팹은 공통 루트 1개와 화면·모달·HUD 33개다. 기존 TMP 텍스트·TMP Font Asset은 0개였으며, 화면 갱신 코드는 `UnityEngine.UI.Text`를 직접 참조했다. 상세 개수는 [조사 기록](PixelTypography/inventory.txt)에 있다. TMP는 설치된 `com.unity.ugui` 2.0.0에 포함되어 있었지만 Essential Resources는 없어 같은 설치 패키지에서 가져왔다. Unity 버전은 6000.3.8f1이다.

`PixelTextBridge`는 기존 Text를 데이터·레이아웃·입력 참조로 유지하고 글자 메시만 비운다. 자식 `TextMeshProUGUI`가 같은 영역에서 문자열과 색상을 표시하며 Raycast Target은 끈다. 문자열 변경, 활성화, 접근성 글자 크기 변경에 대응한다. 브리지를 끄면 원래 Text 표시로 돌아간다. 원본 문자열·버튼 콜백·캠페인 데이터는 바꾸지 않는다.

공통 `Resources/ANIMOLPixelTypography.prefab`과 프로필을 사용해 `SafeArea`가 있는 Canvas에 적용한다. 실행 시 이미 존재하는 화면과 나중에 생성되는 공통 UI 프리팹을 탐색한다. **원본 UI 프리팹의 Text 컴포넌트를 TMP로 직렬화 교체한 작업은 아니다.** 따라서 편집 모드의 원본 프리팹 미리보기는 기존 폰트이고, Play에서 새 폰트를 볼 수 있다. InputField의 입력 문자열·캐럿은 별도 마이그레이션이 필요하므로 제외한다. 월드 TextMesh, 에디터 도구, SafeArea 없는 외부 Canvas도 제외한다.

## 폰트 에셋 설정

| 에셋 | 방식 | 아틀라스 | 용도 |
|---|---|---|---|
| `Pixelroborobo_UI` | Static | 1024×1024, 1장 | 현재 UI 문구의 원본 폰트 지원 문자 584개 |
| `Pixelroborobo_Fallback` | Dynamic, Multi Atlas | 512×512씩 | 정적 에셋에 없는 한글·문자 |
| `Symbols_Fallback` | Dynamic, Multi Atlas | 512×512씩 | Liberation Sans의 기호 |
| `NotoSansSymbols2-Regular_Fallback` | Dynamic, Multi Atlas | 512×512씩 | 체크·교차·방향·경고 등 부족한 기호 |
| `NotoSansSymbols_Fallback` | Dynamic, Multi Atlas | 512×512씩 | 로마 숫자 등의 나머지 기호 |

모두 Sampling Point Size **16**, Padding **2**, `RASTER_HINTED`, `TextMeshPro/Mobile/Bitmap`, **Point 필터**, Clamp, Aniso 0, **밉맵 없음**이다. 실제 텍스처 mipmapCount는 1이며 알파 값은 0·255뿐이었다. SDF 가장자리 보간·소프트니스·가짜 Bold를 사용하지 않는다. 신규 동적 아틀라스도 Point 설정을 다시 적용한다. [실제 에셋 설정](PixelTypography/final-font-settings.txt)을 함께 보관한다.

동적 폴백의 글리프·텍스처 캐시는 저장/빌드 시 비워질 수 있으며 원본 폰트에서 필요할 때 재생성한다. 설정 기록의 동적 문자 수는 검증 문구를 채운 세션의 값이다. 정적 본문 584자만 고정 수록하고, 동적 폴백에는 `Clear Dynamic Data On Build`를 유지했다.

현재 프리팹 문구와 프로젝트 UI 생성·갱신 문자열에서 중복을 제거한 [606개 문자 목록](PixelTypography/ui-corpus.txt)을 사용했다. 일부 에디터 표시 문자열도 보수적으로 포함된다. 원본의 한글 음절 11,172자 지원 여부는 FontEngine으로 조사했지만 이를 정적 아틀라스에 모두 굽지 않았다. 최초 생성에서 부족했던 기호를 폴백으로 보완했으며 최종 문자 누락은 0개다. [최종 커버리지](PixelTypography/final-coverage.txt)와 최초 진단인 `font-build.json`을 구분한다.

Unity의 [폰트 에셋 설정 설명](https://docs.unity.cn/Packages/com.unity.textmeshpro%403.2/manual/FontAssetsProperties.html)을 참고하고 설치된 TMP 소스와 실제 생성 결과로 확인했다. 기호 폰트는 [Google Fonts의 Noto Sans Symbols 2](https://github.com/google/fonts/tree/main/ofl/notosanssymbols2), [Noto Sans Symbols](https://github.com/google/fonts/tree/main/ofl/notosanssymbols)에서 받았으며 OFL 원문을 `Typography/ThirdParty/`에 포함했다. 본문은 사용자 제공 Pixelroborobo이고, 다른 서체는 원본에 없는 기호에만 사용한다.

## 표시 크기와 줄바꿈

텍스트 크기는 Canvas 배율을 고려한 물리 픽셀 기준 16의 배수로 맞춘다. 월궁 HUD·조작 버튼은 32px, 월궁 제목 48px, 결과 제목 64px이며 로비 제목은 80px다. 자동 맞춤은 연속적인 소수 배율 대신 정수 단계로 축소한다. 행간을 추가하고 긴 문단은 단어 경계에서 균형 있게 줄을 나눠 결과 안내문의 마지막 두 글자만 남는 현상을 수정했다. TMP 표시 문자열의 줄바꿈만 조정하며 기존 Text 내용은 유지한다.

공통 프리팹 검사의 활성 문구 290개 중 각 해상도에서 4개는 기존 좁은 영역에 맞추느라 16px가 된다. 위치는 `SC13_Settings/SettingsSummary`, `SC15_GameplayHud/AnimalLabel`, `SC15_GameplayHud/CheckpointLabel`, `SC16_Result/ResultRewardBreakdownDetail`이다. 이 개발용 상세 정보는 실제 기기에서 추가 가독성 검토 대상이다. 월궁 대표 HUD와 실제 결과 화면의 활성 본문·버튼에는 16px 축소가 없었다. 손가락 크기나 입력 영역을 줄여 공간을 확보하지 않았다.

## 실제 화면 캡처

원본 PNG와 문구별 영역·실제 크기·줄 수·누락·넘침 JSON을 [Captures 폴더](PixelTypography/Captures/)에 남겼다. `ScreenCapture.CaptureScreenshotAsTexture`를 프레임 종료 시 호출한 실제 Game View이며 Overlay HUD가 포함된다. 같은 실행에서 원래 Text와 TMP 표시를 전환해 `before`·`after`를 비교할 수 있다.

| 화면 | 1080×1920 | 1080×2400 |
|---|---|---|
| 월궁 HUD | [적용 전](PixelTypography/Captures/MoonGraphicsQA_before_1080x1920.png) · [적용 후](PixelTypography/Captures/MoonGraphicsQA_after_1080x1920.png) | [적용 전](PixelTypography/Captures/MoonGraphicsQA_before_1080x2400.png) · [적용 후](PixelTypography/Captures/MoonGraphicsQA_after_1080x2400.png) |
| 일시정지 버튼 | [확인](PixelTypography/Captures/MoonGraphicsQA_buttons_1080x1920.png) | [확인](PixelTypography/Captures/MoonGraphicsQA_buttons_1080x2400.png) |
| 결과 화면 | [적용 전](PixelTypography/Captures/Results_before_1080x1920.png) · [적용 후](PixelTypography/Captures/Results_after_1080x1920.png) | [적용 전](PixelTypography/Captures/Results_before_1080x2400.png) · [적용 후](PixelTypography/Captures/Results_after_1080x2400.png) |
| 로비 | [적용 후](PixelTypography/Captures/Lobby_after_1080x1920.png) | [적용 후](PixelTypography/Captures/Lobby_after_1080x2400.png) |

결과 화면은 `MOON-QA-TYPOGRAPHY`, 123.4초·방울 3개의 **문구 검사용 개발 결과 데이터**다. 실제 월궁 완주·운영 보상·서버 처리를 주장하지 않는다. `Common_*` 66장은 공통 프리팹 33종을 두 해상도에서 각각 띄운 표시 검사이며, 모든 운영 상태를 통과했다는 의미가 아니다. 전체 캡처는 80장이다. 캡처 문구의 누락·영역 넘침은 0건이며, 대표 화면은 이미지를 직접 확인했다.

## 검증과 원본 보존

작업 시작 커밋은 `561c700a41be99e4831d4838670a9ed68201442e`다. [기준 기록](PixelTypography/baseline.json)에 기존 미커밋 파일 1,041개와 보호 대상 해시를 남겼다. T01-S01 수동 맵과 MoonGraphicsQA 씬은 재생성하거나 저장하지 않았다. 기존 미커밋 공통 UI 프리팹·컨트롤러도 수정하지 않았다. 제공받은 OTF와 메타 파일은 원본 그대로 이번 폰트 기능의 필수 자산으로 포함한다.

최신 코드의 컴파일 오류는 없었다. 전체 결과는 [검증 기록](PixelTypography/final-verification.json), [EditMode XML](PixelTypography/final-editmode.xml), [PlayMode XML](PixelTypography/final-playmode.xml)에 보관한다.

- EditMode: **168건 중 162건 통과, 기존 실패 6건**. 이전 `Docs/RunPrototype/final-editmode.xml`과 실패 테스트 이름·메시지가 모두 같다. 수동 맵의 조합 수·객체 ID·타일 수·초기 백업 해시와 과거 기대값 차이이며 새 실패는 없다.
- PlayMode: **82/82 통과**. 기존 79건과 새 3건을 함께 실행했다.
- 새 폰트 검사: EditMode 3건 + PlayMode 3건, **6/6 통과**.
- `CommercialPolishPolicyTests`: **3/3 통과**.
- 캡처: **80장**, 모두 1080×1920 또는 1080×2400, 문구 누락·영역 넘침 **0건**.
- 기존 미커밋 파일 **1,041개 바이트 보존** 및 T01-S01·MoonGraphicsQA 보호 해시 일치.

새 검사는 래스터·Point·밉맵 설정, 전체 문구 및 미수록 한글 폴백, 실제 월궁 일시정지·계속 입력, Text 갱신·원래 표시 복원, 공통 프리팹의 RectTransform·콜백·컴포넌트 참조 보존을 확인한다. 전체 테스트가 재기록한 기존 주행 프로토타입 텔레메트리의 부동소수점 차이는 이번 실행에서 생긴 변경만 원복해 이전 증거 파일을 유지했다. 실패한 EditMode 실행의 CLI에는 라이선스 토큰 갱신 경고도 표시되었으나 NUnit 결과 파일은 정상 생성됐고 실제 검사 실패는 위 6건이다.

배치 실행의 기본 화면은 640×480이고 Canvas 배율은 0.25다. 이 환경에서 최소 16 물리 픽셀을 요구하는 새 검사가 처음 실패했으므로, 입력·레이아웃 검사에 1080×1920 카메라 렌더 타깃과 1:1 Canvas 배율을 명시했다. 검사에서만 화면 표현 조건을 맞췄으며 제품의 Safe Area·배율 정책은 바꾸지 않았다. 실제 Overlay Game View의 두 세로 해상도 검증은 위 PNG·JSON으로 별도 수행했다. 640×480 편집기 미리보기의 모든 문구 맞춤을 보장하는 작업은 아니다.

재검증 명령은 열린 Unity를 정상 종료한 뒤 저장소 루트에서 실행한다.

```powershell
unity test . --mode EditMode --output Docs/PixelTypography/final-editmode.xml --timeout 600 -- -logFile Logs/pixel-typography-edit.log
unity test . --mode PlayMode --output Docs/PixelTypography/final-playmode.xml --timeout 600 -- -logFile Logs/pixel-typography-play.log
powershell -NoProfile -ExecutionPolicy Bypass -File Docs/PixelTypography/verify.ps1
```

화면을 다시 보려면 `MoonGraphicsQA` 또는 `Results` 씬을 열고 `ANIMOL > Portrait Preview`에서 해상도를 선택한 후 Play한다. 폰트는 실행 시 설치된다. 생성 도구 `Build pilot fonts`는 기존 폰트 에셋 덮어쓰기를 거절하므로 확인용으로 다시 실행할 필요가 없다. 공통 적용 승인 상태는 `Typography/PixelTypographyProfile.asset`의 `CommonUiApproved`이며 이를 끄면 월궁·결과 파일럿 범위로 제한할 수 있다.

## 변경 파일과 남은 범위

- `Assets/ANIMOL/Typography/`: TMP Font Asset 5개·재질/텍스처 서브 에셋, 프로필, 공통 설치 프리팹, 호환 렌더러, 생성·캡처 도구, 테스트, OFL 기호 폰트.
- `Assets/ANIMOL/Fonts/pixelroborobo.otf`와 메타: 사용자 제공 원본, 바이트 변경 없음.
- `Assets/TextMesh Pro/`: 설치된 Unity 패키지의 Essential Resources와 라이선스.
- `Docs/PixelTypography/`와 이 보고서: 조사·생성 설정·문자 목록·캡처·테스트·보존 검증.

원본 프리팹을 TMP 전용으로 전환하려면 Text를 참조하는 Presenter와 접근성 코드의 API 마이그레이션을 별도 작업으로 해야 한다. 이번 호환 방식은 전환 위험을 줄이는 대신 Text와 TMP 두 컴포넌트를 유지하고, 동적 생성 UI를 0.25초 주기로 탐색한다. 저사양 실기기 성능, 극단적 Safe Area, 모든 언어·사용자 입력·큰 글씨 조합, 글자 확대 연출 중 픽셀 정렬은 별도 검토 대상이다. 기존 피드백 연출은 제거하거나 변경하지 않았다.

이 기능도 최신 워크트리의 기존 미커밋 코드·프리팹에 의존한다. 이번 커밋에 그 작업을 섞지는 않는다.
