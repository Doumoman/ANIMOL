using System;
using TMPro;
using UnityEngine;

namespace WilliamParadox.Story
{
    /// <summary>Fixed book with a clipped text-only scroll; ordinary clicks never end the chapter.</summary>
    public sealed class StoryCreditsPlayer
    {
        public const float LastParagraphHoldSeconds = 3;
        private readonly StoryView view;
        private string[] paragraphs = Array.Empty<string>();
        private bool reduced, reachedEnd, localPaused;
        private float position, hold, endPosition;
        private int paragraph;
        public bool Active { get; private set; }
        public bool CanContinue { get { return Active && reachedEnd && hold >= LastParagraphHoldSeconds; } }
        public StoryCreditsPlayer(StoryView storyView) { view = storyView; }

        public void Begin(string[] text, bool reducedMotion, StorySaveData saved = null)
        {
            paragraphs = text ?? Array.Empty<string>();
            reduced = reducedMotion; reachedEnd = false; localPaused = false; hold = 0; paragraph = 0;
            Active = true;
            Layout(false);
            if (saved != null)
            {
                paragraph = Mathf.Clamp(saved.creditsParagraph, 0, Mathf.Max(0, paragraphs.Length - 1));
                reachedEnd = saved.creditsReachedEnd;
                localPaused = saved.creditsPaused;
                hold = Mathf.Clamp(saved.creditsHold, 0, LastParagraphHoldSeconds);
                position = Mathf.Clamp(saved.creditsPosition, -ViewportHeight, endPosition);
                if (reduced) Layout(true);
            }
            Present();
        }
        private float ViewportHeight { get { return view.creditsViewport == null ? 900 : Mathf.Max(1, view.creditsViewport.rect.height); } }
        private float ViewportWidth { get { return view.creditsViewport == null ? 760 : Mathf.Max(1, view.creditsViewport.rect.width); } }
        private void Layout(bool keepProgress)
        {
            if (view.creditsText == null) return;
            view.creditsText.richText = true;
            if (view.creditsContent != null)
            {
                view.creditsContent.anchorMin = view.creditsContent.anchorMax = view.creditsContent.pivot = new Vector2(0.5f, 1);
                view.creditsContent.sizeDelta = new Vector2(ViewportWidth, 0);
            }
            view.creditsText.text = reduced ? Escape(paragraphs.Length == 0 ? "" : paragraphs[paragraph]) : Escape(string.Join("\n\n", paragraphs));
            view.creditsText.alignment = reduced ? TextAlignmentOptions.Center : TextAlignmentOptions.Top;
            Canvas.ForceUpdateCanvases();
            view.creditsText.ForceMeshUpdate();
            var height = Mathf.Max(1, view.creditsText.GetPreferredValues(view.creditsText.text, ViewportWidth, float.PositiveInfinity).y);
            if (view.creditsContent != null) view.creditsContent.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, reduced ? ViewportHeight : height);
            float finalHeight = paragraphs.Length == 0 ? 0 : view.creditsText.GetPreferredValues(Escape(paragraphs[paragraphs.Length - 1]), ViewportWidth, float.PositiveInfinity).y;
            endPosition = Mathf.Max(-ViewportHeight, height - finalHeight * 0.5f - ViewportHeight * 0.5f);
            if (!keepProgress) position = -ViewportHeight;
        }
        private static string Escape(string text)
        {
            return "<noparse>" + (text ?? "").Replace("</noparse>", "") + "</noparse>";
        }
        public void Tick(float delta)
        {
            if (!Active || localPaused) return;
            delta = Mathf.Max(0, delta);
            if (!reachedEnd && !reduced)
            {
                position = Mathf.Min(endPosition, position + 36 * delta);
                if (position >= endPosition) reachedEnd = true;
            }
            if (reachedEnd) hold = Mathf.Min(LastParagraphHoldSeconds, hold + delta);
            Present();
        }
        public void OnAdvance()
        {
            if (!Active) return;
            if (reduced)
            {
                if (paragraph + 1 < paragraphs.Length) { paragraph++; Layout(true); }
                if (paragraph >= paragraphs.Length - 1) reachedEnd = true;
            }
            else localPaused = !localPaused;
            Present();
        }
        public void SetReducedMotion(bool value)
        {
            if (!Active || reduced == value) return;
            reduced = value; localPaused = false; hold = 0; reachedEnd = false;
            if (reduced) { paragraph = 0; Layout(true); if (paragraphs.Length <= 1) reachedEnd = true; }
            else { paragraph = 0; Layout(false); }
            Present();
        }
        private void Present()
        {
            if (!Active) return;
            if (view.creditsContent != null) view.creditsContent.anchoredPosition = reduced ? Vector2.zero : new Vector2(0, position);
            StoryView.SetButton(view.creditsContinueButton, CanContinue);
            if (view.advanceButtonLabel != null) view.advanceButtonLabel.text = reduced ? (reachedEnd ? "마지막 기록" : "다음 기록") : localPaused ? "기록 재개" : "기록 일시정지";
            if (view.advanceButton != null) view.advanceButton.interactable = !reduced || !reachedEnd;
            if (view.statusText != null) view.statusText.text = reduced ? (reachedEnd ? (CanContinue ? "기록을 다 읽었습니다. 계속 버튼을 눌러주세요." : "마지막 기록을 읽는 중…") : (paragraph + 1) + " / " + paragraphs.Length + " · 다음 버튼으로 다음 기록") : (CanContinue ? "기록을 다 읽었습니다. 계속 버튼을 눌러주세요." : localPaused ? "기록 일시정지 · 다음 버튼으로 재개" : "다음 버튼으로 기록 일시정지");
        }
        public void Stop(bool keepVisual)
        {
            Active = false;
            StoryView.SetButton(view.creditsContinueButton, false);
            if (!keepVisual) StoryView.SetGroup(view.creditsGroup, false);
        }
        public void Capture(StorySaveData s)
        {
            s.creditsPosition = position; s.creditsHold = hold; s.creditsParagraph = paragraph;
            s.creditsReachedEnd = reachedEnd; s.creditsPaused = localPaused;
        }
    }
}
