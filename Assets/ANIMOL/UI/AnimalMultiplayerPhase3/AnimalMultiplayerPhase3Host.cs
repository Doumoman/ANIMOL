using System;
using ANIMOL.AnimalUiV2;
using ANIMOL.UI;
using ANIMOL.Typography;
using UnityEngine;

namespace ANIMOL.AnimalMultiplayerPhase3
{
    // Owns only the local window. It does not create/join rooms or modify a saved loadout.
    public sealed class AnimalMultiplayerPhase3Host : MonoBehaviour
    {
        public const string ScreenId = "AnimalMultiplayerPhase3";
        public const string ResourcePath = "ANIMOLAnimalMultiplayerPhase3/MultiplayerAnimalSelect";
        public AnimalUiPresenter Presenter;
        public AnimalMultiplayerIdMap IdMap;
        public MultiplayerSelectionRequest Context => context?.Clone();
        public AnimalLoadout LastReturnedLoadout { get; private set; }
        public event Action<AnimalLoadout> Returned;
        private MultiplayerSelectionRequest context;
        private UiNavigationService navigation;
        private string returnScreen;

        private void Awake()
        {
            navigation = GetComponentInParent<UiNavigationService>();
            var bridge = Presenter.View.ModalBody.GetComponent<PixelTextBridge>();
            if (bridge != null) bridge.SizeScrollContentToText = true;
        }
        private void OnEnable() => Presenter.Cancelled += Return;
        private void OnDisable() => Presenter.Cancelled -= Return;

        // Future verified host contexts pass through unchanged, including the opaque EntryIntent
        // and the distinction between null and empty AllowedAnimalIds. No protocol is invented here.
        public bool Open(MultiplayerSelectionRequest request)
        {
            if (navigation == null || Presenter.IsCommitting || Presenter.IsMultiplayerModalOpen || request == null ||
                AnimalUiRules.IsReadonlyMultiplayerFixture(request)) return false;
            context = request.Clone();
            if (navigation.CurrentScreenId != ScreenId) returnScreen = navigation.CurrentScreenId;
            if (!navigation.Navigate(ScreenId)) return false;
            return Presenter.OpenMultiplayer(context);
        }
        public bool OpenUnconfigured()
        {
            // The present MultiplayerUiPresenter supplies no operational mode catalogue, intent,
            // saved species roster, representative role or policy revision. Unknowns stay unknown.
            return Open(new MultiplayerSelectionRequest
            {
                DisplayName = "모드·진입 의도 설정 대기",
                InitialLoadout = new AnimalLoadout(), AllowedAnimalIds = null,
                Requirements = new SelectionRequirements
                {
                    RequiredRoles = new[] { AnimalRole.Ground, AnimalRole.Special, AnimalRole.Air },
                    RepresentativeRole = (AnimalRole)(-1), PolicyRevision = null
                }
            });
        }
        private void Return(AnimalUiMode mode, AnimalLoadout original)
        {
            if (mode != AnimalUiMode.MultiplayerAnimalSelect) return;
            LastReturnedLoadout = original.Clone(); Returned?.Invoke(original.Clone());
            if (!navigation.Back() && !string.IsNullOrWhiteSpace(returnScreen)) navigation.Navigate(returnScreen, false);
        }
    }
}
