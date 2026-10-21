using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace WilliamParadox.Story
{
    public sealed class StoryView : MonoBehaviour
    {
        public RectTransform artworkRoot;
        public CanvasGroup artworkGroup;
        public Image artImage, dimImage;
        public RectTransform layeredRoot;
        public Image layeredBackground, layeredForeground;
        public CanvasGroup dialogueGroup;
        public TMP_Text speakerText, bodyText, statusText;
        public Button advanceButton, gateButton, pauseButton, restartButton, creditsContinueButton;
        public TMP_Text gateButtonLabel, pauseButtonLabel, advanceButtonLabel;
        public Image insetImage;
        public CanvasGroup insetGroup;
        public RectTransform insetRoot;
        public CanvasGroup creditsGroup;
        public RectTransform creditsViewport, creditsContent;
        public TMP_Text creditsText;
        public Toggle reducedMotionToggle;

        private readonly Dictionary<string, Sprite> sprites = new Dictionary<string, Sprite>();
        private readonly List<Sprite> ownedSprites = new List<Sprite>();
        private StoryGraph graph;
        private string currentArtId, currentInsetId;
        private Vector2 insetHome;
        private string normalStatus;
        private bool insetHomeCaptured;

        public string CurrentInsetId { get { return currentInsetId; } }
        public void Initialize(StoryGraph value)
        {
            foreach (var sprite in ownedSprites) if (sprite != null) Destroy(sprite);
            ownedSprites.Clear(); sprites.Clear();
            graph = value;
            if (insetRoot != null && !insetHomeCaptured) { insetHome = insetRoot.anchoredPosition; insetHomeCaptured = true; }
            if (artImage != null) artImage.raycastTarget = false;
            if (dimImage != null) dimImage.raycastTarget = false;
            if (insetImage != null) insetImage.raycastTarget = false;
            ResetPresentation();
        }

        public void ResetPresentation()
        {
            currentArtId = null;
            if (artworkRoot != null) { artworkRoot.localScale = Vector3.one; artworkRoot.anchoredPosition = Vector2.zero; artworkRoot.localRotation = Quaternion.identity; }
            if (artworkGroup != null) artworkGroup.alpha = 1;
            SetDim(0);
            SetInsetVisible(false);
            SetGroup(creditsGroup, false);
            if (layeredRoot != null) layeredRoot.gameObject.SetActive(false);
        }

        public Sprite ResolveSprite(string assetId)
        {
            if (string.IsNullOrEmpty(assetId)) return null;
            if (sprites.TryGetValue(assetId, out var cached)) return cached;
            var asset = graph == null ? null : graph.Asset(assetId);
            if (asset == null) throw new InvalidOperationException("Unknown story asset " + assetId);
            Sprite sprite = null;
            bool hasRect = StorySpriteRect.IsSpecified(asset.rect);
            if (!hasRect) sprite = Resources.Load<Sprite>(asset.resourcePath);
            if (sprite == null)
            {
                var texture = Resources.Load<Texture2D>(asset.resourcePath);
                if (texture == null) throw new InvalidOperationException("Missing Resources texture: " + asset.resourcePath);
                var r = hasRect ? asset.rect : null;
                var rect = r == null ? new Rect(0, 0, texture.width, texture.height) : new Rect(r.x, r.y, r.width, r.height);
                if (rect.xMax > texture.width || rect.yMax > texture.height) throw new InvalidOperationException(assetId + ": sprite rect is outside the texture.");
                sprite = Sprite.Create(texture, rect, r == null ? new Vector2(0.5f, 0.5f) : new Vector2(r.pivotX, r.pivotY), 100f, 0, SpriteMeshType.FullRect);
                sprite.name = assetId + "_RuntimeSprite";
                ownedSprites.Add(sprite);
            }
            sprites[assetId] = sprite;
            return sprite;
        }

        public void SetArtwork(string assetId)
        {
            if (string.IsNullOrEmpty(assetId) || currentArtId == assetId) return;
            if (artImage != null)
            {
                artImage.sprite = ResolveSprite(assetId);
                artImage.enabled = true;
                var cover = artImage.GetComponent<StoryCoverImage>();
                if (cover != null) cover.Refresh();
                else artImage.preserveAspect = true; // Builder adds the required cover component.
            }
            currentArtId = assetId;
            if (artworkRoot != null) { artworkRoot.localScale = Vector3.one; artworkRoot.anchoredPosition = Vector2.zero; }
            if (artworkGroup != null) artworkGroup.alpha = 1;
            SetDim(0); SetInsetVisible(false);
        }

        public void PresentNode(StoryNode n, bool keepBook)
        {
            SetArtwork(n.artId);
            bool book = n.kind == "credits" || keepBook;
            SetGroup(creditsGroup, book);
            SetGroup(dialogueGroup, !book && n.kind != "end");
            if (speakerText != null) speakerText.text = n.speaker ?? "";
            if (bodyText != null) bodyText.text = n.text ?? "";
            SetButton(advanceButton, n.kind != "gate" && n.kind != "end");
            SetButton(gateButton, n.kind == "gate");
            SetButton(creditsContinueButton, false);
            if (gateButtonLabel != null) gateButtonLabel.text = n.gateLabel ?? "계속";
            if (advanceButtonLabel != null) advanceButtonLabel.text = "다음";
            normalStatus = n.kind == "gate" ? (n.gateId == "BLOCK_LAMP" ? "이솔의 말을 들었습니다. 등불을 가릴 준비가 되면 버튼을 누르세요." : n.gateId == "PREVIEW_FINAL_CONTINUE" ? "마지막 기록을 읽었습니다. 준비되면 마무리하세요." : "스토리 대화 미리보기 · 퍼즐 플레이는 연결하지 않습니다.") : n.kind == "cue" ? "다음 버튼을 눌러 계속" : "";
            if (statusText != null) statusText.text = normalStatus;
        }

        public void SetDim(float alpha)
        {
            if (dimImage == null) return;
            var color = dimImage.color; color.a = Mathf.Clamp01(alpha); dimImage.color = color;
        }
        public float DimAlpha { get { return dimImage == null ? 0 : dimImage.color.a; } }
        public void ShowInset(string assetId, float x, float y, float scale)
        {
            if (insetImage != null) { insetImage.sprite = ResolveSprite(assetId); insetImage.preserveAspect = true; }
            currentInsetId = assetId;
            if (insetRoot != null)
            {
                insetRoot.anchoredPosition = x == 0 && y == 0 ? insetHome : new Vector2(x, y);
                insetRoot.localScale = Vector3.one * (scale > 0 ? scale : 1);
            }
            SetInsetVisible(true);
        }
        public void SetInsetVisible(bool show) { SetGroup(insetGroup, show); }
        public void SetPaused(bool paused)
        {
            if (pauseButtonLabel != null) pauseButtonLabel.text = paused ? "재개" : "일시정지";
            if (statusText != null) statusText.text = paused ? "일시정지 · 화면 밖에서도 시간이 흐르지 않습니다." : normalStatus;
        }
        public void ShowError(string message)
        {
            Debug.LogError(message, this);
            SetGroup(dialogueGroup, true);
            if (speakerText != null) speakerText.text = "스토리 미리보기 오류";
            if (bodyText != null) bodyText.text = message;
            SetButton(advanceButton, false); SetButton(gateButton, false); SetButton(creditsContinueButton, false);
        }
        public void Capture(StorySaveData s)
        {
            s.artScale = artworkRoot == null ? 1 : artworkRoot.localScale.x;
            s.dimAlpha = DimAlpha;
            s.insetAssetId = currentInsetId;
            s.insetVisible = insetGroup != null && insetGroup.alpha > 0;
            if (insetRoot != null) { s.insetX = insetRoot.anchoredPosition.x; s.insetY = insetRoot.anchoredPosition.y; s.insetScale = insetRoot.localScale.x; }
        }
        public void RestorePresentation(StorySaveData s)
        {
            if (artworkRoot != null) { artworkRoot.localScale = Vector3.one * s.artScale; artworkRoot.anchoredPosition = Vector2.zero; }
            if (artworkGroup != null) artworkGroup.alpha = 1;
            SetDim(s.dimAlpha);
            if (s.insetVisible) ShowInset(s.insetAssetId, s.insetX, s.insetY, s.insetScale); else SetInsetVisible(false);
        }
        public static void SetGroup(CanvasGroup g, bool show)
        {
            if (g == null) return;
            g.alpha = show ? 1 : 0; g.interactable = show; g.blocksRaycasts = show;
        }
        public static void SetButton(Button b, bool show)
        {
            if (b == null) return;
            b.gameObject.SetActive(show); b.interactable = show;
        }
        private void OnDestroy()
        {
            foreach (var sprite in ownedSprites) if (sprite != null) Destroy(sprite);
            ownedSprites.Clear(); sprites.Clear();
        }
    }
}
