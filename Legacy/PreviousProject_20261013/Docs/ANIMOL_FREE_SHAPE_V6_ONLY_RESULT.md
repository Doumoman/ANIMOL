# 자유형 타일 V6 고정 결과

사용자의 후속 지시에 따라 이전 버전 보존/선택 정책을 폐기했다. 유일한 설치 아트는 V6다. 계약과 schemaVersion=1, 20 styleId, 위상/마스크/물리 규칙은 유지한다.

- V1/V2/V4 Registry와 runtime PNG/재료/lookup, 구버전 설치기/전용 비교 테스트, V1/V2/V4 소스 패키지 및 ZIP을 삭제했다. V3/V5 Registry는 설치되어 있지 않았다.
- V6 설치기를 독립시켰고 기본 레이어/Registry/새 캠페인 맵은 V6다. 버전 드롭다운 및 전환 API를 제거했다. V1~V5/알 수 없는 버전의 로드/Commit과 구버전 백업 복원을 거부한다.
- 실제 맵 31개 중 28개를 V6로 전환하고 revision을 한 번 증가시켰다. 이미 V6인 3개는 버전/리비전을 유지했다. 비어 있는 구형 레이어는 메타데이터만 정리했다. 전환 전 live Editor 데이터를 별도 스냅샷으로 비교하여 셀, styleId, seed, 객체, stamps, 셀 크기 불변을 확인했다. 비교에서 변경된 필드는 freeShapeTerrain.artVersion와 authoringRevision뿐이었다.
- 사용자 편집 중인 내용은 유지했다. 기존 미커밋 맵은 이번 버전/리비전 변경분만 Git에 반영했으며, 기존 untracked 캠페인 맵은 V6로 저장하되 이번 커밋에 임의로 포함하지 않았다.
- V6 패키지에 포함된 T01_A 보존 기준과 제작 이력은 현재 V6 재현/검수에 필요하므로 패키지 내부 그대로 유지한다. 과거 보고서는 이력이며 사용 가능한 타일 버전이 아니다.

검증: Unity 컴파일 성공, FreeShapeTerrainTests 21/21 PASS (구버전 6종 거부, 3,760 Sprite 참조, Undo/Redo, 저장실패 롤백, mask/위상, 물리/객체 보존 포함). 추가 청크 제거 요청에 따른 최종 PlayMode 및 통합 검증은 셀 범위 전환 보고서에서 기록한다. 상세 전환 목록: Docs/Validation/FreeShapeV6Only/Migration.json.
