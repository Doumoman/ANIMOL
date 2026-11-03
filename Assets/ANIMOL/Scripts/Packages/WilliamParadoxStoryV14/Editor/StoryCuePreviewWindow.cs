#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace WilliamParadox.Story.Editor
{
    /// <summary>Read-only storyboard inspection. It does not run a session or write progress flags.</summary>
    public sealed class StoryCuePreviewWindow : EditorWindow
    {
        [SerializeField] private TextAsset storyJson;
        private StoryPackage package;
        private int selectedIndex;
        private string error;
        private string[] labels = new string[0];
        private Vector2 scroll;
        private float previewTime;

        public static void Open(TextAsset json, string nodeId)
        {
            StoryCuePreviewWindow window = GetWindow<StoryCuePreviewWindow>("장면과 큐 미리 보기");
            if (json != null) window.storyJson = json;
            window.Reload();
            if (window.package != null && !string.IsNullOrEmpty(nodeId))
            {
                int index = Array.FindIndex(window.package.nodes, n => n.id == nodeId);
                if (index >= 0) window.selectedIndex = index;
            }
            window.Show();
        }

        private void OnEnable() { minSize = new Vector2(660, 600); Reload(); }

        private void Reload()
        {
            package = null; error = null; labels = new string[0];
            if (storyJson == null)
                storyJson = AssetDatabase.LoadAssetAtPath<TextAsset>(StoryImportPaths.AssetPathForResource(StoryImportPaths.StoryResourceId, ".json"));
            if (storyJson == null) return;
            try
            {
                package = JsonUtility.FromJson<StoryPackage>(storyJson.text);
                StorySourceImporter.ValidateStory(package);
                labels = package.nodes.Select(n => n.id + " · " + n.kind + (string.IsNullOrEmpty(n.speaker) ? "" : " · " + n.speaker)).ToArray();
                selectedIndex = Mathf.Clamp(selectedIndex, 0, labels.Length - 1);
                previewTime = 0;
            }
            catch (Exception ex) { package = null; error = ex.Message; }
        }

        private void OnGUI()
        {
            EditorGUI.BeginChangeCheck();
            storyJson = (TextAsset)EditorGUILayout.ObjectField("스토리 JSON", storyJson, typeof(TextAsset), false);
            if (EditorGUI.EndChangeCheck()) Reload();
            if (!string.IsNullOrEmpty(error)) EditorGUILayout.HelpBox(error, MessageType.Error);
            if (package == null)
            {
                EditorGUILayout.HelpBox("먼저 소스 검증과 씬 생성을 완료하거나 임포트한 스토리 JSON을 지정하세요.", MessageType.Info);
                return;
            }
            int next = EditorGUILayout.Popup("노드", selectedIndex, labels);
            if (next != selectedIndex) { selectedIndex = next; previewTime = 0; }
            StoryNode node = package.nodes[selectedIndex];
            StoryCue[] cues = node.cues ?? new StoryCue[0];
            float maxTime = Mathf.Max(0.001f, cues.Length == 0 ? 0 : cues.Max(c => c.delay + c.duration));
            previewTime = EditorGUILayout.Slider("큐 시간 탐색 (초)", previewTime, 0, maxTime);
            using (new EditorGUILayout.HorizontalScope())
            {
                Rect preview = GUILayoutUtility.GetRect(250, 445, GUILayout.Width(250), GUILayout.Height(445));
                DrawFrame(preview, node, cues, previewTime);
                using (new EditorGUILayout.VerticalScope())
                {
                    EditorGUILayout.LabelField(node.sceneId + " / " + node.beatId, EditorStyles.boldLabel);
                    EditorGUILayout.LabelField("화자", node.speaker ?? string.Empty);
                    EditorGUILayout.TextArea(node.text ?? string.Empty, GUILayout.MinHeight(92));
                    EditorGUILayout.LabelField("다음 노드", node.nextId ?? "(종료)");
                    EditorGUILayout.LabelField("요구 플래그", Join(node.requiredFlags));
                    EditorGUILayout.LabelField("설정 플래그", Join(node.setFlags));
                    if (node.kind == "gate") EditorGUILayout.HelpBox("별도 입력: " + node.gateLabel + "\n일반 다음 버튼이나 개발 점프로 이 게이트를 완료 처리하지 않습니다.", MessageType.Warning);
                    if (node.credits != null) EditorGUILayout.TextArea(string.Join("\n\n", node.credits), GUILayout.MinHeight(80));
                    EditorGUILayout.HelpBox("이 창은 원화의 cover 배치와 zoom/dim의 정지 시점을 확인합니다. 팝업, 흔들림, 전환, inset의 최종 동작은 Play 모드에서 확인하세요. 진행/저장 데이터는 변경하지 않습니다.", MessageType.Info);
                }
            }
            scroll = EditorGUILayout.BeginScrollView(scroll);
            foreach (StoryCue cue in cues)
                EditorGUILayout.HelpBox(cue.type + " → " + (string.IsNullOrEmpty(cue.target) ? "artwork" : cue.target) +
                    " | 대기 " + cue.delay.ToString("0.###") + "초 | 길이 " + cue.duration.ToString("0.###") + "초" +
                    " | 값 " + cue.amount.ToString("0.###") +
                    (string.IsNullOrEmpty(cue.assetId) ? "" : " | " + cue.assetId + " @ " + cue.x + ", " + cue.y + " × " + cue.scale), MessageType.None);
            if (cues.Length == 0) EditorGUILayout.LabelField("이 노드에는 추가 큐가 없습니다.");
            EditorGUILayout.EndScrollView();
        }

        private void DrawFrame(Rect rect, StoryNode node, StoryCue[] cues, float time)
        {
            EditorGUI.DrawRect(rect, new Color32(20, 24, 50, 255));
            string artId = node.artId;
            if (string.IsNullOrEmpty(artId))
                for (int i = selectedIndex - 1; i >= 0; i--)
                    if (!string.IsNullOrEmpty(package.nodes[i].artId)) { artId = package.nodes[i].artId; break; }
            StoryAsset asset = package.assets.FirstOrDefault(a => a.id == artId);
            if (asset == null) return;
            Texture2D texture = LoadTexture(asset.resourcePath);
            if (texture == null) { GUI.Label(rect, "원화 미임포트\n" + asset.resourcePath); return; }
            float zoom = 1, dim = 0;
            foreach (StoryCue cue in cues)
            {
                float p = time < cue.delay ? 0 : cue.duration == 0 ? 1 : Mathf.Clamp01((time - cue.delay) / cue.duration);
                if (cue.type == "zoom" && cue.target != "inset") zoom = Mathf.Lerp(1, cue.amount, p);
                if (cue.type == "dim") dim = Mathf.Lerp(0, cue.amount, p);
            }
            GUI.BeginGroup(rect);
            Rect local = new Rect((rect.width - rect.width * zoom) * 0.5f, (rect.height - rect.height * zoom) * 0.5f, rect.width * zoom, rect.height * zoom);
            GUI.DrawTexture(local, texture, ScaleMode.ScaleAndCrop, true);
            EditorGUI.DrawRect(new Rect(0, 0, rect.width, rect.height), new Color(0.03f, 0.03f, 0.12f, dim));
            if (node.kind != "credits" && node.kind != "end")
                EditorGUI.DrawRect(new Rect(6, rect.height * 0.72f, rect.width - 12, rect.height * 0.28f - 6), new Color(0.08f, 0.09f, 0.19f, 0.92f));
            GUI.EndGroup();
        }

        private static Texture2D LoadTexture(string resourcePath)
        {
            foreach (string extension in new[] { ".png", ".jpg", ".jpeg" })
            {
                Texture2D texture = AssetDatabase.LoadAssetAtPath<Texture2D>(StoryImportPaths.AssetPathForResource(resourcePath, extension));
                if (texture != null) return texture;
            }
            return null;
        }

        private static string Join(string[] flags) { return flags == null || flags.Length == 0 ? "(없음)" : string.Join(", ", flags); }
    }

    [CustomEditor(typeof(StoryRunner))]
    public sealed class StoryRunnerInspector : UnityEditor.Editor
    {
        private int selectedScene;
        private string jumpMessage;
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
            StoryRunner runner = (StoryRunner)target;
            EditorGUILayout.Space();
            if (GUILayout.Button("장면 원화와 큐 살펴보기")) StoryCuePreviewWindow.Open(runner.storyJson, runner.CurrentNodeId);
            if (!Application.isPlaying || runner.Package == null)
            {
                EditorGUILayout.HelpBox("Play 모드에서는 R01~R17 개발 점프를 사용할 수 있습니다. 현재 진행 플래그와 맞는 구간만 허용합니다. 앞부분을 다시 검토하려면 처음부터 시작하세요. 캠페인 클리어를 지급하지 않습니다.", MessageType.Info);
                return;
            }
            foreach (string message in runner.ValidationErrors) EditorGUILayout.HelpBox(message, MessageType.Error);
            StoryNode[] starts = runner.Package.nodes.GroupBy(n => n.sceneId).Select(g => g.First()).ToArray();
            if (starts.Length == 0) return;
            selectedScene = Mathf.Clamp(selectedScene, 0, starts.Length - 1);
            selectedScene = EditorGUILayout.Popup("개발 점프", selectedScene, starts.Select(n => n.sceneId + " · " + n.id).ToArray());
            if (GUILayout.Button("선택 장면으로 이동"))
            {
                bool accepted = runner.JumpTo(starts[selectedScene].id);
                jumpMessage = accepted ? "장면으로 이동했습니다. 기존 플래그 규칙을 유지합니다." : "현재 진행 플래그와 맞지 않아 이동하지 않았습니다. 필요한 게이트를 진행하거나 처음부터 다시 시작하세요.";
            }
            if (!string.IsNullOrEmpty(jumpMessage)) EditorGUILayout.HelpBox(jumpMessage, MessageType.Info);
            EditorGUILayout.LabelField("현재 노드", runner.CurrentNodeId ?? string.Empty);
            EditorGUILayout.LabelField("일시정지", runner.IsPaused ? "예" : "아니요");
        }
    }
}
#endif
