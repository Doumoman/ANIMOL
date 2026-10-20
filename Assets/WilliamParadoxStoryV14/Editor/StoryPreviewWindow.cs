#if UNITY_EDITOR
using System;
using System.IO;
using System.Text;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace WilliamParadox.Story.Editor
{
    public sealed class StoryPreviewWindow : EditorWindow
    {
        [SerializeField] private string sourceFolder;
        [SerializeField] private TMP_FontAsset koreanFont;
        private string report = "압축을 푼 v14 폴더를 선택하세요. 이 창은 스토리 전용 씬을 별도로 생성합니다.";
        private Vector2 scroll;

        [MenuItem("Tools/William Paradox/Build Story Preview")]
        public static void ShowWindow()
        {
            GetWindow<StoryPreviewWindow>("별길 목장 씬 만들기").Show();
        }

        private void OnEnable()
        {
            minSize = new Vector2(560, 460);
            if (!string.IsNullOrEmpty(sourceFolder)) return;
            StoryPreviewBuildStamp stamp = AssetDatabase.LoadAssetAtPath<StoryPreviewBuildStamp>(StoryImportPaths.StampPath);
            if (stamp != null) sourceFolder = stamp.sourceFolder;
        }

        private void OnGUI()
        {
            EditorGUILayout.LabelField("윌리엄의 역설 · 별길 목장", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("스토리 대화와 화면 연출만 재생합니다. 기존 캠페인 UI, 퍼즐, 보상, 프로젝트 입력 설정은 연결하거나 변경하지 않습니다.", MessageType.Info);
            using (new EditorGUILayout.HorizontalScope())
            {
                sourceFolder = EditorGUILayout.TextField("v14 소스 폴더", sourceFolder ?? string.Empty);
                if (GUILayout.Button("폴더 선택", GUILayout.Width(88)))
                {
                    string chosen = EditorUtility.OpenFolderPanel("data와 art가 들어 있는 v14 폴더 선택", sourceFolder ?? string.Empty, string.Empty);
                    if (!string.IsNullOrEmpty(chosen)) sourceFolder = chosen;
                }
            }
            koreanFont = (TMP_FontAsset)EditorGUILayout.ObjectField("기존 한국어 TMP 글꼴", koreanFont, typeof(TMP_FontAsset), false);
            EditorGUILayout.LabelField("미지정 시 기존 프로젝트 글꼴을 검색합니다. 누락 문자가 있으면 생성이 중단됩니다.", EditorStyles.wordWrappedMiniLabel);
            EditorGUILayout.Space();
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("검증만 실행", GUILayout.Height(34))) Run(true);
                if (GUILayout.Button("씬과 프리팹 생성", GUILayout.Height(34))) Run(false);
            }
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("필요한 문자 복사")) CopyRequiredCharacters();
                using (new EditorGUI.DisabledScope(AssetDatabase.LoadAssetAtPath<SceneAsset>(StoryImportPaths.ScenePath) == null))
                    if (GUILayout.Button("생성한 씬 열기"))
                    {
                        // Opening is an explicit designer action; normal Unity save prompts protect unrelated dirty scenes.
                        if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                            EditorSceneManager.OpenScene(StoryImportPaths.ScenePath);
                    }
                if (GUILayout.Button("노드와 큐 살펴보기")) StoryCuePreviewWindow.Open(null, null);
            }
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("출력 위치", EditorStyles.boldLabel);
            EditorGUILayout.SelectableLabel(StoryImportPaths.ScenePath + "\n" + StoryImportPaths.PrefabPath, GUILayout.Height(40));
            scroll = EditorGUILayout.BeginScrollView(scroll);
            EditorGUILayout.TextArea(report ?? string.Empty, EditorStyles.wordWrappedLabel, GUILayout.ExpandHeight(true));
            EditorGUILayout.EndScrollView();
        }

        private void Run(bool validateOnly)
        {
            try
            {
                report = StoryPreviewBuilder.Build(sourceFolder, koreanFont == null ? null : AssetDatabase.GetAssetPath(koreanFont), validateOnly);
                Debug.Log(report);
            }
            catch (Exception error)
            {
                report = error.Message;
                Debug.LogError(error);
            }
        }

        private void CopyRequiredCharacters()
        {
            try
            {
                string manifestPath = StoryImportPaths.ResolveSourceFile(sourceFolder, StorySourceImporter.ManifestSourcePath);
                StoryImportManifest manifest = JsonUtility.FromJson<StoryImportManifest>(File.ReadAllText(manifestPath, Encoding.UTF8));
                if (manifest == null) throw new InvalidDataException("import_manifest.json을 읽을 수 없습니다.");
                string storyPath = StoryImportPaths.ResolveSourceFile(sourceFolder, manifest.storySourcePath);
                StoryPackage package = JsonUtility.FromJson<StoryPackage>(File.ReadAllText(storyPath, Encoding.UTF8));
                StorySourceImporter.ValidateStory(package);
                EditorGUIUtility.systemCopyBuffer = StorySourceImporter.CollectVisibleText(package);
                report = "대사, 기록문, 고정 UI의 필요한 문자를 클립보드에 복사했습니다. Font Asset Creator의 Custom Characters 입력란에서 사용할 수 있습니다.";
            }
            catch (Exception error) { report = error.Message; Debug.LogError(error); }
        }
    }
}
#endif
