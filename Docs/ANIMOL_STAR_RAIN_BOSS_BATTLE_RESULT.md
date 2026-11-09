# 별비 보스전 독립 모듈 적용 결과

2026-11-09, 실제 ANIMOL 프로젝트의 Unity **6000.3.8f1 / Windows Direct3D12**에서 임포트, 컴파일, 독립 씬 생성과 자동 Play Mode 검사를 실행했다.

## 실행할 씬

- 씬: `Assets/ANIMOL/StarRainBattle/Scenes/StarRainBossBattle.unity`
- 생성 메뉴: `ANIMOL/Star Rain Battle/Create Standalone Preview Scene`
- 실제 연결 객체: `StarRainCanvas/Battle Safe Area/Battle Design 1080x1920/BattleBoard`의 `StarRainBattleController`
- 같은 디자인 루트 아래에 두 HP HUD, 차단/오차단 표시, 일시정지·증강 버튼, 준비/재개/승패 오버레이가 있다.
- 별 5개, 발 머리/꼬리 24쌍, FX 24개가 씬에 직렬화되어 있다. 별도 프리팹은 생성하지 않았다.

Editor는 Play Mode를 종료하고 저장된 이 씬을 연 상태로 남겼다. 씬을 열고 Play를 누르면 준비 화면에서 시작할 수 있다. 이미 같은 경로에 씬이 있으므로 생성 메뉴를 다시 실행하면 덮어쓰지 않고 중단한다.

## 임포트와 보정

원본은 `MCP_INBOX/ANIMOL_StarRain_Battle_v1.zip`이다. ZIP의 `Assets`에 있는 모듈 파일 183개만 가져왔고, `Docs`, `Preview` 등은 Unity Assets로 복사하지 않았다. 기존 `Assets/ANIMOL/BattleCrayonUI`의 프레임·아이콘·이솔 초상·배경·셰이더를 참조한다.

제공 GUID **95개**와 PNG **48장**의 원본 바이트는 유지했다. 원본 PNG 메타의 `spriteMeshType: 1`은 이 Unity 버전에서 Tight이므로, 요구한 Full Rect가 되도록 실제 TextureImporter에서 48장을 교정했다. 모두 Sprite/Single, FullRect, PPU100, Bilinear, Clamp, Mipmap Off, Uncompressed이며 연결선의 border는 사방 24px다. SpriteAtlas를 만들지 않았다. 제공 머티리얼의 A/B/C 연결도 유지했다.

기존 Notebook용 Galmuri14 TMP 자산 경로가 없어 동봉 TTF로 `Fonts/Galmuri14_Battle.asset`과 내부 아틀라스·머티리얼을 만들었다. 숫자 0~9와 네 상태 문구를 Editor에서 미리 준비했고, 현재 글자 테이블은 TMP 보조 글자를 포함해 30자다. 기존 폰트·전역 TMP 기본값은 바꾸지 않았다. 글꼴 원본과 OFL 라이선스도 함께 보존했다.

통합 과정에서 다음을 보정했다.

- 빌더의 전역 `AssetDatabase.SaveAssets()`를 전투용 폰트만 저장하는 `SaveAssetIfDirty`로 바꿨다. 다른 작업 중인 자산을 함께 저장하지 않는다.
- `StarRainBattleSafeArea`로 배경을 제외한 전투판·HUD·팝업 전체를 안전 영역에 등비로 맞췄다. 논리 좌표 400×620, 별머리 판정 반경과 입력 역변환은 유지한다.
- HP 숫자를 밝은 막대 위에서 옆의 어두운 공간으로 옮겨 가독성을 확보했다.
- 원본 모델 테스트에서 드러난 연결선 수명 경계 오류를 수정했다. 60Hz 고정 시뮬레이션 틱 사이에 입력해도 새 연결선은 입력 시점부터 0.5초를 유지한다. 틱 누산기의 부동소수점 오차도 보정하고 회귀 사례 3개를 추가했다.
- `BlocksInput`은 false이며, 전투 일시정지는 모델의 시계만 멈춘다. 전역 `Time.timeScale`은 변경하지 않는다.

## 실제 실행한 검증

| 검사 | 결과와 범위 |
|---|---|
| Unity C# 컴파일 | 완료, 오류 0 |
| 셰이더 | 사용 중인 CrayonFrameBoil의 Editor 셰이더 오류 0, D3D12 렌더링 확인 |
| 저장된 씬 검사 | 오브젝트 136개, Image 126개, Missing Script 0, 필수 참조 누락 0, 깨진 직렬화 참조 0, Collider 0 |
| EditMode `StarRainBattleModelTests` | **13/13 통과** |
| Play Mode `StarRainBattleSceneTests` | **3/3 통과** |
| 화면 크기 | 1080×1920, 1080×2340, 720×1280, 1920×1080에서 RenderTexture 렌더링과 좌표 검사 통과 |
| 안전 영역 | 세로 화면에 위 80px·아래 60px의 합성 안전 영역을 적용하고 전투판, HUD, 버튼, 오버레이·제목·결과 버튼의 화면 경계를 확인 |

EditMode에서는 연결선 교체·만료, 차단/오차단, 아군 통과·저주 별비 반사, 별 어두워짐과 회복, 머리의 연속 충돌 판정, 30/60fps 결과 일치, 일시정지, 같은 Seed 재현과 별 간격을 검사했다. 최초 원본 테스트는 9/10이었으며, 연결선 시계 수정 후 회귀 사례를 포함해 13/13이 됐다.

Play Mode에서는 생성한 실제 씬을 매번 로드했다. EventSystem 레이캐스트와 PointerEventData를 통한 버튼 클릭, 두 번 탭·드래그 연결, 기존 선 교체, 일시정지·재개, 같은 Seed/새 Seed, 실제 코어 충돌에 따른 HP·승패 표시를 검사했다. 승패 검사는 Editor 전용 훅으로 제어된 탄을 넣어 충돌 처리를 통과시켰으며, 자연 발사만으로 수동 완주한 검사는 아니다. 레이아웃 테스트의 다른 해상도 입력은 카메라 좌표를 사용한 합성 포인터다.

별비와 FX가 표시되는 중 움직임 감소를 켰을 때 보이는 아트가 정적 재질로 바뀌고, 새로 사용되는 풀 이미지도 정적 재질을 사용하는지 확인했다. 모델의 시각·탄 이동은 계속 진행한다. `MotionImages`의 마지막 여섯 null 슬롯은 동적 머리/꼬리/FX의 Animated/Still 재질을 찾기 위한 의도된 슬롯이며, 고정 Image 참조가 빠진 오류가 아니다.

검증 원본은 [Validation/StarRainBossBattle](Validation/StarRainBossBattle/)에 있다. `model_tests_final.json`, `scene_tests_final.json`, `scene_validation.json`, `import_validation.json`, `package_preservation.json`, `console_status.json`, `recompile_status.json`을 포함한다. 최초 실패 결과도 `_initial` 파일로 남겼다. Play Mode 최초 실패 하나는 숨겨져 사용하지 않는 풀 이미지까지 정적 재질을 요구한 검사 범위 오류였으며, 보이는 아트와 이후 새로 활성화되는 풀 이미지의 실제 동작을 검사하도록 수정했다.

아래 이미지는 패키지 Preview 합성이 아닌 실제 Unity 렌더링 결과다.

- [준비 화면](Validation/StarRainBossBattle/Ready.png), [일시정지](Validation/StarRainBossBattle/Paused.png)
- [증강·정적 아트](Validation/StarRainBossBattle/Prism_StillArt.png)
- [제어된 코어 충돌 승리](Validation/StarRainBossBattle/Victory_ControlledCoreHits.png), [패배](Validation/StarRainBossBattle/Defeat_ControlledCoreHits.png)
- [1080×1920](Validation/StarRainBossBattle/Layout_1080x1920.png), [1080×2340](Validation/StarRainBossBattle/Layout_1080x2340.png), [720×1280](Validation/StarRainBossBattle/Layout_720x1280.png), [1920×1080](Validation/StarRainBossBattle/Layout_1920x1080.png)

## 기존 프로젝트와 이후 연결

기존 메인 씬·NotebookCanvas·NotebookRoot/Gateway·BattleCrayonUI와 Build Settings는 시작 시 해시와 같다. `Build Fresh Scene`은 실행하지 않았다. 기존 PvP `OpenBattle`과 별비 보스전은 연결하지 않았고, 새 씬을 Build Settings에 추가하지 않았다. 메인·지도·퍼즐 HUD, 재화·진행·서버 처리도 변경하지 않았다.

보호 대상으로 기록한 327개 경로 중 325개는 시작 해시와 같았다. 작업 중 ET-09와 ET-10 맵 내용 변경이 감지됐으며, 이 통합에서는 맵을 쓰거나 되돌리지 않았고 해당 변경도 커밋에 포함하지 않는다. 상세 비교는 `existing_project_preservation.json`에 기록했다.

실제 스토리 사건에서의 씬 진입과 복귀, 해금/보상 저장은 이번 작업 범위 밖이다. 이후 연결 시 프로젝트 흐름에 맞춰 씬을 등록하고, `StarRainBattleController.SetReducedMotion(bool)`에 기존 설정을 전달할 수 있다. 컨트롤러의 `Model.EventRaised`에서 `BattleEventType.Win/Lose`를 관찰할 수 있지만, 현재는 보상이나 진행 상태를 확정하는 외부 연결이 없다.

Android/iOS 빌드·실기기 GPU 성능, 노치 기기의 실제 안전 영역·물리 터치·가독성, 장시간 플레이 밸런스는 실행하지 않았다. 합성 안전 영역과 Windows Editor 검사를 모바일 실기기 통과로 보고하지 않는다. 저장소 전체 테스트나 보관된 `CommercialPolishPolicyTests`도 실행하지 않았으며, 위의 전투 모듈 관련 16개 테스트만 통과했다.
