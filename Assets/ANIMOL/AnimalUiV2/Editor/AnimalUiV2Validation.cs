using System;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace ANIMOL.AnimalUiV2.Editor
{
    public static class AnimalUiV2Validation
    {
        [MenuItem("ANIMOL/Animal UI v2/Validate Upgrade Phase 1")]
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

        [MenuItem("ANIMOL/Animal UI v2/Validate Generated")]
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
