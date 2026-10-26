잔영 극장 연결 장식 — 설치 완성 R2 런타임

추가된 실행 경로
이전 패키지의 Controller.Configure/SetLayout은 호출자가 없는 공개 API였다.
R2의 TheaterConnectedDecorV01Binding.RefreshNow가 실제로 두 메서드를 호출한다.
AuthoredSnapshotSource 또는 PublishedSnapshotSource가 전체 보드 스냅샷을 공급하고,
Source.Changed 이벤트가 확정 변경/사용 가능 상태 변경을 전달한다. Update 폴링은 없다.
기존26종·118PNG·manifest/art pin·기존8개 C# 공개 API는 변경하지 않았다.

공개 타입 (namespace ANIMOL.Art.TheaterConnectedDecorV01)
TheaterConnectedDecorV01BoardSnapshot : 평평한 직렬화 DTO.
TheaterDecorAllowedCell : x,y,logicalLayer,heightKey 네 정수의 승인 셀 DTO.
TheaterConnectedDecorV01BoardAsset : 검증된 스냅샷 복사본을 보관하는 ScriptableObject.
TheaterConnectedDecorV01SnapshotSource : Changed 이벤트와 순수 TryCaptureSnapshot의 공통 계약.
TheaterConnectedDecorV01PublishedSnapshotSource : 실제 호스트가 확정한 상태를 PublishSnapshot으로 전달.
TheaterConnectedDecorV01AuthoredSnapshotSource : 명시적으로 지정된 BoardAsset을 공급.
TheaterConnectedDecorV01Binding : 실제 Controller 호출 및 이벤트/수명주기/실패 정리 담당.

스냅샷 필드
schemaVersion=1, packageId, sourceManifestSha256, boardId, revision(0이상 long),
isWholeBoard=true, isExampleOnly, originX/Y/Z, cellSize(양수), seed, postSpacing(3또는4),
networkCells[], groupPlacements[], allowedDecorCells[].
기존 네트워크/그룹 레코드와 좌표 계약을 그대로 사용한다. null 배열은 실패하고,
비어 있는 배열은 명시된 빈 보드로 인정한다. jagged 배열/Dictionary는 직렬화하지 않는다.
현재 원화 pin은8781ade4a02f32a7a528f76e49e2664f800fbf0dc2e5e0b07a40c967a651e069다.
TryValidate(out error)는 정확한 pin/ID/전체 보드/유한 좌표/고유 셀·그룹 및 모든 점유 칸 승인을
기존 순수 Planner로 확인한다. 승인 셀은 layer/height까지 정확히 일치해야 한다.
DeepCopy()는 배열뿐 아니라 모든 mutable row를 새로 만든다. CreateApprovedCellQuery()는
독립된 비공개 whitelist를 캡처하므로 원래 DTO를 바꿔도 승인 함수가 바뀌지 않는다.

정적 authoring 및 검토 예시
BoardAsset.TrySetSnapshot(snapshot,out error)는 먼저 복사/검증한 뒤 저장한다.
TryGetSnapshot(out snapshot,out error)는 검증된 새 복사본만 반환한다.
AuthoredSnapshotSource.AssignBoard(board,out error)로 연결한다. BoardAsset을 수정한 뒤에는
NotifyBoardChanged(out error)를 명시 호출한다. Inspector/Undo/Redo 연동은 Editor 도구가 담당한다.
예시 스냅샷은 isExampleOnly=true이며 실제 캠페인 상태가 아니다.
예시 Binding에서만 Bind(...,out error,allowExampleSource:true)를 명시한다.
기본 false인 생산 Binding은 예시 source/data를 거절한다.

실제 게임 호스트 경로
기존 맵/장식 편집·로드·undo·재시작이 권위 로직에서 확정된 후 다음 순서로 연결한다.
1. 전체 장식 배열과 실제 승인 영역을 현재 호스트에서 캡처한다. 카메라/보이는 청크만 쓰지 않는다.
2. boardId/revision 및 모든 위치/점유를 정확히 넣고 isExampleOnly=false로 지정한다.
3. PublishedSnapshotSource.PublishSnapshot(snapshot,out error)를 호출한다.
4. Binding은 Changed를 받고 새 복사본으로 Configure+SetLayout을 호출한다.
승인 영역은 호스트의 실제 장식 허용 규칙을 복사한 결과여야 한다. 예시 whitelist를 게임에
그대로 가져오거나 모든 칸을 true로 승인하지 않는다. 새 Collider/통행 장애물/턴/장치 ID는 없다.
PublishedSource는 누락/잘못된/예시 publish를 거절하며 이전 상태도 무효화하고 Changed를 보낸다.
OnDisable에도 권위 스냅샷을 무효화한다. 풀 재사용 시 호스트가 새 상태를 다시 publish해야 한다.
비활성화 이후 비활성 상태에서 새로 publish한 것은 최신 확정 상태로 보존할 수 있다.
AuthoredSource의 명시적 정적 BoardAsset은 풀 비활성화 후에도 동일한 승인 원본으로 남는다.

정확한 Binding API
bool Bind(TheaterConnectedDecorV01Controller target,
    TheaterConnectedDecorV01Catalog latest, string pinnedManifestSha256,
    string actualSortingLayerName, TheaterConnectedDecorV01SnapshotSource snapshotSource,
    out string error, bool allowExampleSource=false)
bool RefreshNow(out string error)
void ClearVisuals()
target Controller는 Binding과 같은 GameObject에 있어야 한다. 다른 개체의 장식을 지우지 않는다.
Getters: Source/Controller/Catalog/ExpectedManifestSha256/SortingLayerName/AllowExampleSource/
LastDiagnostic/LastReport. LastReport는 boardId/revision/네트워크·그룹·점유·실제 시각물 수와 오류를 담는다.

수명주기와 편집 모드
Binding 실행 순서100으로 기존 Controller의 OnEnable 이후에 적용한다. source 변경에는 구 이벤트를
해제하고 새 source를 구독한다. Binding 비활성/파괴 시 구독을 해제하고 자기 소유 시각물만 정리한다.
source 비활성/무효화/누락 시 이전 화면을 복구하지 않고 명확히 정리한다.
비활성 루트에서도 컴포넌트가 enabled면 AssignBoard+Bind+Refresh를 준비할 수 있다.
루트를 활성화한 뒤 Editor에서는 RefreshNow를 명시 호출한다. EditMode OnEnable에 의존하지 않는다.
처음 비활성 적용 후 활성 refresh는 숨겨진 Renderer를 다시 적용해 보여준다.
매번 source를 새로 캡처/검증한다. 내용·설정·plan·계층 행렬·활성 상태가 같을 때만 생성 재작업을
생략한다. 동일 refresh는 중복 소유 노드를 만들지 않는다. 부모 이동/배율 변경은 새로 적용하며,
회전/음수 XY/유한하지 않은 계층은 기존 시각물을 정리하고 실패한다.
양수인 비균일 XY 부모 배율은 기존 Controller의 축별 상쇄를 유지한다.
게임 상태가 달라지는 유효 입력만 호스트가 publish한다. 애니메이션 종료나 벽시계가 턴을 진행하지 않는다.

검증 및 경계
Tests~에 새 optional NUnit 테스트19개를 제공한다. 마지막5개는 실제 생성된 최신 카탈로그가 있어야
실행할 수 있다. 없는 경우 Ignore하도록 명시했으며, 제공 테스트는 모두 현재 NOT_RUN이다.
오프라인 소스/DTO 검수는 컴파일이 아니다. 원 프로젝트가 없어 실제 Unity import/컴파일/TestRunner/
생성 Prefab/씬 플레이/캠페인·대전 호스트 연결/레거시 참조0/모바일 검증은 모두 NOT_RUN이다.
실제 프로젝트의 PublishedSnapshotSource 호출 지점과 저장/편집 연결은 동봉 CLI에 따라 검증해야 한다.
