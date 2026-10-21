낙일 성소 V02 아트 검토용 Unity 씬 생성기

제공 소스의 C#/Unity 실행은 NOT_RUN이다. 이 생성기는 실제 프로젝트에 임포트하고 카탈로그를 먼저 빌드한 뒤 사용한다.

정확한 CLI 메서드:
ANIMOL.Art.SunsetSanctumV02.SunsetSanctumV02ArtReviewSceneBuilder.BuildFromCommandLine
필수 인자:
-sanctumV02ReviewLayout "Assets/<실제부모경로>/ANIMOL_SUNSET_SANCTUM_CARTOON_V02/review_scene_layout.json"

예:
"<UNITY_EDITOR>" -batchmode -quit -projectPath "<UNITY_PROJECT>" -executeMethod ANIMOL.Art.SunsetSanctumV02.SunsetSanctumV02ArtReviewSceneBuilder.BuildFromCommandLine -sanctumV02ReviewLayout "Assets/<실제부모경로>/ANIMOL_SUNSET_SANCTUM_CARTOON_V02/review_scene_layout.json" -logFile "<LOG_DIR>/SanctumV02ArtReview.log"

선행 조건:
- 같은 패키지의 animation_catalog.json 및 animation_catalog_sha256.txt가 있어야 한다.
- SunsetSanctumV02AnimationBuilder가 생성한 _Generated/SunsetSanctumV02VisualCatalog.asset과 모든 .anim이 현재 원화/JSON/GUID/fileID 검증을 통과해야 한다.
- catalog.parts[].key는 최신 runtime PNG의 정확한 상대 파일 경로다. 다른 방향/구버전/빈 파일 fallback은 없다.
- 동일한 출력 검토 씬이 이미 에디터에 열려 있으면 덮어쓰지 않고 실패한다. 해당 검토 씬을 닫은 뒤 재생성한다. 다른 열린 씬이나 미저장 씬은 이 도구가 닫거나 저장하지 않는다.

출력:
_Generated/SunsetSanctumV02ArtReview.unity
_Generated/art_review_scene_build_report.json

씬은 새 EmptyScene을 Additive로 만들고 그 새 씬에만 카메라/원화/검토 플레이어를 생성한다. 검토 씬만 지정 경로로 저장한 후 닫고, 원래 활성 씬을 복구한다. 기존 production 씬, Build Settings, 장치 ID, 맵, Collider, Input은 수정하지 않는다. 기존 씬들을 한꺼번에 저장하는 SaveOpenScenes/SaveAssets도 호출하지 않는다.

검토 플레이어:
SunsetSanctumV02ArtReviewPlayback은 artReviewOnly=true와 같은 catalog pin을 가진 ART_REVIEW_ONLY marker 부모가 동시에 있어야 자동 재생한다. manifest의 실제 clip fps를 사용한다. secondsPerClip은 검토 슬롯의 최소 길이이며, non-loop 동작은 원래 fps로 끝까지 재생한 뒤 마지막 프레임을 유지한다. 프레임 이벤트나 이동/비용/턴 변경은 없다. 일시정지(Time.timeScale=0)에서 진행하지 않는다. 실제 인게임에는 이 자동 검토 플레이어를 연결하지 말고 기존 시뮬레이션의 권위 있는 시각 어댑터를 연결한다.

배경:
coverCamera=true인 한 장의 하늘은 CameraCover만 담당하고 BackgroundRig 목록에서는 제외한다. background=true인 나머지 원화만 명시 z/정렬/파랄랙스/드리프트를 사용한다. 검토 씬은 기존 프로젝트 Sorting Layer를 새로 만들지 않고 Default 안에서 sortingOrder를 명시한다. 모바일 화면 검토 시 Game view를1080×1920 세로 비율로 맞춘다.

레이아웃 DTO(schemaVersion1, 좌표는Unity world XY, North=+Y):
camera: x,y,z,orthographicSize
sprites[]: name,file,x,y,z,scaleX,scaleY,opacity(생략 시1),sortingOrder,coverCamera,background,parallaxMultiplier,driftX,driftY,periodX,periodY
motions[]: name,clipKeys[],x,y,z,sortingOrder,secondsPerClip
모든 scale은 양수, 모든 좌표/시간은 유한수, background drift의 periodX/Y는1초 이상이어야 한다. sprites와motions의name은전체에서고유해야 한다. 카메라는+Z방향으로보며 모든 원화의z는camera.z보다크고camera.z+999보다작아야 한다.

이 씬이 생성·재생된 사실은 실제 게임의 장치 동작/충돌/점유/기존 레거시 참조 교체가 완료됐다는 증거가 아니다. actualGameplayBindingsVerified 및 productionScenesMigrated는 항상 false로 보고한다.

선택적 정적 광선 검토
같은 패키지 루트의 review_scene_effects.json이 없으면 기존 정적 원화/모션 검토만 생성한다. 있으면 schemaVersion1, gridOrigin[x,y], cellSize>0, lightRoute{emitter{x,y},heading,cells[],stop:receiver}를 읽는다. 각 cell은 x,y,kind(open/glass/mirror/receiver),incoming,outgoing,receiverKey(수신기에서만필수)를 제공한다. 정확한 소문자 east/north/west/south만 허용한다. 연결된 순서와 방향 연속성, 거울의 수직90도반사는 공유 SanctumLightRoute.FromAuthoritativeTrace로 검증한다. 말단은 수신기이고 같은 directedcell을 반복하면 실패한다. sortingOrder는 생략 시5이며, 명시한0도 유지한다.
광선은 입력/거울 애니메이션을 보고 다시 계산하지 않는 ART_REVIEW_ONLY snapshot이다. gridOrigin은world0 identity의검토root를기준으로하고+Y가North다. native장치와같은cell중심에붙는다. 현재 제공된예시의거울은slash endpoint로고정되어있으며전환모션중임의ray재해석이없다.
선택적광선파일이있으면shader상태를검사한전용 _Generated/LightBeam.mat과 _Generated/SunsetSanctumV02ArtReviewBeam.asset을생성/갱신한다. 이두개privateasset만SaveAssetIfDirty로저장하며기존프로젝트의다른dirty자산을전체저장하지않는다. transientmesh/MaterialPropertyBlock만씬에남겨재오픈시광선이사라지는문제를피하기위해정적mesh snapshot과material의_PathLength/_Active0을저장한다. 원본BeamRenderer가소유한임시mesh와snapshotasset은별개라씬닫힘/OnDestroy에서도snapshot은유지된다. 실제Unity재오픈검증은NOT_RUN이다.
report의beamCreated/sourceEffects/sourceEffectsSha256/beamMaterialAssetPath/beamMeshAssetPath로선택적snapshot생성범위를기록한다. physics/입력/코스트/장치수신판정은계속없다.
opacity는renderer.color.a에만적용하며0..1의유한수여야한다. backgroundRig가이원래alpha를보존한다.
정적원화만있는검토씬은Editor의strictcatalog검사와프로젝트BuildGuard가검증범위다. 별도정적SceneRuntimeCatalog검사컴포넌트는추가하지않았다. 현재제공된검토씬은모션노드를포함해ReviewPlayback도runtimecatalog pin을검사한다.

V02 포팅 소스 검수: 최종 V01의 검토용 C#3개와 비교했으며 버전명 V01→V02 치환 외의 동작 변경이 없었다. art_review_source_qa.json의 SHA는 현재 V02 C#실파일 기준으로 갱신했다. CLI무인자 진입점, marker+catalog pin, 원래 fps 및 deferred Start, 선택적 정적 beam의 private mesh/material snapshot, opacity생략1/명시0/유한0..1이 유지된다. V01 예시 레이아웃/광선 payload의 hash와 row수를 V02검수 증거로 재사용하지 않는다. 실제 compile/씬생성/재오픈/shader/PlayMode는 모두 NOT_RUN이다.
