using System.Collections;
using UnityEngine;

namespace ANIMOL.AnimalUiV2
{
    /// <summary>Readonly layout fixture only. This is not an actual campaign policy or stage loader.</summary>
    public sealed class AnimalStagePhase2DemoHost : MonoBehaviour
    {
        public AnimalUiPresenter Screen;
        public static StageSelectionRequest CreateReadonlyFixture() => new StageSelectionRequest
        {
            StageId = "PREVIEW_ONLY", DisplayName = "2단계 미리보기 · 예시 지정 편성", Policy = CampaignSelectionPolicy.Fixed,
            FixedLoadout = new AnimalLoadout { Ground = "Rabbit", Special = "DreamFox", Air = "Swallow" },
            InitialLoadout = new AnimalLoadout(),
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
            if (Screen.OpenCampaign(CreateReadonlyFixture())) Screen.View.Status.text = "예시 편성 · 실제 스테이지 입장 서비스 연결 전";
        }
    }
}
