#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.TextCore.LowLevel;
using UnityEngine.UI;

namespace WilliamParadox.Story.Editor
{
    /// <summary>Builds one self-contained theater scene; writes only inside Generated.</summary>
    public static class EchoTheaterSceneBuilder
    {
        public const string StoryResourceId = "WilliamParadox/Theater/Story/theater_story";
        public const string PresentationResourceId = "WilliamParadox/Theater/Story/theater_presentation";
        public const string SoundResourceId = "WilliamParadox/Theater/Story/sound_cues";
        public const string GeneratedRoot = StoryImportPaths.OutputRoot + "/Generated";
        public const string ScenePath = GeneratedRoot + "/EchoTheaterStory.unity";
        public const string StampPath = GeneratedRoot + "/EchoTheaterBuildStamp.asset";
        public const string PreviewSpritePath = GeneratedRoot + "/EchoTheaterFirstFrame.asset";
        public const string BundledFontPath = GeneratedRoot + "/NotoSansKR-SDF.asset";
        public const string BundledFontSourcePath = StoryImportPaths.OutputRoot + "/Fonts/NotoSansKR.ttf";
        private const string OwnerVersion = "echo-theater-builder-1";
        private const string ResourcePrefix = "WilliamParadox/Theater/";

        private sealed class Plan
        {
            public TextAsset story, presentation;
            public StoryPackage package;
            public StoryPresentationPackage presentationData;
            public TMP_FontAsset font;
            public StoryNodeSound[] nodeSounds;
            public readonly List<string> warnings = new List<string>();
        }

        [Serializable] private sealed class SoundDocument { public SoundEntry[] sounds; }
        [Serializable] private sealed class SoundEntry { public string nodeId, resourcePath; public float volume = 1; }

        [MenuItem("Tools/William Paradox/잔영 극장/씬 생성")]
        public static void OpenWindow()
        {
            EchoTheaterBuildWindow window = EditorWindow.GetWindow<EchoTheaterBuildWindow>("잔영 극장 씬 생성");
            window.minSize = new Vector2(520, 260);
            window.Show();
        }

        public static void BuildFromCommandLine()
        {
            try
            {
                string[] args = Environment.GetCommandLineArgs();
                string fontPath = RequiredArgument(args, "-storyFont");
                Debug.Log(args.Contains("-storyValidateGeneratedScene")
                    ? ValidateGeneratedScene(fontPath)
                    : Build(fontPath, args.Contains("-storyValidateOnly")));
                if (Application.isBatchMode) EditorApplication.Exit(0);
            }
            catch (Exception error)
            {
                Debug.LogException(error);
                if (Application.isBatchMode) EditorApplication.Exit(1);
                else throw;
            }
        }

        public static void CreateBundledFontFromCommandLine()
        {
            try
            {
                CreateBundledFontAsset();
                Debug.Log("한국어 TMP 자산 생성 완료: " + BundledFontPath);
                if (Application.isBatchMode) EditorApplication.Exit(0);
            }
            catch (Exception error)
            {
                Debug.LogException(error);
                if (Application.isBatchMode) EditorApplication.Exit(1); else throw;
            }
        }

        public static string ValidateGeneratedScene(string fontAssetPath)
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException("Play 모드를 종료한 뒤 생성 씬의 구조를 검증해 주세요.");
            ValidateInputs(fontAssetPath);
            ValidateDestinationOwnership();
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath) == null)
                throw new FileNotFoundException("먼저 잔영 극장 씬을 생성해 주세요.", ScenePath);
            Scene previous = SceneManager.GetActiveScene();
            Scene scene = SceneManager.GetSceneByPath(ScenePath);
            bool openedHere = !scene.IsValid() || !scene.isLoaded;
            if (openedHere) scene = EditorSceneManager.OpenScene(ScenePath,
                Application.isBatchMode && string.IsNullOrEmpty(previous.path) ? OpenSceneMode.Single : OpenSceneMode.Additive);
            try
            {
                GameObject[] roots = scene.GetRootGameObjects();
                if (roots.Length != 1 || roots[0].name != "EchoTheaterStory")
                    throw new InvalidDataException("생성 씬에는 EchoTheaterStory 루트가 정확히 하나 있어야 합니다.");
                StoryView view = roots[0].GetComponent<StoryView>();
                if (view == null) throw new InvalidDataException("생성 씬의 StoryView가 없습니다.");
                ValidateSceneHierarchy(roots[0], view);
                if (view.artImage.sprite == null || AssetDatabase.GetAssetPath(view.artImage.sprite) != PreviewSpritePath)
                    throw new InvalidDataException("생성 씬 첫 화면의 영구 Sprite 참조가 없습니다.");
                return "생성 씬 구조 검증 통과: 단일 루트·Canvas·입력·SafeArea, 9:16 contain, 독립 초상, 40/36 글꼴, 고정 버튼 hitbox, 효과음 참조. Play 모드 실행 검증은 별도입니다.";
            }
            finally
            {
                if (openedHere && scene.IsValid() && scene.isLoaded) EditorSceneManager.CloseScene(scene, true);
                if (previous.IsValid() && previous.isLoaded) SceneManager.SetActiveScene(previous);
            }
        }

        /// <summary>Explicit opt-in; builds a new local font asset, never changes project TMP settings.</summary>
        public static TMP_FontAsset CreateBundledFontAsset()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException("Play 모드를 종료한 뒤 글꼴 자산을 생성해 주세요.");
            StoryPackage package = JsonUtility.FromJson<StoryPackage>(LoadResource<TextAsset>(StoryResourceId, ".json").text);
            if (!StoryGraph.TryCreate(package, out var unusedGraph, out var errors))
                throw new InvalidDataException("글꼴 문자 수집 전 스토리 검증에 실패했습니다:\n" + string.Join("\n", errors));
            string characters = StorySourceImporter.CollectVisibleText(package) + "\n잔영 극장 연출 줄이기 일시정지 다음 처음부터";
            TMP_FontAsset existing = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(BundledFontPath);
            if (existing != null)
            {
                string missing = StorySourceImporter.MissingGlyphReport(existing, characters);
                if (missing.Length != 0)
                    throw new InvalidDataException("기존 생성 글꼴에 필요한 문자가 없습니다. 기존 자산은 자동 수정하지 않습니다. Font Asset Creator에서 atlas를 갱신하거나 다른 한국어 TMP 글꼴을 선택해 주세요.\n" + missing);
                return existing;
            }
            if (File.Exists(StoryImportPaths.ToAbsoluteAssetPath(BundledFontPath)))
                throw new IOException("TMP 글꼴이 아닌 기존 파일을 덮어쓸 수 없습니다: " + BundledFontPath);
            Font source = AssetDatabase.LoadAssetAtPath<Font>(BundledFontSourcePath);
            if (source == null) throw new InvalidDataException("동봉 Noto Sans KR 원본 글꼴을 불러오지 못했습니다: " + BundledFontSourcePath);
            TMP_FontAsset created = TMP_FontAsset.CreateFontAsset(source, 72, 8, GlyphRenderMode.SDFAA, 2048, 2048, AtlasPopulationMode.Dynamic, true);
            if (created == null) throw new InvalidOperationException("TMP 글꼴 자산을 생성하지 못했습니다. TextMeshPro Essential Resources를 확인해 주세요.");
            bool installed = false;
            try
            {
                created.name = "EchoTheater Noto Sans KR SDF";
                created.TryAddCharacters(characters, out string ignoredMissing);
                string missing = StorySourceImporter.MissingGlyphReport(created, characters);
                if (missing.Length != 0) throw new InvalidDataException("동봉 글꼴의 문자 생성에 실패했습니다:\n" + missing);
                // Story text is pre-baked; no runtime fallback guesses or dynamic atlas writes.
                created.atlasPopulationMode = AtlasPopulationMode.Static;
                StoryImportPaths.EnsureAssetFolder(GeneratedRoot);
                AssetDatabase.CreateAsset(created, BundledFontPath);
                installed = true;
                if (created.material != null && !AssetDatabase.Contains(created.material)) AssetDatabase.AddObjectToAsset(created.material, created);
                foreach (Texture2D atlas in created.atlasTextures)
                    if (atlas != null && !AssetDatabase.Contains(atlas)) AssetDatabase.AddObjectToAsset(atlas, created);
                EditorUtility.SetDirty(created);
                AssetDatabase.SaveAssetIfDirty(created);
                AssetDatabase.ImportAsset(BundledFontPath, ImportAssetOptions.ForceSynchronousImport);
                TMP_FontAsset loaded = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(BundledFontPath);
                if (loaded == null || StorySourceImporter.MissingGlyphReport(loaded, characters).Length != 0)
                    throw new IOException("생성 글꼴 저장 후 실제 로드/문자 검증에 실패했습니다.");
                return loaded;
            }
            catch
            {
                if (installed) AssetDatabase.DeleteAsset(BundledFontPath);
                else UnityEngine.Object.DestroyImmediate(created);
                throw;
            }
        }

        public static string Build(string fontAssetPath, bool validateOnly = false)
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException("Play 모드를 종료한 뒤 잔영 극장 씬을 생성해 주세요.");
            Plan plan = ValidateInputs(fontAssetPath);
            ValidateDestinationOwnership();
            if (validateOnly)
                return "잔영 극장 사전 검증 통과: 22씬, " + plan.package.nodes.Length + "노드, 모든 Resources 원화와 한국어 글꼴을 실제로 불러왔습니다. 에셋 변경 없음.";
            for (int i = 0; i < SceneManager.sceneCount; i++)
                if (SceneManager.GetSceneAt(i).path == ScenePath)
                    throw new InvalidOperationException("생성 대상 씬이 열려 있습니다. 변경 사항을 저장하고 해당 씬을 닫은 뒤 다시 생성해 주세요: " + ScenePath);

            StoryImportPaths.EnsureAssetFolder(GeneratedRoot);
            Scene previous = SceneManager.GetActiveScene();
            // A fresh batch Editor starts with an untitled scene and refuses an
            // additive NewScene until that scene is saved. It contains no user work.
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,
                Application.isBatchMode && string.IsNullOrEmpty(previous.path) ? NewSceneMode.Single : NewSceneMode.Additive);
            try
            {
                SceneManager.SetActiveScene(scene);
                GameObject root = new GameObject("EchoTheaterStory");
                StoryRunner runner = root.AddComponent<StoryRunner>();
                StoryView view = root.AddComponent<StoryView>();
                runner.storyJson = plan.story;
                runner.presentationJson = plan.presentation;
                runner.defaultResourcePath = StoryResourceId;
                runner.presentationResourcePath = PresentationResourceId;
                runner.view = view;
                runner.restoreOnStart = true;
                runner.reducedMotion = false;
                runner.finishLegacyCueBeforeAdvancing = false;
                runner.settleFirstFrameOnStart = true;
                view.showPreviewStatus = false;

                GameObject canvas = StoryPreviewBuilder.CreateCanvas(root.transform, view, plan.font);
                ConfigureTheaterCanvas(canvas, view, runner);
                CreateBackdropCamera(root.transform);
                string input = CreateEventSystem(root.transform, plan.warnings);
                root.GetComponentInChildren<EventSystem>(true).firstSelectedGameObject = view.advanceButton.gameObject;
                AddAudioAndOptionalBridge(root, runner, plan.nodeSounds);
                SetInitialFrame(view, plan.package);
                Canvas.ForceUpdateCanvases();
                ValidateSceneHierarchy(root, view);
                if (!EditorSceneManager.SaveScene(scene, ScenePath))
                    throw new IOException("잔영 극장 생성 씬을 저장하지 못했습니다: " + ScenePath);
                WriteOwnershipStamp(plan.font);
                return "잔영 극장 씬 생성 완료: " + ScenePath + "\n22씬 / " + plan.package.nodes.Length + "노드 · 원화 contain · 화자 얼굴 카드\n입력 모듈: " + input +
                    "\nGameplay adapters: 미연결(0개). 이 씬은 대화 재생 전용입니다." +
                    (plan.warnings.Count == 0 ? "" : "\n" + string.Join("\n", plan.warnings));
            }
            finally
            {
                if (scene.IsValid() && scene.isLoaded) EditorSceneManager.CloseScene(scene, true);
                if (previous.IsValid() && previous.isLoaded) SceneManager.SetActiveScene(previous);
            }
        }

        private static Plan ValidateInputs(string fontPath)
        {
            if (string.IsNullOrWhiteSpace(fontPath))
                throw new InvalidDataException("한국어 TMP_FontAsset을 선택하거나 -storyFont Assets/.../Korean.asset을 지정해 주세요. 기본 라틴 글꼴로 대체하지 않습니다.");
            string normalizedFont = fontPath.Replace('\\', '/');
            if (!normalizedFont.StartsWith("Assets/", StringComparison.Ordinal) || !normalizedFont.EndsWith(".asset", StringComparison.OrdinalIgnoreCase))
                throw new InvalidDataException("-storyFont는 프로젝트 안의 기존 TMP_FontAsset 경로여야 합니다: Assets/.../Korean.asset");
            var plan = new Plan
            {
                story = LoadResource<TextAsset>(StoryResourceId, ".json"),
                presentation = LoadResource<TextAsset>(PresentationResourceId, ".json")
            };
            plan.package = JsonUtility.FromJson<StoryPackage>(plan.story.text);
            plan.presentationData = JsonUtility.FromJson<StoryPresentationPackage>(plan.presentation.text);
            if (!StoryGraph.TryCreate(plan.package, out var graph, out var graphErrors))
                throw new InvalidDataException("잔영 극장 스토리 데이터 오류:\n" + string.Join("\n", graphErrors));
            List<string> presentationErrors = StoryPresentationIndex.Validate(plan.presentationData, graph);
            if (presentationErrors.Count != 0)
                throw new InvalidDataException("잔영 극장 연출 데이터 오류:\n" + string.Join("\n", presentationErrors));
            string[] expected = Enumerable.Range(1, 22).Select(i => "T" + i.ToString("00")).ToArray();
            var actual = new HashSet<string>(plan.package.nodes.Select(n => n.sceneId), StringComparer.Ordinal);
            if (actual.Count != 22 || expected.Any(id => !actual.Contains(id)))
                throw new InvalidDataException("T01~T22의 22개 장면이 모두 있어야 합니다.");
            if (graph.Node(plan.package.startNodeId).sceneId != "T01")
                throw new InvalidDataException("잔영 극장의 시작 노드는 T01이어야 합니다.");
            if (plan.presentationData.scenes.Length != 0)
                throw new InvalidDataException("이 빌더는 원본 CG를 그대로 표시합니다. theater_presentation.scenes는 비워두고 독립 화자 카드로 연출해 주세요.");
            foreach (StoryPresentationShot shot in plan.presentationData.shots)
                if (shot.camera != null && (Mathf.Abs(shot.camera.scale - 1) > 0.0001f || Mathf.Abs(shot.camera.focusX - 0.5f) > 0.0001f || Mathf.Abs(shot.camera.focusY - 0.5f) > 0.0001f))
                    throw new InvalidDataException("CG 전체가 보이도록 카메라는 중앙·배율 1이어야 합니다: " + shot.nodeId);
            foreach (StoryNode node in plan.package.nodes)
                foreach (StoryCue cue in node.cues ?? Array.Empty<StoryCue>())
                    if (cue.type == "zoom" && cue.target != "inset" && Mathf.Abs(cue.amount - 1) > 0.0001f)
                        throw new InvalidDataException("전체 CG를 자르는 zoom 큐를 사용할 수 없습니다: " + node.id);
            foreach (StoryAsset asset in plan.package.assets)
            {
                Texture2D texture = LoadResource<Texture2D>(asset.resourcePath, ".png");
                StorySpriteRect r = asset.rect;
                if (r != null && (r.x + r.width > texture.width || r.y + r.height > texture.height))
                    throw new InvalidDataException("원화 경계를 벗어난 Sprite Rect입니다: " + asset.id);
            }
            SoundDocument soundDocument = JsonUtility.FromJson<SoundDocument>(LoadResource<TextAsset>(SoundResourceId, ".json").text);
            if (soundDocument == null || soundDocument.sounds == null)
                throw new InvalidDataException("sound_cues.json에는 sounds 배열이 필요합니다.");
            var soundNodes = new HashSet<string>(StringComparer.Ordinal);
            var sounds = new List<StoryNodeSound>();
            foreach (SoundEntry entry in soundDocument.sounds)
            {
                if (entry == null || graph.Node(entry.nodeId) == null || !soundNodes.Add(entry.nodeId) ||
                    float.IsNaN(entry.volume) || float.IsInfinity(entry.volume) || entry.volume < 0 || entry.volume > 1)
                    throw new InvalidDataException("중복·미등록 노드 또는 잘못된 음량을 가진 sound cue가 있습니다.");
                AudioClip clip = LoadResource<AudioClip>(entry.resourcePath, ".wav");
                sounds.Add(new StoryNodeSound { nodeId = entry.nodeId, clips = new[] { clip }, volume = entry.volume });
            }
            plan.nodeSounds = sounds.ToArray();
            // No deferred runtime lookup: all JSON, portraits and scene textures were loaded above.
            plan.font = StorySourceImporter.ResolveFont(normalizedFont,
                StorySourceImporter.CollectVisibleText(plan.package) + "\n잔영 극장 연출 줄이기 일시정지 다음 처음부터", plan.warnings);
            return plan;
        }

        private static T LoadResource<T>(string resourceId, string extension) where T : UnityEngine.Object
        {
            string normalized = StoryImportPaths.ValidateRelativePath(resourceId, "잔영 극장 Resources ID");
            if (!normalized.StartsWith(ResourcePrefix, StringComparison.Ordinal) || !string.IsNullOrEmpty(Path.GetExtension(normalized)))
                throw new InvalidDataException("잔영 극장 Resources 경로가 잘못되었습니다: " + resourceId);
            string assetPath = StoryImportPaths.OutputRoot + "/Resources/" + normalized + extension;
            T direct = AssetDatabase.LoadAssetAtPath<T>(assetPath);
            T runtime = Resources.Load<T>(normalized);
            if (direct == null || runtime == null || AssetDatabase.GetAssetPath(runtime) != assetPath)
                throw new InvalidDataException("Resources 에셋이 없거나 같은 ID가 다른 폴더와 충돌합니다: " + assetPath);
            return direct;
        }

        private static void ConfigureTheaterCanvas(GameObject canvasObject, StoryView view, StoryRunner runner)
        {
            CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.referenceResolution = new Vector2(1080, 1920);
            scaler.matchWidthOrHeight = 1; // Desktop keeps 1920 logical pixels high before pillarboxing.
            RectTransform safe = canvasObject.transform.Find("SafeArea") as RectTransform;
            if (safe == null) throw new InvalidOperationException("재사용 Canvas에 SafeArea가 없습니다.");
            RectTransform portrait = MakeRect("PortraitViewport", safe);
            var fitter = portrait.gameObject.AddComponent<AspectRatioFitter>();
            fitter.aspectMode = AspectRatioFitter.AspectMode.FitInParent;
            fitter.aspectRatio = 1080f / 1920f;
            foreach (Transform child in safe.Cast<Transform>().ToArray())
                if (child != portrait) child.SetParent(portrait, false);
            RectTransform artViewport = canvasObject.transform.Find("ArtViewport") as RectTransform;
            if (artViewport == null) throw new InvalidOperationException("재사용 Canvas에 ArtViewport가 없습니다.");
            // The ranch viewport owns a top-aligned 9:16 crop. This scene places art
            // inside the theater's fixed upper window instead.
            var ranchViewport = artViewport.GetComponent<StoryArtViewport>();
            if (ranchViewport != null) UnityEngine.Object.DestroyImmediate(ranchViewport);
            artViewport.SetParent(portrait, false);
            SetFill(artViewport, new Vector2(0, 0.31f), new Vector2(1, 0.94f));
            SetFill(view.artworkRoot, Vector2.zero, Vector2.one);
            var cover = view.artImage.GetComponent<StoryCoverImage>();
            if (cover != null) UnityEngine.Object.DestroyImmediate(cover);
            SetFill(view.artImage.rectTransform, Vector2.zero, Vector2.one);
            view.artImage.preserveAspect = true;
            view.artImage.raycastTarget = false;
            artViewport.SetAsFirstSibling();

            RectTransform dialogue = view.dialogueGroup.transform as RectTransform;
            dialogue.anchorMin = new Vector2(0, 0); dialogue.anchorMax = new Vector2(1, 0.29f);
            dialogue.pivot = new Vector2(0.5f, 0);
            dialogue.offsetMin = new Vector2(24, 118); dialogue.offsetMax = new Vector2(-24, 0);
            PlaceText(view.speakerText, 280, 26, 54, 36);
            PlaceText(view.bodyText, 280, 104, 250, 40);
            // Keep the body inside the tray at every safe-area height; StoryView pages long turns.
            view.bodyText.rectTransform.anchorMin = Vector2.zero;
            view.bodyText.rectTransform.anchorMax = Vector2.one;
            view.bodyText.rectTransform.offsetMin = new Vector2(280, 24);
            view.bodyText.rectTransform.offsetMax = new Vector2(-32, -104);
            view.bodyText.lineSpacing = 7;
            RectTransform card = MakeRect("SpeakerPortraitActor", dialogue);
            card.anchorMin = card.anchorMax = new Vector2(0, 0.5f);
            card.pivot = new Vector2(0, 0.5f);
            card.sizeDelta = new Vector2(216, 216);
            card.anchoredPosition = new Vector2(32, 0);
            Image portraitImage = card.gameObject.AddComponent<Image>();
            portraitImage.color = Color.white; portraitImage.preserveAspect = true; portraitImage.raycastTarget = false;
            portraitImage.enabled = false;
            StorySpeakerPortrait speakerPortrait = card.gameObject.AddComponent<StorySpeakerPortrait>();
            speakerPortrait.actorRect = card; speakerPortrait.actorImage = portraitImage;
            if (!speakerPortrait.Attach(view)) throw new InvalidOperationException("화자 얼굴 카드의 독립 레이어 바인딩에 실패했습니다.");

            RectTransform status = view.statusText.transform.parent as RectTransform;
            status.SetParent(portrait, false);
            // The 2% gap belongs to UI: pause/end status never covers the dialogue or original CG.
            status.anchorMin = new Vector2(0, 0.29f); status.anchorMax = new Vector2(1, 0.31f); status.pivot = new Vector2(0.5f, 0.5f);
            status.offsetMin = new Vector2(40, 0); status.offsetMax = new Vector2(-40, 0);
            view.statusText.rectTransform.offsetMin = new Vector2(16, 0);
            view.statusText.rectTransform.offsetMax = new Vector2(-16, 0);
            view.statusText.fontSize = 22;
            view.statusText.enableAutoSizing = false;
            RectTransform top = portrait.Find("TopControls") as RectTransform;
            top.offsetMin = new Vector2(24, -96); top.offsetMax = new Vector2(-24, 0);
            // Reuse all existing controls; the builder adds no competing next/retry controls.
            foreach (Selectable selectable in portrait.GetComponentsInChildren<Selectable>(true))
            {
                RectTransform rt = selectable.transform as RectTransform;
                if (rt == null) continue;
                LayoutElement minimum = selectable.GetComponent<LayoutElement>() ?? selectable.gameObject.AddComponent<LayoutElement>();
                minimum.minWidth = 96; minimum.minHeight = 96;
                ConfigureButtonFeedback(selectable, runner);
            }
            // All safe-area normalization remains on the one existing outer SafeArea component.
            StorySafeArea safeArea = safe.GetComponent<StorySafeArea>();
            safeArea.target = safe;
            safeArea.Refresh();
        }

        private static void SetInitialFrame(StoryView view, StoryPackage package)
        {
            if (!StoryGraph.TryCreate(package, out var graph, out var errors))
                throw new InvalidDataException(string.Join("\n", errors));
            StoryNode first = graph.Node(package.startNodeId);
            StoryAsset firstArt = graph.Asset(first.artId);
            Texture2D texture = LoadResource<Texture2D>(firstArt.resourcePath, ".png");
            // Persistent generated Sprite prevents a non-asset Sprite.Create reference in the saved scene.
            if (AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(PreviewSpritePath) != null)
                if (!AssetDatabase.DeleteAsset(PreviewSpritePath)) throw new IOException("생성된 첫 화면 Sprite를 갱신하지 못했습니다.");
            Sprite sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 0.5f), 100, 0, SpriteMeshType.FullRect);
            sprite.name = "EchoTheaterFirstFrame";
            AssetDatabase.CreateAsset(sprite, PreviewSpritePath);
            view.artImage.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(PreviewSpritePath);
            if (view.artImage.sprite == null) throw new IOException("생성한 첫 화면 Sprite를 실제로 불러오지 못했습니다.");
            view.speakerText.text = first.speaker ?? "";
            view.bodyText.text = first.text ?? "";
            view.statusText.text = "";
            view.SetPaused(false); // StoryView owns empty/paused/end status visibility.
        }

        private static void ConfigureButtonFeedback(Selectable selectable, StoryRunner runner)
        {
            Image hitbox = selectable.GetComponent<Image>();
            if (hitbox == null) throw new InvalidOperationException("기존 버튼 hitbox Image가 없습니다: " + selectable.name);
            RectTransform visual = MakeRect("FeedbackVisual", selectable.transform);
            Image background = visual.gameObject.AddComponent<Image>();
            background.color = Color.white; background.raycastTarget = false;
            foreach (Transform child in selectable.transform.Cast<Transform>().ToArray())
                if (child != visual) child.SetParent(visual, false);
            hitbox.color = Color.clear; hitbox.raycastTarget = true;
            selectable.targetGraphic = background;
            selectable.transition = Selectable.Transition.ColorTint;
            ColorBlock colors = selectable.colors;
            colors.normalColor = new Color32(249, 235, 210, 255);
            colors.highlightedColor = new Color32(104, 214, 226, 255);
            colors.selectedColor = new Color32(104, 214, 226, 255);
            colors.pressedColor = new Color32(255, 200, 104, 255);
            colors.disabledColor = new Color32(115, 121, 143, 255);
            colors.colorMultiplier = 1; colors.fadeDuration = 0.08f;
            selectable.colors = colors;
            Navigation navigation = selectable.navigation; navigation.mode = Navigation.Mode.Automatic; selectable.navigation = navigation;
            foreach (TMP_Text label in visual.GetComponentsInChildren<TMP_Text>(true)) label.color = new Color32(20, 24, 50, 255);
            StoryButtonFeedback feedback = selectable.gameObject.AddComponent<StoryButtonFeedback>();
            feedback.Configure(runner, visual);
        }

        private static void AddAudioAndOptionalBridge(GameObject root, StoryRunner runner, StoryNodeSound[] sounds)
        {
            var obj = new GameObject("StorySfx", typeof(AudioSource));
            obj.transform.SetParent(root.transform, false);
            AudioSource source = obj.GetComponent<AudioSource>();
            source.playOnAwake = false; source.loop = false; source.spatialBlend = 0;
            StoryAudioFeedback audio = root.AddComponent<StoryAudioFeedback>();
            audio.runner = runner; audio.source = source; audio.nodeSounds = sounds;
            audio.advanceClip = null;
            StoryGameplayBridge bridge = root.AddComponent<StoryGameplayBridge>();
            bridge.runner = runner; bridge.sinks = Array.Empty<MonoBehaviour>();
        }

        public static void ValidateSceneHierarchy(GameObject root, StoryView view)
        {
            if (root.GetComponentsInChildren<Canvas>(true).Length != 1)
                throw new InvalidOperationException("생성 씬에는 Canvas가 정확히 하나 있어야 합니다.");
            if (root.GetComponentsInChildren<StoryRunner>(true).Length != 1 || root.GetComponentsInChildren<StoryView>(true).Length != 1)
                throw new InvalidOperationException("스토리 Runner/View가 중복되었습니다.");
            if (root.GetComponentsInChildren<EventSystem>(true).Length != 1)
                throw new InvalidOperationException("생성 씬에는 EventSystem이 정확히 하나 있어야 합니다.");
            if (root.GetComponentsInChildren<StorySafeArea>(true).Length != 1)
                throw new InvalidOperationException("SafeArea 적용은 정확히 한 번이어야 합니다.");
            if (view.artImage.GetComponent<StoryCoverImage>() != null || !view.artImage.preserveAspect)
                throw new InvalidOperationException("원본 CG contain 배치가 아닙니다.");
            RectTransform artViewport = view.artworkRoot.parent as RectTransform;
            if (artViewport == null || artViewport.anchorMin != new Vector2(0, 0.31f) || artViewport.anchorMax != new Vector2(1, 0.94f))
                throw new InvalidOperationException("원화 창이 지정한 상단 0.31~0.94 범위에 있지 않습니다.");
            AspectRatioFitter fitter = root.GetComponentsInChildren<AspectRatioFitter>(true).Single();
            if (fitter.aspectMode != AspectRatioFitter.AspectMode.FitInParent || Mathf.Abs(fitter.aspectRatio - 9f / 16f) > 0.0001f)
                throw new InvalidOperationException("PC 9:16 컨테이너 설정이 잘못되었습니다.");
            if (view.bodyText.fontSize != 40 || view.speakerText.fontSize != 36)
                throw new InvalidOperationException("대사/화자 글꼴 크기가 40/36이 아닙니다.");
            if (root.GetComponentsInChildren<Button>(true).Length != 5)
                throw new InvalidOperationException("기존 5개 버튼 외 중복 버튼이 추가되었습니다.");
            foreach (Selectable selectable in root.GetComponentsInChildren<Selectable>(true))
            {
                StoryButtonFeedback feedback = selectable.GetComponent<StoryButtonFeedback>();
                if (feedback == null || feedback.visual == null || feedback.visual == selectable.transform || !feedback.visual.IsChildOf(selectable.transform))
                    throw new InvalidOperationException("버튼 피드백이 고정 hitbox 아래의 독립 visual을 사용하지 않습니다.");
                LayoutElement minimum = selectable.GetComponent<LayoutElement>();
                if (minimum == null || minimum.minWidth < 96 || minimum.minHeight < 96 || selectable.navigation.mode == Navigation.Mode.None)
                    throw new InvalidOperationException("버튼 최소 터치 영역 또는 키보드 내비게이션이 빠졌습니다.");
            }
            if (root.GetComponentsInChildren<AudioListener>(true).Length != 1)
                throw new InvalidOperationException("생성 씬에는 AudioListener가 정확히 하나 있어야 합니다.");
            StorySpeakerPortrait portrait = root.GetComponentInChildren<StorySpeakerPortrait>(true);
            if (portrait == null || portrait.actorRect == null || portrait.actorImage == null ||
                !portrait.actorRect.IsChildOf(view.dialogueGroup.transform) || portrait.actorRect.sizeDelta != new Vector2(216, 216) || portrait.actorImage == view.artImage)
                throw new InvalidOperationException("216×216 독립 화자 초상 카드가 올바르게 연결되지 않았습니다.");
            EventSystem system = root.GetComponentInChildren<EventSystem>(true);
            if (system.GetComponents<BaseInputModule>().Length != 1)
                throw new InvalidOperationException("입력 모듈이 중복되거나 누락되었습니다.");
            StoryAudioFeedback audio = root.GetComponent<StoryAudioFeedback>();
            if (audio == null || audio.source == null || audio.advanceClip != null || audio.source.loop || audio.source.spatialBlend != 0 ||
                audio.nodeSounds == null || audio.nodeSounds.Any(s => s == null || s.clips == null || s.clips.Length == 0 || s.clips.Any(c => c == null)))
                throw new InvalidOperationException("명시적인 2D 노드 효과음 연결이 올바르지 않습니다.");
        }

        private static void ValidateDestinationOwnership()
        {
            string fullScene = StoryImportPaths.ToAbsoluteAssetPath(ScenePath);
            string fullStamp = StoryImportPaths.ToAbsoluteAssetPath(StampPath);
            StoryPreviewBuildStamp stamp = AssetDatabase.LoadAssetAtPath<StoryPreviewBuildStamp>(StampPath);
            if (File.Exists(fullStamp) && (stamp == null || stamp.builderVersion != OwnerVersion))
                throw new IOException("다른 생성기의 소유 기록을 덮어쓸 수 없습니다: " + StampPath);
            if (File.Exists(StoryImportPaths.ToAbsoluteAssetPath(PreviewSpritePath)) &&
                (stamp == null || stamp.ownedAssetPaths == null || Array.IndexOf(stamp.ownedAssetPaths, PreviewSpritePath) < 0))
                throw new IOException("소유 기록이 없는 첫 화면 Sprite를 덮어쓸 수 없습니다: " + PreviewSpritePath);
            if (!File.Exists(fullScene)) return;
            if (stamp == null || stamp.builderVersion != OwnerVersion || stamp.sceneGuid != AssetDatabase.AssetPathToGUID(ScenePath))
                throw new IOException("잔영 극장 생성본 소유 기록이 없는 씬을 덮어쓸 수 없습니다: " + ScenePath);
        }

        private static void WriteOwnershipStamp(TMP_FontAsset font)
        {
            StoryPreviewBuildStamp stamp = AssetDatabase.LoadAssetAtPath<StoryPreviewBuildStamp>(StampPath);
            if (stamp == null)
            {
                stamp = ScriptableObject.CreateInstance<StoryPreviewBuildStamp>();
                AssetDatabase.CreateAsset(stamp, StampPath);
            }
            stamp.builderVersion = OwnerVersion;
            stamp.sceneGuid = AssetDatabase.AssetPathToGUID(ScenePath);
            stamp.fontGuid = AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(font));
            stamp.ownedAssetPaths = new[] { ScenePath, StampPath, PreviewSpritePath };
            EditorUtility.SetDirty(stamp);
            AssetDatabase.SaveAssetIfDirty(stamp);
        }

        private static string CreateEventSystem(Transform parent, List<string> warnings)
        {
            var obj = new GameObject("StoryEventSystem", typeof(EventSystem));
            obj.transform.SetParent(parent, false);
            Type newInput = AppDomain.CurrentDomain.GetAssemblies().Select(a => a.GetType("UnityEngine.InputSystem.UI.InputSystemUIInputModule", false))
                .FirstOrDefault(t => t != null && typeof(BaseInputModule).IsAssignableFrom(t));
            int activeInput = -1;
            UnityEngine.Object[] settings = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/ProjectSettings.asset");
            if (settings.Length > 0)
                using (var serialized = new SerializedObject(settings[0]))
                {
                    SerializedProperty p = serialized.FindProperty("activeInputHandler");
                    if (p != null) activeInput = p.intValue;
                }
            if (activeInput != 0 && newInput != null)
            {
                if (newInput.GetMethod("AssignDefaultActions", Type.EmptyTypes) == null)
                    throw new InvalidOperationException("새 Input System의 기본 UI actions API가 없습니다.");
                obj.AddComponent(newInput);
                obj.AddComponent<StoryInputBootstrap>().preferNewInputSystem = true;
                return newInput.FullName;
            }
            if (activeInput == 1)
                throw new InvalidOperationException("새 Input System 전용 프로젝트에 UI 입력 모듈이 없습니다. Input System 패키지를 확인해 주세요. 프로젝트 설정은 변경하지 않습니다.");
            obj.AddComponent<StandaloneInputModule>();
            if (activeInput == -1) warnings.Add("입력 설정을 읽지 못해 StandaloneInputModule을 사용했습니다. Play 모드에서 UI 입력을 확인하세요.");
            return typeof(StandaloneInputModule).FullName;
        }

        private static void CreateBackdropCamera(Transform parent)
        {
            var obj = new GameObject("BackdropCamera", typeof(Camera));
            obj.transform.SetParent(parent, false);
            Camera camera = obj.GetComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color32(20, 24, 50, 255);
            camera.cullingMask = 0; camera.orthographic = true; camera.depth = -100;
            obj.AddComponent<AudioListener>();
        }

        private static RectTransform MakeRect(string name, Transform parent)
        {
            var obj = new GameObject(name, typeof(RectTransform));
            RectTransform rt = obj.GetComponent<RectTransform>();
            rt.SetParent(parent, false);
            SetFill(rt, Vector2.zero, Vector2.one);
            return rt;
        }
        private static void SetFill(RectTransform rt, Vector2 min, Vector2 max)
        {
            rt.anchorMin = min; rt.anchorMax = max; rt.pivot = new Vector2(0.5f, 0.5f);
            rt.offsetMin = rt.offsetMax = Vector2.zero;
            rt.localScale = Vector3.one; rt.localRotation = Quaternion.identity;
        }
        private static void PlaceText(TMP_Text text, float left, float top, float height, float size)
        {
            RectTransform rt = text.rectTransform;
            rt.anchorMin = new Vector2(0, 1); rt.anchorMax = Vector2.one; rt.pivot = new Vector2(0.5f, 1);
            rt.offsetMin = new Vector2(left, -top - height); rt.offsetMax = new Vector2(-32, -top);
            text.fontSize = size; text.enableAutoSizing = false;
        }
        private static string RequiredArgument(string[] args, string name)
        {
            int i = Array.IndexOf(args, name);
            if (i < 0 || i + 1 >= args.Length || args[i + 1].StartsWith("-", StringComparison.Ordinal))
                throw new ArgumentException(name + " Assets/.../Korean.asset을 지정해 주세요.");
            return args[i + 1];
        }
    }

    public sealed class EchoTheaterBuildWindow : EditorWindow
    {
        [SerializeField] private TMP_FontAsset font;
        private string result;
        private MessageType messageType = MessageType.Info;
        private void OnEnable()
        {
            if (font == null && Selection.activeObject is TMP_FontAsset selected) font = selected;
        }
        private void OnGUI()
        {
            EditorGUILayout.LabelField("잔영 극장 · 22씬 연속 재생", EditorStyles.boldLabel);
            font = (TMP_FontAsset)EditorGUILayout.ObjectField("한국어 TMP 글꼴", font, typeof(TMP_FontAsset), false);
            EditorGUILayout.HelpBox("기존 한국어 TMP_FontAsset을 선택하세요. 원화·글꼴의 실제 로드 검증을 통과한 뒤 Generated/EchoTheaterStory.unity만 생성합니다. 다른 열린 씬과 프로젝트 입력 설정은 변경하지 않습니다.", MessageType.Info);
            using (new EditorGUI.DisabledScope(EditorApplication.isPlayingOrWillChangePlaymode))
                if (GUILayout.Button("동봉 글꼴로 TMP 자산 만들기", GUILayout.Height(30)))
                {
                    try
                    {
                        font = EchoTheaterSceneBuilder.CreateBundledFontAsset();
                        result = "동봉 Noto Sans KR의 스토리 문자를 생성하고 실제 로드했습니다. 이 글꼴로 사전 검증 또는 씬 생성을 실행하세요.";
                        messageType = MessageType.Info;
                    }
                    catch (Exception error) { result = error.Message; messageType = MessageType.Error; Debug.LogException(error); }
                }
            using (new EditorGUI.DisabledScope(font == null || EditorApplication.isPlayingOrWillChangePlaymode))
                using (new EditorGUILayout.HorizontalScope())
                {
                    if (GUILayout.Button("사전 검증", GUILayout.Height(36))) Execute(true);
                    if (GUILayout.Button("씬 생성", GUILayout.Height(36))) Execute(false);
                }
            if (!string.IsNullOrEmpty(result)) EditorGUILayout.HelpBox(result, messageType);
        }
        private void Execute(bool validateOnly)
        {
            try
            {
                result = EchoTheaterSceneBuilder.Build(AssetDatabase.GetAssetPath(font), validateOnly);
                messageType = MessageType.Info;
                if (!validateOnly) EditorGUIUtility.PingObject(AssetDatabase.LoadAssetAtPath<SceneAsset>(EchoTheaterSceneBuilder.ScenePath));
            }
            catch (Exception error) { result = error.Message; messageType = MessageType.Error; Debug.LogException(error); }
        }
    }
}
#endif
