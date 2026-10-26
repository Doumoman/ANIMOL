잔영 극장 연결 장식 — Editor 배치 연결 보완 R2

추가 파일
Editor/TheaterConnectedDecorV01BoardJson.cs: 평평한 보드 JSON의 필수/알 수 없는 키,
중복 키, 실제 정수 범위, 유한 float, 배열 타입을 JsonUtility 전에 검사한다.
Editor/TheaterConnectedDecorV01BoardTools.cs: 두 JSON과 소스 pin, 점유, all26 목록을
검증하고 실제 Unity API로 BoardAsset/Prefab 및 선택한 경우 검사 씬을 만든다.
Editor/TheaterConnectedDecorV01BoardWindow.cs: 생성 창과 Binding Inspector의
명시적 갱신/비우기, 선택 Inspector 범위 Undo/Redo 갱신 옵션을 제공한다.
Tests~/TheaterConnectedDecorV01BoardJsonTests.cs: 선택 Editor NUnit 검증 소스 12건.

실행
Tools/ANIMOL/Theater Connected Decor V01/Build example boards…
에서 현재 manifest, Examples/stage_snapshot.json, Examples/all26_snapshot.json,
실제 기존 Sorting Layer를 선택한다. 검사 씬 생성은 별도 체크한다.

명령행 진입점
ANIMOL.Art.TheaterConnectedDecorV01.TheaterConnectedDecorV01BoardTools.BuildExamplesFromCommandLine
필수: -theaterDecorManifest / -theaterDecorStageSnapshot / -theaterDecorAllSnapshot /
-theaterDecorSortingLayer
선택: -theaterDecorCreateScenes true (기본 false)

실제 출력 위치
<package>/_Generated/InspectionBoards/Stage/TheaterDecorStageBoard.asset
<package>/_Generated/InspectionBoards/Stage/TheaterDecorStage.prefab
<package>/_Generated/InspectionBoards/Stage/TheaterDecorStage.unity (선택)
<package>/_Generated/InspectionBoards/All26/TheaterDecorAll26Board.asset
<package>/_Generated/InspectionBoards/All26/TheaterDecorAll26.prefab
<package>/_Generated/InspectionBoards/All26/TheaterDecorAll26.unity (선택)
<package>/_Generated/InspectionBoards/inspection_build_report.json

생성기는 비활성 root에 Controller/Source/Binding을 추가해 AssignBoard/Bind하고,
root 활성화 후 RefreshNow를 명시적으로 호출한다. CurrentPlan/개수뿐 아니라
각 SpriteRenderer의 enabled/activeInHierarchy/current Sprite도 확인한 뒤 저장한다.
이 검사는 실제 호스트에서 생성기를 실행했을 때 수행된다. 이 환경에서는 미실행이다.

범위와 소유
검사 출력만 자체 라벨과 실제 package-root GUID로 식별한다. 빌더는 열린 Prefab Stage가
있으면 저장하고 나올 때까지 실행을 거절한다. 열린 출력 검사 씬이나 아직 저장하지 않은
생성 자산도 덮지 않고 먼저 거절한다. 직접 Refresh는 열린 Prefab Stage에서 사용할 수 있다.
새 임시 Additive 씬만 사용하고 finally에서 원래 활성 씬을 복원한 뒤 새 씬만 닫는다.
기존 씬을 저장/닫거나 게임의 레이어/입력/충돌/카메라/조명/프로젝트 설정을 변경하지 않는다.
새 검사 씬의 카메라는 Untagged다. URP Light2D가 실제로 있으면 이 새 씬에만 선택 조명을
만들며, 실제 공개 프로퍼티와 직렬화 대상 레이어를 확인한다. 지원 형식이 다르면 보고서와
진단으로 실제 호스트 조명 확인을 요구한다. 셰이더·파이프라인·GPU 검증 통과를 의미하지 않는다.
검사 씬은 따로 열어 Play해야 하며 실제 캠페인으로 이관된 씬이 아니다.

Inspector의 Undo 옵션은 기본 꺼짐이다. 켜도 해당 Inspector의 바인딩/보드/카탈로그/
유효 상태/부모 변환 지문이 바뀌었을 때만 그 바인딩을 갱신한다. 다른 씬을 스캔하지 않는다.
임의의 persistent Prefab 자산을 직접 갱신하지 않고, 열린 Prefab Stage 또는 씬 인스턴스를
선택해야 한다. 장식 시각물 캐시를 실제 맵 모델 대신 수정하지 않는다.

검증 상태
소스/형식 검수만 수행했다. C# 컴파일, Unity import, Editor 실행, 생성 .asset/.prefab/.unity,
NUnit 12건, 실제 Play와 원 프로젝트 바인딩은 모두 NOT_RUN이다.
Tests~는 Unity가 바로 컴파일하는 폴더가 아니다. 실제 Editor Test Assembly로 옮겨
Unity TestRunner에서 실행해야 한다. 기존 아트 118 PNG와 원래 8개 C#는 이 작업에서 수정하지 않았다.
소스 검수 스크립트는 --sources <package-root>를 지정해 합쳐진 패키지의 새 소스만 검사할 수 있다.

확인한 Unity 기본 API 문서 (구현 근거, 실행 증거 아님)
https://docs.unity3d.com/6000.0/Documentation/ScriptReference/SceneManagement.EditorSceneManager.NewScene.html
https://docs.unity3d.com/6000.0/Documentation/ScriptReference/SceneManagement.EditorSceneManager.SaveScene.html
https://docs.unity3d.com/6000.0/Documentation/ScriptReference/PrefabUtility.SaveAsPrefabAsset.html
https://docs.unity3d.com/6000.0/Documentation/ScriptReference/SceneManagement.PrefabStageUtility.GetCurrentPrefabStage.html
https://docs.unity3d.com/6000.0/Documentation/ScriptReference/SceneManagement.PrefabStage-assetPath.html
https://docs.unity3d.com/Packages/com.unity.render-pipelines.universal@17.0/api/UnityEngine.Rendering.Universal.Light2D.html
https://docs.unity3d.com/Packages/com.unity.render-pipelines.universal@17.0/api/UnityEngine.Rendering.Universal.Light2D.LightType.html
