using ANIMOL.AnimalUiV2;
using ANIMOL.ProductionV1;
using ANIMOL.UI;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ANIMOL.AnimalStagePhase2
{
    [DefaultExecutionOrder(800)]
    public sealed class AnimalStagePhase2Host : MonoBehaviour
    {
        public const string ScreenId = "SC04_StageDetail";
        public const string ResourcePath = "ANIMOLAnimalStagePhase2/StageAnimalSelect";
        public AnimalUiPresenter Presenter;
        public AnimalStageIdMap IdMap;
        private UiNavigationService navigation;
        private ProductionController source;
        private StageSelectionRequest context;
        public StageSelectionRequest Context => context?.Clone();
        public string AccessPolicySummary { get; private set; }

        private void Awake()
        {
            var bridge = Presenter.View.ModalBody.GetComponent<ANIMOL.Typography.PixelTextBridge>();
            if (bridge != null) bridge.SizeScrollContentToText = true;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Reset() => SceneManager.sceneLoaded -= Install;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Register() { SceneManager.sceneLoaded -= Install; SceneManager.sceneLoaded += Install; }
        public static void Install(Scene scene, LoadSceneMode mode)
        {
            foreach (var root in scene.GetRootGameObjects())
            foreach (var nav in root.GetComponentsInChildren<UiNavigationService>(true))
                if (nav.GetComponent<ProductionController>() != null && nav.transform.Find("SafeArea/ScreenHost/" + ScreenId) != null &&
                    nav.GetComponent<AnimalStagePhase2Entry>() == null) nav.gameObject.AddComponent<AnimalStagePhase2Entry>();
        }
        private void OnEnable()
        {
            navigation = GetComponentInParent<UiNavigationService>();
            source = navigation == null ? null : navigation.GetComponent<ProductionController>();
            Presenter.Cancelled += ReturnToList;
            Presenter.CampaignRefreshRequested += OpenSelectedStage;
            OpenSelectedStage();
        }
        private void OnDisable()
        {
            Presenter.Cancelled -= ReturnToList;
            Presenter.CampaignRefreshRequested -= OpenSelectedStage;
        }
        public void OpenSelectedStage()
        {
            if (Presenter.IsCommitting) return;
            var stage = source == null ? null : source.SelectedStage;
            if (Presenter.Backend is AnimalStageProjectAdapter project)
            {
                project.StageCatalog = source?.Catalog?.Campaign;
                project.IdMap = IdMap;
                project.Catalog = Presenter.Catalog;
            }
            context = AnimalStageContext.Read(stage, IdMap, Presenter.Catalog);
            AccessPolicySummary = AnimalStageContext.AccessSummary(stage);
            // Existing ProductionCatalog art keys are keyed by the selected asset's ThemeId, not list position.
            var background = stage == null || source.Catalog == null ? null : source.Catalog.Find("BG_UI_Campaign_" + stage.ThemeId);
            Presenter.View.CampaignBackground = background;
            Presenter.View.Background.enabled = background != null;
            Presenter.OpenCampaign(context);
        }
        private void LateUpdate()
        {
            // This reports catalog access rules, not an account-specific admission grant.
            if (Presenter != null && (Presenter.Backend == null || Presenter.Backend is AnimalStageProjectAdapter))
                Presenter.View.SelectionInfo.text = AccessPolicySummary;
        }
        private void ReturnToList(AnimalUiMode mode, AnimalLoadout original)
        {
            if (navigation != null && !navigation.Back()) navigation.Navigate("SC03_StageSelect", false);
        }
    }
}
