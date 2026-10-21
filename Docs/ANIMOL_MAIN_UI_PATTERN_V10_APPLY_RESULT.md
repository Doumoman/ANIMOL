# 윌리엄의 역설 UI v10 패턴 적용 결과

2026-10-21 기준 활성 Unity 프로젝트 `Assets/ANIMOL/Scenes/Runtime/ANIMOL_Main.unity`에 적용했다. 이 프로젝트의 메인 UI는 v8 기반이며, 작업 전에는 v9의 `DiagonalPatternBackground`, `AnimolPatternMotion`, 단색 셰이더, 반복 텍스처가 설치되어 있지 않았다. v10 패키지는 `MCP_INBOX/ANIMOL_Main_UI_v10.zip`에서 읽었다. 패키지 안의 `preserved-v9`는 시각 참고 자료이며 활성 Unity 씬은 아니다.

## 실제 적용

- `Assets/ANIMOL/Sprites/UI/Pattern/Faces/Face_*.png` 16장과 `Ornaments/Star_FourPoint.png`를 원본 바이트 그대로 추가했다. 16장 모두 576×576, 별은 128×128이며 한 파일에 한 캐릭터만 있다. 별도의 스프라이트 분할이나 타이트 크롭을 하지 않았다.
- 조립 텍스처 `Assets/ANIMOL/Sprites/UI/Pattern/Textures/CharacterFacesStars_All_2048.png`를 추가했다. GUID는 `c88ec790f7934404499f066e4fad4269`이며 7개 RawImage의 직렬화된 텍스처 참조로 사용된다. 패턴 셰이더 GUID는 `dc0dc0d8e45eabc41ba7e532cfce7375`다.
- 제공된 패턴 셰이더, `DiagonalPatternBackground`, `AnimolPatternMotion`, Editor 생성 메뉴를 활성 프로젝트의 폴더 규칙에 맞춰 설치했다. 기존 공용 `SpriteImportPolicy`는 이 패턴 폴더에만 적용되지 않게 하여 반복 텍스처가 `Default/Repeat`로 유지되게 했다.
- 활성 씬의 기존 하단 도크, 캠페인 빈 면, 카드 설명 4곳, 팝업 안쪽에 RawImage를 각각 하나씩 배치했다. 기존 면의 첫 자식으로 넣고 프레임 안쪽으로 들여, 기존 그림·문구·버튼이 위에 그려진다. `RawImage.raycastTarget=false`다. 풍경 창, 전면 캐릭터, 타이틀, 버튼, 광고 면에는 새 패턴을 얹지 않았다.
- 렌더 설정은 잉크 `#A66E48`, 불투명도 도크 `.18` / 캠페인 `.16` / 카드 `.12` / 팝업 `.10`, 투명 배경, 타일 `(960,960)`, 속도 `(8,8)` Canvas 단위/초, 비스케일 시간이다. 120초에 한 타일을 오른쪽 위로 이동한다.
- 기존 설정 토글은 `SetReducedMotion`을 통해 모든 패턴에 연결했고, 팝업 열기/닫기는 `SetModalOpen`에 연결했다. 앱 일시정지·초점 손실은 패턴 컴포넌트가 처리한다. 캠페인 열기만으로 패턴을 멈추지 않으며, 캠페인을 닫았다 다시 열어도 위상을 초기화하지 않는다. 기존 풍경 카메라와 전면 캐릭터 연출 및 대전 해금 판정은 변경하지 않았다.
- 기존 메인 씬과 관련 스크립트의 작업 전 상태를 `Docs/Backups/MainUI_PreV10_20261021.zip`에 저장했다. 현재 Unity 설치본이 v9가 아니므로 이 백업은 **활성 v8 기반 UI의 적용 직전 상태**다. v9 시안은 원본 패키지의 `preserved-v9`에서 볼 수 있다.

## 확인한 결과

- 패키지 원본과 복사한 얼굴 PNG 16장의 SHA-256이 각각 일치한다. 모든 얼굴은 비투명 픽셀의 RGB가 흰색이고 투명 외곽 여백이 남아 있다. 패키지 배치 JSON에서 얼굴 16개의 표시 긴 쪽은 352px, 별 16개의 가로 폭은 56px, 얼굴 512px 칸 간격과 홀수 줄 48px 이동을 확인했다.
- Unity 6000.3.8f1 배치 실행으로 씬 설치와 `Tools > ANIMOL > Pattern > Validate Installation` 메서드를 실행해 통과했다. 씬 참조 7개와 셰이더·임포트 설정을 확인했다.
- [EditMode 결과](Validation/MainUiPatternV10/editmode.xml): 2/2 통과. 개별 Sprite/반복 Texture 임포트, 동일 텍스처 참조, 배치·색·속도·투명도·터치 통과 설정을 검증했다.
- [PlayMode 결과](Validation/MainUiPatternV10/playmode.xml): 2/2 통과. 캠페인 진입 후 패턴 이동, 타일 경계 위상 계산, 화면 복귀 시 위상 유지, 팝업·모션 줄이기·앱 일시정지/초점 복귀, 풍경·인물 고정을 검증했다.
- [기존 대전 해금 회귀 결과](Validation/MainUiPatternV10/battle_gate.xml): 1/1 통과. 미확인·미완료 상태의 잠금, 필수 완료 후 네 테마, 상태 해제 시 재잠금을 확인했다. 테스트의 완료 상태는 임시 검증 프로브로만 주입했고 실제 진행 상태를 변경하지 않았다.
- Unity 배치 PlayMode의 Game View에서 [메인](Validation/MainUiPatternV10/main_1080x1920.png), [캠페인](Validation/MainUiPatternV10/campaign_1080x1920.png), [설정](Validation/MainUiPatternV10/settings_1080x1920.png), [긴 세로 화면](Validation/MainUiPatternV10/main_1080x2400.png)을 캡처했다. 화면에서 패턴 간격은 그룹 경계마다 벌어지지 않고, 기존 풍경·타이틀·버튼·광고 면을 가리지 않는다. 웹 미리보기 결과를 Unity 실행 증거로 사용하지 않았다.

## 남은 확인

- 실제 휴대기기의 노치 Safe Area, GPU별 반복 경계, 실제 터치 입력은 기기에서 확인하지 못했다. Unity에서는 1080×1920과 1080×2400 화면 및 `raycastTarget=false`를 확인했다.
- 기존 설정 설명 문구가 팝업 오른쪽 경계를 넘는 현상이 Game View에서 보인다. 이 패턴 적용으로 생긴 변경은 아니며 기존 텍스트 레이아웃을 이번 작업에서 변경하지 않았다.
