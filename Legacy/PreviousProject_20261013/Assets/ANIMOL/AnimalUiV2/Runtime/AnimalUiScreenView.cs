using UnityEngine;
using UnityEngine.UI;

namespace ANIMOL.AnimalUiV2
{
    public sealed class AnimalUiScreenView : MonoBehaviour
    {
        public CanvasGroup BodyInteraction;
        public Image Background;
        public Sprite GrowthBackground;
        public Sprite CampaignBackground;
        public Sprite MultiplayerBackground;
        public Text Title;
        public Text Subtitle;
        public Button Back;
        public Text Balances;
        public GameObject SelectionSlots;
        public AnimalCardView[] Slots;
        public Image HeroPortrait;
        public Text HeroName;
        public Text HeroRole;
        public Text HeroDescription;
        public Text HeroState;
        public Button SelectionDetailsButton;
        public Button[] RoleTabs;
        public Text[] RoleLabels;
        public RectTransform RosterContent;
        public GridLayoutGroup RosterLayout;
        public ScrollRect RosterScroll;
        public AnimalCardView CardTemplate;
        public AnimalUpgradeTrackView ActiveTrack;
        public AnimalUpgradeTrackView PassiveTrack;
        public GameObject UpgradeTracks;
        public Text SelectionInfo;
        public Text Status;
        public Button Refresh;
        public Button Primary;
        public Text PrimaryLabel;
        public ScrollRect BodyScroll;
        public RectTransform BodyContent;
        public RectTransform HeroPanel;
        public RectTransform TabsPanel;
        public RectTransform RosterPanel;
        public RectTransform TracksPanel;
        public RectTransform InfoPanel;
        public GameObject Modal;
        public Text ModalTitle;
        public Text ModalBody;
        public ScrollRect ModalBodyScroll;
        public Text ModalConfirmLabel;
        public Button ModalConfirm;
        public Button ModalCancel;
        public Text ModalCancelLabel;

        public void SetMode(AnimalUiMode mode)
        {
            bool upgrade = mode == AnimalUiMode.CharacterUpgrade;
            Background.sprite = upgrade ? GrowthBackground : mode == AnimalUiMode.StageAnimalSelect ? CampaignBackground : MultiplayerBackground;
            SelectionSlots.SetActive(!upgrade);
            UpgradeTracks.SetActive(upgrade);
            SelectionInfo.gameObject.SetActive(!upgrade);
            InfoPanel.gameObject.SetActive(!upgrade);
            Balances.gameObject.SetActive(upgrade);
            bool stage = mode == AnimalUiMode.StageAnimalSelect;
            PrimaryLabel.text = upgrade ? "강화 정보 새로고침" : stage ? "스테이지 시작" : "대기방으로";
            if (SelectionDetailsButton != null) SelectionDetailsButton.gameObject.SetActive(!upgrade);
            // Header and footer remain fixed inside Safe Area; the middle content scrolls on shorter screens.
            Top(TabsPanel, upgrade ? 0 : 940, 132);
            Top(RosterPanel, upgrade ? 156 : 1096, upgrade ? 284 : 592);
            Top(HeroPanel, upgrade ? 464 : 328, upgrade ? 420 : 588);
            Top(TracksPanel, 908, 744);
            Top(InfoPanel, 1712, 164);
            SelectionInfo.rectTransform.anchoredPosition = new Vector2(60, -42);
            SelectionInfo.rectTransform.sizeDelta = new Vector2(852, 80);
            BodyContent.sizeDelta = new Vector2(0, upgrade ? 1664 : 1888);
            var stateRect = HeroState.rectTransform;
            stateRect.anchoredPosition = new Vector2(upgrade ? 348 : 60, upgrade ? -292 : -448);
            stateRect.sizeDelta = new Vector2(upgrade ? 564 : 852, 68);
            RosterLayout.cellSize = upgrade ? new Vector2(180, 284) : new Vector2(308, 284);
            RosterLayout.spacing = upgrade ? new Vector2(18, 0) : new Vector2(24, 24);
            RosterLayout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            RosterLayout.constraintCount = upgrade ? 5 : 3;
            RosterContent.sizeDelta = new Vector2(972, upgrade ? 284 : 592);
            RosterScroll.horizontal = false; RosterScroll.vertical = false;
            // This roster never scrolls independently; route card drags to BodyScroll.
            RosterScroll.enabled = false;
            BodyScroll.verticalNormalizedPosition = 1;
        }
        private static void Top(RectTransform rect, float top, float height)
        {
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 1);
            rect.pivot = new Vector2(0.5f, 1);
            rect.anchoredPosition = new Vector2(0, -top); rect.sizeDelta = new Vector2(972, height);
        }
    }
}
