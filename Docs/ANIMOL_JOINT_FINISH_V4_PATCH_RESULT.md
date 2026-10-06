# ANIMOL joint_finish_v4 적용 상태 — 패키지 손상으로 미적용

2026-10-06. 사용자의 지시에 따라 미커밋 v3 적용 작업을 폐기했다. v4 적용 명세는 읽었으나, 현재 첨부 ZIP이 불완전하여 v4 임포트와 레지스트리 추가는 수행하지 않았다.

## v3 작업 폐기와 기존 데이터 보존

- 이번 v3 작업이 변경했던, 작업 시작 시 clean 상태였던 코드/설정 8개 파일만 기존 HEAD로 복원했다. 테스트가 갱신한 v2 비교 결과도 원상 복원했다.
- Unity AssetDatabase로 v3 아트 폴더, v3 Resources 레지스트리, v3 Builder와 해당 meta를 제거했다. v3 제작 자료/보고서/검수 산출물은 운영 작업 트리에서 제거했다. 첨부 원본 ZIP은 보존했다. 폐기 작업의 복구용 사본은 Git 제외 경로 `Library/DiscardedBrickRestoreV3`에만 둔다.
- 기존 사용자 변경을 일괄 restore/reset/clean하지 않았다. 기존 맵을 v3로 전환하지 않았다.
- 기존 맵 22개는 전체 Editor JSON, content hash, revision이 작업 시작 상태와 동일하다.
- v1/v2 Sprite GlobalObjectId 7,560개가 동일하다. 기존 맵/아트/meta 등 830개 파일 SHA256이 모두 동일하다.
- 복원 후 실제 연결된 Unity 6000.3.8f1에서 컴파일 완료를 확인했다. `compilationFailed=false`, `compiling=false`, 현재 Console error 0이다. 기존 경고 8개는 남아 있다.

[파일 보존](Validation/JointFinishV4/DiscardFilePreservation.json), [맵·참조 보존](Validation/JointFinishV4/DiscardUnityPreservation.json).

## v4 ZIP 확인 결과

- 파일: `ANIMOL_FreeShape_Sprites_joint_finish_v4.zip`
- 크기: 13,526,806 bytes
- SHA256: `cf9bc3e098390df412f62b32604a36b3196150c412218aa21bf4631eb93cfcad`
- 표준 ZIP 열기: `BadZipFile: File is not a zip file`.
- 중앙 디렉터리 레코드가 없다. 로컬 헤더를 읽어 확인한 마지막 파일은 `SourceArt/Approved/T05.png`이며, 이 파일의 압축 데이터/CRC 검증도 실패한다.
- 정상적으로 읽을 수 있는 `Docs/ANIMOL_JOINT_FINISH_V4_APPLY.txt` 항목은 CRC 검증 후 읽었다. 명암, radius1 모서리, 등록된 곡선의 2×2 ink 허용, 등록되지 않은 접합 오류 0이라는 기준을 확인했다.
- 필수 `Tools/validate_joint_finish_v4.mjs`, `Tools/build_joint_native_sources_v4.mjs`는 현재 읽을 수 있는 파일 목록에 없다. 불완전한 내용으로 운영 아트를 설치하거나 다른 버전으로 대체하지 않았다.

[ZIP 진단](Validation/JointFinishV4/ZipIntegrity.json).

## 미실행

완전한 ZIP이 필요하므로 다음 v4 항목은 모두 **미실행**이다.

- 전체 패키지 압축 해제 및 manifest 파일 목록·크기·SHA256 검증.
- v4 접합/연결/Sprite/gallery Node 검사, 나머지 19스타일과 v3의 전체 비교.
- v4 운영 PNG 임포트와 별도 레지스트리 등록.
- 선택 개발 맵의 v4 전환, Undo/Redo, 저장/재열기, 실패 롤백 검사.
- v4 Scene/Runtime 1× 및 최근접 8× 렌더 비교, 접합·명암·둥근 모서리 검수.
- v4 관련 EditMode/PlayMode, 충돌/착지/one-way/Local Play 및 모바일 검사.

이 문서는 v4 적용 완료 보고가 아니다. 정상 ZIP을 다시 받은 뒤 전체 무결성 검사부터 재개해야 한다. 폐기한 v3의 테스트 결과를 v4 PASS로 사용하지 않았다.
