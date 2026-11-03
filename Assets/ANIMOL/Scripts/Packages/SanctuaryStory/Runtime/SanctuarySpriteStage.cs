using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace ANIMOL.SanctuaryStory
{
    /// <summary>
    /// The concrete story world presenter. Install beneath the existing SC18 Canvas,
    /// before its dialogue/choice siblings. Owns only its child visuals and cue tokens.
    /// It never moves the host player, changes timeScale, writes a save, or owns UI cues.
    /// </summary>
    [DisallowMultipleComponent, RequireComponent(typeof(RectTransform))]
    public sealed class SanctuarySpriteStage : MonoBehaviour
    {
        [SerializeField] private SanctuaryStoryRunner runner;
        [SerializeField] private SanctuaryVisualCatalog catalog;
        [SerializeField] private TextAsset presentationJson;
        [SerializeField] private AudioSource optionalAudioSource;
        [Header("1080 x 1920 story composition, inside the existing safe-area parent")]
        [SerializeField, Range(.3f, .5f)] private float normalFeetHeight = .40f;
        [SerializeField, Range(.60f, .67f)] private float choiceFeetHeight = .615f;
        [SerializeField, Range(.7f, 1f)] private float choiceActorScale = .84f;
        [Header("Starway speaker bounce: once per utterance, 0.32 seconds")]
        [SerializeField, Range(0f, 1f)] private float speakerBounceStrength = 1f;
        [SerializeField] private bool reducedMotion;

        private enum Pose { Idle, Move, Jump, Prepare, Lower, Boundary }
        private sealed class Actor
        {
            public string id;
            public Image image;
            public SanctuaryCharacterVisual visual;
            public Pose pose;
            public float animationClock, poseProgress;
            public Vector2 offset;
            public bool flipped;
            public string variantId = "neutral";
            public float authoredX, authoredFeet = .4f, authoredScale = 1f;
            public bool visible;
        }

        private static readonly string[] ActorIds = { "lumi", "doyun", "sein", "daon", "watcher" };
        private readonly List<Actor> actors = new List<Actor>();
        private RectTransform stageRect;
        private CanvasGroup stageGroup;
        private Image background, worldFade;
        private SanctuaryFoldGraphic path, boundary, fold;
        private SanctuaryStoryRunner subscribedRunner;
        private SanctuaryScene currentScene;
        private Coroutine cueRoutine;
        private bool laboratory, choiceLayout, denseChoices, watcherPrepared, cueContactPlayed, cueFoldPlayed, cueLabRestored;
        private bool pauseObserved, skipNextDelta, warnedMissingPrepare, warnedMissingAssets;
        private float layoutBlend, pathAmount, frameDelta;
        private Vector2 cueStartDoyunOffset, cueStartWatcherOffset;
        private int epoch, activeToken, clockFrame = -1;
        private const float SpeakerBounceDuration = .32f;
        private Actor bouncingActor;
        private SanctuaryLine presentedLine;
        private float bounceElapsed, bounceStrength;
        private SanctuaryPresentationData presentation;
        private SanctuaryScenePresentation scenePresentation;
        private TextAsset loadedPresentationAsset;
        private string loadedPresentationText;
        private SanctuaryChapter validatedChapter;
        private SanctuaryVisualCatalog validatedCatalog;
        private bool presentationReady, warnedPresentation;

        public SanctuaryVisualCatalog Catalog { get { return catalog; } }
        public TextAsset PresentationJson { get { return presentationJson; } }

        /// <summary>Editor installer and host entry point. Creates no Canvas or EventSystem.</summary>
        public void Configure(SanctuaryStoryRunner targetRunner, SanctuaryVisualCatalog visualCatalog)
        {
            Configure(targetRunner, visualCatalog, presentationJson);
        }

        public void Configure(SanctuaryStoryRunner targetRunner, SanctuaryVisualCatalog visualCatalog, TextAsset visualPresentation)
        {
            FinishSpeakerBounce();
            CancelCue();
            Unsubscribe();
            runner = targetRunner;
            catalog = visualCatalog;
            presentationJson = visualPresentation;
            presentationReady = false;
            warnedPresentation = false;
            EnsureVisuals();
            if (isActiveAndEnabled) Subscribe();
            Synchronize();
        }

        public void SetOptionalAudioSource(AudioSource source) { optionalAudioSource = source; }

        /// <summary>Connect the existing accessibility setting; creates no settings owner.</summary>
        public void SetReducedMotion(bool enabled)
        {
            reducedMotion = enabled;
            if (enabled) FinishSpeakerBounce();
        }

        /// <summary>Idempotent and limited to children of this owned stage object.</summary>
        public void EnsureVisuals()
        {
            FinishSpeakerBounce();
            stageRect = GetComponent<RectTransform>();
            stageGroup = GetComponent<CanvasGroup>();
            if (stageGroup == null) stageGroup = gameObject.AddComponent<CanvasGroup>();
            stageGroup.interactable = false;
            stageGroup.blocksRaycasts = false;
            background = EnsureImage("Background"); Stretch(background.rectTransform);
            background.preserveAspect = false;
            path = EnsureGraphic("NearFootPath", SanctuaryWorldShape.Path);
            path.color = new Color(.77f, .83f, .65f, .36f);
            boundary = EnsureGraphic("BoundaryBeyondHand", SanctuaryWorldShape.Boundary);
            boundary.color = new Color(.025f, .035f, .055f, .62f);
            actors.Clear();
            foreach (string id in ActorIds)
            {
                Image image = EnsureImage("Actor_" + id);
                image.preserveAspect = true;
                image.rectTransform.anchorMin = image.rectTransform.anchorMax = new Vector2(.5f, 0f);
                image.rectTransform.pivot = new Vector2(.5f, 0f);
                var actor = new Actor { id = id, image = image, visual = catalog == null ? null : catalog.FindCharacter(id) };
                actors.Add(actor);
                SetPose(actor, Pose.Idle, 0f);
            }
            fold = EnsureGraphic("SingleAngularFold", SanctuaryWorldShape.Fold); Stretch(fold.rectTransform);
            fold.color = new Color(.15f, .20f, .25f, 1f);
            worldFade = EnsureImage("WorldTransition"); Stretch(worldFade.rectTransform);
            worldFade.color = new Color(.035f, .055f, .075f, 0f);
            SetChildOrder();
            ResetEffects();
            stageGroup.alpha = currentScene == null ? 0f : 1f;
        }

        private void OnEnable() { EnsureVisuals(); Subscribe(); Synchronize(); }
        private void Start() { Synchronize(); }
        private void OnDisable() { FinishSpeakerBounce(); CancelCue(); Unsubscribe(); if (stageGroup != null) stageGroup.alpha = 0f; }
        private void OnDestroy() { FinishSpeakerBounce(); CancelCue(); Unsubscribe(); }

        private void Update()
        {
            if (subscribedRunner != runner) { Unsubscribe(); Subscribe(); Synchronize(); }
            float dt = PresentationDelta();
            if (currentScene == null || stageRect == null) return;
            if (reducedMotion) FinishSpeakerBounce();
            if (bouncingActor != null)
            {
                bounceElapsed = Mathf.Min(SpeakerBounceDuration, bounceElapsed + dt);
                if (bounceElapsed >= SpeakerBounceDuration) bouncingActor = null;
            }
            float target = choiceLayout ? 1f : 0f;
            layoutBlend = Mathf.MoveTowards(layoutBlend, target, dt / .18f);
            foreach (Actor actor in actors)
            {
                if (actor.pose == Pose.Idle || actor.pose == Pose.Move) actor.animationClock += dt;
                ApplySprite(actor);
            }
            LayoutActors();
        }

        private float PresentationDelta()
        {
            // Host input can pause after Update but before coroutine resumption in the same frame.
            if (runner == null || runner.IsPaused) { pauseObserved = true; return frameDelta = 0f; }
            if (clockFrame == Time.frameCount) return frameDelta;
            clockFrame = Time.frameCount;
            if (pauseObserved || skipNextDelta)
            {
                pauseObserved = skipNextDelta = false;
                return frameDelta = 0f;
            }
            return frameDelta = Mathf.Max(0f, Time.unscaledDeltaTime);
        }

        private void OnApplicationPause(bool paused)
        {
            if (paused) pauseObserved = true;
            else skipNextDelta = true;
        }

        private void Subscribe()
        {
            if (runner == null || subscribedRunner == runner) return;
            subscribedRunner = runner;
            runner.SceneChanged += OnSceneChanged;
            runner.LineChanged += OnLineChanged;
            runner.ChoicesPresented += OnChoicesPresented;
            runner.CueRequested += OnCueRequested;
            runner.StateChanged += OnStateChanged;
            runner.PlaybackCompleted += OnPlaybackCompleted;
        }

        private void Unsubscribe()
        {
            if (subscribedRunner != null)
            {
                subscribedRunner.SceneChanged -= OnSceneChanged;
                subscribedRunner.LineChanged -= OnLineChanged;
                subscribedRunner.ChoicesPresented -= OnChoicesPresented;
                subscribedRunner.CueRequested -= OnCueRequested;
                subscribedRunner.StateChanged -= OnStateChanged;
                subscribedRunner.PlaybackCompleted -= OnPlaybackCompleted;
            }
            subscribedRunner = null;
        }

        private void Synchronize()
        {
            if (runner == null || runner.Flow == null || runner.Flow.CurrentScene == null)
            {
                if (stageGroup != null) stageGroup.alpha = 0f;
                return;
            }
            OnSceneChanged(runner.Flow.CurrentScene);
            if (!presentationReady) return;
            RestoreAuthoredStateAtCurrentStep();
            presentedLine = runner.Flow.CurrentLine;
            if (runner.Flow.CurrentChoices != null && (runner.Flow.State == SanctuaryFlowState.PreparingChoice ||
                runner.Flow.State == SanctuaryFlowState.Choice || runner.Flow.State == SanctuaryFlowState.Hesitating))
                OnChoicesPresented(runner.Flow.CurrentChoices);
            if (runner.Flow.State == SanctuaryFlowState.Cue && runner.Flow.PendingCue != null)
                OnCueRequested(runner.Flow.PendingCue, runner.Flow.PendingCueToken);
        }

        private void OnSceneChanged(SanctuaryScene scene)
        {
            FinishSpeakerBounce();
            presentedLine = null;
            CancelCue();
            currentScene = scene;
            if (scene == null) { stageGroup.alpha = 0f; return; }
            if (!EnsurePresentation()) { stageGroup.alpha = 0f; return; }
            laboratory = IsLaboratory(scene.id);
            choiceLayout = denseChoices = false; layoutBlend = 0f;
            watcherPrepared = scene.id == "S12";
            ResetEffects();
            // Reconstruct from scene identity, including direct checkpoint entry and skip.
            pathAmount = laboratory || scene.id == "S02" || scene.id == "S03" ? 0f : scene.id == "S06" ? .32f : .64f;
            path.SetProgress(pathAmount);
            boundary.SetProgress(scene.id == "S04" || scene.id == "S08" || scene.id == "S09" ? .38f : 0f);
            ApplyScenePresentation(scene.id);
            foreach (Actor actor in actors)
            {
                actor.offset = Vector2.zero;
                if (actor.id == "doyun" && !laboratory)
                {
                    float forward = scene.id == "S12" ? 56f : scene.id == "S04" || scene.id == "S08" || scene.id == "S09" || scene.id == "S11" ? 44f : 0f;
                    actor.offset = new Vector2(forward * stageRect.rect.width / 1080f, 0f);
                }
                if (actor.id == "watcher" && !laboratory && scene.id != "S02" && scene.id != "S03" && scene.id != "S06")
                    actor.offset = new Vector2(-7f * stageRect.rect.width / 1080f, 0f);
                actor.animationClock = 0f;
                SetPose(actor, actor.id == "watcher" && watcherPrepared ? Pose.Prepare : Pose.Idle, 1f);
            }
            stageGroup.alpha = 1f;
            LayoutActors();
            WarnMissingAssets();
        }

        private static bool IsLaboratory(string id)
        {
            return id == "S01" || id == "S05" || id == "S07" || id == "S10" || id == "S13" || id == "S14";
        }

        private bool EnsurePresentation()
        {
            if (runner == null || runner.Chapter == null) return false;
            if (presentationReady && presentationJson == loadedPresentationAsset && presentationJson != null &&
                presentationJson.text == loadedPresentationText && runner.Chapter == validatedChapter && catalog == validatedCatalog) return true;
            presentationReady = false;
            try
            {
                if (presentationJson == null) throw new InvalidOperationException("Assign Data/sanctuary_presentation_v1.json with the v4 installer.");
                if (catalog == null) throw new InvalidOperationException("Assign the generated visual catalog before story playback.");
                SanctuaryPresentationData parsed = JsonUtility.FromJson<SanctuaryPresentationData>(presentationJson.text);
                SanctuaryPresentationValidator.Validate(parsed, runner.Chapter, catalog);
                presentation = parsed;
                loadedPresentationAsset = presentationJson;
                loadedPresentationText = presentationJson.text;
                validatedChapter = runner.Chapter;
                validatedCatalog = catalog;
                presentationReady = true;
                warnedPresentation = false;
            }
            catch (Exception exception)
            {
                if (!warnedPresentation) Debug.LogError("SanctuarySpriteStage: presentation binding failed: " + exception.Message, this);
                warnedPresentation = true;
            }
            return presentationReady;
        }

        private void ApplyScenePresentation(string sceneId)
        {
            if (!presentationReady) return;
            scenePresentation = presentation.FindScene(sceneId);
            if (scenePresentation == null) throw new InvalidOperationException("Missing scene composition " + sceneId);
            foreach (SanctuaryActorPresentation state in scenePresentation.actors)
            {
                Actor actor = FindActor(state.actorId);
                if (actor == null) throw new InvalidOperationException("Unknown composition actor " + state.actorId);
                actor.authoredX = state.x;
                actor.authoredFeet = state.feetY;
                actor.authoredScale = state.scale;
                actor.visible = state.visible;
                actor.flipped = state.flipped;
                actor.variantId = state.variantId;
                ApplySprite(actor);
            }
            ApplyBackground();
        }

        private void ApplyLinePresentation(string lineId)
        {
            SanctuaryLinePresentation line = presentation.FindLine(lineId);
            if (line == null) throw new InvalidOperationException("Missing authored line expressions " + lineId);
            foreach (SanctuaryActorState state in line.actors)
            {
                Actor actor = FindActor(state.actorId);
                if (actor == null || actor.visual == null || actor.visual.ResolveExpression(state.variantId) == null)
                    throw new InvalidOperationException("Missing authored expression " + state.actorId + "/" + state.variantId);
                actor.variantId = state.variantId;
                actor.visible = state.visible;
                ApplySprite(actor);
            }
        }

        private void RestoreAuthoredStateAtCurrentStep()
        {
            // Reconstruct listeners too: omitted actor states intentionally hold from prior lines.
            // This never emits LineChanged or restarts the speaker bounce after re-enabling.
            SanctuaryFlow flow = runner.Flow;
            foreach (SanctuaryStep step in currentScene.steps)
            {
                bool current = ReferenceEquals(step.line, flow.CurrentLine) && flow.CurrentLine != null ||
                    ReferenceEquals(step.cue, flow.PendingCue) && flow.PendingCue != null ||
                    ReferenceEquals(step.choice, flow.CurrentChoices) && flow.CurrentChoices != null;
                if (step.kind == "line")
                {
                    ResetLinePoses();
                    ApplyLinePresentation(step.line.id);
                }
                if (current) break;
                if (step.kind == "cue" && OwnsCue(step.cue.id))
                {
                    // Restore completed world changes (notably S12's lowered hand) silently.
                    Actor doyun = FindActor("doyun"), watcher = FindActor("watcher");
                    cueStartDoyunOffset = doyun == null ? Vector2.zero : doyun.offset;
                    cueStartWatcherOffset = watcher == null ? Vector2.zero : watcher.offset;
                    cueContactPlayed = cueFoldPlayed = true;
                    cueLabRestored = false;
                    ApplyCue(step.cue.id, 1f, pathAmount);
                }
            }
            LayoutActors();
        }

        private void ResetLinePoses()
        {
            foreach (Actor actor in actors)
                SetPose(actor, actor.id == "watcher" && watcherPrepared ? Pose.Prepare : Pose.Idle, 1f);
        }

        private void OnLineChanged(SanctuaryLine line)
        {
            choiceLayout = denseChoices = false;
            if (ReferenceEquals(line, presentedLine)) return;
            // Exactly settle the old speaker before starting another utterance, as in Starway.
            FinishSpeakerBounce();
            presentedLine = line;
            if (!presentationReady || line == null || runner == null || runner.Chapter == null || currentScene == null) return;
            ResetLinePoses();
            ApplyLinePresentation(line.id);
            LayoutActors();
            if (reducedMotion) return;
            string id = line.speaker;
            foreach (SanctuaryCharacter character in runner.Chapter.characters)
                if (character.id == line.speaker || character.displayName == line.speaker) { id = character.id; break; }
            Actor actor = FindActor(id);
            bool visible = actor != null && actor.visible;
            if (actor == null || !visible || actor.image.sprite == null) return; // Off-screen voices remain off-screen.
            bounceStrength = Mathf.Clamp01(speakerBounceStrength) * SceneBounceStrength(currentScene.id);
            if (bounceStrength <= 0f) return;
            bouncingActor = actor;
            bounceElapsed = 0f;
            LayoutActors();
        }

        private static float SceneBounceStrength(string id)
        {
            switch (id)
            {
                case "S04": case "S08": case "S09": case "S11": return .6f;
                case "S05": case "S07": case "S10": case "S13": return .7f;
                case "S12": case "S14": return .35f;
                default: return 1f;
            }
        }

        // Same curve as Starway v19 StoryPresentationMath.Bounce, sampled in design pixels.
        private static void SampleSpeakerBounce(float progress, out Vector3 scale, out float y, out float rotation)
        {
            float t = Mathf.Clamp01(progress);
            scale = Vector3.one; y = rotation = 0f;
            if (t <= 0f || t >= 1f) return;
            float stretch;
            if (t < .2f) stretch = Mathf.Lerp(0f, -.025f, Ease(t / .2f));
            else if (t < .5f) stretch = Mathf.Lerp(-.025f, .022f, Ease((t - .2f) / .3f));
            else if (t < .8f) stretch = Mathf.Lerp(.022f, -.008f, Ease((t - .5f) / .3f));
            else stretch = Mathf.Lerp(-.008f, 0f, Ease((t - .8f) / .2f));
            scale = new Vector3(1f - stretch, 1f + stretch, 1f);
            y = Mathf.Sin(Mathf.PI * t) * 10f;
            rotation = Mathf.Sin(Mathf.PI * 2f * t) * .45f;
        }

        private void FinishSpeakerBounce()
        {
            bouncingActor = null;
            bounceElapsed = bounceStrength = 0f;
            if (stageRect != null && path != null && boundary != null) LayoutActors();
        }

        private void OnChoicesPresented(SanctuaryChoiceSet choices)
        {
            FinishSpeakerBounce();
            choiceLayout = true;
            denseChoices = choices != null && choices.options != null && choices.options.Length >= 4;
        }

        private void OnStateChanged()
        {
            if (runner == null || runner.Flow == null) { CancelCue(); return; }
            if (runner.IsPaused) { pauseObserved = true; frameDelta = 0f; }
            else if (pauseObserved) { skipNextDelta = true; clockFrame = -1; }
            // StateChanged precedes the next LineChanged. A fast next tap settles immediately.
            if (runner.Flow.State != SanctuaryFlowState.Line || !ReferenceEquals(runner.Flow.CurrentLine, presentedLine))
                FinishSpeakerBounce();
            if (runner.Flow.State == SanctuaryFlowState.Idle && runner.Flow.CurrentScene == null)
            {
                CancelCue(); currentScene = null;
                if (stageGroup != null) stageGroup.alpha = 0f;
            }
            else if (activeToken != 0 && (runner.Flow.State != SanctuaryFlowState.Cue || runner.Flow.PendingCueToken != activeToken))
                CancelCue();
        }

        private void OnPlaybackCompleted() { FinishSpeakerBounce(); CancelCue(); }

        private void OnCueRequested(SanctuaryCue cue, int token)
        {
            FinishSpeakerBounce(); // Also settle before the View-owned UI cues.
            if (!isActiveAndEnabled || cue == null || !OwnsCue(cue.id)) return; // UI owns choices_hide, naming_title and ending_settle.
            CancelCue();
            // A production cue must never become a timer-only success when its real visual is absent.
            if (!HasRequiredVisuals(cue.id)) { WarnMissingAssets(); return; }
            choiceLayout = false;
            activeToken = token;
            Actor doyun = FindActor("doyun"), watcher = FindActor("watcher");
            cueStartDoyunOffset = doyun == null ? Vector2.zero : doyun.offset;
            cueStartWatcherOffset = watcher == null ? Vector2.zero : watcher.offset;
            cueContactPlayed = cueFoldPlayed = cueLabRestored = false;
            int cueEpoch = epoch;
            cueRoutine = StartCoroutine(PlayCue(cue, token, cueEpoch));
        }

        public static bool OwnsCue(string id)
        {
            switch (id)
            {
                case "scene_settle": case "platform_climb_first": case "platform_climb_repeat":
                case "path_reveal": case "path_reveal_repeat": case "path_hold": case "boundary_risk":
                case "watcher_prepare_rewind": case "rewind_first": case "rewind_repeat":
                case "recognition_beat": case "doyun_stop": case "rewind_stop": case "return_forward": return true;
                default: return false;
            }
        }

        private IEnumerator PlayCue(SanctuaryCue cue, int token, int cueEpoch)
        {
            float elapsed = 0f;
            float duration = Mathf.Max(.02f, cue.duration);
            float initialPath = pathAmount;
            ApplyCue(cue.id, 0f, initialPath);
            while (elapsed < duration)
            {
                yield return null;
                if (!IsCurrent(token, cueEpoch)) yield break;
                float dt = PresentationDelta();
                if (dt <= 0f) continue;
                elapsed = Mathf.Min(duration, elapsed + dt);
                ApplyCue(cue.id, elapsed / duration, initialPath);
            }
            // The settled final pose must actually reach a render frame before acknowledging.
            yield return null;
            while (IsCurrent(token, cueEpoch) && (runner.IsPaused || PresentationDelta() <= 0f)) yield return null;
            if (!IsCurrent(token, cueEpoch)) yield break;
            cueRoutine = null; activeToken = 0;
            runner.CompleteCue(token);
        }

        private bool IsCurrent(int token, int cueEpoch)
        {
            return isActiveAndEnabled && epoch == cueEpoch && activeToken == token && runner != null && runner.Flow != null &&
                runner.Flow.State == SanctuaryFlowState.Cue && runner.Flow.PendingCueToken == token;
        }

        private bool HasRequiredVisuals(string cueId)
        {
            if (!presentationReady || catalog == null || background == null || catalog.laboratoryBackground == null || catalog.chaosBackground == null) return false;
            foreach (Actor actor in actors)
            {
                if (actor.visual == null || actor.visual.FirstIdle == null) return false;
            }
            bool needsHand = cueId == "watcher_prepare_rewind" || cueId == "rewind_first" || cueId == "rewind_repeat" ||
                cueId == "rewind_stop" || cueId == "scene_settle" && currentScene != null && currentScene.id == "S12";
            SanctuaryCharacterVisual watcher = catalog.FindCharacter("watcher");
            if (needsHand && (watcher == null || watcher.prepareFrames == null || watcher.prepareFrames.Length == 0 || watcher.prepareFrames[0] == null ||
                watcher.prepareFrames[0] == watcher.FirstIdle)) return false;
            return true;
        }

        private void CancelCue()
        {
            unchecked { epoch++; }
            activeToken = 0;
            if (cueRoutine != null) StopCoroutine(cueRoutine);
            cueRoutine = null;
        }

        private void ApplyCue(string id, float t, float initialPath)
        {
            Actor doyun = FindActor("doyun"), watcher = FindActor("watcher");
            float scale = stageRect.rect.width / 1080f;
            if (id == "scene_settle")
            {
                SetFade(0f); // SceneChanged already restored the complete canonical composition.
            }
            else if (id == "platform_climb_first" || id == "platform_climb_repeat")
            {
                float step = Ease(Mathf.Clamp01(t / .72f));
                if (doyun != null)
                {
                    doyun.offset = cueStartDoyunOffset + new Vector2(72f * step, 48f * step + Mathf.Sin(step * Mathf.PI) * 30f) * scale;
                    SetPose(doyun, t < .2f ? Pose.Move : Pose.Jump, step);
                }
                if (t >= .72f && !cueContactPlayed)
                {
                    cueContactPlayed = true;
                    PlaySound(catalog == null ? null : catalog.platformContactSound);
                }
                SetFade(t > .76f ? Ease((t - .76f) / .24f) : 0f);
            }
            else if (id == "path_reveal" || id == "path_reveal_repeat")
            {
                // The body's movement leads; light starts only after the foot has moved.
                float move = Ease(Mathf.Clamp01(t / .45f));
                if (doyun != null) { doyun.offset = cueStartDoyunOffset + new Vector2(44f * move, 0f) * scale; SetPose(doyun, t < .45f ? Pose.Move : Pose.Idle, t); }
                pathAmount = Mathf.Lerp(initialPath, .64f, Ease(Mathf.Clamp01((t - .32f) / .42f)));
                path.SetProgress(pathAmount);
                // Only after the near-foot patch exists does the Watcher turn to observe it.
                if (watcher != null) watcher.offset = cueStartWatcherOffset + new Vector2(t > .76f ? -7f * Ease((t - .76f) / .24f) : 0f, 0f) * scale;
            }
            else if (id == "path_hold")
            {
                path.SetProgress(pathAmount);
                if (doyun != null) SetPose(doyun, Pose.Idle, 1f);
            }
            else if (id == "boundary_risk")
            {
                if (doyun != null) SetPose(doyun, Pose.Boundary, t);
                boundary.SetProgress(Mathf.Lerp(.38f, .9f, Ease(t)));
                // The shadow is behind the full, opaque actor. Hands are never clipped or erased.
            }
            else if (id == "watcher_prepare_rewind")
            {
                watcherPrepared = true;
                if (watcher != null) SetPose(watcher, Pose.Prepare, t);
                fold.SetProgress(0f);
            }
            else if (id == "rewind_first" || id == "rewind_repeat")
            {
                if (watcher != null && t < .18f) SetPose(watcher, Pose.Prepare, Ease(t / .18f));
                watcherPrepared = true;
                if (t >= .18f && !cueFoldPlayed)
                {
                    cueFoldPlayed = true;
                    PlaySound(catalog == null ? null : catalog.rewindFoldSound);
                }
                float foldProgress = Mathf.Clamp01((t - .18f) / .82f);
                fold.SetProgress(foldProgress);
                if (foldProgress >= .5f && !cueLabRestored)
                {
                    cueLabRestored = true; laboratory = true; watcherPrepared = false;
                    pathAmount = 0f; path.SetProgress(0f); boundary.SetProgress(0f);
                    foreach (Actor actor in actors) { actor.offset = Vector2.zero; SetPose(actor, Pose.Idle, 1f); }
                    // The fold reveals the next laboratory composition, not a stale chaos profile.
                    ApplyScenePresentation(currentScene.nextSceneId);
                }
            }
            else if (id == "recognition_beat")
            {
                if (doyun != null) SetPose(doyun, Pose.Boundary, t);
                boundary.SetProgress(.75f); // Same S04 boundary, no hallucination or combat hit stop.
            }
            else if (id == "doyun_stop")
            {
                if (doyun != null)
                {
                    doyun.offset = cueStartDoyunOffset + new Vector2(12f * Ease(Mathf.Clamp01(t / .4f)), 0f) * scale;
                    SetPose(doyun, t < .4f ? Pose.Move : Pose.Idle, t);
                }
            }
            else if (id == "rewind_stop")
            {
                // A real hand-down frame replaces the real prepared pose; the world stays put.
                watcherPrepared = t < .5f;
                if (watcher != null)
                {
                    if (!watcherPrepared) watcher.variantId = "observe";
                    SetPose(watcher, t < .5f ? Pose.Prepare : Pose.Lower, t);
                }
                fold.SetProgress(0f);
            }
            else if (id == "return_forward")
            {
                if (doyun != null)
                {
                    doyun.flipped = t >= .2f;
                    doyun.offset = cueStartDoyunOffset + new Vector2(-38f * Ease(t), 0f) * scale;
                    SetPose(doyun, Pose.Move, t);
                }
                // Normal stable transition: deliberately no fold and no reverse sound.
                fold.SetProgress(0f);
                SetFade(t > .42f ? Ease((t - .42f) / .58f) : 0f);
            }
            LayoutActors();
        }

        private void LayoutActors()
        {
            if (stageRect == null) return;
            float width = stageRect.rect.width, height = stageRect.rect.height;
            float scale = width / 1080f;
            foreach (Actor actor in actors)
            {
                bool visible = actor.visible;
                if (denseChoices && actor.id != "doyun") visible = false;
                actor.image.enabled = visible && actor.image.sprite != null;
                float feet = Mathf.Lerp(actor.authoredFeet, choiceFeetHeight, Ease(layoutBlend)) * height;
                float actorScale = actor.authoredScale * Mathf.Lerp(1f, choiceActorScale, Ease(layoutBlend));
                float x = actor.authoredX;
                if (denseChoices && actor.id == "doyun") x = -.25f;
                RectTransform rect = actor.image.rectTransform;
                Vector2 basePosition = new Vector2(width * x, feet) + actor.offset;
                Vector3 bounceScale = Vector3.one;
                float bounceY = 0f, bounceRotation = 0f;
                if (actor == bouncingActor)
                {
                    SampleSpeakerBounce(bounceElapsed / SpeakerBounceDuration, out bounceScale, out bounceY, out bounceRotation);
                    bounceScale = new Vector3(1f + (bounceScale.x - 1f) * bounceStrength, 1f + (bounceScale.y - 1f) * bounceStrength, 1f);
                    bounceY *= bounceStrength * scale;
                    bounceRotation *= bounceStrength;
                }
                rect.anchoredPosition = basePosition + new Vector2(0f, bounceY);
                float designHeight = actor.visual == null ? 480f : Mathf.Max(120f, actor.visual.designHeight);
                float displayHeight = designHeight * scale * actorScale;
                Sprite sprite = actor.image.sprite;
                float aspect = sprite == null || sprite.rect.height <= 0f ? .7f : sprite.rect.width / sprite.rect.height;
                SanctuaryExpressionVisual metadata = actor.visual == null ? null : actor.visual.FindSpriteMetadata(sprite);
                if (metadata != null && metadata.hasContentBounds)
                {
                    // Scale by actual opaque body height, not by differing transparent margins.
                    float bodyFraction = metadata.contentMaxY - metadata.contentMinY;
                    float wholeHeight = displayHeight / bodyFraction;
                    float centerX = metadata.hasRegistrationX ? metadata.registrationX : (metadata.contentMinX + metadata.contentMaxX) * .5f;
                    // The pivot lies on actual body feet, so squash/rotation cannot drift on margins.
                    rect.pivot = new Vector2(centerX, metadata.contentMinY);
                    rect.sizeDelta = new Vector2(wholeHeight * aspect, wholeHeight);
                }
                else
                {
                    rect.pivot = new Vector2(.5f, 0f);
                    rect.sizeDelta = new Vector2(displayHeight * aspect, displayHeight);
                }
                rect.localScale = new Vector3((actor.flipped ? -1f : 1f) * bounceScale.x, bounceScale.y, 1f);
                rect.localRotation = Quaternion.Euler(0f, 0f, bounceRotation);
            }
            Actor doyun = FindActor("doyun");
            float doyunX = doyun == null ? -.21f : doyun.authoredX;
            if (denseChoices) doyunX = -.25f;
            float floorFeet = Mathf.Lerp(doyun == null ? normalFeetHeight : doyun.authoredFeet, choiceFeetHeight, Ease(layoutBlend)) * height;
            // The boundary follows authored world movement, never the speech-only bounce.
            Vector2 origin = new Vector2(width * doyunX, floorFeet) + (doyun == null ? Vector2.zero : doyun.offset);
            path.rectTransform.anchorMin = path.rectTransform.anchorMax = new Vector2(.5f, 0f);
            path.rectTransform.pivot = new Vector2(.12f, .38f);
            // Floor geometry belongs to the world, not the moving actor's transform.
            path.rectTransform.anchoredPosition = new Vector2(width * doyunX, floorFeet) + new Vector2(-16f, 5f) * scale;
            path.rectTransform.sizeDelta = new Vector2(560f, 180f) * scale;
            boundary.rectTransform.anchorMin = boundary.rectTransform.anchorMax = new Vector2(.5f, 0f);
            boundary.rectTransform.pivot = new Vector2(0f, .5f);
            boundary.rectTransform.anchoredPosition = origin + new Vector2(95f, 155f) * scale;
            boundary.rectTransform.sizeDelta = new Vector2(300f, 260f) * scale;
            path.enabled = !laboratory;
            boundary.enabled = !laboratory;
        }

        private void SetPose(Actor actor, Pose pose, float progress)
        {
            if (actor == null) return;
            if (actor.pose != pose) actor.animationClock = 0f;
            actor.pose = pose; actor.poseProgress = Mathf.Clamp01(progress);
            ApplySprite(actor);
        }

        private void ApplySprite(Actor actor)
        {
            SanctuaryCharacterVisual visual = actor.visual;
            if (visual == null) { actor.image.sprite = null; return; }
            Sprite[] frames = actor.pose == Pose.Move ? visual.moveFrames : actor.pose == Pose.Jump ? visual.jumpFrames :
                actor.pose == Pose.Prepare ? visual.prepareFrames : actor.pose == Pose.Lower ? visual.lowerFrames :
                actor.pose == Pose.Boundary ? visual.boundaryFrames : null;
            Sprite chosen = null;
            if (frames != null && frames.Length > 0)
            {
                int index = actor.pose == Pose.Idle || actor.pose == Pose.Move
                    ? (int)(actor.animationClock * Mathf.Max(1f, visual.framesPerSecond)) % frames.Length
                    : Mathf.Min(frames.Length - 1, Mathf.FloorToInt(actor.poseProgress * frames.Length));
                chosen = frames[index];
            }
            if (actor.id == "watcher" && watcherPrepared && visual.prepareFrames != null && visual.prepareFrames.Length > 0)
                chosen = visual.prepareFrames[0];
            if (chosen == null && actor.pose == Pose.Boundary && actor.id == "doyun")
                chosen = visual.ResolveExpression("terror");
            // Empty movement clips translate the currently authored expression. They must not
            // reset it to neutral or replace the approved body with the SD gameplay library.
            actor.image.sprite = chosen != null ? chosen : visual.ResolveExpression(actor.variantId);
            actor.image.color = Color.white;
        }

        private Actor FindActor(string id) { foreach (Actor actor in actors) if (actor.id == id) return actor; return null; }
        private void ApplyBackground()
        {
            background.sprite = catalog == null || scenePresentation == null ? null : catalog.FindBackground(scenePresentation.backgroundId);
            background.enabled = background.sprite != null;
            if (scenePresentation == null) return;
            RectTransform rect = background.rectTransform;
            Stretch(rect);
            float scale = scenePresentation.backgroundScale;
            rect.localScale = new Vector3(scale, scale, 1f);
            Vector2 requested = new Vector2(scenePresentation.backgroundX, scenePresentation.backgroundY) * (stageRect.rect.width / 1080f);
            float marginX = stageRect.rect.width * (scale - 1f) * .5f;
            float marginY = stageRect.rect.height * (scale - 1f) * .5f;
            // Safe-area aspect variations must never expose the edge of the background image.
            rect.anchoredPosition = new Vector2(Mathf.Clamp(requested.x, -marginX, marginX), Mathf.Clamp(requested.y, -marginY, marginY));
        }
        private static float Ease(float t) { t = Mathf.Clamp01(t); return t * t * (3f - 2f * t); }
        private void SetFade(float alpha) { Color tint = worldFade.color; tint.a = Mathf.Clamp01(alpha); worldFade.color = tint; }
        private void PlaySound(AudioClip clip) { if (clip != null && optionalAudioSource != null) optionalAudioSource.PlayOneShot(clip); }
        private void ResetEffects()
        {
            if (fold != null) fold.SetProgress(0f);
            if (path != null) path.SetProgress(0f);
            if (boundary != null) boundary.SetProgress(0f);
            if (worldFade != null) SetFade(0f);
        }

        private void WarnMissingAssets()
        {
            if (catalog == null || catalog.laboratoryBackground == null || catalog.chaosBackground == null)
            {
                if (!warnedMissingAssets) Debug.LogError("SanctuarySpriteStage: bind the supplied visual catalog and both clean world backgrounds before production.", this);
                warnedMissingAssets = true;
            }
            foreach (Actor actor in actors)
                if (actor.visual == null || actor.visual.FirstIdle == null)
                {
                    if (!warnedMissingAssets) Debug.LogError("SanctuarySpriteStage: missing approved story body for " + actor.id + ".", this);
                    warnedMissingAssets = true;
                }
            SanctuaryCharacterVisual watcher = catalog == null ? null : catalog.FindCharacter("watcher");
            if (!warnedMissingPrepare && (watcher == null || watcher.prepareFrames == null || watcher.prepareFrames.Length == 0 || watcher.prepareFrames[0] == null || watcher.prepareFrames[0] == watcher.FirstIdle))
            {
                warnedMissingPrepare = true;
                Debug.LogWarning("SanctuarySpriteStage: assign the supplied actual Watcher hand-up sprite to prepareFrames; idle cannot demonstrate raising the hand.", this);
            }
        }

        private Image EnsureImage(string name)
        {
            Transform child = transform.Find(name);
            GameObject target = child == null ? new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image)) : child.gameObject;
            if (child == null) target.transform.SetParent(transform, false);
            Image image = target.GetComponent<Image>();
            if (image == null) image = target.AddComponent<Image>();
            image.raycastTarget = false;
            return image;
        }

        private SanctuaryFoldGraphic EnsureGraphic(string name, SanctuaryWorldShape shape)
        {
            Transform child = transform.Find(name);
            GameObject target = child == null ? new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(SanctuaryFoldGraphic)) : child.gameObject;
            if (child == null) target.transform.SetParent(transform, false);
            SanctuaryFoldGraphic graphic = target.GetComponent<SanctuaryFoldGraphic>();
            if (graphic == null) graphic = target.AddComponent<SanctuaryFoldGraphic>();
            graphic.SetShape(shape);
            return graphic;
        }

        private static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one;
            rect.offsetMin = rect.offsetMax = Vector2.zero;
        }

        private void SetChildOrder()
        {
            background.transform.SetSiblingIndex(0); path.transform.SetSiblingIndex(1); boundary.transform.SetSiblingIndex(2);
            for (int i = 0; i < actors.Count; i++) actors[i].image.transform.SetSiblingIndex(i + 3);
            fold.transform.SetSiblingIndex(8); worldFade.transform.SetSiblingIndex(9);
        }
    }
}
