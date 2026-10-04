# MissingUiV1 프로젝트 연결

이 폴더의 `Runtime`/`Editor`/`Sprites`/JSON은 제공 패키지에서 가져온 공용 소스다.
`Project`는 현재 ANIMOL 프로젝트용 연결이며 패키지 재수입으로 덮어쓰면 안 된다.

- 전체 생성: **ANIMOL > Missing UI V1 > Build Project Task 1 Store and Modals**.
- 기존 pixelroborobo/TMP 프로필과 ProductionV1 공용 프레임·버튼 GUID를 재사용한다.
- 독립 시안: `Generated/ANIMOL_MissingUiV1_ReadOnlyPreview.unity`, 23개 화면. 운영 서비스가 아니다.
- 운영 상점: `Project/Resources/ANIMOLMissingUiV1/Store.prefab` 및 `Art.asset`.
- `MissingUiProjectEntry`가 기존 UiNavigationService의 SC11 자식에 운영 프리팹을 설치한다.
  기존 Phase1~3과 같은 sceneLoaded 연결 방식으로, 레거시 생성물을 수동으로 덮어쓰지 않는다.
- 기존 SC11 자식은 삭제하지 않고 비활성으로 보존한다. 활성 뒤로 버튼은 기존 계약 이름
  `StoreBackButton`을 유지한다. 찾을 때는 활성 화면 또는 `MissingStoreView.Back`을 사용한다.
- ProductDetail/ServiceError/RewardedAd/ConfirmExit/MultiplayerPause의 버튼과 Text 인스턴스를
  유지하면서 런타임에 스크롤 본문·고정 버튼 판면을 적용한다. 기존 리스너를 교체하지 않는다.
- 현재 실제 상품 공급자·구매·복원 API는 미연결이다. 가격/코인/보유는 `--`, 실행 버튼은 비활성.
  `AuthenticatedOfferSnapshot`은 값 계약이며 실제 결제 공급자나 요청/receipt 관리자가 아니다.
- 처리 중/거절/Unknown·ActionId 보존은 실제 거래 서비스와 중앙 요청 관리자가 제공되어야
  연결할 수 있다. 로컬 가짜 구매나 성공 응답을 추가하지 않는다.
- 현재 소스의 컴파일 호환 수정: TextureImporterSettings, Image.Type 정규화,
  임시 씬을 닫은 뒤 데모 씬 생성, 전역 SaveAssets 제거, 밝은 카드의 텍스트 대비 수정.

상세 증거와 미검증 범위는 저장소 루트 `ANIMOL_MISSING_UI_V1_TASK1_RESULT.md` 참조.
