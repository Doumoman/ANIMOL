using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace ANIMOL.AnimalUiV2.Editor
{
    public static class AnimalUiV2Builder
    {
        public const string Root = "Assets/ANIMOL/AnimalUiV2";
        private const string Generated = Root + "/Generated";
        private static readonly Color Ink = Hex("#1a1c2c"), Paper = Hex("#f4f4f4"), Gold = Hex("#ffcd75"), Teal = Hex("#257179");
        private static Font _font;
        private static Sprite _panel, _primary, _secondary, _check, _lock;

        public static void BuildPreview() => BuildDemo(null);

        public static void BuildUpgradePhase1() => BuildDemo(AnimalUiMode.CharacterUpgrade);

        public static void BuildStagePhase2() => BuildDemo(AnimalUiMode.StageAnimalSelect);

        public static void BuildMultiplayerPhase3() => BuildDemo(AnimalUiMode.MultiplayerAnimalSelect);

        private static void BuildDemo(AnimalUiMode? singleMode)
        {
            bool upgradeOnly = singleMode == AnimalUiMode.CharacterUpgrade;
            bool stageOnly = singleMode == AnimalUiMode.StageAnimalSelect;
            bool multiplayerOnly = singleMode == AnimalUiMode.MultiplayerAnimalSelect;
            if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Play 모드를 종료한 뒤 프리뷰를 생성해주세요.");
            // Never replace/unload the user's loaded scenes. Temporary content is built in an additive scene only.
            var originalActive = SceneManager.GetActiveScene();
            var temporary = default(Scene);
            try
            {
                EnsureFolder(Generated);
                ImportOwnArt();
                _font = Need<Font>(Root + "/Art/Fonts/NotoSansKR-UI.ttf");
                _panel = Sprite("Frames/UI_Common_Panel_Main.png"); _primary = Sprite("Frames/UI_Common_Button_Primary.png");
                _secondary = Sprite("Frames/UI_Common_Button_Secondary.png"); _check = Sprite("Icons/UI_Icon_Check.png"); _lock = Sprite("Icons/UI_Icon_Lock.png");
                var catalog = BuildCatalog();
                if (upgradeOnly) AnimalUiV2Validation.RunUpgradeContractChecks(catalog);
                else if (stageOnly) AnimalUiV2Validation.RunStageContractChecks(catalog);
                else if (multiplayerOnly) AnimalUiMultiplayerPhase3Validation.RunContractChecks(catalog);
                else AnimalUiV2Validation.RunContractChecks(catalog);
                temporary = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
                SceneManager.SetActiveScene(temporary);
                var demoRoot = new GameObject(upgradeOnly ? "AnimalUpgradePhase1ReadonlyDemo" : stageOnly ? "AnimalStagePhase2ReadonlyDemo" : multiplayerOnly ? "AnimalMultiplayerPhase3ReadonlyDemo" : "AnimalUiV2ReadonlyDemo");
                var screens = new List<AnimalUiPresenter>();
                var modes = singleMode.HasValue ? new[] { singleMode.Value } : (AnimalUiMode[])Enum.GetValues(typeof(AnimalUiMode));
                foreach (AnimalUiMode mode in modes)
                {
                    var instance = BuildScreen(mode, catalog);
                    if (stageOnly || multiplayerOnly) instance.OpenReadonlyPreviewOnStart = false;
                    var prefabPath = Generated + "/" + mode + ".prefab";
                    PrefabUtility.SaveAsPrefabAsset(instance.gameObject, prefabPath);
                    instance.transform.SetParent(demoRoot.transform, false);
                    instance.gameObject.SetActive(singleMode.HasValue ? mode == singleMode.Value : mode == AnimalUiMode.CharacterUpgrade); screens.Add(instance);
                }
                if (upgradeOnly)
                {
                    var host = demoRoot.AddComponent<AnimalUpgradePhase1DemoHost>(); host.Screen = screens[0];
                }
                else if (stageOnly)
                {
                    var host = demoRoot.AddComponent<AnimalStagePhase2DemoHost>(); host.Screen = screens[0];
                }
                else if (multiplayerOnly)
                {
                    var host = demoRoot.AddComponent<AnimalMultiplayerPhase3DemoHost>(); host.Screen = screens[0];
                }
                else
                {
                    var switcher = demoRoot.AddComponent<AnimalUiDemoSwitcher>(); switcher.Screens = screens.ToArray();
                }
                EnsureDemoEventSystem();
                string sceneName = upgradeOnly ? "AnimalUpgradePhase1Demo.unity" : stageOnly ? "AnimalStagePhase2Demo.unity" : multiplayerOnly ? "AnimalMultiplayerPhase3Demo.unity" : "AnimalUiV2Demo.unity";
                if (!EditorSceneManager.SaveScene(temporary, Generated + "/" + sceneName))
                    throw new IOException("프리뷰 씬을 저장하지 못했습니다.");
                Debug.Log("ANIMOL Animal UI 생성 완료. Generated/" + sceneName + "를 열고 Play를 누르세요. Backend 미연결 상태에서는 일러스트 열람만 가능합니다.");
            }
            finally
            {
                if (temporary.IsValid() && temporary.isLoaded) EditorSceneManager.CloseScene(temporary, true);
                if (originalActive.IsValid() && originalActive.isLoaded) SceneManager.SetActiveScene(originalActive);
                _font = null; _panel = _primary = _secondary = _check = _lock = null;
            }
        }
        private static void ImportOwnArt()
        {
            var folder = Root + "/Art";
            if (!Directory.Exists(folder)) throw new DirectoryNotFoundException(folder + "에 제공된 아트를 먼저 복사해주세요.");
            AssetDatabase.Refresh();
            foreach (var path in Directory.GetFiles(folder, "*.png", SearchOption.AllDirectories))
            {
                var unityPath = path.Replace('\\', '/');
                var importer = AssetImporter.GetAtPath(unityPath) as TextureImporter;
                if (importer == null) throw new IOException("PNG를 가져오지 못했습니다: " + unityPath);
                AnimalUiV2AssetPostprocessor.Configure(importer, unityPath); importer.SaveAndReimport();
            }
        }
        private static AnimalCatalog BuildCatalog()
        {
            var path = Generated + "/AnimalCatalogV2.asset";
            var catalog = AssetDatabase.LoadAssetAtPath<AnimalCatalog>(path);
            bool created = catalog == null; if (created) catalog = ScriptableObject.CreateInstance<AnimalCatalog>();
            catalog.Animals = new List<AnimalDefinition>();
            string[] ids = { "Rabbit", "Wolf", "WhiteFerret", "MountainGoat", "Otter", "DreamFox", "StarCat", "MirrorDeer", "DreamMole", "ClockMoth", "Swallow", "Owl", "FlyingSquirrel", "Hummingbird", "Bat" };
            string[] names = { "토끼", "늑대", "흰족제비", "산양", "수달", "몽환여우", "별고양이", "거울사슴", "꿈두더지", "시계나방", "제비", "올빼미", "하늘다람쥐", "벌새", "박쥐" };
            for (int i = 0; i < ids.Length; i++) catalog.Animals.Add(new AnimalDefinition
            {
                Id = ids[i], DisplayName = names[i], Role = (AnimalRole)(i / 5),
                Portrait = Sprite("Portraits/" + (i + 1).ToString("00") + "_" + ids[i] + ".png"), ArtDescription = "v17 원본 초상"
            });
            if (created) AssetDatabase.CreateAsset(catalog, path); else { EditorUtility.SetDirty(catalog); AssetDatabase.SaveAssetIfDirty(catalog); }
            if (!catalog.IsValid(out var error)) throw new InvalidOperationException(error);
            return catalog;
        }
        private static AnimalUiPresenter BuildScreen(AnimalUiMode mode, AnimalCatalog catalog)
        {
            var root = Node(mode.ToString(), null, typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            var canvas = root.GetComponent<Canvas>(); canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = root.GetComponent<CanvasScaler>(); scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080, 1920); scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0; scaler.referencePixelsPerUnit = 100;
            var view = root.AddComponent<AnimalUiScreenView>();
            var presenter = root.AddComponent<AnimalUiPresenter>(); presenter.Catalog = catalog; presenter.View = view;
            presenter.PreviewMode = mode; presenter.OpenReadonlyPreviewOnStart = true;
            view.GrowthBackground = Sprite("Backgrounds/BG_UI_Growth_Common.png");
            view.CampaignBackground = Sprite("Backgrounds/BG_UI_Campaign_T01.png");
            view.MultiplayerBackground = Sprite("Backgrounds/BG_UI_Campaign_Common.png");
            var bg = Node("Background", root.transform, typeof(Image), typeof(AspectRatioFitter));
            Rect(bg).anchorMin = Rect(bg).anchorMax = new Vector2(0.5f, 0.5f);
            var aspect = bg.GetComponent<AspectRatioFitter>(); aspect.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent; aspect.aspectRatio = 0.5f;
            view.Background = bg.GetComponent<Image>(); view.Background.raycastTarget = false; view.Background.useSpriteMesh = false;
            var shade = Node("BackgroundShade", root.transform, typeof(Image)); Stretch(Rect(shade)); shade.GetComponent<Image>().color = new Color(0.102f, 0.11f, 0.173f, 0.25f); shade.GetComponent<Image>().raycastTarget = false;
            var safe = Node("SafeArea", root.transform, typeof(AnimalUiSafeArea)); Stretch(Rect(safe));
            var body = Node("BodyInteraction", safe.transform, typeof(CanvasGroup)); Stretch(Rect(body)); view.BodyInteraction = body.GetComponent<CanvasGroup>();
            var header = Panel("Header", body.transform, 0, 72, 972, 300);
            view.Back = Button("Back", header.transform, 60, 84, 132, 132, "", false);
            Icon("BackIcon", view.Back.transform, Sprite("Icons/UI_Icon_Back.png"), 34, 34, 64, 64);
            view.Title = Text("Title", header.transform, 224, 60, 688, 78, "동물 강화", 46, Ink);
            view.Subtitle = Text("Subtitle", header.transform, 228, 146, 684, 46, "15종 동물 · 액티브 / 패시브", 28, Teal);
            view.Balances = Text("Balances", header.transform, 228, 202, 684, 38, "코인 -- · 토끼 숙련도 --", 24, Ink);
            var scroll = Node("BodyScroll", body.transform, typeof(ScrollRect)); Stretch(Rect(scroll), 54, 260, -54, -396);
            view.BodyScroll = scroll.GetComponent<ScrollRect>(); view.BodyScroll.horizontal = false; view.BodyScroll.vertical = true; view.BodyScroll.movementType = ScrollRect.MovementType.Clamped;
            var viewport = Node("Viewport", scroll.transform, typeof(Image), typeof(RectMask2D)); Stretch(Rect(viewport)); viewport.GetComponent<Image>().color = Color.clear;
            var content = Node("Content", viewport.transform); Rect(content).anchorMin = new Vector2(0, 1); Rect(content).anchorMax = new Vector2(1, 1); Rect(content).pivot = new Vector2(0.5f, 1);
            Rect(content).sizeDelta = new Vector2(0, 1720); view.BodyContent = Rect(content); view.BodyScroll.viewport = Rect(viewport); view.BodyScroll.content = Rect(content);
            var slots = Node("RoleSlots", content.transform, typeof(HorizontalLayoutGroup)); Top(Rect(slots), 0, 972, 304); view.SelectionSlots = slots;
            var slotsLayout = slots.GetComponent<HorizontalLayoutGroup>(); slotsLayout.spacing = 24; slotsLayout.childAlignment = TextAnchor.UpperCenter;
            slotsLayout.childControlWidth = slotsLayout.childControlHeight = false; slotsLayout.childForceExpandWidth = slotsLayout.childForceExpandHeight = false;
            view.Slots = new AnimalCardView[3];
            for (int i = 0; i < 3; i++) view.Slots[i] = Card("Slot" + i, slots.transform, 308, 304, true);
            var hero = Panel("AnimalDetail", content.transform, 0, 328, 972, 420); view.HeroPanel = Rect(hero);
            view.HeroPortrait = Icon("Portrait", hero.transform, catalog.Find("Rabbit").Portrait, 60, 60, 240, 300);
            view.HeroName = Text("Name", hero.transform, 348, 60, 564, 78, "토끼", 50, Ink);
            view.HeroRole = Text("Role", hero.transform, 348, 146, 564, 46, "지상 동물", 30, Teal);
            view.HeroDescription = Text("Description", hero.transform, 348, 208, 564, 72, "동물 초상과 정보를 확인합니다.", 24, Ink);
            view.HeroState = Text("Availability", hero.transform, 348, 292, 564, 68, "데이터 연결 전", 26, Teal);
            view.SelectionDetailsButton = Button("SelectionDetails", hero.transform, 348, 292, 300, 132, "동물 정보", false);
            var tabs = Node("RoleTabs", content.transform); Top(Rect(tabs), 772, 972, 132); view.TabsPanel = Rect(tabs);
            view.RoleTabs = new Button[3]; view.RoleLabels = new Text[3];
            for (int i = 0; i < 3; i++)
            {
                view.RoleTabs[i] = Button("Role" + i, tabs.transform, i * 332, 0, 308, 132, AnimalUiRules.RoleName((AnimalRole)i), false);
                view.RoleLabels[i] = view.RoleTabs[i].GetComponentInChildren<Text>();
            }
            var roster = Node("RosterScroll", content.transform, typeof(ScrollRect)); Top(Rect(roster), 928, 972, 592); view.RosterPanel = Rect(roster); view.RosterScroll = roster.GetComponent<ScrollRect>();
            var rosterViewport = Node("Viewport", roster.transform, typeof(Image), typeof(RectMask2D)); Stretch(Rect(rosterViewport)); rosterViewport.GetComponent<Image>().color = Color.clear;
            var rosterContent = Node("Cards", rosterViewport.transform, typeof(GridLayoutGroup)); Top(Rect(rosterContent), 0, 972, 592);
            view.RosterContent = Rect(rosterContent); view.RosterLayout = rosterContent.GetComponent<GridLayoutGroup>();
            view.RosterLayout.cellSize = new Vector2(308, 284); view.RosterLayout.spacing = new Vector2(24, 24); view.RosterLayout.constraint = GridLayoutGroup.Constraint.FixedColumnCount; view.RosterLayout.constraintCount = 3;
            view.RosterScroll.content = Rect(rosterContent); view.RosterScroll.viewport = Rect(rosterViewport); view.RosterScroll.horizontal = view.RosterScroll.vertical = false;
            view.CardTemplate = Card("CardTemplate", root.transform, 308, 284, false); view.CardTemplate.gameObject.SetActive(false);
            var tracks = Node("UpgradeTracks", content.transform); Top(Rect(tracks), 908, 972, 744); view.UpgradeTracks = tracks; view.TracksPanel = Rect(tracks);
            view.ActiveTrack = Track("Active", tracks.transform, 0); view.PassiveTrack = Track("Passive", tracks.transform, 384);
            var info = Panel("SelectionInfo", content.transform, 0, 1544, 972, 164); view.InfoPanel = Rect(info);
            view.SelectionInfo = Text("Hint", info.transform, 60, 60, 852, 44, "편성 정보를 연결해주세요.", 26, Ink);
            var footer = Node("Footer", body.transform); Rect(footer).anchorMin = Rect(footer).anchorMax = new Vector2(0.5f, 0); Rect(footer).pivot = new Vector2(0.5f, 0);
            Rect(footer).sizeDelta = new Vector2(972, 204); Rect(footer).anchoredPosition = new Vector2(0, 32);
            view.Refresh = Button("Refresh", footer.transform, 0, 0, 192, 132, "새로고침", false);
            view.Primary = Button("Primary", footer.transform, 216, 0, 756, 132, "선택 완료", true); view.PrimaryLabel = view.Primary.GetComponentInChildren<Text>();
            view.Status = Text("Status", footer.transform, 0, 144, 972, 60, "데이터 연결 전 · 일러스트 열람", 26, Paper);
            BuildModal(view, safe.transform);
            view.SetMode(mode); view.Modal.SetActive(false);
            return presenter;
        }
        private static AnimalUpgradeTrackView Track(string name, Transform parent, float top)
        {
            var panel = Panel(name, parent, 0, top, 972, 360); var track = panel.AddComponent<AnimalUpgradeTrackView>();
            track.Title = Text("Title", panel.transform, 60, 60, 540, 56, name == "Active" ? "액티브 강화 · 상세" : "패시브 강화 · 상세", 34, Ink);
            track.Level = Text("Level", panel.transform, 60, 124, 540, 48, "Lv. --", 30, Teal);
            track.Effect = Text("Effect", panel.transform, 60, 184, 540, 116, "효과 정보 준비 중", 24, Ink);
            track.Cost = Text("Cost", panel.transform, 624, 226, 288, 74, "비용 --", 22, Ink);
            track.UpgradeButton = Button("Upgrade", panel.transform, 624, 68, 288, 132, "강화", true);
            var detail = Node("Details", panel.transform, typeof(Image), typeof(Button)); Place(Rect(detail), 60, 60, 540, 240); detail.GetComponent<Image>().color = Color.clear;
            track.DetailsButton = detail.GetComponent<Button>(); track.DetailsButton.targetGraphic = detail.GetComponent<Image>();
            track.ButtonLabel = track.UpgradeButton.GetComponentInChildren<Text>(); return track;
        }
        private static AnimalCardView Card(string name, Transform parent, float width, float height, bool slot)
        {
            var go = Node(name, parent, typeof(Image), typeof(Button)); Rect(go).sizeDelta = new Vector2(width, height);
            var image = go.GetComponent<Image>(); image.sprite = _panel; image.type = Image.Type.Sliced; image.pixelsPerUnitMultiplier = 1;
            var card = go.AddComponent<AnimalCardView>(); card.Button = go.GetComponent<Button>(); card.Button.targetGraphic = image;
            card.Portrait = Icon("Portrait", go.transform, null, 0, slot ? 60 : 24, 128, 160); CenterX(Rect(card.Portrait.gameObject));
            card.Name = Text("Name", go.transform, 24, slot ? 228 : 190, width - 48, 40, "선택 전", 26, Ink, TextAnchor.MiddleCenter); StretchWidth(Rect(card.Name.gameObject), 24);
            card.Badge = Text("Badge", go.transform, 24, slot ? 24 : 232, width - 48, slot ? 36 : 28, slot ? "역할" : "", slot ? 24 : 18, Teal, TextAnchor.MiddleCenter); StretchWidth(Rect(card.Badge.gameObject), 24);
            card.SelectionMark = Icon("Selected", go.transform, _check, 0, 12, 32, 32); Right(Rect(card.SelectionMark.gameObject), 12, 12); card.SelectionMark.gameObject.SetActive(false);
            card.LockMark = Icon("Lock", go.transform, _lock, 12, 12, 32, 32); card.LockMark.gameObject.SetActive(false);
            return card;
        }
        private static void BuildModal(AnimalUiScreenView view, Transform parent)
        {
            var root = Node("Modal", parent, typeof(Image)); Stretch(Rect(root)); root.GetComponent<Image>().color = new Color(0.102f, 0.11f, 0.173f, 0.85f);
            var panel = Panel("Dialog", root.transform, 0, 0, 972, 812); Rect(panel).anchorMin = Rect(panel).anchorMax = new Vector2(0.5f, 0.5f); Rect(panel).pivot = new Vector2(0.5f, 0.5f); Rect(panel).anchoredPosition = Vector2.zero;
            view.Modal = root; view.ModalTitle = Text("Title", panel.transform, 60, 60, 852, 78, "확인", 48, Ink);
            var scroll = Node("DescriptionScroll", panel.transform, typeof(ScrollRect)); Place(Rect(scroll), 60, 160, 852, 424); scroll.GetComponent<ScrollRect>().horizontal = false;
            var viewport = Node("Viewport", scroll.transform, typeof(Image), typeof(RectMask2D)); Stretch(Rect(viewport)); viewport.GetComponent<Image>().color = Color.clear;
            view.ModalBody = Text("Body", viewport.transform, 0, 0, 852, 424, "설명", 32, Ink, TextAnchor.UpperLeft);
            var fitter = view.ModalBody.gameObject.AddComponent<ContentSizeFitter>(); fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            var bodyRect = Rect(view.ModalBody.gameObject); bodyRect.pivot = new Vector2(0, 1); bodyRect.anchorMin = bodyRect.anchorMax = new Vector2(0, 1);
            var scrollRect = scroll.GetComponent<ScrollRect>(); scrollRect.viewport = Rect(viewport); scrollRect.content = bodyRect; scrollRect.movementType = ScrollRect.MovementType.Clamped; view.ModalBodyScroll = scrollRect;
            view.ModalCancel = Button("Cancel", panel.transform, 60, 620, 402, 132, "취소", false); view.ModalCancelLabel = view.ModalCancel.GetComponentInChildren<Text>();
            view.ModalConfirm = Button("Confirm", panel.transform, 510, 620, 402, 132, "확정", true); view.ModalConfirmLabel = view.ModalConfirm.GetComponentInChildren<Text>();
        }
        private static void EnsureDemoEventSystem()
        {
            var go = new GameObject("AnimalUiV2EventSystem", typeof(EventSystem));
#if ENABLE_INPUT_SYSTEM
            // Reflection avoids adding a compile-time dependency on the Input System package.
            var type = Type.GetType("UnityEngine.InputSystem.UI.InputSystemUIInputModule, Unity.InputSystem");
            if (type == null) throw new InvalidOperationException("활성 Input System의 UI 모듈을 찾지 못했습니다. Input System 패키지를 확인해주세요.");
            var module = go.AddComponent(type);
            type.GetMethod("AssignDefaultActions", Type.EmptyTypes)?.Invoke(module, null);
#elif ENABLE_LEGACY_INPUT_MANAGER
            go.AddComponent<StandaloneInputModule>();
#else
            throw new InvalidOperationException("활성 입력 설정을 확인해주세요. 기존 EventSystem을 사용하는 프로젝트에서는 데모의 입력 모듈만 설정하면 됩니다.");
#endif
        }
        private static GameObject Panel(string name, Transform parent, float x, float top, float width, float height)
        {
            var go = Node(name, parent, typeof(Image)); Top(Rect(go), top, width, height); Rect(go).anchoredPosition += new Vector2(x, 0);
            var image = go.GetComponent<Image>(); image.sprite = _panel; image.type = Image.Type.Sliced; image.pixelsPerUnitMultiplier = 1f / 3f; image.raycastTarget = false; return go;
        }
        private static Button Button(string name, Transform parent, float x, float top, float width, float height, string label, bool primary)
        {
            var go = Node(name, parent, typeof(Image), typeof(Button)); Place(Rect(go), x, top, width, height);
            var image = go.GetComponent<Image>(); image.sprite = primary ? _primary : _secondary; image.type = Image.Type.Sliced; image.pixelsPerUnitMultiplier = 1;
            var button = go.GetComponent<Button>(); button.targetGraphic = image; var colors = button.colors; colors.disabledColor = Hex("#94b0c2"); button.colors = colors;
            Text("Label", go.transform, 26, 28, width - 52, height - 56, label, width < 230 ? 24 : 36, primary ? Paper : Ink, TextAnchor.MiddleCenter); return button;
        }
        private static Text Text(string name, Transform parent, float x, float top, float width, float height, string content, int size, Color color, TextAnchor alignment = TextAnchor.UpperLeft)
        {
            var go = Node(name, parent, typeof(Text)); Place(Rect(go), x, top, width, height); var text = go.GetComponent<Text>();
            text.font = _font; text.fontStyle = FontStyle.Normal; text.fontSize = size; text.text = content; text.color = color; text.alignment = alignment;
            text.horizontalOverflow = HorizontalWrapMode.Wrap; text.verticalOverflow = VerticalWrapMode.Truncate; text.raycastTarget = false; text.supportRichText = false; return text;
        }
        private static Image Icon(string name, Transform parent, Sprite sprite, float x, float top, float width, float height)
        {
            var go = Node(name, parent, typeof(Image)); Place(Rect(go), x, top, width, height); var image = go.GetComponent<Image>();
            image.sprite = sprite; image.preserveAspect = true; image.useSpriteMesh = false; image.raycastTarget = false; return image;
        }
        private static GameObject Node(string name, Transform parent, params Type[] components)
        {
            var types = new List<Type> { typeof(RectTransform) }; types.AddRange(components);
            var go = new GameObject(name, types.ToArray()); if (parent != null) go.transform.SetParent(parent, false); return go;
        }
        private static RectTransform Rect(GameObject go) => (RectTransform)go.transform;
        private static void Place(RectTransform rect, float x, float top, float width, float height)
        {
            rect.anchorMin = rect.anchorMax = new Vector2(0, 1); rect.pivot = new Vector2(0, 1);
            rect.anchoredPosition = new Vector2(x, -top); rect.sizeDelta = new Vector2(width, height);
        }
        private static void Top(RectTransform rect, float top, float width, float height)
        {
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 1); rect.pivot = new Vector2(0.5f, 1);
            rect.anchoredPosition = new Vector2(0, -top); rect.sizeDelta = new Vector2(width, height);
        }
        private static void Stretch(RectTransform rect, float left = 0, float bottom = 0, float right = 0, float top = 0)
        { rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one; rect.offsetMin = new Vector2(left, bottom); rect.offsetMax = new Vector2(right, top); }
        private static void CenterX(RectTransform rect) { rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 1); rect.pivot = new Vector2(0.5f, 1); rect.anchoredPosition = new Vector2(0, rect.anchoredPosition.y); }
        private static void StretchWidth(RectTransform rect, float inset) { rect.anchorMin = new Vector2(0, 1); rect.anchorMax = new Vector2(1, 1); rect.pivot = new Vector2(0.5f, 1); rect.anchoredPosition = new Vector2(0, rect.anchoredPosition.y); rect.sizeDelta = new Vector2(-inset * 2, rect.sizeDelta.y); }
        private static void Right(RectTransform rect, float right, float top) { rect.anchorMin = rect.anchorMax = new Vector2(1, 1); rect.pivot = new Vector2(1, 1); rect.anchoredPosition = new Vector2(-right, -top); }
        private static Color Hex(string html) { ColorUtility.TryParseHtmlString(html, out var color); return color; }
        private static Sprite Sprite(string relative) => Need<Sprite>(Root + "/Art/" + relative);
        private static T Need<T>(string path) where T : UnityEngine.Object
        { var asset = AssetDatabase.LoadAssetAtPath<T>(path); if (asset == null) throw new FileNotFoundException("필수 에셋을 찾지 못했습니다: " + path); return asset; }
        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return; string parent = Path.GetDirectoryName(path).Replace('\\', '/'); EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, Path.GetFileName(path));
        }
    }
}
