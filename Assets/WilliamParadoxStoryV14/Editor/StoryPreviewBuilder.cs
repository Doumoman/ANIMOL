#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace WilliamParadox.Story.Editor
{
    /// <summary>Creates a new story-only preview without touching project-wide settings or existing campaign UI.</summary>
    public static class StoryPreviewBuilder
    {
        private static readonly Color Navy = new Color32(20, 24, 50, 255);
        private static readonly Color Paper = new Color32(249, 235, 210, 255);
        private static readonly Color Cyan = new Color32(104, 214, 226, 255);
        private static readonly Color Gold = new Color32(255, 200, 104, 255);

        public static void BuildFromCommandLine()
        {
            try
            {
                string[] args = Environment.GetCommandLineArgs();
                string source = Argument(args, "-storySource");
                string font = Argument(args, "-storyFont", false);
                bool validateOnly = args.Contains("-storyValidateOnly");
                string result = Build(source, font, validateOnly);
                Debug.Log(result);
                if (Application.isBatchMode) EditorApplication.Exit(0);
            }
            catch (Exception error)
            {
                Debug.LogException(error);
                if (Application.isBatchMode) EditorApplication.Exit(1);
                else throw;
            }
        }

        public static string Build(string sourceRoot, string fontAssetPath = null, bool validateOnly = false)
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException("플레이 모드를 종료한 뒤 미리 보기 씬을 생성해 주세요.");
            StorySourcePlan plan = StorySourceImporter.Validate(sourceRoot, fontAssetPath);
            plan.fingerprint += "_input" + ReadActiveInputHandler() + "_new" + (FindType("UnityEngine.InputSystem.UI.InputSystemUIInputModule") != null);
            ValidateDestinationOwnership(plan);
            string report = MakeReport(plan, validateOnly ? "검증 완료 · 파일 변경 없음" : "씬 생성 완료");
            if (validateOnly) return report;

            StoryPreviewBuildStamp oldStamp = AssetDatabase.LoadAssetAtPath<StoryPreviewBuildStamp>(StoryImportPaths.StampPath);
            if (IsUnchanged(plan, oldStamp))
                return MakeReport(plan, "동일 소스 · 기존 씬과 프리팹 재사용");

            for (int i = 0; i < SceneManager.sceneCount; i++)
                if (SceneManager.GetSceneAt(i).path == StoryImportPaths.ScenePath)
                    throw new InvalidOperationException("생성 대상 미리 보기 씬이 열려 있습니다. 해당 씬의 변경 사항을 저장하고 닫은 뒤 다시 실행해 주세요. 다른 열린 씬은 변경하지 않습니다.");

            string backupFolder = BackupOwnedOutputs(oldStamp);
            ReserveOwnership(plan, oldStamp);
            ImportFiles(plan);
            TextAsset storyJson = AssetDatabase.LoadAssetAtPath<TextAsset>(StoryImportPaths.AssetPathForResource(StoryImportPaths.StoryResourceId, ".json"));
            if (storyJson == null) throw new IOException("복사한 스토리 JSON을 TextAsset으로 불러오지 못했습니다.");
            Scene previousScene = SceneManager.GetActiveScene();
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
            string inputModule;
            try
            {
                SceneManager.SetActiveScene(scene);
                var root = new GameObject("WilliamParadoxStoryPreview");
                StoryRunner runner = root.AddComponent<StoryRunner>();
                StoryView view = root.AddComponent<StoryView>();
                runner.storyJson = storyJson;
                runner.defaultResourcePath = StoryImportPaths.StoryResourceId;
                runner.view = view;
                runner.restoreOnStart = true;
                runner.reducedMotion = false;
                GameObject canvas = CreateCanvas(root.transform, view, plan.font);
                CreateBackdropCamera(root.transform);
                inputModule = CreateEventSystem(root.transform, plan.warnings);
                StoryImportPaths.EnsureAssetFolder(StoryImportPaths.OutputRoot + "/Prefabs");
                GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, StoryImportPaths.PrefabPath);
                if (prefab == null) throw new IOException("프리팹 저장에 실패했습니다: " + StoryImportPaths.PrefabPath);
                StoryImportPaths.EnsureAssetFolder(StoryImportPaths.OutputRoot + "/Scenes");
                if (!EditorSceneManager.SaveScene(scene, StoryImportPaths.ScenePath))
                    throw new IOException("씬 저장에 실패했습니다: " + StoryImportPaths.ScenePath);
                // Keep a usable, populated reference in the editor scene before closing it.
                if (canvas.GetComponent<CanvasScaler>().referenceResolution != new Vector2(1080, 1920))
                    throw new InvalidOperationException("Canvas 기준 해상도 검증에 실패했습니다.");
            }
            finally
            {
                if (scene.IsValid() && scene.isLoaded) EditorSceneManager.CloseScene(scene, true);
                if (previousScene.IsValid() && previousScene.isLoaded) SceneManager.SetActiveScene(previousScene);
            }
            WriteStamp(plan);
            report = MakeReport(plan, "씬 생성 완료") + "\n입력 모듈: " + inputModule +
                (string.IsNullOrEmpty(backupFolder) ? "" : "\n이전 생성본 백업: " + backupFolder);
            File.WriteAllText(StoryImportPaths.ToAbsoluteAssetPath(StoryImportPaths.ReportPath), report, new UTF8Encoding(false));
            AssetDatabase.ImportAsset(StoryImportPaths.ReportPath, ImportAssetOptions.ForceSynchronousImport);
            return report;
        }

        private static GameObject CreateCanvas(Transform parent, StoryView view, TMP_FontAsset font)
        {
            GameObject canvasObject = new GameObject("StoryCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasObject.transform.SetParent(parent, false);
            Canvas canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.pixelPerfect = false;
            CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080, 1920);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;

            RectTransform fullBleed = Rect("FullBleedBackdrop", canvasObject.transform);
            FillImage(fullBleed, Navy);
            RectTransform viewport = Rect("ArtViewport", canvasObject.transform);
            viewport.gameObject.AddComponent<StoryArtViewport>();
            viewport.gameObject.AddComponent<RectMask2D>();
            view.artworkRoot = Rect("ArtworkRoot", viewport);
            view.artworkGroup = view.artworkRoot.gameObject.AddComponent<CanvasGroup>();
            view.artImage = FillImage(Rect("ArtImage", view.artworkRoot), Color.white);
            view.artImage.preserveAspect = false; // StoryCoverImage sets a matching-size rectangle, never a stretched sprite.
            StoryCoverImage cover = view.artImage.gameObject.AddComponent<StoryCoverImage>();
            cover.viewport = viewport;
            cover.image = view.artImage;
            view.dimImage = FillImage(Rect("DimImage", viewport), new Color(0.03f, 0.03f, 0.12f, 0));

            view.insetRoot = Rect("InsetRoot", viewport, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));
            view.insetRoot.sizeDelta = new Vector2(880, 820);
            view.insetRoot.anchoredPosition = new Vector2(0, 260);
            view.insetGroup = view.insetRoot.gameObject.AddComponent<CanvasGroup>();
            view.insetImage = FillImage(Rect("InsetImage", view.insetRoot), Color.white);
            view.insetImage.preserveAspect = true;
            SetInitiallyHidden(view.insetGroup);

            RectTransform safe = Rect("SafeArea", canvasObject.transform);
            StorySafeArea safeArea = safe.gameObject.AddComponent<StorySafeArea>();
            safeArea.target = safe;

            RectTransform dialogue = Rect("DialogueRoot", safe, new Vector2(0, 0), new Vector2(1, 0));
            dialogue.pivot = new Vector2(0.5f, 0);
            dialogue.offsetMin = new Vector2(24, 118);
            dialogue.offsetMax = new Vector2(-24, 538);
            view.dialogueGroup = dialogue.gameObject.AddComponent<CanvasGroup>();
            FillImage(Rect("NavyFrame", dialogue), Navy);
            RectTransform inner = Rect("FlatPanel", dialogue);
            inner.offsetMin = new Vector2(7, 7); inner.offsetMax = new Vector2(-7, -7);
            FillImage(inner, new Color32(35, 39, 68, 250));
            RectTransform strip = Rect("CyanRule", dialogue, new Vector2(0, 1), new Vector2(1, 1));
            strip.offsetMin = new Vector2(7, -10); strip.offsetMax = new Vector2(-7, -4);
            FillImage(strip, Cyan);
            view.speakerText = Text("Speaker", dialogue, font, 34, Gold, TextAlignmentOptions.TopLeft);
            TopTextRect(view.speakerText.rectTransform, 32, 26, 48);
            view.bodyText = Text("Dialogue", dialogue, font, 36, Paper, TextAlignmentOptions.TopLeft);
            TopTextRect(view.bodyText.rectTransform, 32, 90, 232);
            view.bodyText.lineSpacing = 7;

            RectTransform status = Rect("StatusBar", safe, new Vector2(0, 0), new Vector2(1, 0));
            status.pivot = new Vector2(0.5f, 0); status.offsetMin = new Vector2(31, 125); status.offsetMax = new Vector2(-31, 203);
            FillImage(status, new Color32(35, 39, 68, 250));
            view.statusText = Text("Status", status, font, 25, Cyan, TextAlignmentOptions.MidlineLeft);
            view.statusText.rectTransform.offsetMin = new Vector2(25, 6); view.statusText.rectTransform.offsetMax = new Vector2(-25, -6);

            // These controls are siblings of DialogueRoot so reduced-motion credits can reuse Advance.
            RectTransform actions = Rect("ActionControls", safe, new Vector2(0, 0), new Vector2(1, 0));
            actions.pivot = new Vector2(0.5f, 0); actions.offsetMin = new Vector2(24, 12); actions.offsetMax = new Vector2(-24, 108);
            view.advanceButton = Button("AdvanceButton", actions, font, "다음", Gold, Navy);
            view.advanceButtonLabel = view.advanceButton.GetComponentInChildren<TMP_Text>();
            view.gateButton = Button("GateButton", actions, font, "등불을 막는다", Cyan, Navy);
            view.gateButtonLabel = view.gateButton.GetComponentInChildren<TMP_Text>();
            view.creditsContinueButton = Button("CreditsContinueButton", actions, font, "기록을 덮는다", Gold, Navy);
            view.gateButton.gameObject.SetActive(false);
            view.creditsContinueButton.gameObject.SetActive(false);

            RectTransform credits = Rect("CreditsRoot", safe);
            view.creditsGroup = credits.gameObject.AddComponent<CanvasGroup>();
            // The new book art keeps its upper sky dark and the physical book in the lower part.
            view.creditsViewport = Rect("CreditsViewport", credits, new Vector2(0.08f, 0.40f), new Vector2(0.92f, 0.88f));
            view.creditsViewport.gameObject.AddComponent<RectMask2D>();
            view.creditsContent = Rect("CreditsContent", view.creditsViewport, new Vector2(0, 1), new Vector2(1, 1));
            view.creditsContent.pivot = new Vector2(0.5f, 1);
            view.creditsContent.sizeDelta = new Vector2(0, 1110);
            view.creditsContent.anchoredPosition = Vector2.zero;
            view.creditsText = Text("CreditsText", view.creditsContent, font, 34, Paper, TextAlignmentOptions.TopLeft);
            view.creditsText.richText = true; // StoryCreditsPlayer escapes every paragraph with <noparse>.
            view.creditsText.margin = new Vector4(32, 24, 32, 24);
            view.creditsText.lineSpacing = 14;
            SetInitiallyHidden(view.creditsGroup);

            // Topmost UI: never hidden behind a transparent credits or illustration surface.
            RectTransform top = Rect("TopControls", safe, new Vector2(0, 1), new Vector2(1, 1));
            top.pivot = new Vector2(0.5f, 1); top.offsetMin = new Vector2(24, -118); top.offsetMax = new Vector2(-24, -20);
            view.pauseButton = Button("PauseButton", top, font, "일시정지", Navy, Paper);
            HorizontalSlot(view.pauseButton.GetComponent<RectTransform>(), 0, 0.21f);
            view.pauseButtonLabel = view.pauseButton.GetComponentInChildren<TMP_Text>();
            view.restartButton = Button("RestartButton", top, font, "처음부터", Navy, Paper);
            HorizontalSlot(view.restartButton.GetComponent<RectTransform>(), 0.23f, 0.44f);
            view.reducedMotionToggle = CreateToggle(top, font);
            HorizontalSlot(view.reducedMotionToggle.GetComponent<RectTransform>(), 0.50f, 1);
            // Credit presentation is below action controls in hit-test order.
            actions.SetAsLastSibling();
            status.SetAsLastSibling();
            top.SetAsLastSibling();
            return canvasObject;
        }

        private static void CreateBackdropCamera(Transform parent)
        {
            GameObject cameraObject = new GameObject("BackdropCamera", typeof(Camera));
            cameraObject.transform.SetParent(parent, false);
            Camera camera = cameraObject.GetComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = Navy;
            camera.cullingMask = 0;
            camera.orthographic = true;
            camera.depth = -100;
            // No AudioListener: the story-only scene has no audio system dependency.
        }

        private static string CreateEventSystem(Transform parent, List<string> warnings)
        {
            GameObject input = new GameObject("StoryEventSystem", typeof(EventSystem));
            input.transform.SetParent(parent, false);
            Type newModuleType = FindType("UnityEngine.InputSystem.UI.InputSystemUIInputModule");
            int activeInput = ReadActiveInputHandler(); // 0: legacy, 1: new, 2: both, -1: unknown.
            if ((activeInput == 1 || activeInput == 2 || activeInput == -1) && newModuleType != null)
            {
                Component module = input.AddComponent(newModuleType);
                var assignDefaults = newModuleType.GetMethod("AssignDefaultActions", Type.EmptyTypes);
                if (assignDefaults == null)
                    throw new InvalidOperationException("InputSystemUIInputModule.AssignDefaultActions()를 찾을 수 없습니다. 패키지의 기본 UI actions를 준비한 뒤 다시 빌드해 주세요.");
                StoryInputBootstrap bootstrap = input.AddComponent<StoryInputBootstrap>();
                bootstrap.preferNewInputSystem = true;
                return newModuleType.FullName + " (실행 시 기본 UI actions 연결)";
            }
            if (activeInput == 1)
                throw new InvalidOperationException("프로젝트가 새 Input System만 사용하지만 UI 입력 모듈을 찾을 수 없습니다. 기존 프로젝트의 Input System 패키지 설정을 확인하세요. 빌더는 패키지나 Active Input Handling을 바꾸지 않습니다.");
            if (activeInput == -1)
                warnings.Add("Active Input Handling을 읽지 못해 기존 StandaloneInputModule을 사용했습니다. Play 모드에서 버튼 입력을 확인하세요.");
            input.AddComponent<StandaloneInputModule>();
            return typeof(StandaloneInputModule).FullName;
        }

        private static int ReadActiveInputHandler()
        {
            UnityEngine.Object[] settings = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/ProjectSettings.asset");
            if (settings.Length == 0) return -1;
            using (var serialized = new SerializedObject(settings[0]))
            {
                SerializedProperty property = serialized.FindProperty("activeInputHandler");
                return property == null ? -1 : property.intValue;
            }
        }

        private static Type FindType(string fullName)
        {
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                Type type = assembly.GetType(fullName, false);
                if (type != null && typeof(BaseInputModule).IsAssignableFrom(type)) return type;
            }
            return null;
        }

        private static void ValidateDestinationOwnership(StorySourcePlan plan)
        {
            StoryPreviewBuildStamp stamp = AssetDatabase.LoadAssetAtPath<StoryPreviewBuildStamp>(StoryImportPaths.StampPath);
            var owned = new HashSet<string>(stamp == null || stamp.ownedAssetPaths == null ? new string[0] : stamp.ownedAssetPaths, StringComparer.Ordinal);
            var targets = plan.files.Select(f => f.assetPath).Concat(new[] { StoryImportPaths.ScenePath, StoryImportPaths.PrefabPath, StoryImportPaths.ReportPath });
            foreach (string path in targets)
            {
                if (File.Exists(StoryImportPaths.ToAbsoluteAssetPath(path)) && !owned.Contains(path))
                    throw new IOException("생성본 소유 기록이 없는 파일을 덮어쓸 수 없습니다: " + path + "\n기존 파일을 보관한 뒤 다른 위치로 옮기거나 깨끗한 패키지 폴더에서 다시 실행하세요.");
            }
            if (File.Exists(StoryImportPaths.ToAbsoluteAssetPath(StoryImportPaths.StampPath)) && stamp == null)
                throw new IOException("알 수 없는 빌드 소유 기록 파일이 있습니다: " + StoryImportPaths.StampPath);
        }

        private static bool IsUnchanged(StorySourcePlan plan, StoryPreviewBuildStamp stamp)
        {
            if (stamp == null || stamp.fingerprint != plan.fingerprint || stamp.builderVersion != StorySourceImporter.BuilderVersion) return false;
            if (stamp.sceneGuid != AssetDatabase.AssetPathToGUID(StoryImportPaths.ScenePath) || stamp.prefabGuid != AssetDatabase.AssetPathToGUID(StoryImportPaths.PrefabPath)) return false;
            return OwnedPaths(plan).All(path => File.Exists(StoryImportPaths.ToAbsoluteAssetPath(path)));
        }

        private static string BackupOwnedOutputs(StoryPreviewBuildStamp stamp)
        {
            if (stamp == null) return null;
            string folder = StoryImportPaths.OutputRoot + "/Editor/Backups/" + DateTime.UtcNow.ToString("yyyyMMdd_HHmmss_fff");
            foreach (string path in new[] { StoryImportPaths.ScenePath, StoryImportPaths.PrefabPath })
            {
                if (!File.Exists(StoryImportPaths.ToAbsoluteAssetPath(path))) continue;
                StoryImportPaths.EnsureAssetFolder(folder);
                if (!AssetDatabase.CopyAsset(path, folder + "/" + Path.GetFileName(path))) throw new IOException("이전 생성본 백업에 실패했습니다: " + path);
            }
            return folder;
        }

        private static void ImportFiles(StorySourcePlan plan)
        {
            foreach (StoryImportFile file in plan.files)
            {
                StoryImportPaths.EnsureAssetFolder(Path.GetDirectoryName(file.assetPath).Replace('\\', '/'));
                string destination = StoryImportPaths.ToAbsoluteAssetPath(file.assetPath);
                File.Copy(file.sourcePath, destination, true);
                AssetDatabase.ImportAsset(file.assetPath, ImportAssetOptions.ForceSynchronousImport | ImportAssetOptions.ForceUpdate);
                var importer = AssetImporter.GetAtPath(file.assetPath) as TextureImporter;
                if (importer == null) continue;
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.spritePixelsPerUnit = 100;
                importer.mipmapEnabled = false;
                importer.alphaIsTransparency = true;
                importer.sRGBTexture = true;
                importer.npotScale = TextureImporterNPOTScale.None;
                importer.maxTextureSize = 8192;
                importer.textureCompression = TextureImporterCompression.Uncompressed;
                importer.filterMode = FilterMode.Bilinear;
                var settings = new TextureImporterSettings();
                importer.ReadTextureSettings(settings);
                settings.spriteMeshType = SpriteMeshType.FullRect;
                importer.SetTextureSettings(settings);
                importer.SaveAndReimport();
            }
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
        }

        private static void WriteStamp(StorySourcePlan plan)
        {
            StoryImportPaths.EnsureAssetFolder(StoryImportPaths.OutputRoot + "/Editor");
            StoryPreviewBuildStamp stamp = AssetDatabase.LoadAssetAtPath<StoryPreviewBuildStamp>(StoryImportPaths.StampPath);
            if (stamp == null)
            {
                stamp = ScriptableObject.CreateInstance<StoryPreviewBuildStamp>();
                AssetDatabase.CreateAsset(stamp, StoryImportPaths.StampPath);
            }
            stamp.builderVersion = StorySourceImporter.BuilderVersion;
            stamp.fingerprint = plan.fingerprint;
            stamp.sourceFolder = plan.sourceRoot;
            stamp.fontGuid = AssetDatabase.AssetPathToGUID(plan.fontAssetPath);
            stamp.sceneGuid = AssetDatabase.AssetPathToGUID(StoryImportPaths.ScenePath);
            stamp.prefabGuid = AssetDatabase.AssetPathToGUID(StoryImportPaths.PrefabPath);
            stamp.ownedAssetPaths = (stamp.ownedAssetPaths ?? new string[0]).Concat(OwnedPaths(plan)).Distinct().ToArray();
            EditorUtility.SetDirty(stamp);
            AssetDatabase.SaveAssetIfDirty(stamp);
        }

        private static void ReserveOwnership(StorySourcePlan plan, StoryPreviewBuildStamp stamp)
        {
            // A failed import must remain retryable. Collisions were checked before reserving these paths.
            StoryImportPaths.EnsureAssetFolder(StoryImportPaths.OutputRoot + "/Editor");
            if (stamp == null)
            {
                stamp = ScriptableObject.CreateInstance<StoryPreviewBuildStamp>();
                AssetDatabase.CreateAsset(stamp, StoryImportPaths.StampPath);
            }
            stamp.fingerprint = string.Empty; // Not a completed build; never take the unchanged shortcut.
            stamp.ownedAssetPaths = (stamp.ownedAssetPaths ?? new string[0]).Concat(OwnedPaths(plan)).Distinct().ToArray();
            EditorUtility.SetDirty(stamp);
            AssetDatabase.SaveAssetIfDirty(stamp);
        }

        private static IEnumerable<string> OwnedPaths(StorySourcePlan plan)
        {
            return plan.files.Select(f => f.assetPath).Concat(new[] { StoryImportPaths.ScenePath, StoryImportPaths.PrefabPath, StoryImportPaths.ReportPath });
        }

        private static string MakeReport(StorySourcePlan plan, string result)
        {
            return "윌리엄의 역설 · 별길 목장 · 대화 전용 미리 보기\n" + result +
                "\n소스: " + plan.sourceRoot + "\n패키지: " + plan.package.packageId + " / " + plan.package.contentVersion +
                "\n노드: " + plan.package.nodes.Length + " / 임포트 파일: " + plan.files.Count +
                "\n기준 화면: 1080 × 1920 (9:16), cover 배치, 원본 재인코딩 없음" +
                "\n글꼴: " + plan.fontAssetPath + "\n씬: " + StoryImportPaths.ScenePath + "\n프리팹: " + StoryImportPaths.PrefabPath +
                "\n지문: " + plan.fingerprint + "\n" + string.Join("\n", plan.warnings.ToArray()) +
                "\n이 결과는 에디터 임포트/생성 결과입니다. 실제 Play 모드와 기기 확인은 별도입니다.";
        }

        private static string Argument(string[] args, string name, bool required = true)
        {
            int index = Array.IndexOf(args, name);
            if (index >= 0 && index + 1 < args.Length && !args[index + 1].StartsWith("-", StringComparison.Ordinal)) return args[index + 1];
            if (required) throw new ArgumentException(name + " 인자 뒤에 경로를 지정해 주세요.");
            return null;
        }

        private static RectTransform Rect(string name, Transform parent, Vector2? min = null, Vector2? max = null)
        {
            var obj = new GameObject(name, typeof(RectTransform));
            var rect = obj.GetComponent<RectTransform>();
            rect.SetParent(parent, false); rect.anchorMin = min ?? Vector2.zero; rect.anchorMax = max ?? Vector2.one;
            rect.offsetMin = Vector2.zero; rect.offsetMax = Vector2.zero;
            return rect;
        }

        private static Image FillImage(RectTransform rect, Color color)
        {
            Image image = rect.gameObject.AddComponent<Image>(); image.color = color; image.raycastTarget = false; return image;
        }

        private static TextMeshProUGUI Text(string name, Transform parent, TMP_FontAsset font, float size, Color color, TextAlignmentOptions alignment)
        {
            TextMeshProUGUI text = Rect(name, parent).gameObject.AddComponent<TextMeshProUGUI>();
            text.font = font; text.fontSize = size; text.color = color; text.alignment = alignment;
            text.enableWordWrapping = true; text.richText = false; text.raycastTarget = false;
            text.overflowMode = TextOverflowModes.Overflow;
            return text;
        }

        private static Button Button(string name, Transform parent, TMP_FontAsset font, string label, Color fill, Color ink)
        {
            RectTransform rect = Rect(name, parent);
            Image image = FillImage(rect, fill); image.raycastTarget = true;
            Button button = rect.gameObject.AddComponent<Button>(); button.targetGraphic = image;
            ColorBlock colors = button.colors; colors.highlightedColor = new Color(0.88f, 0.93f, 1); colors.pressedColor = new Color(0.7f, 0.76f, 0.9f); button.colors = colors;
            Navigation navigation = button.navigation; navigation.mode = Navigation.Mode.Automatic; button.navigation = navigation;
            TextMeshProUGUI text = Text("Label", rect, font, 30, ink, TextAlignmentOptions.Center);
            text.text = label; text.margin = new Vector4(12, 4, 12, 4);
            return button;
        }

        private static Toggle CreateToggle(Transform parent, TMP_FontAsset font)
        {
            RectTransform root = Rect("ReducedMotionToggle", parent);
            Image hit = FillImage(root, Navy); hit.raycastTarget = true;
            Toggle toggle = root.gameObject.AddComponent<Toggle>(); toggle.targetGraphic = hit;
            RectTransform box = Rect("Checkbox", root, new Vector2(0, 0.5f), new Vector2(0, 0.5f));
            box.pivot = new Vector2(0, 0.5f); box.sizeDelta = new Vector2(48, 48); box.anchoredPosition = new Vector2(18, 0);
            FillImage(box, Paper);
            RectTransform mark = Rect("Checkmark", box); mark.offsetMin = new Vector2(9, 9); mark.offsetMax = new Vector2(-9, -9);
            toggle.graphic = FillImage(mark, Cyan);
            TextMeshProUGUI label = Text("Label", root, font, 28, Paper, TextAlignmentOptions.MidlineLeft);
            label.rectTransform.offsetMin = new Vector2(82, 0); label.rectTransform.offsetMax = new Vector2(-8, 0); label.text = "연출 줄이기";
            toggle.isOn = false;
            return toggle;
        }

        private static void TopTextRect(RectTransform rect, float horizontalMargin, float top, float height)
        {
            rect.anchorMin = new Vector2(0, 1); rect.anchorMax = new Vector2(1, 1); rect.pivot = new Vector2(0.5f, 1);
            rect.offsetMin = new Vector2(horizontalMargin, -top - height); rect.offsetMax = new Vector2(-horizontalMargin, -top);
        }

        private static void BottomRect(RectTransform rect, float x, float bottom, float width, float height)
        {
            rect.anchorMin = rect.anchorMax = Vector2.zero; rect.pivot = Vector2.zero;
            rect.sizeDelta = new Vector2(width, height); rect.anchoredPosition = new Vector2(x, bottom);
        }

        private static void HorizontalSlot(RectTransform rect, float min, float max)
        {
            rect.anchorMin = new Vector2(min, 0); rect.anchorMax = new Vector2(max, 1); rect.pivot = new Vector2(0.5f, 0.5f);
            rect.offsetMin = Vector2.zero; rect.offsetMax = Vector2.zero;
        }

        private static void SetInitiallyHidden(CanvasGroup group)
        {
            group.alpha = 0; group.interactable = false; group.blocksRaycasts = false;
        }
    }
}
#endif
