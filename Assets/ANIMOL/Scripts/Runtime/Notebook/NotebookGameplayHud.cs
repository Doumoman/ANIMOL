using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using Animol.UI.Pattern;
using UnityEngine;
using UnityEngine.UI;

namespace William.NotebookUI
{
    /// <summary>Presentation only: game-confirmed cost, stars, pause and clear. Never owns puzzle rules or rewards.</summary>
    public sealed class NotebookGameplayHud : MonoBehaviour
    {
        public NotebookGameplayGateway gateway;
        public Canvas ownerCanvas;
        // The legacy field now identifies only the upper-right cluster, never a full-width header.
        public RectTransform headerRect, costRect, restartRect;
        public const float HeaderReservedHeight = 0f;
        public RectTransform flightLayer;
        public Image jarImage, flightStar, flightTrail;
        public NotebookGameplayJarState[] jarStates = Array.Empty<NotebookGameplayJarState>();
        public Sprite[] starFrames = Array.Empty<Sprite>(), trailFrames = Array.Empty<Sprite>();
        public NotebookSpriteNumber costNumber;
        public TMP_Text collectedText, hudStatus;
        public Button pauseButton, restartButton, resumeButton, pauseMapButton;
        public Image restartImage;
        public Sprite[] retryGaugeSprites = Array.Empty<Sprite>(), retryGaugePressedSprites = Array.Empty<Sprite>();
        public GameObject modalBackdrop;
        public AnimolPatternMotion pausePatternMotion, clearPatternMotion;
        public GameObject pauseOverlay, clearOverlay;
        public RectTransform pausePanel, clearPanel;
        public TMP_Text pauseStageText, pauseStatus, clearStageText, clearStatus, nextLabel;
        public Button nextButton, clearMapButton;
        [Range(.2f, .7f)] public float pickupDuration = .44f;
        [Range(.2f, .5f)] public float maximumClearWait = .5f;
        public bool reducedMotion;

        private sealed class Pickup { public Vector2 screen; public int displayCount; }
        private readonly Queue<Pickup> pickups = new Queue<Pickup>();
        private readonly HashSet<string> collectedIds = new HashSet<string>();
        private string stageId, stageLabel, pendingOperation;
        private int generation, authoritativeCount, displayedCount, requiredCount = 3;
        private bool paused, cleared, processing;
        private NotebookGameplayClearSnapshot pendingClear;
        private NotebookGameplayRetryGaugeSnapshot retryGauge;
        private bool retryGaugeSynchronized;
        private bool patternScrolling = true;
        public const int RetryGaugeCapacity = 5;
        public int RetryGaugeRemaining => retryGaugeSynchronized && retryGauge != null ? retryGauge.remaining : 0;
        public bool RetryGaugeReady => retryGaugeSynchronized;
        private Coroutine pickupRoutine, clearRoutine, panelRoutine;
        private Camera UiCamera => ownerCanvas && ownerCanvas.renderMode != RenderMode.ScreenSpaceOverlay ? ownerCanvas.worldCamera : null;
        public int AuthoritativeCollected => authoritativeCount;
        public int DisplayedCollected => displayedCount;
        public int RequiredStars => requiredCount;
        public bool IsPaused => paused;
        /// <summary>Compatibility alias for the local upper-right cluster; no top strip is reserved.</summary>
        public Rect HeaderScreenRect => TopRightHUDScreenRect;
        public Rect TopRightHUDScreenRect => ScreenRectangle(headerRect);
        public Rect CostHUDScreenRect => ScreenRectangle(costRect);
        public Rect RestartHUDScreenRect => ScreenRectangle(restartRect);
        public bool IsScreenPointOverHud(Vector2 screenPoint) =>
            TopRightHUDScreenRect.Contains(screenPoint) || CostHUDScreenRect.Contains(screenPoint) || RestartHUDScreenRect.Contains(screenPoint);
        private Rect ScreenRectangle(RectTransform rectangle)
        {
            if (!rectangle) return new Rect();
            Vector3[] corners = new Vector3[4]; rectangle.GetWorldCorners(corners);
            Vector2 lower = RectTransformUtility.WorldToScreenPoint(UiCamera, corners[0]);
            Vector2 upper = RectTransformUtility.WorldToScreenPoint(UiCamera, corners[2]);
            return Rect.MinMaxRect(lower.x, lower.y, upper.x, upper.y);
        }
        /// <summary>One-based collected ordinal; arrival lands in the corresponding goal slot.</summary>
        public Vector2 GetStarSlotScreenPosition(int collectedOrdinal) =>
            jarImage ? RectTransformUtility.WorldToScreenPoint(UiCamera, StarSlotWorldPosition(collectedOrdinal)) : Vector2.zero;
        private Vector3 StarSlotWorldPosition(int collectedOrdinal)
        {
            RectTransform rectangle = jarImage.rectTransform;
            float index = Mathf.Clamp(collectedOrdinal, 1, requiredCount) - 1;
            float step = rectangle.rect.width / 4f;
            Vector2 local = rectangle.rect.center + Vector2.right * ((index - (requiredCount - 1) * .5f) * step);
            return rectangle.TransformPoint(local);
        }

        private void Awake()
        {
            if (pauseButton) pauseButton.onClick.AddListener(() => RequestPaused(true));
            if (resumeButton) resumeButton.onClick.AddListener(() => RequestPaused(false));
            if (restartButton) restartButton.onClick.AddListener(RequestRestart);
            if (pauseMapButton) pauseMapButton.onClick.AddListener(() => Navigate(NotebookGameplayDestination.Map));
            if (clearMapButton) clearMapButton.onClick.AddListener(() => Navigate(NotebookGameplayDestination.Map));
            if (nextButton) nextButton.onClick.AddListener(() => Navigate(NotebookGameplayDestination.NextStage));
            HideEffects(); SetActive(pauseOverlay, false); SetActive(clearOverlay, false); SetActive(modalBackdrop, false); RenderCount(0); RefreshButtons();
        }

        public void BindGateway(NotebookGameplayGateway provider) { gateway = provider; RefreshButtons(); }

        /// <summary>Call on initial entry and every retry/new stage. Cancels stale animation and approval callbacks.</summary>
        public void BeginStage(string actualStageId, string actualStageLabel, int currentCost, int collectedStars = 0, int requiredStars = 3)
        {
            generation++; StopAllCoroutines(); pickupRoutine = clearRoutine = panelRoutine = null;
            stageId = actualStageId ?? ""; stageLabel = actualStageLabel ?? stageId;
            pendingOperation = null; pickups.Clear(); collectedIds.Clear(); pendingClear = null;
            paused = cleared = processing = false;
            // A loaded stage must supply an explicit host snapshot. A retry never grants five new uses.
            retryGaugeSynchronized = false;
            requiredCount = Mathf.Clamp(requiredStars, 1, 4);
            authoritativeCount = Mathf.Clamp(collectedStars, 0, requiredCount);
            HideEffects(); RenderCount(authoritativeCount); SetCost(currentCost);
            SetActive(pauseOverlay, false); SetActive(clearOverlay, false); RefreshBackdrop();
            SetStatus(""); RefreshButtons();
        }
        public void SetCost(int authoritativeCost)
        {
            if (costNumber) costNumber.SetNumber(authoritativeCost);
        }

        /// <summary>Call exactly after the game confirms a pickup, with the game-provided count and unique per-stage ID.</summary>
        public bool ConfirmStarCollected(string pickupId, Vector2 screenPosition, int gameCollectedCount)
        {
            if (string.IsNullOrEmpty(stageId) || cleared || pendingClear != null || string.IsNullOrEmpty(pickupId)
                || collectedIds.Contains(pickupId) || gameCollectedCount <= authoritativeCount || gameCollectedCount > requiredCount)
                return false;
            // At most the stage target count of accepted IDs. Duplicates cannot fill the jar twice.
            if (collectedIds.Count >= requiredCount) return false;
            collectedIds.Add(pickupId); authoritativeCount = gameCollectedCount;
            gateway?.Feedback(NotebookGameplayFeedback.StarTouched);
            pickups.Enqueue(new Pickup { screen = screenPosition, displayCount = gameCollectedCount });
            if (!processing) pickupRoutine = StartCoroutine(PlayPickups(generation));
            return true;
        }
        public bool ConfirmWorldStarCollected(string pickupId, Vector3 worldPosition, Camera worldCamera, int gameCollectedCount)
        {
            if (!worldCamera) return false;
            Vector3 screen = worldCamera.WorldToScreenPoint(worldPosition);
            if (screen.z <= 0) return false;
            return ConfirmStarCollected(pickupId, screen, gameCollectedCount);
        }
        /// <summary>For load/reconnect/skip: use the game's snapshot, not a UI-inferred count.</summary>
        public void SynchronizeCollected(int gameCollectedCount)
        {
            CancelPickupVisuals(); authoritativeCount = Mathf.Clamp(gameCollectedCount, 0, requiredCount); RenderCount(authoritativeCount);
        }
        public void SetReducedMotion(bool enabled)
        {
            reducedMotion = enabled;
            ApplyPatternMotion();
            if (enabled)
            {
                FinishPickupVisuals();
                if (panelRoutine != null) StopCoroutine(panelRoutine); panelRoutine = null;
                if (pausePanel) pausePanel.localScale = Vector3.one;
                if (clearPanel) clearPanel.localScale = Vector3.one;
            }
        }
        public void SetPatternScrolling(bool enabled)
        {
            patternScrolling = enabled; ApplyPatternMotion();
        }
        private void ApplyPatternMotion()
        {
            if (pausePatternMotion) pausePatternMotion.SetReducedMotion(reducedMotion || !patternScrolling);
            if (clearPatternMotion) clearPatternMotion.SetReducedMotion(reducedMotion || !patternScrolling);
        }
        public void FinishPickupVisuals()
        {
            CancelPickupVisuals(); RenderCount(authoritativeCount);
        }
        private void CancelPickupVisuals()
        {
            if (pickupRoutine != null) StopCoroutine(pickupRoutine);
            pickupRoutine = null; processing = false; pickups.Clear(); HideEffects();
        }
        private IEnumerator PlayPickups(int token)
        {
            processing = true;
            while (pickups.Count > 0 && token == generation)
            {
                Pickup pickup = pickups.Dequeue();
                if (!reducedMotion && flightLayer && flightStar && jarImage && starFrames.Length > 0)
                {
                    RectTransformUtility.ScreenPointToLocalPointInRectangle(flightLayer, pickup.screen, UiCamera, out Vector2 start);
                    Vector2 end = flightLayer.InverseTransformPoint(StarSlotWorldPosition(pickup.displayCount));
                    Vector2 bend = (start + end) * .5f + Vector2.up * Mathf.Min(180, Vector2.Distance(start, end) * .18f);
                    SetActive(flightStar.gameObject, true); if (flightTrail) SetActive(flightTrail.gameObject, true);
                    float elapsed = 0; Vector2 previous = start;
                    while (elapsed < pickupDuration && token == generation)
                    {
                        float t = Mathf.Clamp01(elapsed / Mathf.Max(.01f, pickupDuration)), eased = 1 - Mathf.Pow(1 - t, 3);
                        Vector2 position = (1 - eased) * (1 - eased) * start + 2 * (1 - eased) * eased * bend + eased * eased * end;
                        flightStar.rectTransform.anchoredPosition = position;
                        flightStar.rectTransform.localScale = Vector3.one * Mathf.Lerp(1.12f, .52f, eased);
                        flightStar.rectTransform.localRotation = Quaternion.Euler(0, 0, -360 * t);
                        flightStar.sprite = starFrames[Mathf.FloorToInt(elapsed * 4) % starFrames.Length];
                        if (flightTrail)
                        {
                            Vector2 direction = position - previous; if (direction.sqrMagnitude < .001f) direction = end - start;
                            flightTrail.rectTransform.anchoredPosition = position - direction.normalized * 72;
                            flightTrail.rectTransform.localRotation = Quaternion.Euler(0, 0, Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg);
                            if (trailFrames.Length > 0) flightTrail.sprite = trailFrames[Mathf.Min(trailFrames.Length - 1, Mathf.FloorToInt(t * trailFrames.Length))];
                        }
                        previous = position; elapsed += Time.unscaledDeltaTime; yield return null;
                    }
                }
                if (token != generation) yield break;
                HideEffects(); RenderCount(pickup.displayCount); gateway?.Feedback(NotebookGameplayFeedback.StarArrived);
                if (!reducedMotion && jarImage)
                {
                    float settle = 0;
                    while (settle < .12f && token == generation)
                    {
                        jarImage.rectTransform.localScale = Vector3.one * (1 + Mathf.Sin(settle / .12f * Mathf.PI) * .07f);
                        settle += Time.unscaledDeltaTime; yield return null;
                    }
                    jarImage.rectTransform.localScale = Vector3.one;
                }
            }
            processing = false; pickupRoutine = null;
        }
        private void RenderCount(int value)
        {
            displayedCount = Mathf.Clamp(value, 0, requiredCount);
            if (jarImage)
            {
                NotebookGameplayJarState state = Array.Find(jarStates, s => s != null && s.required == requiredCount && s.collected == displayedCount);
                if (state != null && state.sprite) jarImage.sprite = state.sprite;
            }
            if (collectedText) collectedText.text = displayedCount + " / " + requiredCount;
        }
        private void HideEffects()
        {
            if (flightStar) { SetActive(flightStar.gameObject, false); flightStar.rectTransform.localRotation = Quaternion.identity; }
            if (flightTrail) SetActive(flightTrail.gameObject, false);
            if (jarImage) jarImage.rectTransform.localScale = Vector3.one;
        }

        /// <summary>Only the game's verified exit/clear event opens this popup. Filling the current target never opens it alone.</summary>
        public bool ShowVerifiedClear(NotebookGameplayClearSnapshot snapshot)
        {
            if (string.IsNullOrEmpty(stageId) || snapshot == null || !snapshot.verified || !snapshot.exitReached
                || snapshot.requiredStars != requiredCount || snapshot.collectedStars < requiredCount
                || snapshot.stageId != stageId || cleared || pendingClear != null) return false;
            pendingClear = new NotebookGameplayClearSnapshot
            {
                verified = snapshot.verified, exitReached = snapshot.exitReached, stageId = snapshot.stageId,
                stageLabel = snapshot.stageLabel, collectedStars = snapshot.collectedStars, requiredStars = snapshot.requiredStars,
                hasNextStage = snapshot.hasNextStage, canEnterNext = snapshot.canEnterNext && !string.IsNullOrEmpty(snapshot.nextStageId),
                nextStageId = snapshot.nextStageId, nextBlockedReason = snapshot.nextBlockedReason
            };
            if (pendingClear.hasNextStage && string.IsNullOrEmpty(pendingClear.nextStageId))
                pendingClear.nextBlockedReason = "다음 스테이지 연결을 확인해 주세요";
            pendingOperation = null; authoritativeCount = requiredCount; RefreshButtons();
            clearRoutine = StartCoroutine(WaitForClear(generation)); return true;
        }
        private IEnumerator WaitForClear(int token)
        {
            float waited = 0;
            while (processing && waited < Mathf.Clamp(maximumClearWait, .2f, .5f) && token == generation)
            { waited += Time.unscaledDeltaTime; yield return null; }
            if (token != generation || pendingClear == null) yield break;
            FinishPickupVisuals(); cleared = true; paused = false; SetActive(pauseOverlay, false);
            if (clearStageText) clearStageText.text = string.IsNullOrEmpty(pendingClear.stageLabel) ? stageLabel : pendingClear.stageLabel;
            if (nextLabel) nextLabel.text = pendingClear.hasNextStage ? "다음 스테이지" : "별자리 지도";
            if (clearStatus) clearStatus.text = pendingClear.hasNextStage && !pendingClear.canEnterNext
                ? (string.IsNullOrEmpty(pendingClear.nextBlockedReason) ? "다음 스테이지 입장 조건을 확인해 주세요" : pendingClear.nextBlockedReason)
                : "클리어 완료";
            if (clearMapButton) SetActive(clearMapButton.gameObject, pendingClear.hasNextStage);
            SetActive(clearOverlay, true); RefreshBackdrop(); RefreshButtons(); EnterPanel(clearPanel);
            gateway?.Feedback(NotebookGameplayFeedback.StageClear); clearRoutine = null;
        }
        public void RequestPaused(bool requested)
        {
            if (string.IsNullOrEmpty(stageId) || cleared || pendingClear != null || pendingOperation != null || paused == requested) return;
            if (!gateway) { SetStatus("일시정지는 게임 연결 후 사용할 수 있어요"); return; }
            string operation = Guid.NewGuid().ToString("N"); pendingOperation = operation; int token = generation;
            RefreshButtons(); gateway.Feedback(NotebookGameplayFeedback.ButtonPressed);
            gateway.RequestPause(requested, operation, accepted =>
            {
                if (!this || !isActiveAndEnabled || token != generation || operation != pendingOperation) return;
                pendingOperation = null;
                if (accepted)
                {
                    paused = requested; if (pauseStageText) pauseStageText.text = stageLabel;
                    SetActive(pauseOverlay, paused); RefreshBackdrop(); if (paused) EnterPanel(pausePanel); SetStatus("");
                }
                else SetStatus("일시정지 상태를 변경하지 못했어요");
                RefreshButtons();
            });
        }
        private void Navigate(NotebookGameplayDestination destination)
        {
            if (destination == NotebookGameplayDestination.Retry) { RequestRestart(); return; }
            if (pendingOperation != null || string.IsNullOrEmpty(stageId)) return;
            if (destination == NotebookGameplayDestination.NextStage && !cleared) return;
            if (destination == NotebookGameplayDestination.NextStage && pendingClear != null && !pendingClear.hasNextStage)
                destination = NotebookGameplayDestination.Map;
            if (destination == NotebookGameplayDestination.NextStage && (pendingClear == null || !pendingClear.canEnterNext)) return;
            if (!gateway) { SetStatus("이동은 기존 게임 서비스 연결 후 사용할 수 있어요"); return; }
            string target = destination == NotebookGameplayDestination.NextStage ? pendingClear.nextStageId : destination == NotebookGameplayDestination.Retry ? stageId : "";
            string current = stageId, operation = Guid.NewGuid().ToString("N"); int token = generation;
            pendingOperation = operation; RefreshButtons(); gateway.Feedback(NotebookGameplayFeedback.ButtonPressed);
            gateway.RequestNavigation(destination, current, target, operation, accepted =>
            {
                if (!this || !isActiveAndEnabled || token != generation || operation != pendingOperation) return;
                // Keep the operation locked after approval until the host changes stage or disables this overlay.
                // Duplicate approval callbacks cannot hand off twice.
                if (!accepted) { pendingOperation = null; SetStatus("이동할 수 없어요. 입장 조건을 확인해 주세요"); RefreshButtons(); return; }
                pendingOperation = "opened:" + operation; FinishPickupVisuals();
                gateway.OpenApprovedNavigation(destination, current, target, operation);
            });
        }
        private void EnterPanel(RectTransform panel)
        {
            if (panelRoutine != null) StopCoroutine(panelRoutine);
            if (!panel) return;
            if (reducedMotion) { panel.localScale = Vector3.one; return; }
            panelRoutine = StartCoroutine(AnimatePanel(panel, generation));
        }
        private IEnumerator AnimatePanel(RectTransform panel, int token)
        {
            float elapsed = 0;
            while (elapsed < .16f && token == generation)
            { float t = Mathf.Clamp01(elapsed / .16f); panel.localScale = Vector3.one * Mathf.Lerp(.96f, 1, 1 - Mathf.Pow(1 - t, 3)); elapsed += Time.unscaledDeltaTime; yield return null; }
            if (panel) panel.localScale = Vector3.one; panelRoutine = null;
        }
        private void SetStatus(string value)
        {
            if (hudStatus) hudStatus.text = value;
            if (pauseStatus) pauseStatus.text = value;
            if (clearStatus && cleared && !string.IsNullOrEmpty(value)) clearStatus.text = value;
        }
        private void RefreshButtons()
        {
            bool available = pendingOperation == null;
            RenderRetryGauge();
            if (restartButton) restartButton.interactable = available && gateway && retryGaugeSynchronized && retryGauge != null
                && retryGauge.remaining > 0 && !paused && !cleared && pendingClear == null && !string.IsNullOrEmpty(stageId);
            if (pauseButton) pauseButton.interactable = available && !paused && !cleared && pendingClear == null && !string.IsNullOrEmpty(stageId);
            if (resumeButton) resumeButton.interactable = available;
            if (pauseMapButton) pauseMapButton.interactable = available;
            if (clearMapButton) clearMapButton.interactable = available;
            if (nextButton) nextButton.interactable = available && cleared && pendingClear != null && (!pendingClear.hasNextStage || pendingClear.canEnterNext);
        }
        private void OnEnable()
        {
            RefreshBackdrop();
            if (pendingClear != null && !cleared) clearRoutine = StartCoroutine(WaitForClear(generation));
        }
        private void OnDisable()
        {
            generation++; StopAllCoroutines(); pickups.Clear(); processing = false;
            pickupRoutine = clearRoutine = panelRoutine = null; pendingOperation = null;
            HideEffects(); RenderCount(authoritativeCount); SetActive(modalBackdrop, false);
            if (pausePanel) pausePanel.localScale = Vector3.one;
            if (clearPanel) clearPanel.localScale = Vector3.one;
        }
        /// <summary>Only call with the existing service's authoritative retry-gauge state.</summary>
        public bool SynchronizeRetryGauge(NotebookGameplayRetryGaugeSnapshot snapshot)
        {
            if (snapshot == null || string.IsNullOrEmpty(stageId) || string.IsNullOrEmpty(snapshot.sessionId)
                || snapshot.remaining < 0 || snapshot.remaining > RetryGaugeCapacity || snapshot.revision < 0) return false;
            if (retryGauge != null && retryGauge.sessionId == snapshot.sessionId)
            {
                if (snapshot.revision < retryGauge.revision
                    || (snapshot.revision == retryGauge.revision && snapshot.remaining != retryGauge.remaining)) return false;
            }
            retryGauge = new NotebookGameplayRetryGaugeSnapshot { sessionId = snapshot.sessionId, remaining = snapshot.remaining, revision = snapshot.revision };
            retryGaugeSynchronized = true; RefreshButtons(); return true;
        }
        public void RequestRestart()
        {
            if (pendingOperation != null || string.IsNullOrEmpty(stageId) || paused || cleared || pendingClear != null
                || !retryGaugeSynchronized || retryGauge == null || retryGauge.remaining <= 0) return;
            if (!gateway) { SetStatus("재시작은 망원경 서비스 연결 후 사용할 수 있어요"); return; }
            string operation = Guid.NewGuid().ToString("N"), currentStage = stageId, session = retryGauge.sessionId;
            int token = generation, before = retryGauge.remaining; long expectedRevision = retryGauge.revision;
            pendingOperation = operation; RefreshButtons(); gateway.Feedback(NotebookGameplayFeedback.ButtonPressed);
            gateway.RequestRestart(currentStage, session, expectedRevision, operation, approval =>
            {
                if (!this || !isActiveAndEnabled || token != generation || pendingOperation != operation) return;
                bool sameBefore = retryGaugeSynchronized && retryGauge != null && retryGauge.sessionId == session
                    && retryGauge.revision == expectedRevision && retryGauge.remaining == before;
                // A host may publish its committed snapshot before completing this callback.
                bool acknowledgedAfter = approval != null && retryGaugeSynchronized && retryGauge != null
                    && retryGauge.sessionId == session && retryGauge.revision == approval.revision
                    && retryGauge.remaining == approval.remaining;
                bool newerAfter = approval != null && retryGaugeSynchronized && retryGauge != null
                    && retryGauge.sessionId == session && retryGauge.revision > approval.revision
                    && retryGauge.remaining <= approval.remaining;
                bool accepted = approval != null && approval.approved && approval.operationId == operation
                    && approval.sessionId == session && approval.remaining == before - 1
                    && approval.revision > expectedRevision && (sameBefore || acknowledgedAfter || newerAfter);
                if (!accepted)
                {
                    pendingOperation = null; SetStatus("재시작이 승인되지 않았어요. 망원경 상태를 확인해 주세요"); RefreshButtons(); return;
                }
                // The host already committed the charge. Copy its snapshot once; never subtract a second time here.
                if (!newerAfter)
                    retryGauge = new NotebookGameplayRetryGaugeSnapshot { sessionId = session, remaining = approval.remaining, revision = approval.revision };
                retryGaugeSynchronized = true; pendingOperation = "opened:" + operation;
                FinishPickupVisuals(); RenderRetryGauge(); RefreshButtons();
                gateway.OpenApprovedNavigation(NotebookGameplayDestination.Retry, currentStage, currentStage, operation);
            });
        }
        private void RenderRetryGauge()
        {
            int count = RetryGaugeRemaining;
            if (restartImage && retryGaugeSprites != null && retryGaugeSprites.Length > count)
                restartImage.sprite = retryGaugeSprites[count];
            if (restartButton && retryGaugeSprites != null && retryGaugeSprites.Length >= RetryGaugeCapacity + 1)
            {
                Sprite pressed = retryGaugePressedSprites != null && retryGaugePressedSprites.Length > count ? retryGaugePressedSprites[count] : retryGaugeSprites[count];
                restartButton.spriteState = new SpriteState
                {
                    highlightedSprite = retryGaugeSprites[count], selectedSprite = retryGaugeSprites[count],
                    pressedSprite = pressed, disabledSprite = retryGaugeSprites[count]
                };
            }
        }
        private void RefreshBackdrop()
        {
            SetActive(modalBackdrop, paused || cleared);
            ApplyPatternMotion();
        }
        private static void SetActive(GameObject target, bool active) { if (target) target.SetActive(active); }
    }
}
