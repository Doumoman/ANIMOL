using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace William.NotebookUI
{
    /// <summary>Presentation-only map navigation. Theme browsing never enters a stage or changes its entitlement.</summary>
    [DisallowMultipleComponent]
    public sealed class NotebookMapThemeNavigation : MonoBehaviour, IBeginDragHandler, IScrollHandler
    {
        public NotebookRoot owner;
        public ScrollRect scroll;
        public RectTransform rail;
        public Button[] buttons = new Button[4];
        public Sprite normalSprite, selectedSprite;
        public float[] themeStartY = new float[4];

        public const float RailBottom = 24f, HiddenBottom = -270f;
        public const float HideSeconds = .28f, RevealSeconds = .18f, ScrollSeconds = .42f;
        private CanvasGroup railGroup;
        private Button[] boundButtons;
        private UnityAction[] actions;
        private Coroutine railRoutine, scrollRoutine;
        private Action afterHidden;
        private bool wantedVisible, reducedMotion, inertiaCaptured, previousInertia;
        private float scrollTarget;
        private int activeTheme = -1;
        public int ActiveTheme => Mathf.Max(0, activeTheme);
        public bool RailVisible => rail && rail.gameObject.activeSelf;

        public void Initialize(NotebookRoot root, NotebookCatalog catalog)
        {
            owner = root;
            if (root && root.mapScroll) scroll = root.mapScroll;
            if (catalog != null && catalog.themes != null)
            {
                themeStartY = new float[Mathf.Min(4, catalog.themes.Length)];
                for (int i = 0; i < themeStartY.Length; i++) themeStartY[i] = catalog.themes[i].startY;
            }
            EnsureGroup(); BindButtons(); activeTheme = -1; RefreshActiveTheme();
        }

        private void Awake() { EnsureGroup(); BindButtons(); }
        private void OnEnable()
        {
            EnsureGroup(); BindButtons();
            if (owner && owner.stageScreen && owner.stageScreen.activeInHierarchy && !owner.MapSelectionVisible) ShowRail(false);
        }
        private void LateUpdate()
        {
            RefreshActiveTheme();
            if (!Allowed() && scrollRoutine != null) CancelScroll();
            SetInteractive(wantedVisible && railRoutine == null && Allowed());
        }
        private bool Allowed() => owner && owner.CanNavigateMapThemes;
        private void EnsureGroup()
        {
            if (!rail) return;
            railGroup = rail.GetComponent<CanvasGroup>();
            if (!railGroup) railGroup = rail.gameObject.AddComponent<CanvasGroup>();
        }
        private void BindButtons()
        {
            if (buttons == null) buttons = Array.Empty<Button>();
            if (boundButtons != null && boundButtons.Length == buttons.Length)
            {
                bool same = true;
                for (int i = 0; i < buttons.Length; i++) if (boundButtons[i] != buttons[i]) same = false;
                if (same) return;
            }
            UnbindButtons();
            boundButtons = (Button[])buttons.Clone(); actions = new UnityAction[boundButtons.Length];
            for (int i = 0; i < boundButtons.Length; i++)
            {
                int index = i; actions[i] = () => NavigateToTheme(index);
                if (boundButtons[i]) boundButtons[i].onClick.AddListener(actions[i]);
            }
        }
        private void UnbindButtons()
        {
            if (boundButtons != null && actions != null)
                for (int i = 0; i < boundButtons.Length; i++)
                    if (boundButtons[i]) boundButtons[i].onClick.RemoveListener(actions[i]);
            boundButtons = null; actions = null;
        }
        private float MaxScroll => scroll && scroll.content && scroll.viewport
            ? Mathf.Max(0, scroll.content.rect.height - scroll.viewport.rect.height) : 0;
        private float ClampedStart(int index) => Mathf.Clamp(themeStartY[index], 0, MaxScroll);
        private void RefreshActiveTheme()
        {
            if (!scroll || !scroll.content || themeStartY == null || themeStartY.Length == 0) return;
            float y = Mathf.Clamp(scroll.content.anchoredPosition.y, 0, MaxScroll); int selected = 0;
            for (int i = 1; i < themeStartY.Length; i++) if (y + 1 >= ClampedStart(i)) selected = i;
            if (selected == activeTheme) return;
            activeTheme = selected;
            for (int i = 0; i < buttons.Length; i++)
                if (buttons[i] && buttons[i].image)
                    buttons[i].image.sprite = i == selected && selectedSprite ? selectedSprite : normalSprite;
        }
        private void SetInteractive(bool enabled)
        {
            if (!enabled && rail && EventSystem.current)
            {
                GameObject selected = EventSystem.current.currentSelectedGameObject;
                if (selected && (selected.transform == rail || selected.transform.IsChildOf(rail)))
                {
                    GameObject fallback = owner && owner.mapBack && owner.mapBack.gameObject.activeInHierarchy
                        ? owner.mapBack.gameObject : null;
                    EventSystem.current.SetSelectedGameObject(fallback);
                }
            }
            if (railGroup) { railGroup.interactable = enabled; railGroup.blocksRaycasts = enabled; railGroup.alpha = 1; }
            foreach (Button button in buttons) if (button) button.interactable = enabled;
        }
        private void SetBottom(float y)
        {
            if (rail) rail.anchoredPosition = new Vector2(0, y);
        }
        private void StopRailAnimation()
        {
            if (railRoutine != null) StopCoroutine(railRoutine);
            railRoutine = null;
        }
        public void ShowRail(bool animate)
        {
            if (!rail || !isActiveAndEnabled || !owner || !owner.stageScreen || !owner.stageScreen.activeInHierarchy)
            { HideForExit(); return; }
            bool alreadyVisible = wantedVisible && rail.gameObject.activeSelf && railRoutine == null;
            wantedVisible = true; afterHidden = null; StopRailAnimation(); EnsureGroup();
            if (alreadyVisible) { SetBottom(RailBottom); SetInteractive(Allowed()); return; }
            float from = rail.gameObject.activeSelf ? rail.anchoredPosition.y : HiddenBottom;
            rail.gameObject.SetActive(true); SetInteractive(false);
            if (!animate || reducedMotion || !Application.isPlaying) { SetBottom(RailBottom); SetInteractive(Allowed()); }
            else railRoutine = StartCoroutine(RevealRail(from));
            RefreshActiveTheme();
        }
        private IEnumerator RevealRail(float from)
        {
            float elapsed = 0;
            while (elapsed < RevealSeconds)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / RevealSeconds), eased = 1 - Mathf.Pow(1 - t, 3);
                SetBottom(Mathf.LerpUnclamped(from, RailBottom, eased)); yield return null;
            }
            SetBottom(RailBottom); railRoutine = null; SetInteractive(Allowed());
        }
        public void HideForSelection(Action hidden)
        {
            CancelScroll(); wantedVisible = false; StopRailAnimation(); afterHidden = hidden; SetInteractive(false);
            if (!rail || !rail.gameObject.activeSelf || reducedMotion || !isActiveAndEnabled)
            { FinishHidden(); return; }
            railRoutine = StartCoroutine(BounceAway(rail.anchoredPosition.y));
        }
        private IEnumerator BounceAway(float from)
        {
            float elapsed = 0, peak = from + 14;
            while (elapsed < HideSeconds)
            {
                elapsed += Time.unscaledDeltaTime;
                if (elapsed <= .07f)
                {
                    float t = Mathf.Clamp01(elapsed / .07f), eased = 1 - Mathf.Pow(1 - t, 3);
                    SetBottom(Mathf.LerpUnclamped(from, peak, eased));
                }
                else
                {
                    float t = Mathf.Clamp01((elapsed - .07f) / .21f);
                    SetBottom(Mathf.LerpUnclamped(peak, HiddenBottom, t * t));
                }
                yield return null;
            }
            railRoutine = null; FinishHidden();
        }
        private void FinishHidden()
        {
            SetBottom(HiddenBottom); if (rail) rail.gameObject.SetActive(false);
            Action callback = afterHidden; afterHidden = null; callback?.Invoke();
        }
        public void HideForExit()
        {
            CancelScroll(); StopRailAnimation(); afterHidden = null; wantedVisible = false; SetInteractive(false);
            if (rail) { rail.gameObject.SetActive(false); SetBottom(RailBottom); }
        }
        public void SetReducedMotion(bool enabled)
        {
            reducedMotion = enabled;
            if (enabled) CompleteMotion();
        }
        public void CompleteMotion()
        {
            if (scrollRoutine != null) SetScrollY(scrollTarget);
            CancelScroll();
            if (railRoutine == null) return;
            StopRailAnimation();
            if (wantedVisible) { if (rail) rail.gameObject.SetActive(true); SetBottom(RailBottom); SetInteractive(Allowed()); }
            else FinishHidden();
        }
        public void NavigateToTheme(int index)
        {
            if (!Allowed() || !wantedVisible || railRoutine != null || !scroll || !scroll.content
                || themeStartY == null || index < 0 || index >= themeStartY.Length) return;
            CancelScroll(); scrollTarget = ClampedStart(index); scroll.StopMovement();
            if (reducedMotion) { SetScrollY(scrollTarget); return; }
            previousInertia = scroll.inertia; inertiaCaptured = true; scroll.inertia = false;
            scrollRoutine = StartCoroutine(ScrollTo(scroll.content.anchoredPosition.y, scrollTarget));
        }
        private IEnumerator ScrollTo(float from, float target)
        {
            float elapsed = 0;
            while (elapsed < ScrollSeconds)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / ScrollSeconds), eased = t * t * (3 - 2 * t);
                SetScrollY(Mathf.LerpUnclamped(from, target, eased)); yield return null;
            }
            SetScrollY(target); scrollRoutine = null; RestoreInertia();
        }
        private void SetScrollY(float y)
        {
            if (!scroll || !scroll.content) return;
            scroll.StopMovement();
            Vector2 position = scroll.content.anchoredPosition; position.y = Mathf.Clamp(y, 0, MaxScroll);
            scroll.content.anchoredPosition = position; RefreshActiveTheme();
        }
        private void RestoreInertia()
        {
            if (inertiaCaptured && scroll) scroll.inertia = previousInertia;
            inertiaCaptured = false;
        }
        public void CancelScroll()
        {
            if (scrollRoutine != null) StopCoroutine(scrollRoutine);
            scrollRoutine = null; RestoreInertia(); if (scroll) scroll.StopMovement();
        }
        public void OnBeginDrag(PointerEventData eventData) => CancelScroll();
        public void OnScroll(PointerEventData eventData) => CancelScroll();
        private void OnApplicationFocus(bool focused) { if (!focused) CompleteMotion(); }
        private void OnApplicationPause(bool paused) { if (paused) CompleteMotion(); }
        private void OnDisable() => HideForExit();
        private void OnDestroy() { HideForExit(); UnbindButtons(); }
    }
}
