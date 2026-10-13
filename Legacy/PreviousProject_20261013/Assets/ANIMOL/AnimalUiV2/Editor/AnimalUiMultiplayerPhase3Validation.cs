using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace ANIMOL.AnimalUiV2.Editor
{
    /// <summary>Editor contract/import checks. Run in the actual Unity project; these are not local execution results.</summary>
    public static class AnimalUiMultiplayerPhase3Validation
    {
        public static void ValidateMultiplayerPhase3()
        {
            const string generated = AnimalUiV2Builder.Root + "/Generated/";
            var catalog = AssetDatabase.LoadAssetAtPath<AnimalCatalog>(generated + "AnimalCatalogV2.asset");
            Assert(catalog != null, "먼저 Build Multiplayer Phase 3을 실행해주세요.");
            int checks = RunContractChecks(catalog) + ValidatePortraits(catalog);
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(generated + "MultiplayerAnimalSelect.prefab");
            checks += ValidatePrefab(prefab, catalog);
            checks += ValidateReadonlyDemo(generated + "AnimalMultiplayerPhase3Demo.unity");
            Debug.Log("ANIMOL 대기방 전 동물 선택 3단계 계약/import/prefab 검사 통과: " + checks +
                "개. 실제 Play 모드, 계정 권한, 입장 승인 서비스 검증은 별도로 실행하세요.");
        }

        public static int RunContractChecks(AnimalCatalog catalog)
        {
            int checks = 0;
            Action<bool, string> check = (condition, label) => { Assert(condition, label); checks++; };
            check(catalog != null && catalog.IsValid(out _), "카탈로그 유효성");
            check(catalog.Animals.Count == 15, "15종 초상");
            check(Enum.GetValues(typeof(AnimalRole)).Cast<AnimalRole>().All(role => catalog.ForRole(role).Count() == 5), "역할별 5종");
            // Synthetic editor-only values exercise contract boundaries; no account or room receives them.
            var snapshot = CreateGrantedSnapshot(catalog);
            var loadout = new AnimalLoadout { Ground = "Rabbit", Special = "DreamFox", Air = "Swallow" };
            var context = new MultiplayerSelectionRequest
            {
                ModeId = "EDITOR_MODE_ONLY", DisplayName = "EDITOR ONLY", EntryIntent = "EDITOR_INTENT_ONLY", InitialLoadout = loadout.Clone(),
                Requirements = new SelectionRequirements
                {
                    RequiredRoles = new[] { AnimalRole.Ground, AnimalRole.Special, AnimalRole.Air },
                    RepresentativeRole = AnimalRole.Ground, PolicyRevision = "EDITOR_POLICY_ONLY"
                }
            };
            var request = new SelectionCommitRequest
            {
                ActionId = "EDITOR_ACTION_ONLY", ContextId = context.ModeId, EntryIntent = context.EntryIntent,
                SnapshotRevision = snapshot.Revision, PolicyRevision = context.Requirements.PolicyRevision, Loadout = loadout.Clone()
            };
            Func<bool> validContext = () => AnimalUiRules.ValidateMultiplayerContext(catalog, context, out _);
            Func<bool> can = () => AnimalUiRules.CanConfirmMultiplayer(catalog, snapshot, loadout, context, true, true, out _);
            Func<bool> dispatch = () => AnimalUiRules.ValidateMultiplayerDispatch(catalog, snapshot, context, request, out _);
            check(validContext(), "3역할 문맥 허용"); check(can(), "권한 있는 3역할 편성 허용"); check(dispatch(), "문맥/정책/권한 버전 일치 요청 허용");
            check(!AnimalUiRules.ValidateMultiplayerContext(catalog, null, out _), "문맥 누락 차단");
            check(!AnimalUiRules.ValidateMultiplayerContext(null, context, out _), "카탈로그 누락 차단");
            context.InitialLoadout.Ground = "UNKNOWN"; check(!validContext(), "기존 편성의 알 수 없는 ID 차단");
            context.InitialLoadout.Ground = "Swallow"; check(!validContext(), "기존 편성의 잘못된 역할 차단");
            context.InitialLoadout.Ground = null; check(validContext(), "기존 선택이 없는 슬롯은 정상 미선택 문맥"); context.InitialLoadout.Ground = "Rabbit";
            context.ModeId = " "; check(!validContext(), "모드 ID 공백 차단"); context.ModeId = request.ContextId;
            context.EntryIntent = null; check(!validContext(), "입장 의도 누락 차단"); context.EntryIntent = request.EntryIntent;
            context.Requirements.PolicyRevision = " "; check(!validContext(), "정책 버전 공백 차단"); context.Requirements.PolicyRevision = request.PolicyRevision;
            var requirements = context.Requirements; context.Requirements = null;
            check(!validContext(), "선택 요구 설정 누락 차단"); context.Requirements = requirements;
            var roles = requirements.RequiredRoles; requirements.RequiredRoles = null;
            check(!validContext(), "필수 역할 누락 차단"); requirements.RequiredRoles = Array.Empty<AnimalRole>();
            check(!validContext(), "빈 필수 역할 차단"); requirements.RequiredRoles = new[] { AnimalRole.Air };
            check(!validContext(), "불완전한 3역할 차단"); requirements.RequiredRoles = new[] { AnimalRole.Ground, AnimalRole.Ground, AnimalRole.Air };
            check(!validContext(), "중복 역할 차단"); requirements.RequiredRoles = new[] { AnimalRole.Ground, AnimalRole.Special, (AnimalRole)999 };
            check(!validContext(), "정의되지 않은 역할 차단"); requirements.RequiredRoles = roles;
            requirements.RepresentativeRole = (AnimalRole)999; check(!validContext(), "정의되지 않은 대표 역할 차단");
            requirements.RepresentativeRole = AnimalRole.Air; check(can(), "공중 대표 역할은 유효한 3역할 편성"); requirements.RepresentativeRole = AnimalRole.Ground;
            context.AllowedAnimalIds = null; check(can(), "null pool은 추가 client 제한 없음, 실제 권한 필수");
            context.AllowedAnimalIds = Array.Empty<string>(); check(!can(), "빈 pool은 선택 허용 없음");
            context.AllowedAnimalIds = new[] { "Rabbit", "DreamFox", "Swallow" }; check(can(), "명시적 허용 pool 편성");
            context.AllowedAnimalIds = new[] { "Rabbit", "DreamFox" }; check(!can(), "pool 밖 동물 차단");
            context.AllowedAnimalIds = new[] { "Rabbit", "DreamFox", "Swallow", "UNKNOWN" }; check(!validContext(), "pool의 알 수 없는 ID 차단");
            context.AllowedAnimalIds = new[] { "Rabbit", "DreamFox", "Swallow", " " }; check(!validContext(), "pool의 공백 ID 차단"); context.AllowedAnimalIds = null;
            check(!AnimalUiRules.CanConfirmMultiplayer(catalog, snapshot, loadout, context, false, true, out _), "backend 미연결 승인 차단");
            check(!AnimalUiRules.CanConfirmMultiplayer(catalog, snapshot, loadout, context, true, false, out _), "미검증 조회 승인 차단");
            check(!AnimalUiRules.CanConfirmMultiplayer(catalog, null, loadout, context, true, true, out _), "스냅샷 누락 차단");
            snapshot.Revision = " "; check(!can(), "스냅샷 버전 공백 차단"); snapshot.Revision = request.SnapshotRevision;
            snapshot.Animals.Add(null); check(!can(), "스냅샷의 빈 항목 차단"); snapshot.Animals.RemoveAt(snapshot.Animals.Count - 1);
            snapshot.Animals.Add(new AnimalProgress { AnimalId = "Rabbit" }); check(!can(), "스냅샷의 중복 ID 차단"); snapshot.Animals.RemoveAt(snapshot.Animals.Count - 1);
            snapshot.Animals.Add(new AnimalProgress { AnimalId = " " }); check(!can(), "스냅샷의 공백 ID 차단"); snapshot.Animals.RemoveAt(snapshot.Animals.Count - 1);
            check(!AnimalUiRules.CanConfirmMultiplayer(catalog, snapshot, null, context, true, true, out _), "편성 누락 차단");
            loadout.Ground = "Swallow"; check(!can(), "슬롯과 다른 역할 ID 차단"); loadout.Ground = "UNKNOWN";
            check(!can(), "알 수 없는 선택 ID 차단"); loadout.Ground = null; check(!can(), "미선택 슬롯 차단"); loadout.Ground = "Rabbit";
            var rabbit = snapshot.Find("Rabbit"); rabbit.Unlocked = false;
            check(can(), "계정 잠김이어도 실제 문맥 grant가 있으면 허용"); rabbit.Unlocked = true;
            rabbit.HasContextPermission = false; check(!can(), "문맥 권한 미연결 차단"); rabbit.HasContextPermission = true;
            rabbit.CanUseInContext = false; check(!can(), "문맥 권한 거절 차단"); rabbit.CanUseInContext = true;
            rabbit.Implemented = false; check(!can(), "플레이 미구현 동물 차단"); rabbit.Implemented = true;
            snapshot.Animals.Remove(rabbit); check(!can(), "스냅샷 동물 데이터 누락 차단"); snapshot.Animals.Add(rabbit);
            var anotherPlayerSameLoadout = loadout.Clone();
            check(can() && AnimalUiRules.CanConfirmMultiplayer(catalog, snapshot, anotherPlayerSameLoadout, context, true, true, out _), "다른 플레이어와 같은 동물 선택 허용");
            check(!AnimalUiRules.ValidateMultiplayerDispatch(catalog, snapshot, context, null, out _), "요청 누락 차단");
            request.ActionId = " "; check(!dispatch(), "ActionId 공백 차단"); request.ActionId = "EDITOR_ACTION_ONLY";
            request.ContextId = "OTHER"; check(!dispatch(), "다른 모드 요청 차단"); request.ContextId = context.ModeId;
            request.EntryIntent = "OTHER"; check(!dispatch(), "다른 입장 의도 요청 차단"); request.EntryIntent = context.EntryIntent;
            request.PolicyRevision = "OTHER"; check(!dispatch(), "다른 정책 버전 차단"); request.PolicyRevision = requirements.PolicyRevision;
            request.SnapshotRevision = "OTHER"; check(!dispatch(), "오래된 권한 스냅샷 요청 차단"); request.SnapshotRevision = snapshot.Revision;
            request.Loadout.Air = "Rabbit"; check(!dispatch(), "요청 payload의 잘못된 역할 차단"); request.Loadout.Air = "Swallow";
            var requestCopy = request.Clone(); requestCopy.Loadout.Ground = "Wolf"; requestCopy.EntryIntent = "OTHER"; requestCopy.ActionId = "OTHER";
            check(request.Loadout.Ground == "Rabbit" && request.EntryIntent == context.EntryIntent && request.ActionId == "EDITOR_ACTION_ONLY", "요청 복사 수정은 재시도 원본을 변경하지 않음");
            context.AllowedAnimalIds = new[] { "Rabbit", "DreamFox", "Swallow" };
            var contextCopy = context.Clone(); contextCopy.InitialLoadout.Ground = "Wolf"; contextCopy.AllowedAnimalIds[0] = "Wolf";
            contextCopy.Requirements.RequiredRoles[0] = AnimalRole.Air; contextCopy.EntryIntent = "OTHER";
            check(context.InitialLoadout.Ground == "Rabbit" && context.AllowedAnimalIds[0] == "Rabbit" &&
                context.Requirements.RequiredRoles[0] == AnimalRole.Ground && context.EntryIntent == request.EntryIntent, "입장 문맥 깊은 복사");
            context.AllowedAnimalIds = null;
            var result = new AnimalUiCommitResult
            {
                Status = CommitStatus.Accepted, AcceptanceToken = "EDITOR_RECEIPT_ONLY", AcceptedContextId = context.ModeId,
                AcceptedPolicyRevision = requirements.PolicyRevision, AcceptedEntryIntent = context.EntryIntent, AcceptedLoadout = loadout.Clone()
            };
            Func<bool> accepted = () => AnimalUiRules.IsAcceptedMultiplayerResult(catalog, snapshot, context, request, result, out _);
            check(accepted(), "명시적 문맥/의도/정책/편성 승인 receipt 허용");
            check(!AnimalUiRules.IsAcceptedMultiplayerResult(catalog, snapshot, context, request, null, out _), "승인 receipt 누락 차단");
            foreach (var status in new[] { CommitStatus.Rejected, CommitStatus.Unavailable, CommitStatus.Unknown })
            { result.Status = status; check(!accepted(), status + "는 대기방 입장 승인 아님"); } result.Status = CommitStatus.Accepted;
            result.AcceptanceToken = " "; check(!accepted(), "승인 token 공백 차단"); result.AcceptanceToken = "EDITOR_RECEIPT_ONLY";
            result.AcceptedContextId = "OTHER"; check(!accepted(), "다른 모드 receipt 차단"); result.AcceptedContextId = context.ModeId;
            result.AcceptedPolicyRevision = "OTHER"; check(!accepted(), "다른 정책 receipt 차단"); result.AcceptedPolicyRevision = requirements.PolicyRevision;
            result.AcceptedEntryIntent = "OTHER"; check(!accepted(), "다른 입장 의도 receipt 차단"); result.AcceptedEntryIntent = context.EntryIntent;
            result.AcceptedLoadout.Ground = "Wolf"; check(!accepted(), "다른 승인 편성 차단"); result.AcceptedLoadout.Ground = "Rabbit";
            result.AcceptedLoadout = null; check(!accepted(), "승인 편성 누락 차단"); result.AcceptedLoadout = loadout.Clone();
            result.Snapshot = CreateGrantedSnapshot(catalog); result.Snapshot.Revision = "EDITOR_NEW_SNAPSHOT_ONLY";
            check(accepted(), "실제 승인 스냅샷의 유효한 새 revision 허용");
            result.Snapshot.Find("Rabbit").CanUseInContext = false; check(!accepted(), "승인 스냅샷의 사용 거절 차단");
            result.Snapshot.Find("Rabbit").CanUseInContext = true; result.Snapshot.Find("Rabbit").HasContextPermission = false;
            check(!accepted(), "승인 스냅샷의 권한 누락 차단"); result.Snapshot = new AnimalUiSnapshot();
            check(!accepted(), "빈 승인 스냅샷은 이전 권한으로 대체하지 않음"); result.Snapshot = null;
            rabbit.CanUseInContext = false; check(!accepted(), "fallback 스냅샷도 사용 권한 재확인"); rabbit.CanUseInContext = true;
            var fixture = AnimalMultiplayerPhase3DemoHost.CreateReadonlyFixture();
            check(fixture.ModeId == "PREVIEW_ONLY" && fixture.EntryIntent == "PREVIEW_ONLY" && fixture.Requirements.PolicyRevision == "PREVIEW_ONLY", "데모 문맥은 명시적 PREVIEW_ONLY");
            check(AnimalUiRules.ValidateMultiplayerContext(catalog, fixture, out _), "읽기 전용 데모 구조는 정상");
            check(fixture.InitialLoadout != null && fixture.InitialLoadout.Fingerprint() == "Rabbit|DreamFox|Owl", "데모 편성은 디자인 예시이며 실제 준비/보유 상태를 만들지 않음");
            check(!AnimalUiRules.CanConfirmMultiplayer(catalog, new AnimalUiSnapshot(), fixture.InitialLoadout, fixture, false, false, out _), "읽기 전용 예시로 실행 권한 생성 안 함");
            var previewRequest = new SelectionCommitRequest
            {
                ActionId = "EDITOR_ACTION_ONLY", ContextId = fixture.ModeId, EntryIntent = fixture.EntryIntent,
                SnapshotRevision = snapshot.Revision, PolicyRevision = fixture.Requirements.PolicyRevision, Loadout = loadout.Clone()
            };
            check(!AnimalUiRules.ValidateMultiplayerDispatch(catalog, snapshot, fixture, previewRequest, out _), "PREVIEW_ONLY는 권한 fixture를 갖춰도 실제 입장 요청 차단");
            return checks;
        }

        private static AnimalUiSnapshot CreateGrantedSnapshot(AnimalCatalog catalog)
        {
            var snapshot = new AnimalUiSnapshot { Revision = "EDITOR_SNAPSHOT_ONLY", ContextId = "EDITOR_MODE_ONLY",
                PolicyRevision = "EDITOR_POLICY_ONLY", EntryIntent = "EDITOR_INTENT_ONLY" };
            foreach (var animal in catalog.Animals) snapshot.Animals.Add(new AnimalProgress
            { AnimalId = animal.Id, Implemented = true, HasContextPermission = true, CanUseInContext = true });
            return snapshot;
        }

        private static int ValidatePortraits(AnimalCatalog catalog)
        {
            for (int i = 0; i < catalog.Animals.Count; i++)
            {
                var animal = catalog.Animals[i];
                string path = AnimalUiV2Builder.Root + "/Art/Portraits/" + (i + 1).ToString("00") + "_" + animal.Id + ".png";
                Assert(AssetDatabase.GetAssetPath(animal.Portrait) == path, animal.Id + " 원본 초상 1:1 연결");
                Assert(animal.Portrait.rect.width == 128 && animal.Portrait.rect.height == 160, animal.Id + " 전체 캔버스");
                var importer = AssetImporter.GetAtPath(path) as TextureImporter;
                Assert(importer != null && importer.filterMode == FilterMode.Point && !importer.mipmapEnabled && importer.textureCompression == TextureImporterCompression.Uncompressed, animal.Id + " 픽셀 import");
                var settings = new TextureImporterSettings(); importer.ReadTextureSettings(settings);
                Assert(settings.spriteMeshType == SpriteMeshType.FullRect, animal.Id + " Full Rect");
            }
            return catalog.Animals.Count * 4;
        }

        private static int ValidatePrefab(GameObject prefab, AnimalCatalog catalog)
        {
            Assert(prefab != null, "MultiplayerAnimalSelect prefab");
            var presenter = prefab.GetComponent<AnimalUiPresenter>();
            Assert(presenter != null && presenter.Catalog == catalog && presenter.View != null, "presenter 연결");
            Assert(presenter.Backend == null && presenter.PreviewMode == AnimalUiMode.MultiplayerAnimalSelect && !presenter.OpenReadonlyPreviewOnStart, "운영 prefab은 호스트의 명시적 OpenMultiplayer로만 열림");
            var view = presenter.View;
            Assert(view.Slots.Length == 3 && view.RoleTabs.Length == 3 && view.CardTemplate != null, "3역할 선택 슬롯/탭/템플릿");
            Assert(view.BodyScroll != null && view.BodyScroll.viewport != null && view.BodyScroll.content != null && view.ModalConfirm != null && view.ModalCancel != null, "스크롤/승인 모달");
            Assert(view.HeroPortrait.preserveAspect && !view.HeroPortrait.useSpriteMesh && view.CardTemplate.Portrait.preserveAspect && !view.CardTemplate.Portrait.useSpriteMesh, "전체 초상 contain");
            Assert(view.SelectionDetailsButton != null && view.SelectionDetailsButton.gameObject.activeSelf && !view.UpgradeTracks.activeSelf, "동물 정보 열람, 강화 요청 비활성");
            var scaler = prefab.GetComponent<CanvasScaler>();
            Assert(scaler != null && scaler.referenceResolution == new Vector2(1080, 1920) && scaler.matchWidthOrHeight == 0, "모바일 세로 CanvasScaler");
            Assert(prefab.GetComponentInChildren<AnimalUiSafeArea>(true) != null, "SafeArea 적용");
            return 9;
        }

        private static int ValidateReadonlyDemo(string path)
        {
            Assert(AssetDatabase.LoadAssetAtPath<SceneAsset>(path) != null, "3단계 읽기 전용 데모 씬");
            var previousActive = SceneManager.GetActiveScene();
            var scene = SceneManager.GetSceneByPath(path);
            bool openedForCheck = !scene.IsValid() || !scene.isLoaded;
            if (openedForCheck) scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Additive);
            try
            {
                var hosts = scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<AnimalMultiplayerPhase3DemoHost>(true)).ToArray();
                Assert(hosts.Length == 1 && hosts[0].Screen != null, "읽기 전용 데모 호스트 연결");
                Assert(hosts[0].Screen.Backend == null && !hosts[0].Screen.OpenReadonlyPreviewOnStart && hosts[0].Screen.PreviewMode == AnimalUiMode.MultiplayerAnimalSelect, "데모 자동 운영 backend/입장 실행 없음");
            }
            finally
            {
                if (openedForCheck) EditorSceneManager.CloseScene(scene, true);
                if (previousActive.IsValid() && previousActive.isLoaded) SceneManager.SetActiveScene(previousActive);
            }
            return 3;
        }

        private static void Assert(bool condition, string message)
        { if (!condition) throw new InvalidOperationException("Animal UI 3단계 검증 실패: " + message); }
    }
}
