using System;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace ANIMOL.AnimalUiV2.Editor
{
    public static class AnimalUiV2Validation
    {
        public static void ValidateStagePhase2()
        {
            const string generated = AnimalUiV2Builder.Root + "/Generated/";
            var catalog = AssetDatabase.LoadAssetAtPath<AnimalCatalog>(generated + "AnimalCatalogV2.asset");
            Assert(catalog != null, "먼저 Build Stage Phase 2를 실행해주세요.");
            int checks = RunStageContractChecks(catalog) + ValidatePortraitReferences(catalog);
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(generated + "StageAnimalSelect.prefab");
            checks += ValidatePrefab(prefab, catalog, AnimalUiMode.StageAnimalSelect);
            var presenter = prefab.GetComponent<AnimalUiPresenter>();
            Assert(!presenter.OpenReadonlyPreviewOnStart, "실제 prefab은 호스트의 OpenCampaign 호출로만 열림");
            Assert(presenter.View.SelectionDetailsButton != null && presenter.View.SelectionDetailsButton.gameObject.activeSelf, "읽기 전용 동물 정보 버튼");
            Assert(!presenter.View.UpgradeTracks.activeSelf, "스테이지 화면의 강화 요청 UI 비활성");
            Assert(AssetDatabase.LoadAssetAtPath<SceneAsset>(generated + "AnimalStagePhase2Demo.unity") != null, "2단계 읽기 전용 데모 씬");
            Debug.Log("ANIMOL 스테이지 동물 확인 2단계 계약/import/prefab 연결 검사 통과: " + (checks + 4) + "개. 실제 Play 모드와 기존 스테이지 입장 서비스 검증은 별도로 실행하세요.");
        }

        public static void ValidateUpgradePhase1()
        {
            const string generated = AnimalUiV2Builder.Root + "/Generated/";
            var catalog = AssetDatabase.LoadAssetAtPath<AnimalCatalog>(generated + "AnimalCatalogV2.asset");
            Assert(catalog != null, "먼저 Build Upgrade Phase 1을 실행해주세요.");
            int checks = RunUpgradeContractChecks(catalog) + ValidatePortraitReferences(catalog);
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(generated + "CharacterUpgrade.prefab");
            checks += ValidatePrefab(prefab, catalog, AnimalUiMode.CharacterUpgrade);
            Assert(AssetDatabase.LoadAssetAtPath<SceneAsset>(generated + "AnimalUpgradePhase1Demo.unity") != null, "1단계 데모 씬");
            Debug.Log("ANIMOL 동물 강화 1단계 계약/import/prefab 연결 검사 통과: " + (checks + 1) + "개. 실제 Play 모드와 저장/차감 서비스 검증은 별도로 실행하세요.");
        }

        public static void ValidateGenerated()
        {
            const string generated = AnimalUiV2Builder.Root + "/Generated/";
            var catalog = AssetDatabase.LoadAssetAtPath<AnimalCatalog>(generated + "AnimalCatalogV2.asset");
            Assert(catalog != null, "먼저 Build Preview를 실행해주세요.");
            int checks = RunContractChecks(catalog);
            checks += ValidatePortraitReferences(catalog);
            foreach (AnimalUiMode mode in Enum.GetValues(typeof(AnimalUiMode)))
            {
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(generated + mode + ".prefab");
                checks += ValidatePrefab(prefab, catalog, mode);
            }
            Debug.Log("Animal UI v2 정적 계약/import/prefab 연결 검사 통과: " + checks + "개. 실제 Game View와 서비스 연결 검증은 별도로 실행하세요.");
        }
        private static int ValidatePortraitReferences(AnimalCatalog catalog)
        {
            int checks = 0;
            for (int i = 0; i < catalog.Animals.Count; i++)
            {
                var animal = catalog.Animals[i];
                string expectedPath = AnimalUiV2Builder.Root + "/Art/Portraits/" + (i + 1).ToString("00") + "_" + animal.Id + ".png";
                Assert(AssetDatabase.GetAssetPath(animal.Portrait) == expectedPath, animal.Id + " 원본 초상 1:1 연결");
                Assert(animal.Portrait.rect.width == 128 && animal.Portrait.rect.height == 160, animal.Id + " 초상 전체 캔버스 크기");
                var importer = AssetImporter.GetAtPath(expectedPath) as TextureImporter;
                Assert(importer != null && importer.filterMode == FilterMode.Point && !importer.mipmapEnabled && importer.textureCompression == TextureImporterCompression.Uncompressed, animal.Id + " 픽셀 import 설정");
                var settings = new TextureImporterSettings(); importer.ReadTextureSettings(settings);
                Assert(settings.spriteMeshType == SpriteMeshType.FullRect, animal.Id + " Full Rect 설정"); checks += 4;
            }
            return checks;
        }
        private static int ValidatePrefab(GameObject prefab, AnimalCatalog catalog, AnimalUiMode mode)
        {
                Assert(prefab != null, mode + " prefab"); var presenter = prefab.GetComponent<AnimalUiPresenter>();
                Assert(presenter != null && presenter.Catalog == catalog && presenter.View != null, mode + " presenter 연결");
                var view = presenter.View;
                Assert(view.Slots.Length == 3 && view.RoleTabs.Length == 3 && view.CardTemplate != null, mode + " 슬롯/탭/템플릿");
                Assert(view.BodyScroll != null && view.BodyScroll.viewport != null && view.BodyScroll.content != null && view.ModalConfirm != null && view.ModalCancel != null, mode + " 스크롤/모달");
                Assert(view.HeroPortrait.preserveAspect && !view.HeroPortrait.useSpriteMesh && view.CardTemplate.Portrait.preserveAspect && !view.CardTemplate.Portrait.useSpriteMesh, mode + " 초상 contain");
                var scaler = prefab.GetComponent<CanvasScaler>();
                Assert(scaler != null && scaler.referenceResolution == new Vector2(1080, 1920) && scaler.matchWidthOrHeight == 0, mode + " CanvasScaler");
                Assert(presenter.PreviewMode == mode && presenter.Backend == null, mode + " 읽기 전용 생성 모드");
                return 7;
        }

        internal static int RunStageContractChecks(AnimalCatalog catalog)
        {
            int checks = 0;
            Action<bool, string> check = (condition, label) => { Assert(condition, label); checks++; };
            check(catalog.IsValid(out _), "카탈로그 유효성"); check(catalog.Animals.Count == 15, "15종 초상");
            check(Enum.GetValues(typeof(AnimalRole)).Cast<AnimalRole>().All(role => catalog.ForRole(role).Count() == 5), "역할별 5종");
            // These synthetic values test contracts only. They never populate the actual stage catalogue/account.
            var snapshot = new AnimalUiSnapshot { Revision = "EDITOR_SNAPSHOT_ONLY", ContextId = "EDITOR_STAGE_ONLY", PolicyRevision = "EDITOR_POLICY_ONLY" };
            foreach (var animal in catalog.Animals) snapshot.Animals.Add(new AnimalProgress
            { AnimalId = animal.Id, Implemented = true, Unlocked = true, HasContextPermission = true, CanUseInContext = true });
            var loadout = new AnimalLoadout { Ground = "Rabbit", Special = "DreamFox", Air = "Swallow" };
            var stage = new StageSelectionRequest { StageId = "EDITOR_STAGE_ONLY", Policy = CampaignSelectionPolicy.Fixed,
                FixedLoadout = loadout.Clone(), Requirements = new SelectionRequirements
                { RequiredRoles = new[] { AnimalRole.Ground, AnimalRole.Special, AnimalRole.Air }, RepresentativeRole = AnimalRole.Ground, PolicyRevision = "EDITOR_POLICY_ONLY" } };
            var request = new SelectionCommitRequest { ActionId = "EDITOR_ACTION_ONLY", ContextId = stage.StageId,
                SnapshotRevision = snapshot.Revision, PolicyRevision = stage.Requirements.PolicyRevision, Loadout = loadout.Clone() };
            Func<bool> can = () => AnimalUiRules.CanConfirmFixedStage(catalog, snapshot, loadout, stage, true, true, out _);
            Func<bool> dispatch = () => AnimalUiRules.ValidateFixedStageDispatch(catalog, snapshot, stage, request, out _);
            check(can(), "실제 권한 형태의 Fixed 지정3편성 허용"); check(dispatch(), "문맥/버전 일치 요청 허용");
            snapshot.ContextId = "OTHER"; check(!can(), "다른 스테이지 조회 응답 차단"); snapshot.ContextId = stage.StageId;
            snapshot.PolicyRevision = "OTHER"; check(!can(), "다른 정책 조회 응답 차단"); snapshot.PolicyRevision = stage.Requirements.PolicyRevision;
            check(!AnimalUiRules.CanConfirmFixedStage(catalog, snapshot, loadout, stage, false, true, out _), "backend 미연결 시작 차단");
            check(!AnimalUiRules.CanConfirmFixedStage(catalog, snapshot, loadout, stage, true, false, out _), "조회 실패/미검증 시작 차단");
            snapshot.Revision = " "; check(!can(), "스냅샷 버전 공백 차단"); snapshot.Revision = request.SnapshotRevision;
            stage.StageId = " "; check(!can(), "StageId 공백 차단"); stage.StageId = request.ContextId;
            foreach (var policy in new[] { CampaignSelectionPolicy.Free, CampaignSelectionPolicy.AllowedPool, (CampaignSelectionPolicy)999 })
            { stage.Policy = policy; check(!can(), policy + " 2단계 시작 차단"); }
            stage.Policy = CampaignSelectionPolicy.Fixed;
            stage.Requirements.PolicyRevision = null; check(!can(), "정책 버전 누락 차단"); stage.Requirements.PolicyRevision = request.PolicyRevision;
            var roles = stage.Requirements.RequiredRoles;
            stage.Requirements.RequiredRoles = null; check(!can(), "필수 역할 누락 차단");
            stage.Requirements.RequiredRoles = new[] { AnimalRole.Ground }; check(!can(), "세 역할 미완성 차단");
            stage.Requirements.RequiredRoles = new[] { AnimalRole.Ground, AnimalRole.Ground, AnimalRole.Air }; check(!can(), "중복 역할 차단");
            stage.Requirements.RequiredRoles = new[] { AnimalRole.Ground, AnimalRole.Special, (AnimalRole)999 }; check(!can(), "잘못된 역할 차단");
            stage.Requirements.RequiredRoles = roles;
            stage.Requirements.RepresentativeRole = (AnimalRole)999; check(!can(), "시작 동물 역할 연결 누락 차단"); stage.Requirements.RepresentativeRole = AnimalRole.Ground;
            stage.FixedLoadout.Ground = "Swallow"; check(!can(), "지정 동물의 잘못된 역할 차단"); stage.FixedLoadout.Ground = "Rabbit";
            stage.FixedLoadout.Special = "UNKNOWN"; check(!can(), "정의되지 않은 지정 동물 차단"); stage.FixedLoadout.Special = "DreamFox";
            loadout.Ground = "Wolf"; check(!can(), "다른 동물 열람이 지정 편성을 바꿀 수 없음"); loadout.Ground = "Rabbit";
            loadout.Air = null; check(!can(), "빈 지정 슬롯 차단"); loadout.Air = "Swallow";
            var rabbit = snapshot.Find("Rabbit");
            rabbit.Unlocked = false; check(can(), "계정 잠김이어도 실제 스테이지 grant 허용");
            rabbit.CanUseInContext = false; check(!can(), "스테이지 사용 권한 거절"); rabbit.CanUseInContext = true;
            rabbit.HasContextPermission = false; check(!can(), "사용 권한 미연결 차단"); rabbit.HasContextPermission = true;
            rabbit.Implemented = false; check(!can(), "플레이 미구현 동물 차단"); rabbit.Implemented = true;
            request.ActionId = " "; check(!dispatch(), "ActionId 공백 차단"); request.ActionId = "EDITOR_ACTION_ONLY";
            request.ContextId = "OTHER"; check(!dispatch(), "다른 스테이지 요청 차단"); request.ContextId = stage.StageId;
            request.PolicyRevision = "OTHER"; check(!dispatch(), "다른 정책 버전 요청 차단"); request.PolicyRevision = stage.Requirements.PolicyRevision;
            request.SnapshotRevision = "OTHER"; check(!dispatch(), "오래된 스냅샷 요청 차단"); request.SnapshotRevision = snapshot.Revision;
            request.Loadout.Ground = "Wolf"; check(!dispatch(), "지정 편성과 다른 payload 차단"); request.Loadout.Ground = "Rabbit";
            var copied = request.Clone(); copied.Loadout.Ground = "Wolf"; copied.ActionId = "OTHER";
            check(request.Loadout.Ground == "Rabbit" && request.ActionId == "EDITOR_ACTION_ONLY", "재조회에 전달한 복사가 저장 요청을 변경하지 않음");
            var stageCopy = stage.Clone(); stageCopy.FixedLoadout.Ground = "Wolf"; stageCopy.Requirements.RequiredRoles[0] = AnimalRole.Air;
            check(stage.FixedLoadout.Ground == "Rabbit" && stage.Requirements.RequiredRoles[0] == AnimalRole.Ground, "stage 입력 깊은 복사");
            var result = new AnimalUiCommitResult { Status = CommitStatus.Accepted, AcceptanceToken = "EDITOR_RECEIPT_ONLY",
                AcceptedContextId = stage.StageId, AcceptedPolicyRevision = stage.Requirements.PolicyRevision, AcceptedLoadout = loadout.Clone() };
            Func<bool> accepted = () => AnimalUiRules.IsAcceptedFixedStageResult(catalog, snapshot, stage, request, result, out _);
            check(accepted(), "명시적 승인 문맥/정책/편성 receipt 허용");
            foreach (var status in new[] { CommitStatus.Rejected, CommitStatus.Unavailable, CommitStatus.Unknown })
            { result.Status = status; check(!accepted(), status + "는 입장 승인 아님"); }
            result.Status = CommitStatus.Accepted;
            result.AcceptanceToken = " "; check(!accepted(), "승인 토큰 공백 재확인"); result.AcceptanceToken = "EDITOR_RECEIPT_ONLY";
            result.AcceptedContextId = "OTHER"; check(!accepted(), "다른 스테이지 승인 재확인"); result.AcceptedContextId = stage.StageId;
            result.AcceptedPolicyRevision = "OTHER"; check(!accepted(), "다른 정책 승인 재확인"); result.AcceptedPolicyRevision = stage.Requirements.PolicyRevision;
            result.AcceptedLoadout.Ground = "Wolf"; check(!accepted(), "다른 승인 편성 재확인"); result.AcceptedLoadout.Ground = "Rabbit";
            result.AcceptedLoadout = null; check(!accepted(), "승인 편성 누락 재확인"); result.AcceptedLoadout = loadout.Clone();
            result.Snapshot = new AnimalUiSnapshot(); check(!accepted(), "승인 스냅샷 연결 누락 재확인"); result.Snapshot = null;
            rabbit.CanUseInContext = false; check(!accepted(), "승인 편성 권한 없음 재확인"); rabbit.CanUseInContext = true;
            var fixture = AnimalStagePhase2DemoHost.CreateReadonlyFixture();
            check(fixture.StageId == "PREVIEW_ONLY" && fixture.Policy == CampaignSelectionPolicy.Fixed && fixture.FixedLoadout.Fingerprint() == loadout.Fingerprint(), "데모는 명시된 읽기 전용 예시 Fixed 편성");
            check(!AnimalUiRules.CanConfirmFixedStage(catalog, new AnimalUiSnapshot(), fixture.FixedLoadout, fixture, false, false, out _), "예시만으로 시작 권한을 생성하지 않음");
            return checks;
        }

        internal static int RunUpgradeContractChecks(AnimalCatalog catalog)
        {
            int checks = 0;
            Action<bool, string> check = (condition, label) => { Assert(condition, label); checks++; };
            check(catalog.IsValid(out _), "카탈로그 유효성"); check(catalog.Animals.Count == 15, "15종 초상");
            check(Enum.GetValues(typeof(AnimalRole)).Cast<AnimalRole>().All(role => catalog.ForRole(role).Count() == 5), "역할별 5종");
            var snapshot = new AnimalUiSnapshot();
            foreach (var animal in catalog.Animals) snapshot.Animals.Add(new AnimalProgress
            { AnimalId = animal.Id, Implemented = true, Unlocked = true, ActiveLevel = 0, PassiveLevel = 0 });
            var quote = new UpgradeQuote { AnimalId = "Rabbit", Track = UpgradeTrack.Active, State = UpgradeQuoteState.Ready,
                CurrentLevel = 0, NextLevel = 1, CurrentEffect = "검사 전용 현재 효과", NextEffect = "검사 전용 다음 효과", QuoteToken = "EDITOR_CHECK_ONLY" };
            // Synthetic values below test contract boundaries only; they never populate game assets/services.
            quote.Costs.Add(new UpgradeCost { CurrencyId = "EDITOR_CHECK_CURRENCY", Amount = 0 });
            Func<bool> can = () => AnimalUiRules.CanPurchaseUpgrade(catalog, snapshot, quote, true, true, out _);
            check(can(), "유효한 Ready 견적 허용");
            check(!AnimalUiRules.CanPurchaseUpgrade(catalog, snapshot, quote, false, true, out _), "backend 미연결 구매 차단");
            check(!AnimalUiRules.CanPurchaseUpgrade(catalog, snapshot, quote, true, false, out _), "스냅샷 미검증 구매 차단");
            quote.CurrentEffect = " "; check(!can(), "현재 효과 공백 구매 차단"); quote.CurrentEffect = "검사 전용 현재 효과";
            quote.NextEffect = null; check(!can(), "다음 효과 누락 구매 차단"); quote.NextEffect = "검사 전용 다음 효과";
            quote.QuoteToken = " "; check(!can(), "견적 토큰 공백 구매 차단"); quote.QuoteToken = "EDITOR_CHECK_ONLY";
            quote.NextLevel = 0; check(!can(), "다음 레벨 증가 누락 차단"); quote.NextLevel = 1;
            var cost = quote.Costs[0]; quote.Costs[0] = null; check(!can(), "null 비용 항목 차단"); quote.Costs[0] = cost;
            cost.CurrencyId = " "; check(!can(), "재화 ID 공백 차단"); cost.CurrencyId = "EDITOR_CHECK_CURRENCY";
            cost.Amount = -1; check(!can(), "음수 비용 차단"); cost.Amount = 0;
            quote.Costs.Clear(); check(!can(), "명시적 비용 또는 무료 설정 누락 차단");
            quote.IsFree = true; check(can(), "명시적 무료 강화 허용");
            quote.Costs.Add(null); check(!can(), "무료여도 잘못된 비용 항목 차단"); quote.Costs.Clear();
            var rabbit = snapshot.Find("Rabbit"); rabbit.Unlocked = false; check(!can(), "잠긴 동물 구매 차단"); rabbit.Unlocked = true;
            rabbit.Implemented = false; check(!can(), "미구현 동물 구매 차단"); rabbit.Implemented = true;
            rabbit.ActiveLevel = 1; check(!can(), "스냅샷/견적 현재 레벨 불일치 차단"); rabbit.ActiveLevel = 0;
            quote.Track = (UpgradeTrack)999; check(!can(), "알 수 없는 트랙 차단"); quote.Track = UpgradeTrack.Passive;
            check(can(), "패시브 견적과 레벨 연결 허용");
            foreach (UpgradeQuoteState state in Enum.GetValues(typeof(UpgradeQuoteState)))
                if (state != UpgradeQuoteState.Ready) { quote.State = state; check(!can(), state + " 구매 차단"); }
            return checks;
        }
        internal static int RunContractChecks(AnimalCatalog catalog)
        {
            int checks = 0;
            Action<bool, string> check = (condition, label) => { Assert(condition, label); checks++; };
            check(catalog.IsValid(out _), "카탈로그 유효성"); check(catalog.Animals.Count == 15, "15종 초상");
            check(Enum.GetValues(typeof(AnimalRole)).Cast<AnimalRole>().All(role => catalog.ForRole(role).Count() == 5), "역할별 5종");
            var snapshot = new AnimalUiSnapshot();
            foreach (var animal in catalog.Animals) snapshot.Animals.Add(new AnimalProgress { AnimalId = animal.Id, Implemented = true, Unlocked = true, HasContextPermission = true, CanUseInContext = true });
            var required = new SelectionRequirements { RequiredRoles = new[] { AnimalRole.Ground, AnimalRole.Special, AnimalRole.Air }, RepresentativeRole = AnimalRole.Ground };
            var stage = new StageSelectionRequest { StageId = "VALIDATION", Policy = CampaignSelectionPolicy.Free, Requirements = required };
            var loadout = new AnimalLoadout { Ground = "Rabbit", Special = "DreamFox", Air = "Swallow" };
            check(AnimalUiRules.ValidateLoadout(catalog, snapshot, loadout, AnimalUiMode.StageAnimalSelect, stage, null, out _), "Free 편성 허용");
            var rabbit = snapshot.Find("Rabbit"); rabbit.Unlocked = false;
            check(AnimalUiRules.ValidateLoadout(catalog, snapshot, loadout, AnimalUiMode.StageAnimalSelect, stage, null, out _), "계정 잠김이어도 실제 stage grant 허용");
            check(!AnimalUiRules.IsSelectable(catalog.Find("Rabbit"), snapshot, AnimalUiMode.CharacterUpgrade, null, null, out _), "잠긴 동물 구매 차단");
            rabbit.CanUseInContext = false;
            check(!AnimalUiRules.ValidateLoadout(catalog, snapshot, loadout, AnimalUiMode.StageAnimalSelect, stage, null, out _), "사용 권한 거절");
            rabbit.CanUseInContext = true; rabbit.HasContextPermission = false;
            check(!AnimalUiRules.ValidateLoadout(catalog, snapshot, loadout, AnimalUiMode.StageAnimalSelect, stage, null, out _), "미연결 권한 실행 차단");
            rabbit.HasContextPermission = true; rabbit.Implemented = false;
            check(!AnimalUiRules.ValidateLoadout(catalog, snapshot, loadout, AnimalUiMode.StageAnimalSelect, stage, null, out _), "구현되지 않은 동물 실행 차단");
            rabbit.Implemented = true; stage.Policy = CampaignSelectionPolicy.Fixed; stage.FixedLoadout = loadout.Clone();
            check(AnimalUiRules.ValidateLoadout(catalog, snapshot, loadout, AnimalUiMode.StageAnimalSelect, stage, null, out _), "Fixed 지정 동물 허용");
            check(!AnimalUiRules.IsSelectable(catalog.Find("Wolf"), snapshot, AnimalUiMode.StageAnimalSelect, stage, null, out _), "Fixed 다른 동물 차단");
            stage.Policy = CampaignSelectionPolicy.AllowedPool; stage.AllowedAnimalIds = new[] { "Rabbit", "DreamFox", "Swallow" };
            check(AnimalUiRules.ValidateLoadout(catalog, snapshot, loadout, AnimalUiMode.StageAnimalSelect, stage, null, out _), "AllowedPool 승인 편성");
            check(!AnimalUiRules.IsSelectable(catalog.Find("Wolf"), snapshot, AnimalUiMode.StageAnimalSelect, stage, null, out _), "AllowedPool 목록 밖 동물 차단");
            stage.AllowedAnimalIds = null;
            check(!AnimalUiRules.ValidateLoadout(catalog, snapshot, loadout, AnimalUiMode.StageAnimalSelect, stage, null, out _), "누락된 AllowedPool fail closed");
            stage.Policy = (CampaignSelectionPolicy)999;
            check(!AnimalUiRules.ValidateLoadout(catalog, snapshot, loadout, AnimalUiMode.StageAnimalSelect, stage, null, out _), "알 수 없는 스테이지 정책 차단");
            stage.Policy = CampaignSelectionPolicy.Free; stage.Requirements = new SelectionRequirements();
            check(!AnimalUiRules.ValidateLoadout(catalog, snapshot, loadout, AnimalUiMode.StageAnimalSelect, stage, null, out _), "필수 슬롯 누락 차단");
            stage.Requirements = new SelectionRequirements { RequiredRoles = new[] { AnimalRole.Ground, AnimalRole.Ground } };
            check(!AnimalUiRules.ValidateLoadout(catalog, snapshot, loadout, AnimalUiMode.StageAnimalSelect, stage, null, out _), "중복 역할 차단");
            var single = new SelectionRequirements { RequiredRoles = new[] { AnimalRole.Air }, RepresentativeRole = AnimalRole.Air };
            var multiplayer = new MultiplayerSelectionRequest { ModeId = "VALIDATION", Requirements = single };
            var airOnly = AnimalUiRules.NormalizeLoadout(loadout, single);
            check(airOnly.Ground == null && airOnly.Special == null && airOnly.Air == "Swallow", "비필수 슬롯 제거");
            check(AnimalUiRules.ValidateLoadout(catalog, snapshot, airOnly, AnimalUiMode.MultiplayerAnimalSelect, null, multiplayer, out _), "단일 캐릭터 모드에서 세 역할 강요 안 함");
            airOnly.Air = "Rabbit";
            check(!AnimalUiRules.ValidateLoadout(catalog, snapshot, airOnly, AnimalUiMode.MultiplayerAnimalSelect, null, multiplayer, out _), "잘못된 역할 차단");
            multiplayer.AllowedAnimalIds = Array.Empty<string>(); airOnly.Air = "Swallow";
            check(!AnimalUiRules.ValidateLoadout(catalog, snapshot, airOnly, AnimalUiMode.MultiplayerAnimalSelect, null, multiplayer, out _), "빈 모드 허용 목록 차단");
            var original = new StageSelectionRequest { InitialLoadout = loadout, Requirements = required };
            var clone = original.Clone(); clone.InitialLoadout.Ground = "Wolf"; clone.Requirements.RequiredRoles[0] = AnimalRole.Air;
            check(original.InitialLoadout.Ground == "Rabbit" && original.Requirements.RequiredRoles[0] == AnimalRole.Ground, "초안/진입 데이터 복사 분리");
            check(new AnimalUiCommitResult().Status != CommitStatus.Accepted, "빈 결과를 승인으로 처리하지 않음");
            return checks + RunUpgradeContractChecks(catalog);
        }
        private static void Assert(bool condition, string message) { if (!condition) throw new InvalidOperationException("Animal UI v2 검증 실패: " + message); }
    }
}
