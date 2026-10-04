using System;
using System.IO;
using System.Linq;
using ANIMOL.AnimalUiV2;
using ANIMOL.Core;
using ANIMOL.Typography;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace ANIMOL.AnimalStagePhase2.Editor
{
    public static class AnimalStagePhase2Builder
    {
        public const string Root = "Assets/ANIMOL/UI/AnimalStagePhase2";
        public const string PrefabPath = Root + "/Resources/ANIMOLAnimalStagePhase2/StageAnimalSelect.prefab";
        public const string CatalogPath = Root + "/AnimalCatalog.asset";
        public const string MapPath = Root + "/AnimalStageIdMap.asset";

        [MenuItem("ANIMOL/Animal UI v2/Create missing production Stage Phase 2 assets")]
        public static void CreateMissingProductionAssets()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Stop Play first.");
            const string generated = "Assets/ANIMOL/AnimalUiV2/Generated/";
            if (AssetDatabase.LoadAssetAtPath<GameObject>(generated + "StageAnimalSelect.prefab") == null)
                throw new InvalidOperationException("Run ANIMOL > Animal UI v2 > Build Stage Phase 2 first.");
            Directory.CreateDirectory(Path.GetDirectoryName(PrefabPath));
            AssetDatabase.Refresh();
            if (AssetDatabase.LoadAssetAtPath<AnimalCatalog>(CatalogPath) == null)
                AssetDatabase.CopyAsset(generated + "AnimalCatalogV2.asset", CatalogPath);
            var catalog = AssetDatabase.LoadAssetAtPath<AnimalCatalog>(CatalogPath);
            if (AssetDatabase.LoadAssetAtPath<AnimalStageIdMap>(MapPath) == null)
            {
                var map = ScriptableObject.CreateInstance<AnimalStageIdMap>();
                map.Entries = catalog.Animals.Select(a => new AnimalStageIdMap.Entry { ArtId = a.Id }).ToArray();
                // The only verified named campaign animal. DEV form IDs are not species IDs.
                map.Entries.Single(e => e.ArtId == "Rabbit").CampaignAnimal =
                    AssetDatabase.LoadAssetAtPath<CampaignAnimalDefinition>("Assets/ANIMOL/Data/Campaign/Animals/RABBIT.asset");
                AssetDatabase.CreateAsset(map, MapPath);
            }
            // Never regenerate project overrides or an operational backend from a demo build.
            if (AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath) != null) return;
            var root = PrefabUtility.LoadPrefabContents(generated + "StageAnimalSelect.prefab");
            try
            {
                UnityEngine.Object.DestroyImmediate(root.GetComponent<GraphicRaycaster>());
                UnityEngine.Object.DestroyImmediate(root.GetComponent<CanvasScaler>());
                UnityEngine.Object.DestroyImmediate(root.GetComponent<Canvas>());
                UnityEngine.Object.DestroyImmediate(root.GetComponentInChildren<AnimalUiSafeArea>());
                Stretch((RectTransform)root.transform);
                Stretch((RectTransform)root.transform.Find("SafeArea"));
                root.AddComponent<AnimalStageWidthFit>();
                var presenter = root.GetComponent<AnimalUiPresenter>();
                presenter.Catalog = catalog;
                presenter.Backend = null;
                presenter.OpenReadonlyPreviewOnStart = false;
                var host = root.AddComponent<AnimalStagePhase2Host>();
                host.Presenter = presenter;
                host.IdMap = AssetDatabase.LoadAssetAtPath<AnimalStageIdMap>(MapPath);
                presenter.View.CampaignBackground = null;
                presenter.View.Background.sprite = null;
                PrepareRoster(root);
                var profile = AssetDatabase.LoadAssetAtPath<PixelTypographyProfile>("Assets/ANIMOL/Typography/PixelTypographyProfile.asset");
                if (profile == null) throw new InvalidOperationException("Existing Korean typography profile missing.");
                foreach (var text in root.GetComponentsInChildren<Text>(true))
                    (text.GetComponent<PixelTextBridge>() ?? text.gameObject.AddComponent<PixelTextBridge>()).Profile = profile;
                PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            }
            finally { PrefabUtility.UnloadPrefabContents(root); }
            Debug.Log("Animal Stage Phase 2 production assets created. Existing operational assets are preserved on rerun.");
        }

        private static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one;
            rect.offsetMin = rect.offsetMax = Vector2.zero; rect.localScale = Vector3.one;
        }

        public static void PrepareRoster(GameObject root)
        {
            var view = root.GetComponent<AnimalUiPresenter>().View;
            int count = view.RosterContent.GetComponentsInChildren<AnimalCardView>(true).Length;
            for (int i = count; i < 5; i++)
            {
                var card = UnityEngine.Object.Instantiate(view.CardTemplate, view.RosterContent);
                card.name = "RosterCard" + i;
                card.gameObject.SetActive(true);
            }
        }
    }
}
