# 이전 프로젝트 전체 잔여분 격리

기준일 2026-10-13. 기준 커밋 d82ce954. Assets에서 기존 UI/로비/서비스/맵 제작/데이터/아트/테스트를 전부 격리하고 현재 V04 아트 QA만 유지한다. 이전 1차 보관본 Legacy/Platformer_20261013은 그대로 둔다.

원본 상대 경로와 .meta는 이 폴더 아래 그대로다. 공용 서비스도 현재 게임 로직으로 재사용하지 않는다. 원래 폰트 중 현재 필요한 8개는 활성 V04/Fonts로 AssetDatabase.MoveAsset 이동했고, 이전 경로의 원본 사본은 SharedDependenciesSnapshot에 있다. Unity 설정의 변경 전 사본은 ProjectSettings 아래에 보존한다.

이동 목록: ../../Docs/Validation/ActiveSlice/archive_inventory.json 및 moved_roots.txt, font_moves.tsv. 모든 이동 전 실제 바이트를 SHA-256으로 검증한다. 기존 미커밋 파일은 같은 내용을 보관 경로에 유지하며 이번 커밋에 혼합하지 않는다. 추적 파일의 이동 커밋은 기준 HEAD의 blob을 새 경로에 기록한다. 따라서 이전 사용자 수정은 새 Legacy 경로 또는 활성 폰트 경로의 미커밋 수정으로 남는다. 해시 목록은 실제 작업 트리의 보존 내용을 기록하므로 커밋 기준 바이트와 다른 항목이 있을 수 있다. 이전부터 추적되지 않은 파일은 보관 후에도 추적되지 않은 상태를 유지한다.

복원할 때 이 폴더를 통째로 Assets에 복사하지 않는다. 복원 목적의 별도 작업 디렉터리/브랜치를 만들고 필요한 상대 경로와 .meta를 쌍으로 되돌린다. SharedDependenciesSnapshot의 폰트를 복원하기 전 동일 GUID의 활성 V04/Fonts 파일을 Assets 밖으로 옮겨 중복 GUID를 방지한다. 원래 EditorBuildSettings/GraphicsSettings/QualitySettings와 ANIMOL 캐시는 필요할 때만 복원하고 GUID·컴파일·메뉴·씬 참조를 재검증한다. 기존 두 Legacy 폴더에 동명 경로가 있으면 실제 보존 해시와 각 기준 커밋을 비교해 자동 덮어쓰기하지 않는다.
