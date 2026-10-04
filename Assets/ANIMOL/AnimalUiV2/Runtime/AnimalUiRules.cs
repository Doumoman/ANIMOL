using System;

namespace ANIMOL.AnimalUiV2
{
    public static class AnimalUiRules
    {
        // Used by the purchase button, confirmation and commit. The backend still owns charging.
        public static bool CanPurchaseUpgrade(AnimalCatalog catalog, AnimalUiSnapshot snapshot, UpgradeQuote quote,
            bool backendReady, bool snapshotValid, out string reason)
        {
            if (!backendReady || !snapshotValid || catalog == null || snapshot == null)
            { reason = "성장 데이터 연결 전"; return false; }
            if (quote == null || quote.State != UpgradeQuoteState.Ready)
            { reason = "현재 강화할 수 없습니다."; return false; }
            if (!IsSelectable(catalog.Find(quote.AnimalId), snapshot, AnimalUiMode.CharacterUpgrade, null, null, out reason)) return false;
            if (!Enum.IsDefined(typeof(UpgradeTrack), quote.Track) || string.IsNullOrWhiteSpace(quote.QuoteToken) ||
                quote.CurrentLevel < 0 || quote.NextLevel <= quote.CurrentLevel)
            { reason = "강화 레벨 또는 견적 연결 전"; return false; }
            var progress = snapshot.Find(quote.AnimalId);
            int current = quote.Track == UpgradeTrack.Active ? progress.ActiveLevel : progress.PassiveLevel;
            if (current != quote.CurrentLevel)
            { reason = "최신 강화 정보를 다시 확인해주세요."; return false; }
            if (string.IsNullOrWhiteSpace(quote.CurrentEffect) || string.IsNullOrWhiteSpace(quote.NextEffect))
            { reason = "현재/다음 효과 연결 전"; return false; }
            if (quote.Costs == null || (!quote.IsFree && quote.Costs.Count == 0))
            { reason = "강화 비용 연결 전"; return false; }
            foreach (var cost in quote.Costs)
                if (cost == null || cost.Amount < 0 || string.IsNullOrWhiteSpace(cost.CurrencyId))
                { reason = "강화 비용 설정을 확인해주세요."; return false; }
            reason = null; return true;
        }

        public static string RoleName(AnimalRole role) => role == AnimalRole.Ground ? "지상" : role == AnimalRole.Special ? "특수" : "공중";
        public static bool Contains(string[] ids, string id)
        {
            if (ids == null) return true;
            return Array.IndexOf(ids, id) >= 0;
        }
        public static AnimalLoadout NormalizeLoadout(AnimalLoadout loadout, SelectionRequirements requirements)
        {
            var normalized = new AnimalLoadout();
            if (loadout == null || requirements?.RequiredRoles == null) return normalized;
            foreach (var role in requirements.RequiredRoles)
                if (Enum.IsDefined(typeof(AnimalRole), role)) normalized.Set(role, loadout.Get(role));
            return normalized;
        }
        public static bool IsSelectable(AnimalDefinition animal, AnimalUiSnapshot snapshot, AnimalUiMode mode,
            StageSelectionRequest stage, MultiplayerSelectionRequest multiplayer, out string reason)
        {
            if (animal == null) { reason = "동물 정보를 찾을 수 없습니다."; return false; }
            var state = snapshot?.Find(animal.Id);
            if (state == null) { reason = "동물 데이터 연결 전"; return false; }
            if (!state.Implemented) { reason = "플레이 데이터 준비 중"; return false; }
            if (mode != AnimalUiMode.CharacterUpgrade)
            {
                if (!state.HasContextPermission) { reason = "이 화면의 사용 권한 연결 전"; return false; }
                if (!state.CanUseInContext) { reason = string.IsNullOrEmpty(state.AvailabilityMessage) ? "현재 선택할 수 없습니다." : state.AvailabilityMessage; return false; }
            }
            else if (!state.Unlocked) { reason = string.IsNullOrEmpty(state.AvailabilityMessage) ? "잠긴 동물" : state.AvailabilityMessage; return false; }
            if (mode == AnimalUiMode.StageAnimalSelect)
            {
                if (stage == null || string.IsNullOrEmpty(stage.StageId)) { reason = "스테이지 연결 전"; return false; }
                if (stage.Requirements == null || !stage.Requirements.Requires(animal.Role)) { reason = "이 스테이지에서 요구하지 않는 역할입니다."; return false; }
                if (stage.Policy == CampaignSelectionPolicy.Fixed && stage.FixedLoadout?.Get(animal.Role) != animal.Id)
                { reason = "이 스테이지의 지정 동물이 아닙니다."; return false; }
                if (stage.Policy == CampaignSelectionPolicy.AllowedPool && (stage.AllowedAnimalIds == null || !Contains(stage.AllowedAnimalIds, animal.Id)))
                { reason = "이 스테이지에서 사용할 수 없습니다."; return false; }
                if (!Enum.IsDefined(typeof(CampaignSelectionPolicy), stage.Policy)) { reason = "스테이지 정책 연결 전"; return false; }
            }
            if (mode == AnimalUiMode.MultiplayerAnimalSelect)
            {
                if (multiplayer == null || string.IsNullOrEmpty(multiplayer.ModeId)) { reason = "멀티플레이 모드 연결 전"; return false; }
                if (multiplayer.Requirements == null || !multiplayer.Requirements.Requires(animal.Role)) { reason = "이 모드에서 요구하지 않는 역할입니다."; return false; }
                if (!Contains(multiplayer.AllowedAnimalIds, animal.Id)) { reason = "이 모드에서 사용할 수 없습니다."; return false; }
            }
            reason = null;
            return true;
        }
        public static bool ValidateLoadout(AnimalCatalog catalog, AnimalUiSnapshot snapshot, AnimalLoadout loadout,
            AnimalUiMode mode, StageSelectionRequest stage, MultiplayerSelectionRequest multiplayer, out string reason)
        {
            if (catalog == null || loadout == null) { reason = "편성 정보를 연결해주세요."; return false; }
            var requirements = mode == AnimalUiMode.StageAnimalSelect ? stage?.Requirements : multiplayer?.Requirements;
            if (requirements?.RequiredRoles == null || requirements.RequiredRoles.Length == 0)
            { reason = "필수 선택 슬롯이 연결되지 않았습니다."; return false; }
            if (!Enum.IsDefined(typeof(AnimalRole), requirements.RepresentativeRole) || !requirements.Requires(requirements.RepresentativeRole))
            { reason = "시작/대표 동물 역할을 확인해주세요."; return false; }
            var roles = new System.Collections.Generic.HashSet<AnimalRole>();
            foreach (var role in requirements.RequiredRoles)
            {
                if (!Enum.IsDefined(typeof(AnimalRole), role) || !roles.Add(role)) { reason = "선택 슬롯 설정을 확인해주세요."; return false; }
                var animal = catalog.Find(loadout.Get(role));
                if (animal == null || animal.Role != role) { reason = RoleName(role) + " 동물을 선택해주세요."; return false; }
                if (!IsSelectable(animal, snapshot, mode, stage, multiplayer, out reason))
                { reason = RoleName(role) + ": " + reason; return false; }
            }
            reason = null;
            return true;
        }
    }
}
