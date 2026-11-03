# William Notebook UI 적용 결과 (2026-11-03)

## 활성 경로

- `MCP_INBOX/William_Notebook_UI.zip`의 `Assets/WilliamNotebookUI`, 원본 패턴 파일, `SourceArt/OriginalScrolling`을 설치했다. `Docs/ART_PRESERVATION.json`의 SHA-256 71개가 설치 전후 모두 일치한다.
- `NotebookInstaller.BuildFreshScene`을 110개 노드 카탈로그로 다시 실행했다. 생성물은 `Assets/WilliamNotebookUI/Generated/NotebookCanvas.prefab`, `NotebookUI.unity`, `Galmuri14.asset`이다. 씬의 Canvas는 생성 프리팹 인스턴스다.
- Build Settings의 첫 씬은 `NotebookUI.unity`다. 구형 `ANIMOL_Main.unity`는 빌드 목록에서 제외했다. 극장 `Lobby` 버튼과 Android 로비 검증 빌드의 씬 경로도 Notebook 씬으로 변경했다. 구형 메인 컨트롤러는 활성 Notebook 씬에 존재하지 않는다. 퍼즐 HUD·일시정지·결과 화면은 변경하지 않았다.

## 시각 및 데이터 바인딩

- 런타임 스크롤은 기존 `AnimolPatternMotion`과 `DiagonalPatternBackground` 타입을 사용한다. 패키지의 원본 파일은 변경 없이 보관하고, 동일 타입의 중복 컴파일만 asmdef 제약으로 막았다. 잉크는 `#A66E48`, 불투명도 `0.16`, 속도 `(8,8)`로 생성했다.
- Unity TextureImporter에서 새 PNG 335개의 Sprite/Single/FullRect/PPU100/무압축/Mipmap Off와 플랫폼 압축 오버라이드 없음, 36px 프레임 테두리를 확인했다. 프리팹의 상태 PNG 버튼 15개는 SpriteSwap이며 참조가 모두 있다. Sprite가 있는 Image는 모두 White Tint다.
- 지도에는 `Map/Region_<theme>.png` 4종과 `NotebookMapBackdropLayout`이 연결된다. 110개 별(20/30/30/30), 130개 PNG 연결선의 참조를 확인했다. 같은 지역의 노드 최소 중심 거리는 220px 이상이다. 완성 연결선은 두 끝의 실제 클리어 ID가 모두 기록될 때만 선택한다.
- 목장 슬롯은 실제 맵 ID `SW01`~`SW20`, 극장 슬롯은 `ET-01`~`ET-30`으로 연결했다. 극장 30개만 기존 `ANIMOL_Echo_Theater` 씬과 매칭된다. 목장에는 슬롯별 로더가 없고, 낙일 성소·별너울 해안에는 실제 퍼즐 맵 바인딩이 없어 총 80개 슬롯의 `sceneName`을 비워 두었다. 이 슬롯은 입장 가능으로 표시하지 않는다.
- `NotebookProjectGateway`는 기존 `EchoTheaterCampaignController.IsCleared`의 ET-01~30 클리어 기록을 읽는다. Product ID·현지화 가격·결제/복원 SDK·광고 검증·입장 승인 원장·확정된 대전 필수 목록은 저장소에 없고, 사용자도 자료 없음으로 확인했다. 따라서 유료 지역, 광고 보상, 입장, 대전, 로그인, 커스텀 호출은 성공 처리하지 않는다. 여섯 개 임시 필수 ID는 제거해 대전은 잠금 상태다. 망원경은 기존 원장이 없어 Notebook 로컬 모델이 소유한다.
- 설정의 전체 음소거와 원본 배경 스크롤은 적용된다. BGM/SFX 개별 믹서와 진동 서비스는 활성 프로젝트에서 찾지 못해 설정 값 보존까지만 연결된다.

## Unity 검증

- Unity 6000.3.8f1 Import 및 C# 컴파일 통과. 활성 Notebook 씬에서 프리팹 인스턴스, 노드 110개, 연결선 130개, Gateway 참조, 구형 `MainLobbyController` 0개를 확인했다.
- Play Mode의 실제 Game View를 360×640, 390×844, 445×773, 1280×720으로 설정해 메인/지도/설정을 확인했다. [목장 메인 445×773](NotebookUI_QA/Notebook_Main_Starway_445x773.png)은 패키지 `Preview/Main_445x773.png`와 같은 목장 원화·기본 상태로 비교했고 배치와 손발 노출이 일치한다. [360×640 메인](NotebookUI_QA/Notebook_Main_Actual360x640.png), [1280×720 메인](NotebookUI_QA/Notebook_Main_Actual1280x720.png)에서도 버튼과 캐릭터가 잘리지 않는다. [390×844 지도](NotebookUI_QA/Notebook_Map_Actual390x844.png), [설정](NotebookUI_QA/Notebook_Settings_Actual390x844.png), [미연결 구매 팝업](NotebookUI_QA/Notebook_Purchase_Unconnected_360x640.png)은 실제 Game View 캡처다. 지도 Preview는 클리어 3/20인 예시여서 현재 실제 0/20 지도와 같은 진행 상태로 취급하지 않았다.
- Unity에서 별도 모델 16개 조건을 실행해 망원경 0/8/10개, 180초 충전, 광고 취소/승인/중복, 입장 거절/승인/중복, 순차 잠금과 필수 목록 미확정 잠금을 확인했다. 이는 모델 검증이며 미연결 SDK의 실제 성공 테스트가 아니다. Play Mode에서 구매 복원을 눌러도 극장 잠금이 유지됐다. 설정 BGM 값 0.42를 저장하고 Play Mode 재진입 후 0.42를 확인한 뒤 0.70으로 복원했다.
- `ArtQaPlayTests`: 1/1 통과. `ArtAssetTests`: 3/4 통과; `ManifestImportsAndExactClipDurations`는 이번 UI에서 변경하지 않은 `StarSwellCoastV01Device`의 스프라이트 누락으로 실패했다.
- D3D12에서 지도 전환 중 Unity가 두 번 그래픽 장치 오류로 종료됐다. Editor를 `-force-d3d11`로 다시 열어 지도와 설정의 실제 Play Mode 캡처를 완료했다. 기본 D3D12 재실행의 안정성은 미확인이다. 복구 대화상자가 보존한 `Assets/_Recovery`는 이번 변경에 포함하지 않는다.

## 남은 연결

실제 목장 스테이지 로더, 낙일 성소·별너울 해안 퍼즐 데이터, 지역 Product ID와 가격을 제공하는 스토어, 구매/복원 검증, 광고 승인, 입장 승인, 확정된 대전 필수 목록, 로그인/커스텀/오디오·진동 서비스를 연결해야 해당 기능을 활성화할 수 있다. 현재 미연결 기능은 잠금 또는 준비 상태로 표시한다.
