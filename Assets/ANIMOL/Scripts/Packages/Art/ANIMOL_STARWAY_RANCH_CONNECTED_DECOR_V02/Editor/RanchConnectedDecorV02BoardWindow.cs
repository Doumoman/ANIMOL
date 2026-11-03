#if UNITY_EDITOR
using System;
using System.Globalization;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace ANIMOL.Art.RanchConnectedDecorV02
{
    public sealed class RanchConnectedDecorV02BoardWindow : EditorWindow
    {
        [SerializeField] private TextAsset manifest, stageSnapshot, all26Snapshot;
        [SerializeField] private string sortingLayer;
        [SerializeField] private bool createInspectionScenes;
        private string diagnostic;

        [MenuItem("Tools/ANIMOL/Ranch Connected Decor V02/Build example boards…")]
        private static void Open()
        {
            var window = GetWindow<RanchConnectedDecorV02BoardWindow>("Ranch decoration boards");
            window.minSize = new Vector2(500, 350); window.TrySelectedManifest(); window.Show();
        }
        private void TrySelectedManifest()
        {
            var selected = Selection.activeObject as TextAsset;
            if (selected == null) return;
            string path = AssetDatabase.GetAssetPath(selected);
            try
            {
                string root = RanchConnectedDecorV02BoardTools.PackageRoot(path); manifest = selected;
                stageSnapshot = AssetDatabase.LoadAssetAtPath<TextAsset>(root + "/Examples/stage_snapshot.json");
                all26Snapshot = AssetDatabase.LoadAssetAtPath<TextAsset>(root + "/Examples/all26_snapshot.json");
            }
            catch (Exception) { /* An unrelated selected asset does not change the window inputs. */ }
        }
        private void OnGUI()
        {
            EditorGUILayout.LabelField("Ranch connected decoration — installation revision 1", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("Build two approved board assets and working inspection prefabs. Scene generation is optional and isolated; no campaign scene, game rule, input or collider is changed.", MessageType.Info);
            manifest = (TextAsset)EditorGUILayout.ObjectField("Current manifest.json", manifest, typeof(TextAsset), false);
            stageSnapshot = (TextAsset)EditorGUILayout.ObjectField("Stage snapshot JSON", stageSnapshot, typeof(TextAsset), false);
            all26Snapshot = (TextAsset)EditorGUILayout.ObjectField("All 26 snapshot JSON", all26Snapshot, typeof(TextAsset), false);
            if (GUILayout.Button("Use selected current manifest and bundled examples")) TrySelectedManifest();
            var layers = SortingLayer.layers; var names = new string[layers.Length + 1]; names[0] = "-- Select the actual existing Sorting Layer --";
            int current = 0;
            for (int i = 0; i < layers.Length; i++) { names[i + 1] = layers[i].name; if (sortingLayer == layers[i].name) current = i + 1; }
            int chosen = EditorGUILayout.Popup("Decoration Sorting Layer", current, names);
            sortingLayer = chosen > 0 ? layers[chosen - 1].name : null;
            createInspectionScenes = EditorGUILayout.ToggleLeft("Also create isolated inspection .unity scenes", createInspectionScenes);
            EditorGUILayout.HelpBox("Open a generated inspection scene separately for Play verification. The builder keeps your current active and unsaved scenes open. This is an inspection board, not a migrated campaign map.", MessageType.None);
            using (new EditorGUI.DisabledScope(manifest == null || stageSnapshot == null || all26Snapshot == null || string.IsNullOrEmpty(sortingLayer)))
            {
                if (GUILayout.Button("Build board assets and prefabs"))
                {
                    try
                    {
                        RanchConnectedDecorV02BoardTools.BuildExamples(AssetDatabase.GetAssetPath(manifest), AssetDatabase.GetAssetPath(stageSnapshot),
                            AssetDatabase.GetAssetPath(all26Snapshot), sortingLayer, createInspectionScenes);
                        diagnostic = "Generated assets saved under this package's _Generated/InspectionBoards. Check the build report, then verify Play Mode on the host.";
                    }
                    catch (Exception error) { diagnostic = error.Message; Debug.LogException(error); }
                }
            }
            if (!string.IsNullOrEmpty(diagnostic)) EditorGUILayout.HelpBox(diagnostic, MessageType.Info);
        }
    }

    [CustomEditor(typeof(RanchConnectedDecorV02Binding))]
    public sealed class RanchConnectedDecorV02BindingInspector : Editor
    {
        private bool refreshAfterUndo, queued;
        private string diagnostic;
        private string observedAuthoringState;
        private void OnEnable() { observedAuthoringState = Fingerprint(target as RanchConnectedDecorV02Binding); Undo.undoRedoPerformed += OnUndoRedo; }
        private void OnDisable() { Undo.undoRedoPerformed -= OnUndoRedo; EditorApplication.delayCall -= RefreshAfterUndo; queued = false; }
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
            var binding = target as RanchConnectedDecorV02Binding;
            EditorGUILayout.HelpBox("A Binding is the concrete Configure + SetLayout caller. A bare Controller does not initialize itself. Production sources must publish complete approved board snapshots after real edits/load/undo.", MessageType.Info);
            refreshAfterUndo = EditorGUILayout.ToggleLeft("Refresh this inspected binding after Undo/Redo", refreshAfterUndo);
            bool usable = binding != null && !EditorUtility.IsPersistent(binding) && binding.gameObject.scene.IsValid() && binding.gameObject.scene.isLoaded;
            using (new EditorGUI.DisabledScope(!usable))
            {
                if (GUILayout.Button("Validate catalog and refresh this binding")) Refresh(binding);
                if (GUILayout.Button("Clear this binding's owned visuals"))
                {
                    binding.ClearVisuals();
                    if (!Application.isPlaying) EditorSceneManager.MarkSceneDirty(binding.gameObject.scene);
                    diagnostic = "Owned decoration visuals cleared.";
                }
                var authored = binding == null ? null : binding.Source as RanchConnectedDecorV02AuthoredSnapshotSource;
                if (authored != null && GUILayout.Button("Notify this authored source that its board changed"))
                {
                    string error;
                    if (!authored.NotifyBoardChanged(out error)) diagnostic = error;
                    else Refresh(binding);
                }
            }
            if (binding != null)
            {
                var report = binding.LastReport;
                EditorGUILayout.LabelField("Last board", report.boardId ?? "(none)");
                EditorGUILayout.LabelField("Revision", report.revision.ToString());
                EditorGUILayout.LabelField("Networks / props / claims / visuals", report.networkCount + " / " + report.groupCount + " / " + report.claimCount + " / " + report.visualCount);
                if (!string.IsNullOrEmpty(binding.LastDiagnostic)) EditorGUILayout.HelpBox(binding.LastDiagnostic, MessageType.Warning);
            }
            if (!usable) EditorGUILayout.HelpBox("Open the prefab in Prefab Stage or select its loaded scene instance to refresh. Persistent prefab assets are not mutated directly by this inspector.", MessageType.Info);
            if (!string.IsNullOrEmpty(diagnostic)) EditorGUILayout.HelpBox(diagnostic, MessageType.Info);
            if (!queued) observedAuthoringState = Fingerprint(binding);
        }
        private void OnUndoRedo()
        {
            // Explicit opt-in for this inspected component only: no scene scans or unrelated regeneration.
            if (!refreshAfterUndo || queued || target == null) return;
            string current = Fingerprint(target as RanchConnectedDecorV02Binding);
            if (current == observedAuthoringState) return;
            observedAuthoringState = current;
            queued = true; EditorApplication.delayCall += RefreshAfterUndo;
        }
        private void RefreshAfterUndo()
        {
            queued = false;
            if (this == null || target == null || !refreshAfterUndo) return;
            Refresh(target as RanchConnectedDecorV02Binding); Repaint();
        }
        private void Refresh(RanchConnectedDecorV02Binding binding)
        {
            string error;
            diagnostic = RanchConnectedDecorV02BoardTools.TryRefreshBinding(binding, out error) ? "Current whole board refreshed." : error;
            observedAuthoringState = Fingerprint(binding);
        }
        private static string Fingerprint(RanchConnectedDecorV02Binding binding)
        {
            if (binding == null) return "missing";
            var value = new StringBuilder(EditorJsonUtility.ToJson(binding));
            value.Append('|').Append(binding.enabled).Append('|').Append(binding.gameObject.activeInHierarchy);
            if (binding.Controller != null) value.Append('|').Append(binding.Controller.enabled);
            if (binding.Catalog != null) value.Append('|').Append(EditorJsonUtility.ToJson(binding.Catalog));
            if (binding.Source != null)
            {
                value.Append('|').Append(EditorJsonUtility.ToJson(binding.Source)).Append('|').Append(binding.Source.enabled).Append('|').Append(binding.Source.gameObject.activeInHierarchy);
                var authored = binding.Source as RanchConnectedDecorV02AuthoredSnapshotSource;
                if (authored != null && authored.Board != null) value.Append('|').Append(EditorJsonUtility.ToJson(authored.Board));
            }
            for (Transform node = binding.transform; node != null; node = node.parent)
            {
                value.Append('|').Append(node.GetInstanceID()).Append('|').Append(node.gameObject.activeSelf);
                Append(value, node.localPosition.x); Append(value, node.localPosition.y); Append(value, node.localPosition.z);
                Append(value, node.localScale.x); Append(value, node.localScale.y); Append(value, node.localScale.z);
                Append(value, node.localRotation.x); Append(value, node.localRotation.y); Append(value, node.localRotation.z); Append(value, node.localRotation.w);
            }
            return RanchConnectedDecorV02Catalog.Sha256(Encoding.UTF8.GetBytes(value.ToString()));
        }
        private static void Append(StringBuilder value, float number) { value.Append('|').Append(number.ToString("R", CultureInfo.InvariantCulture)); }
    }
}
#endif
