using System;
using System.IO;
using ANIMOL.Core;
using ANIMOL.Gameplay;
using ANIMOL.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace ANIMOL.Editor
{
    public static class RunPrototypeBuilder
    {
        public const string Root = "Assets/ANIMOL/RunPrototype";
        public const string DownScene = Root + "/RunDown.unity";
        public const string UpScene = Root + "/RunUp.unity";

        public static void Build()
        {
            if (EditorApplication.isPlaying) throw new InvalidOperationException("Stop play mode first.");
            for (int i = 0; i < SceneManager.sceneCount; i++)
                if (SceneManager.GetSceneAt(i).isDirty) throw new InvalidOperationException("Preserve unsaved scene changes first.");
            Directory.CreateDirectory(Root); AssetDatabase.Refresh();
            BuildCase(false); BuildCase(true);
            AssetDatabase.SaveAssets();
            EditorSceneManager.OpenScene(DownScene);
            Debug.Log("[RunPrototype] Reserved 80+80 tiles before terrain. Existing maps, prefabs and build settings untouched.");
        }

        private static void BuildCase(bool up)
        {
            string id = up ? "Up" : "Down";
            var draft = RunRoutePlan.Reserve(up);
            var plan = AssetDatabase.LoadAssetAtPath<RunRoutePlan>(Root + "/" + id + "Plan.asset");
            if (plan == null) { plan = draft; AssetDatabase.CreateAsset(plan, Root + "/" + id + "Plan.asset"); }
            else { EditorUtility.CopySerialized(draft, plan); UnityEngine.Object.DestroyImmediate(draft); }
            var newMap = ScriptableObject.CreateInstance<StageMapDefinition>(); plan.PopulateEmptyMap(newMap);
            var map = AssetDatabase.LoadAssetAtPath<StageMapDefinition>(Root + "/" + id + "Map.asset");
            if (map == null) { map = newMap; AssetDatabase.CreateAsset(map, Root + "/" + id + "Map.asset"); }
            else { EditorUtility.CopySerialized(newMap, map); UnityEngine.Object.DestroyImmediate(newMap); }
            EditorUtility.SetDirty(plan); EditorUtility.SetDirty(map);
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var camera = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener), typeof(PortraitWorldCameraPolicy), typeof(RunPrototypeCamera)).GetComponent<Camera>();
            camera.tag = "MainCamera"; camera.orthographic = true; camera.backgroundColor = new Color32(14, 27, 43, 255);
            camera.clearFlags = CameraClearFlags.SolidColor; camera.transform.position = new Vector3(1, plan.Legs[0].FloorY + 3, -10);
            var ui = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/ANIMOL/Prefabs/UI/UI_CommonRoot.prefab"));
            var hud = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/ANIMOL/Prefabs/UI/SC15_GameplayHud.prefab"), ui.transform.Find("SafeArea/ScreenHost"));
            if (ui.GetComponentInChildren<DevMobileInputRouter>(true) == null) ui.AddComponent<DevMobileInputRouter>();
            var font = hud.GetComponentInChildren<Text>(true).font;
            var panel = new GameObject("Run probe header", typeof(RectTransform), typeof(Image));
            panel.transform.SetParent(ui.transform.Find("SafeArea"), false);
            var rect = panel.GetComponent<RectTransform>(); rect.anchorMin = new Vector2(0, 1); rect.anchorMax = new Vector2(1, 1);
            rect.pivot = new Vector2(.5f, 1); rect.sizeDelta = new Vector2(0, 320); rect.anchoredPosition = Vector2.zero;
            panel.GetComponent<Image>().color = new Color(.035f, .07f, .12f, .95f); panel.GetComponent<Image>().raycastTarget = false;
            var session = new GameObject("80 tile running prototype", typeof(RunPrototypeSession)).GetComponent<RunPrototypeSession>();
            session.Plan = plan; session.Map = map;
            session.PlayerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/ANIMOL/Prefabs/Gameplay/DevPlayer.prefab");
            session.Header = Text(panel.transform, "Title", 20, 60, 40, font);
            session.Readout = Text(panel.transform, "Metrics", 90, 132, 32, font);
            session.Instructions = Text(panel.transform, "Instructions", 235, 65, 23, font);
            camera.GetComponent<RunPrototypeCamera>().Session = session;
            new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
            new GameObject("Existing feedback policy", typeof(GameplayFeedbackDirector));
            EditorSceneManager.SaveScene(scene, up ? UpScene : DownScene);
        }

        private static Text Text(Transform parent, string name, float y, float height, int size, Font font)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Text)); go.transform.SetParent(parent, false);
            var rect = go.GetComponent<RectTransform>(); rect.anchorMin = new Vector2(0, 1); rect.anchorMax = new Vector2(1, 1);
            rect.pivot = new Vector2(.5f, 1); rect.anchoredPosition = new Vector2(0, -y); rect.sizeDelta = new Vector2(-80, height);
            var text = go.GetComponent<Text>(); text.font = font; text.fontSize = size; text.color = new Color32(205, 231, 216, 255);
            text.raycastTarget = false; text.alignment = TextAnchor.UpperLeft; return text;
        }

        public static void OpenDown() => Open(DownScene);
        public static void OpenUp() => Open(UpScene);
        private static void Open(string path)
        {
            if (EditorApplication.isPlaying) throw new InvalidOperationException("Stop play mode first.");
            for (int i = 0; i < SceneManager.sceneCount; i++)
                if (SceneManager.GetSceneAt(i).isDirty) throw new InvalidOperationException("Preserve unsaved scene changes first.");
            EditorSceneManager.OpenScene(path); PortraitGameViewSetup.Use1080x1920();
        }
        public static void Capture()
        {
            var session = UnityEngine.Object.FindFirstObjectByType<RunPrototypeSession>();
            if (!EditorApplication.isPlaying || session == null) throw new InvalidOperationException("Play a RunPrototype scene first.");
            Application.runInBackground = true; EditorApplication.isPaused = false;
            session.CapturePlay("Docs/RunPrototype/Captures");
        }
    }
}
