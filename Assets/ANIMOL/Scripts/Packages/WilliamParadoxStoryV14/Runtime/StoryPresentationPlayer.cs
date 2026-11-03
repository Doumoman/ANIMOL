using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace WilliamParadox.Story
{
    public struct StoryBounceSample
    {
        public Vector3 scale;
        public float y, rotation;
    }
    public static class StoryPresentationMath
    {
        public const float BounceDuration = 0.32f;
        public static StoryBounceSample Bounce(float normalizedTime)
        {
            float t = Mathf.Clamp01(normalizedTime);
            if (t <= 0 || t >= 1) return new StoryBounceSample { scale = Vector3.one };
            float stretch;
            if (t < 0.2f) stretch = Mathf.Lerp(0, -0.025f, Smooth(t / 0.2f));
            else if (t < 0.5f) stretch = Mathf.Lerp(-0.025f, 0.022f, Smooth((t - 0.2f) / 0.3f));
            else if (t < 0.8f) stretch = Mathf.Lerp(0.022f, -0.008f, Smooth((t - 0.5f) / 0.3f));
            else stretch = Mathf.Lerp(-0.008f, 0, Smooth((t - 0.8f) / 0.2f));
            return new StoryBounceSample
            {
                scale = new Vector3(1 - stretch, 1 + stretch, 1),
                y = Mathf.Sin(Mathf.PI * t) * 10,
                rotation = Mathf.Sin(Mathf.PI * 2 * t) * 0.45f
            };
        }
        public static Vector2 CameraPosition(Vector2 viewport, Vector2 content, float scale, Vector2 focus)
        {
            float zoom = Mathf.Clamp(scale, 1, 2.2f);
            Vector2 desired = new Vector2(-(focus.x - 0.5f) * content.x * zoom, -(focus.y - 0.5f) * content.y * zoom);
            Vector2 margin = new Vector2(Mathf.Max(0, (content.x * zoom - viewport.x) * 0.5f), Mathf.Max(0, (content.y * zoom - viewport.y) * 0.5f));
            return new Vector2(Mathf.Clamp(desired.x, -margin.x, margin.x), Mathf.Clamp(desired.y, -margin.y, margin.y));
        }
        public static float Smooth(float value) { var t = Mathf.Clamp01(value); return t * t * (3 - 2 * t); }
    }

    /// <summary>Only Tick(delta) advances animation. Each actor owns an independent transform.</summary>
    public sealed class StoryPresentationPlayer : IDisposable
    {
        private sealed class Actor
        {
            public RectTransform root;
            public Image image;
            public Vector2 home;
        }
        private readonly StoryView view;
        private readonly StoryGraph graph;
        private readonly StoryPresentationIndex index;
        private readonly Func<string, Sprite> resolveSprite;
        private readonly StorySpeakerPortrait speakerPortrait;
        private readonly Dictionary<string, Actor> actors = new Dictionary<string, Actor>(StringComparer.Ordinal);
        private RectTransform actorContainer;
        private StoryLayerStage stage;
        private string layeredSceneId, lastSceneId, lastArtId;
        private string lastPortraitAssetId, lastPortraitSceneId;
        private Actor bouncing;
        private float bounceElapsed, cameraElapsed;
        private float bounceStrength = 1;
        private bool cameraPlaying, disposed;
        private StoryCameraShot cameraFrom = Wide(), cameraTo = Wide(), cameraCurrent = Wide();
        public bool HasPresentation { get { return index != null; } }
        public bool IsPlaying { get { return bouncing != null || cameraPlaying || speakerPortrait != null && speakerPortrait.IsPlaying; } }
        public StoryPresentationIndex Index { get { return index; } }
        public StorySpeakerPortrait SpeakerPortrait { get { return speakerPortrait; } }
        public RectTransform ActorTransform(string actorId) { return actors.TryGetValue(actorId, out var actor) ? actor.root : null; }
        public StoryPresentationPlayer(StoryView storyView, StoryGraph storyGraph, StoryPresentationPackage package, Func<string, Sprite> spriteResolver = null)
        {
            view = storyView ?? throw new ArgumentNullException(nameof(storyView));
            graph = storyGraph ?? throw new ArgumentNullException(nameof(storyGraph));
            resolveSprite = spriteResolver ?? new Func<string, Sprite>(view.ResolveSprite);
            index = package == null ? null : new StoryPresentationIndex(package, graph);
            speakerPortrait = view.GetComponentInChildren<StorySpeakerPortrait>(true);
            if (speakerPortrait != null) { speakerPortrait.Attach(view); speakerPortrait.Hide(); }
            if (index != null) EnsureStage();
        }

        public void Present(StoryNode node, bool reducedMotion, bool restoring)
        {
            if (disposed || index == null || node == null) return;
            FinishAll();
            var scene = index.Scene(node.sceneId);
            // A scene's alternate CG (for example T16_AFTER) is not a book or an ending.
            // Keep the pre-credits R17 background hold compatible with the original ranch data.
            bool legacyBookHold = node.sceneId == "R17" && node.kind == "cue" && scene != null &&
                node.artId != scene.sceneId && graph.Asset(node.artId)?.kind == "background";
            bool bookOrEnd = node.kind == "credits" || node.kind == "end" || node.gateId == "PREVIEW_FINAL_CONTINUE" || legacyBookHold;
            bool alternateArtwork = scene != null && node.artId != scene.sceneId;
            bool useLayers = scene != null && !bookOrEnd && !alternateArtwork && (node.kind == "line" || node.kind == "cue");
            // Delivery labels can differ while resolving to the same character card.
            // Only two non-null portrait assets in the same scene count as one speaker turn.
            var portraitAsset = node.kind == "line" ? index.Portrait(node.speaker) : null;
            bool samePortraitSpeaker = lastPortraitSceneId == node.sceneId &&
                !string.IsNullOrEmpty(lastPortraitAssetId) && portraitAsset != null && lastPortraitAssetId == portraitAsset.assetId;
            bool portraitTurnChanged = !samePortraitSpeaker;
            if (node.kind == "line") { lastPortraitSceneId = node.sceneId; lastPortraitAssetId = portraitAsset?.assetId; }
            if (useLayers)
            {
                ConfigureScene(scene);
                var states = index.ResolveActorStates(graph, node);
                foreach (var pair in actors)
                    if (states.TryGetValue(pair.Key, out var assetId)) pair.Value.image.sprite = resolveSprite(assetId);
                view.layeredRoot.gameObject.SetActive(true);
                if (view.artImage != null) view.artImage.enabled = false;
            }
            else
            {
                if (view.layeredRoot != null) view.layeredRoot.gameObject.SetActive(false);
                if (view.artImage != null) view.artImage.enabled = true;
            }

            bool newArtwork = lastSceneId != node.sceneId || lastArtId != node.artId;
            cameraFrom = Copy(newArtwork ? Wide() : cameraCurrent);
            cameraTo = Copy(bookOrEnd ? Wide() : index.ResolveCamera(graph, node));
            cameraElapsed = 0;
            bool different = Mathf.Abs(cameraFrom.scale - cameraTo.scale) > 0.0001f || Mathf.Abs(cameraFrom.focusX - cameraTo.focusX) > 0.0001f || Mathf.Abs(cameraFrom.focusY - cameraTo.focusY) > 0.0001f;
            cameraPlaying = different && !restoring && !reducedMotion && cameraTo.duration > 0;
            cameraCurrent = Copy(cameraPlaying ? cameraFrom : cameraTo);
            lastSceneId = node.sceneId; lastArtId = node.artId;
            if (speakerPortrait != null)
            {
                var portrait = !useLayers && !bookOrEnd && node.kind == "line" && index.AllowsPortrait(node.sceneId) ? portraitAsset : null;
                if (portrait == null) speakerPortrait.Hide();
                else if (speakerPortrait.Show(view, resolveSprite(portrait.assetId), portrait.independentLayer) &&
                    portraitTurnChanged && !restoring && !reducedMotion && index.ShouldBounce(node))
                    speakerPortrait.BeginBounce(index.BounceStrength(node.sceneId));
            }
            if (useLayers && !restoring && !reducedMotion && index.ShouldBounce(node))
            {
                string speaker = index.SpeakerActor(node);
                if (!string.IsNullOrEmpty(speaker) && actors.TryGetValue(speaker, out var actor)) { bouncing = actor; bounceElapsed = 0; bounceStrength = index.BounceStrength(node.sceneId); }
            }
            RefreshLayout();
        }

        public void Tick(float delta)
        {
            if (disposed || index == null) return;
            float step = Mathf.Max(0, delta);
            if (cameraPlaying)
            {
                cameraElapsed += step;
                float t = cameraTo.duration <= 0 ? 1 : Mathf.Clamp01(cameraElapsed / cameraTo.duration);
                float smooth = StoryPresentationMath.Smooth(t);
                cameraCurrent = new StoryCameraShot
                {
                    focusX = Mathf.Lerp(cameraFrom.focusX, cameraTo.focusX, smooth),
                    focusY = Mathf.Lerp(cameraFrom.focusY, cameraTo.focusY, smooth),
                    scale = Mathf.Lerp(cameraFrom.scale, cameraTo.scale, smooth)
                };
                if (t >= 1) { cameraPlaying = false; cameraCurrent = Copy(cameraTo); }
            }
            if (bouncing != null)
            {
                bounceElapsed += step;
                float t = Mathf.Clamp01(bounceElapsed / StoryPresentationMath.BounceDuration);
                var sample = StoryPresentationMath.Bounce(t);
                bouncing.root.anchoredPosition = bouncing.home + new Vector2(0, sample.y * bounceStrength);
                bouncing.root.localScale = Vector3.one + (sample.scale - Vector3.one) * bounceStrength;
                bouncing.root.localRotation = Quaternion.Euler(0, 0, sample.rotation * bounceStrength);
                if (t >= 1) ResetBounce();
            }
            if (speakerPortrait != null) speakerPortrait.Tick(step);
            RefreshLayout();
        }
        public void FinishAll()
        {
            ResetBounce();
            if (speakerPortrait != null) speakerPortrait.FinishAll();
            cameraPlaying = false; cameraCurrent = Copy(cameraTo);
            RefreshLayout();
        }
        public void Capture(StorySaveData saved)
        {
            // Authored camera intent, not a mid-tween transform. Poses are rebuilt from the node route.
            if (index != null) saved.artScale = cameraTo.scale;
        }
        public void RefreshLayout()
        {
            if (disposed || index == null || view.artworkRoot == null) return;
            if (stage != null) stage.Refresh();
            Vector2 viewportSize = view.artworkRoot.rect.size;
            if (viewportSize.x <= 0 || viewportSize.y <= 0) return;
            bool layered = view.layeredRoot != null && view.layeredRoot.gameObject.activeSelf;
            if (!layered && view.artImage != null) view.artImage.GetComponent<StoryCoverImage>()?.Refresh();
            Vector2 content = layered && stage != null ? stage.DisplayedSize : view.artImage == null ? viewportSize : view.artImage.rectTransform.rect.size;
            if (content.x <= 0 || content.y <= 0) content = viewportSize;
            view.artworkRoot.localScale = Vector3.one * cameraCurrent.scale;
            view.artworkRoot.anchoredPosition = StoryPresentationMath.CameraPosition(viewportSize, content, cameraCurrent.scale, new Vector2(cameraCurrent.focusX, cameraCurrent.focusY));
        }
        public void Dispose()
        {
            if (disposed) return;
            FinishAll(); ClearActors();
            if (speakerPortrait != null) speakerPortrait.Hide();
            if (actorContainer != null)
            {
                actorContainer.gameObject.SetActive(false);
                if (Application.isPlaying) UnityEngine.Object.Destroy(actorContainer.gameObject); else UnityEngine.Object.DestroyImmediate(actorContainer.gameObject);
                actorContainer = null;
            }
            if (view.layeredRoot != null) view.layeredRoot.gameObject.SetActive(false);
            disposed = true;
        }
        private void EnsureStage()
        {
            if (view.artworkRoot == null) throw new InvalidOperationException("ArtworkRoot is required for layered presentation.");
            if (view.layeredRoot == null) view.layeredRoot = MakeRect("LayeredStage", view.artworkRoot);
            stage = view.layeredRoot.GetComponent<StoryLayerStage>() ?? view.layeredRoot.gameObject.AddComponent<StoryLayerStage>();
            stage.viewport = view.artworkRoot;
            stage.logicalSize = new Vector2(index.Package.canvasWidth, index.Package.canvasHeight);
            stage.Refresh();
            if (view.layeredBackground == null) view.layeredBackground = MakeImage("LayerBackground", view.layeredRoot);
            view.layeredBackground.rectTransform.SetAsFirstSibling();
            var cover = view.layeredBackground.GetComponent<StoryCoverImage>() ?? view.layeredBackground.gameObject.AddComponent<StoryCoverImage>();
            cover.viewport = view.layeredRoot; cover.image = view.layeredBackground;
            actorContainer = MakeRect("Actors", view.layeredRoot); Fill(actorContainer);
            if (view.layeredForeground == null) view.layeredForeground = MakeImage("LayerForeground", view.layeredRoot);
            view.layeredForeground.rectTransform.SetAsLastSibling();
            view.layeredRoot.gameObject.SetActive(false);
        }
        private void ConfigureScene(StoryLayeredScene scene)
        {
            if (layeredSceneId != scene.sceneId)
            {
                ClearActors();
                foreach (var config in (scene.actors ?? Array.Empty<StoryActorPlacement>()).OrderBy(x => x.sortOrder))
                {
                    var image = MakeImage("Actor_" + config.actorId, actorContainer);
                    var actor = new Actor { root = image.rectTransform, image = image };
                    Place(actor.root, config); actor.home = actor.root.anchoredPosition;
                    actor.image.preserveAspect = true; actor.image.sprite = resolveSprite(config.assetId);
                    actors.Add(config.actorId, actor);
                }
                layeredSceneId = scene.sceneId;
            }
            view.layeredBackground.sprite = resolveSprite(scene.backgroundArtId);
            view.layeredBackground.GetComponent<StoryCoverImage>().Refresh();
            bool foreground = !string.IsNullOrEmpty(scene.foregroundArtId);
            view.layeredForeground.enabled = foreground;
            if (foreground)
            {
                view.layeredForeground.sprite = resolveSprite(scene.foregroundArtId);
                view.layeredForeground.preserveAspect = true;
                Place(view.layeredForeground.rectTransform, scene.foreground ?? new StoryStagePlacement());
            }
        }
        private void ResetBounce()
        {
            if (bouncing != null && bouncing.root != null)
            {
                bouncing.root.anchoredPosition = bouncing.home;
                bouncing.root.localScale = Vector3.one;
                bouncing.root.localRotation = Quaternion.identity;
            }
            bouncing = null; bounceElapsed = 0;
        }
        private void ClearActors()
        {
            ResetBounce();
            foreach (var actor in actors.Values)
            {
                if (actor.root == null) continue;
                actor.root.gameObject.SetActive(false);
                if (Application.isPlaying) UnityEngine.Object.Destroy(actor.root.gameObject); else UnityEngine.Object.DestroyImmediate(actor.root.gameObject);
            }
            actors.Clear(); layeredSceneId = null;
        }
        private void Place(RectTransform root, StoryStagePlacement p)
        {
            root.anchorMin = root.anchorMax = Vector2.zero;
            root.pivot = new Vector2(p.pivotX, p.pivotY);
            root.sizeDelta = new Vector2(p.width, p.height);
            root.anchoredPosition = new Vector2(p.x * index.Package.canvasWidth, p.y * index.Package.canvasHeight);
            root.localScale = Vector3.one; root.localRotation = Quaternion.identity;
        }
        private static RectTransform MakeRect(string name, Transform parent)
        {
            var root = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
            root.SetParent(parent, false); return root;
        }
        private static void Fill(RectTransform root)
        {
            root.anchorMin = Vector2.zero; root.anchorMax = Vector2.one; root.offsetMin = root.offsetMax = Vector2.zero;
        }
        private static Image MakeImage(string name, Transform parent)
        {
            var rect = MakeRect(name, parent); Fill(rect);
            var image = rect.gameObject.AddComponent<Image>(); image.raycastTarget = false; return image;
        }
        private static StoryCameraShot Wide() { return new StoryCameraShot { focusX = 0.5f, focusY = 0.5f, scale = 1, duration = 0 }; }
        private static StoryCameraShot Copy(StoryCameraShot c) { return new StoryCameraShot { focusX = c.focusX, focusY = c.focusY, scale = c.scale, duration = c.duration }; }
    }
}
