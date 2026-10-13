# 스프라이트 정본 정리 결과

활성 프로젝트에서 버전 폴더를 제거하고 PNG 177개와 AnimationClip 15개를 `Assets/ANIMOL/Sprites`로 정리했다. 파일명은 spr_/anim_ 접두사와 소문자 snake_case이며 프레임은 f00부터다. 프리팹·씬·코드·폰트·머티리얼·재생 데이터도 각각 Prefabs/Scenes/Scripts/Fonts/Materials/Data로 분리했다.

AssetDatabase.MoveAsset으로 이동했고 PNG/폰트 바이트, .meta GUID를 유지했다. 인게임 UI·토끼·바닥·벽·장식·배경을 실제 사용에 맞게 분류했으며 구현되지 않은 M## 장치 ID는 부여하지 않았다. 클립은 manifest.clipPath와 새 PNG 경로를 사용한다. SourcePath/source_file은 원본 ZIP의 출처 정보다.

어셈블리/클래스의 버전 식별자를 일반 Presentation 이름으로 바꿨으며 직렬화 호환을 위해 MovedFrom을 선언했다. 모든 프리팹·씬을 새 코드로 다시 연결하고 저장했다. GUID 변경·중복·누락 참조·잘못된 PNG/클립명·활성 버전 경로는 각각 0이다. 실제 씬의 누락 스크립트/Sprite도 0이며 컴파일 오류는 없다.

현재 EditMode 4/4, PlayMode 1/1 통과다. 마지막 시작 씬은 `Assets/ANIMOL/Scenes/ANIMOL_Starway_Art_QA.unity`다. QA이며 실제 퍼즐 구현은 아직 없다. 새로운 sprite/importer 기본값은 미등록 32px 그림의 PPU 32이며 이번 원본은 manifest의 기존 80/128 PPU와 프레임 시간을 유지한다.

증거: [이동 목록](Validation/SpriteLayout/move_manifest.json), [감사](Validation/SpriteLayout/audit.json), [씬 검사](Validation/SpriteLayout/scene_check.json), [EditMode](Validation/SpriteLayout/editmode.json), [PlayMode](Validation/SpriteLayout/playmode.json), [1080×1920 캡처](../Screenshots/ArtQA/qa_clean_project.png).

이전 적용 보고서와 옛 검수 도구는 레거시에 보존했다. 원본 패키지 이름 및 SourcePath, MovedFrom의 예전 식별자는 이력이며 활성 파일/폴더의 버전 이름이 아니다. 기존 사용자 폰트 수정과 이전 레거시 미커밋 작업은 이번 커밋에 섞지 않고 새 경로의 미커밋 수정으로 유지한다.

단일 수풀 연결부의 작은 원본 알파 틈과 실제 퍼즐 로직 미연결은 이전과 동일한 미확정 항목이다. 이번 정리가 그림 수정·32px 축소·게임 구현 완료를 뜻하지 않는다. 모바일 실기기 검사는 이번 파일 정리에서는 미실행이다.
