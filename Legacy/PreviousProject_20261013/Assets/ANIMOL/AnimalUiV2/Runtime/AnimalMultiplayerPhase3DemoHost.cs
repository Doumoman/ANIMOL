using System.Collections;
using UnityEngine;

namespace ANIMOL.AnimalUiV2
{
    /// <summary>Readonly art/layout fixture. No room-entry service, matchmaking or ready grants.</summary>
    public sealed class AnimalMultiplayerPhase3DemoHost : MonoBehaviour
    {
        public AnimalUiPresenter Screen;
        public static MultiplayerSelectionRequest CreateReadonlyFixture() => new MultiplayerSelectionRequest
        {
            ModeId = "PREVIEW_ONLY", EntryIntent = "PREVIEW_ONLY", DisplayName = "멀티플레이 편성 예시 · 실제 데이터 연결 전",
            InitialLoadout = new AnimalLoadout { Ground = "Rabbit", Special = "DreamFox", Air = "Owl" }, AllowedAnimalIds = null,
            Requirements = new SelectionRequirements { RequiredRoles = new[] { AnimalRole.Ground, AnimalRole.Special, AnimalRole.Air },
                RepresentativeRole = AnimalRole.Ground, PolicyRevision = "PREVIEW_ONLY" }
        };
        private void Start()
        {
            if (Screen == null || Screen.Backend != null) return;
            Screen.Cancelled += OnCancelled; OpenReadonly();
        }
        private void OnDestroy() { if (Screen != null) Screen.Cancelled -= OnCancelled; }
        private void OnCancelled(AnimalUiMode mode, AnimalLoadout loadout)
        { if (Screen != null && Screen.Backend == null) StartCoroutine(ReopenReadonly()); }
        private IEnumerator ReopenReadonly() { yield return null; OpenReadonly(); }
        private void OpenReadonly()
        {
            if (Screen == null || Screen.Backend != null) return;
            if (Screen.OpenMultiplayer(CreateReadonlyFixture()))
                Screen.View.Status.text = "15종 정보 열람 · 실제 모드 권한과 입장 서비스 연결 전";
        }
    }
}
