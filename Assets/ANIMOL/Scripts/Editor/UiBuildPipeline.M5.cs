using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ANIMOL.Core;
using ANIMOL.Gameplay;
using ANIMOL.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace ANIMOL.Editor
{
    public static partial class UiBuildPipeline
    {
        private const string M5LogName = "animol-m5-validation.txt";

        [MenuItem("ANIMOL/Build/M5 Acceptance Validation")]
        public static void RunM5AcceptanceValidation() => ValidateM5();

        static partial void BuildM5()
        {
            ValidateM5();
        }

        static partial void ValidateM5()
        {
            var errors = new List<string>();
            var notes = new List<string>();
            var catalog = AssetDatabase.LoadAssetAtPath<CampaignCatalog>(CampaignCatalogGenerator.CatalogPath);
            var catalogReport = CampaignCatalogValidator.Validate(catalog);
            if (!catalogReport.IsValid || catalogReport.ThemeCount != 5 || catalogReport.StageCount != 100 || catalogReport.DuplicateStageIdCount != 0)
                errors.Add("A1 campaign catalog is not exactly 5x20 with zero duplicate IDs.");

            var campaignScreenPaths = new[] { "SC02_ThemeSelect", "SC03_StageSelect", "SC04_StageDetail" };
            var campaignAnimalButtons = 0;
            foreach (var screen in campaignScreenPaths)
            {
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{UiPrefabFolder}/{screen}.prefab");
                if (prefab == null)
                {
                    errors.Add("Missing campaign screen " + screen);
                    continue;
                }
                campaignAnimalButtons += prefab.GetComponentsInChildren<Button>(true).Count(button =>
                    button.name.IndexOf("Animal", StringComparison.OrdinalIgnoreCase) >= 0 ||
                    button.name.IndexOf("Loadout", StringComparison.OrdinalIgnoreCase) >= 0);
            }
            if (campaignAnimalButtons != 0) errors.Add($"A2 campaign selection entry points={campaignAnimalButtons}.");

            var progression = new CampaignProgressionService(catalog);
            if (progression.GetNextStageId("T01-S20") != "T02-S01" || progression.GetNextStageId("T05-S20") != string.Empty)
                errors.Add("A4 campaign boundary rule mismatch.");
            var milestone = AssetDatabase.LoadAssetAtPath<GameObject>($"{UiPrefabFolder}/SC18_CampaignMilestone.prefab");
            var nextTheme = milestone == null ? null : milestone.GetComponentsInChildren<Button>(true).FirstOrDefault(x => x.name == "NextThemeButton");
            var a4Pass = progression.GetNextStageId("T01-S20") == "T02-S01" &&
                         string.IsNullOrEmpty(progression.GetNextStageId("T05-S20")) &&
                         nextTheme != null && !nextTheme.interactable;
            if (!a4Pass) errors.Add("A4 milestone boundary rule or default-disabled next button mismatch.");

            ValidateMetaContracts(errors);
            ValidateExternalServices(errors);
            ValidateScenesAndRoutes(errors, notes);

            var lines = new List<string>
            {
                "ANIMOL M5 acceptance validation",
                $"valid={errors.Count == 0}",
                $"unityVersion={Application.unityVersion}",
                "A1=" + (catalogReport.IsValid && catalogReport.ThemeCount == 5 && catalogReport.StageCount == 100 && catalogReport.DuplicateStageIdCount == 0 ? "PASS" : "FAIL"),
                $"catalogThemes={catalogReport.ThemeCount}",
                $"catalogStages={catalogReport.StageCount}",
                $"duplicateStageIds={catalogReport.DuplicateStageIdCount}",
                $"campaignAnimalSelectionEntryPoints={campaignAnimalButtons}",
                "A2=" + (campaignAnimalButtons == 0 ? "PASS" : "FAIL"),
                "A3=AUTOMATED_TEST_REQUIRED",
                "A4=" + (a4Pass ? "PASS" : "FAIL"),
                "A5=STATIC_PASS_PLAYMODE_REQUIRED",
                "A6=STATIC_PASS_EDITMODE_REQUIRED",
                "A7=STATIC_PASS_PLAYMODE_AND_VISUAL_REQUIRED",
                "screenAspect16x9=VISUAL_CAPTURE_REQUIRED",
                "screenAspect20x9=VISUAL_CAPTURE_REQUIRED",
                "physicalDeviceSafeArea=ENVIRONMENT_BLOCKED",
                "realServerPaymentAds=ENVIRONMENT_BLOCKED",
                "notes=" + string.Join(" | ", notes),
                "errors=" + string.Join(" | ", errors)
            };
            Directory.CreateDirectory(Path.Combine(Directory.GetCurrentDirectory(), "Logs"));
            File.WriteAllLines(Path.Combine(Directory.GetCurrentDirectory(), "Logs", M5LogName), lines);
            AssetDatabase.SaveAssets();
            if (errors.Count > 0) throw new InvalidOperationException(string.Join("\n", errors));
            Debug.Log("[ANIMOL][M5] Static acceptance validation completed. PlayMode and visual checks remain separate evidence steps.");
        }

        private static void ValidateMetaContracts(List<string> errors)
        {
            var upgrades = AssetDatabase.LoadAssetAtPath<AccountUpgradeCatalog>("Assets/ANIMOL/Data/Meta/AccountUpgradeCatalog.asset");
            if (upgrades == null || upgrades.Tracks.Count != 3 || upgrades.Tracks.Any(x => x.MaxLevel != 10 || x.PercentByLevel.Count != 11 || x.CostByLevel.Count != 10))
                errors.Add("A6 account upgrade tracks must be exactly three versioned Lv.0-10 tracks.");
            var emotes = AssetDatabase.LoadAssetAtPath<EmoteCatalog>("Assets/ANIMOL/Data/Meta/EmoteCatalog.asset");
            if (emotes == null || emotes.Emotes.Count(x => x.GrantedByDefault) != 3)
                errors.Add("A6 default free emote count must be three.");
            if (new EmoteLoadoutService(Array.Empty<string>()).Slots.Count != 4) errors.Add("A6 quick slot count must be four.");
            var characters = AssetDatabase.LoadAssetAtPath<CharacterUpgradeCatalog>("Assets/ANIMOL/Data/Meta/CharacterUpgradeCatalog.asset");
            if (characters == null || characters.Definitions.Count != 0)
                errors.Add("A6 unconfigured character upgrade definitions must remain unavailable.");
        }

        private static void ValidateExternalServices(List<string> errors)
        {
            var services = AssetDatabase.LoadAssetAtPath<ExternalServiceConfiguration>("Assets/ANIMOL/Data/Services/ExternalServiceConfiguration.asset");
            if (services == null || services.AccountServerConnected || services.MatchServerConnected || services.PurchaseSdkConnected || services.RewardedAdSdkConnected)
                errors.Add("A5/A6 disconnected services must not be presented as connected.");
            var competitive = AssetDatabase.LoadAssetAtPath<MultiplayerPreviewDefinition>("Assets/ANIMOL/Data/Development/DEV-MULTI-COMPETITIVE.asset");
            var coop = AssetDatabase.LoadAssetAtPath<MultiplayerPreviewDefinition>("Assets/ANIMOL/Data/Development/DEV-MULTI-COOP.asset");
            if (competitive == null || coop == null || !competitive.DevelopmentPreviewOnly || !coop.DevelopmentPreviewOnly)
                errors.Add("A5 multiplayer previews must remain DEV-only.");
        }

        private static void ValidateScenesAndRoutes(List<string> errors, List<string> notes)
        {
            var lobby = EditorSceneManager.OpenScene($"{SceneFolder}/Lobby.unity", OpenSceneMode.Single);
            var nav = UnityEngine.Object.FindFirstObjectByType<UiNavigationService>(FindObjectsInactive.Include);
            var campaignPresenter = UnityEngine.Object.FindFirstObjectByType<CampaignUiPresenter>(FindObjectsInactive.Include);
            var metaPresenter = UnityEngine.Object.FindFirstObjectByType<MetaUiPresenter>(FindObjectsInactive.Include);
            var multiPresenter = UnityEngine.Object.FindFirstObjectByType<MultiplayerUiPresenter>(FindObjectsInactive.Include);
            var safeArea = UnityEngine.Object.FindFirstObjectByType<SafeAreaLayout>(FindObjectsInactive.Include);
            var scaler = UnityEngine.Object.FindFirstObjectByType<CanvasScaler>(FindObjectsInactive.Include);
            if (!lobby.IsValid() || nav == null || campaignPresenter == null || metaPresenter == null || multiPresenter == null)
                errors.Add("A7 Lobby navigation presenter chain is incomplete.");
            if (safeArea == null || scaler == null || scaler.uiScaleMode != CanvasScaler.ScaleMode.ScaleWithScreenSize)
                errors.Add("A7 Safe Area or responsive CanvasScaler is missing.");

            var requiredScreens = new[]
            {
                "SC01_Lobby", "SC02_ThemeSelect", "SC03_StageSelect", "SC04_StageDetail", "SC09_MapLoading", "SC13_Settings", "SC14_Help", "SC18_CampaignMilestone",
                "SC10_UpgradeHub", "SC10A_AccountUpgrade", "SC10B_CharacterUpgrade", "SC11_Store", "SC12_EmoteCollection", "SC17_ProfileRecords",
                "SC05_CompetitiveHub", "SC08_CoopHub", "SC06_MatchRoom", "SC07_MultiplayerAnimalSelect", "HUD_Competitive", "HUD_Coop"
            };
            var host = nav == null ? null : nav.transform.Find("SafeArea/ScreenHost");
            var missingScreens = requiredScreens.Where(id => host == null || host.Find(id) == null).ToArray();
            if (missingScreens.Length > 0) errors.Add("A7 missing Lobby screens: " + string.Join(",", missingScreens));

            var requiredModals = new[] { "ConfirmExitModal", "RewardedAdModal", "ProductDetailModal", "ServiceErrorModal", "MultiplayerPauseModal" };
            var modalHost = nav == null ? null : nav.transform.Find("SafeArea/ModalHost");
            var missingModals = requiredModals.Where(id => modalHost == null || modalHost.Find(id) == null).ToArray();
            if (missingModals.Length > 0) errors.Add("A7 missing modals: " + string.Join(",", missingModals));

            var gameplay = EditorSceneManager.OpenScene($"{SceneFolder}/Gameplay.unity", OpenSceneMode.Single);
            var touch = UnityEngine.Object.FindObjectsByType<DevTouchControl>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            if (!gameplay.IsValid() || touch.Length != 4 || touch.Select(x => x.Action).Distinct().Count() != 4)
                errors.Add("A7 gameplay must expose four independent multitouch regions.");
            var result = EditorSceneManager.OpenScene($"{SceneFolder}/Results.unity", OpenSceneMode.Single);
            var resultPresenter = UnityEngine.Object.FindFirstObjectByType<DevResultsPresenter>(FindObjectsInactive.Include);
            if (!result.IsValid() || resultPresenter == null) errors.Add("A7 result return route presenter is missing.");
            EditorSceneManager.OpenScene($"{SceneFolder}/Lobby.unity", OpenSceneMode.Single);
            notes.Add($"lobbyScreens={requiredScreens.Length - missingScreens.Length}/{requiredScreens.Length}");
            notes.Add($"modals={requiredModals.Length - missingModals.Length}/{requiredModals.Length}");
            notes.Add($"multitouchRegions={touch.Length}");
        }
    }
}
