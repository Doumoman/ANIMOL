# V6 전용 · 셀 단위 맵 편집 결과

후속 요청에 따라 16×16 청크 편집/저장/가시성 기능을 제거했다. 맵의 시작 X/Y와 너비/높이는 이제 셀 단위이며, 16의 배수 제한이 없다.

## 편집과 데이터

- Scene 편집기의 맵 설정에서 시작 X/Y, 너비/높이를 입력한 뒤 `맵 범위 적용`을 누른다. 새 캠페인 맵의 기본 범위는 32×24셀이다.
- 16셀 구분선과 청크 추가/삭제 UI를 제거했다. 보조 시험 화면의 범위 확장은 1셀 단위다. 맵 에디터 제목의 V4 표기도 제거했다.
- `StageMapDefinition`의 minChunkX/minChunkY/chunkWidth/chunkHeight를 minCellX/minCellY/cellWidth/cellHeight로 교체했다. CellToChunk/청크 점유/청크 포함 판정/API를 삭제했다.
- 기존 맵 31개는 실제 경계가 같도록 원래 값×16으로 명시적으로 전환했다. 원본 셀·객체·스탬프·seed·styleId·V6 버전·셀 크기 불변을 스냅샷으로 비교했다. 빈 미설정 맵의 경계/셀 크기를 임의로 확정하지 않았다.
- Resize는 기존 데이터가 잘리는 축소를 거부한다. 음수 좌표, 양수 크기/정수 overflow 검사, 저장과 Undo/Redo, 미리보기 실패 롤백을 검증한다. 아트/경계 계약 이전 백업은 복원 전에 거부한다.
- 경계는 content hash에 반영되고 변경 시 revision/검수 상태를 기존 규칙으로 갱신한다. Ready/보상/스테이지 배정 검증을 우회하지 않았다.

## 렌더링과 물리

- 자유형 아트를 맵 전체의 단일 Tilemap으로 그린다. 커스텀 청크 GameObject와 청크별 가시성/소유권/커버리지 인덱스를 제거했다.
- 이웃 mask·phase·문양 지지는 전역 점유에서 계속 계산한다. 변경 주변 3×3셀과 의존 문양만 갱신하는 증분 캐시는 유지한다.
- 16셀 청크를 강제하던 terrain catalog 검사/필드를 제거했다. 기존 별도 terrainPlacements schema=3 데이터와 물리 소유자는 유지했다.
- 아트에는 Collider를 추가하지 않았다. 경계의 4개 물리 벽과 기존 지형/one-way 충돌은 셀 경계를 사용한다.

## 검사와 보존

`Docs/Validation/CellBounds`에 이번 실행 결과를 기록한다. 초기 셀 범위 전용 EditMode 5/5 PASS. V6 고정 단계에서는 FreeShapeTerrainTests 21/21, CommercialPolishPolicyTests 3/3 PASS. 최종 고유 테스트 **76/76 PASS** (EditMode 71, PlayMode 5). 편집/렌더 회귀 55개, 기존 지형 통합 13개, 정책 3개, PlayMode 5개다. 마지막 충돌 캐시 무효화 보완 뒤 영향 범위인 셀 경계 EditMode 5개·PlayMode 1개·정책 3개를 재실행했다.

단일 Tilemap 전환 후 Scene/Runtime 960개, 64,348,160픽셀의 RGB/alpha 차이 0. 앞선 V6 검사 결과를 대신 사용하지 않고 이번 수정 뒤 실제 Unity에서 다시 렌더했다.

초기 경계 PlayMode 테스트는 빈 맵의 충돌 revision 불일치로 실패했다. 경계 변경 API가 충돌 캐시를 자동 무효화하도록 보완한 최종 구현에서 별도 테스트용 동기화 우회 없이 재실행하여 PASS. 최초 실패 결과는 PlayMode.json에 보존했다. 최종 consoleErrors=0, compilationFailed=false.

`FinalMaps.json`: 전환 전 유효했던 충돌 표식은 동일한 실제 경계/점유에서 새 revision에도 유효하게 유지했다. 6개 미설정 캠페인 슬롯의 셀 크기/빈 경계 오류는 기존 미설정 상태로 남기고 Ready를 승인하지 않았다. T01-S01은 구조 검증 PASS이며 V6/셀 범위 설정을 열어두었다.

`Migration.json`: 31개 맵의 실제 경계와 원본 콘텐츠 일치. `AssetReferenceAudit.json`: V6 에셋/meta 219개 불변, 삭제한 구버전 GUID 335개 검사, 남은 Unity 에셋의 참조 0.

셀 경계 일괄 저장 중 CLI 응답이 5초 제한을 넘겼지만 Unity 작업은 계속 완료됐다. 완료 결과 파일과 31개 맵 전수 비교를 확인했고 마이그레이션을 재실행하지 않았다. 대기 중 테스트 요청은 CLI 30초 제한으로 실행되지 않아 상태를 확인한 뒤 다시 요청했다.

## Git 작업 경계

기존 미커밋 사용자 변경을 유지했다. 기존 수정 맵은 이번 메타데이터 전환분만 커밋한다. 기존 미추적 캠페인 맵/이전 개발 도구 호출부도 로컬에서 새 계약으로 맞췄으나 원래의 전체 미추적 파일을 임의로 커밋하지 않았다. 해당 경로와 전후 SHA256은 `LocalUntrackedChanges.json`에 기록한다. 새 CellBounds 테스트 2개 파일은 이번 작업 산출물로 포함한다.
