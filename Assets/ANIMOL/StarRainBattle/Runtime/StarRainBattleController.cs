using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace ANIMOL.StarRainBattle
{
    /// <summary>
    /// Binds the deterministic battle model to pre-painted uGUI sprites.
    /// This component only moves, rotates, sizes and shows existing Images. It
    /// never draws a line, creates pixels, or generates a sprite at runtime.
    /// Attach it to the board RectTransform and assign its transparent child
    /// InputSurface Image as the board's pointer target.
    /// </summary>
    public sealed class StarRainBattleController : MonoBehaviour,
        IPointerDownHandler, IDragHandler, IPointerUpHandler
    {
        [Serializable]
        public sealed class ShotView
        {
            public Image Head;
            public Image Trail;
            [NonSerialized] public int MeteorId = -1;
            [NonSerialized] public MeteorTeam LastTeam;
        }

        [Serializable]
        public sealed class FxView
        {
            public Image Image;
            [NonSerialized] public float Remaining;
            [NonSerialized] public float Lifetime;
        }

        public RectTransform Board;
        public Image InputSurface;
        public Image[] Nodes;
        public Image[] SelectedRings;
        public Image[] DimHatches;
        public RectTransform IsolCore;
        public RectTransform CurseCore;
        public Image Link;
        public Image DragLine;
        public ShotView[] ShotViews;
        public FxView[] FxViews;

        public Image IsolFill;
        public Image CurseFill;
        public TMP_Text IsolHpText;
        public TMP_Text CurseHpText;
        public TMP_Text BlockedText;
        public TMP_Text MisblockedText;
        public TMP_Text OverlayTitle;

        public Button StartButton;
        public Button ReplayButton;
        public Button FreshButton;
        public Button PauseButton;
        public Button AugmentButton;
        public GameObject Overlay;
        public Image PauseIcon;
        public Image AugmentIcon;
        public Sprite PauseSprite;
        public Sprite ResumeSprite;

        public Sprite IsolHeadSprite;
        public Sprite CurseHeadSprite;
        public Sprite TrailSprite;
        public Material IsolHeadMaterial;
        public Material CurseHeadMaterial;
        public Material TrailMaterial;
        public Sprite BlockFxSprite;
        public Sprite ReflectFxSprite;
        public Sprite CoreHitFxSprite;
        public Material BlockFxMaterial;
        public Material ReflectFxMaterial;
        public Material CoreHitFxMaterial;

        // Matching arrays are assigned by the scene builder. Replacing a
        // material only chooses pre-painted animation or the still A frame.
        public Image[] MotionImages;
        public Material[] AnimatedMaterials;
        public Material[] StillMaterials;

        [SerializeField] private int initialSeed;

        private const float LogicalWidth = 400f;
        private const float LogicalHeight = 620f;
        private const float PickRadiusSquared = 30f * 30f;
        private const float DragThresholdSquared = 15f * 15f;
        private const float FxDuration = 0.38f;

        private readonly Dictionary<int, ShotView> shotById = new Dictionary<int, ShotView>();
        private readonly HashSet<int> visibleIds = new HashSet<int>();
        private StarRainBattleModel model;
        private int selectedNode = -1;
        private int pressedNode = -1;
        private int pointerId = int.MinValue;
        private bool dragging;
        private bool moved;
        private Vector2 dragPoint;
        [SerializeField] private bool reducedMotion;
        private bool warnedPool;

        public StarRainBattleModel Model { get { return model; } }
        public bool BlocksInput => false;

        private void Awake()
        {
            if (Board == null) Board = transform as RectTransform;
            if (InputSurface == null) InputSurface = GetComponent<Image>();
            if (InputSurface != null) InputSurface.raycastTarget = true;
            DisableBoardHitTargets();

            model = new StarRainBattleModel();
            model.EventRaised += OnBattleEvent;
            if (StartButton != null) StartButton.onClick.AddListener(StartOrResume);
            if (ReplayButton != null) ReplayButton.onClick.AddListener(Replay);
            if (FreshButton != null) FreshButton.onClick.AddListener(FreshBattle);
            if (PauseButton != null) PauseButton.onClick.AddListener(TogglePause);
            if (AugmentButton != null) AugmentButton.onClick.AddListener(ToggleAugment);
        }

        private void Start()
        {
            model.Prepare(initialSeed != 0 ? initialSeed : NewSeed());
            ResetTransientViews();
            SetReducedMotion(reducedMotion);
            RefreshView();
        }

        private void OnDestroy()
        {
            if (model != null) model.EventRaised -= OnBattleEvent;
            if (StartButton != null) StartButton.onClick.RemoveListener(StartOrResume);
            if (ReplayButton != null) ReplayButton.onClick.RemoveListener(Replay);
            if (FreshButton != null) FreshButton.onClick.RemoveListener(FreshBattle);
            if (PauseButton != null) PauseButton.onClick.RemoveListener(TogglePause);
            if (AugmentButton != null) AugmentButton.onClick.RemoveListener(ToggleAugment);
        }

        private void OnDisable()
        {
            CancelPointer();
        }

        private void Update()
        {
            if (model == null) return;
            if (model.Phase == BattlePhase.Running)
                model.Tick(Time.deltaTime);
            // Impact art finishes fading after a terminal hit; a pause freezes it.
            if (model.Phase != BattlePhase.Paused)
                AdvanceFx(Time.deltaTime);

            if (model.Phase != BattlePhase.Running)
            {
                selectedNode = -1;
                if (dragging) CancelPointer();
            }
            if (selectedNode >= 0 && !CanSelect(selectedNode)) selectedNode = -1;
            RefreshView();
        }

        public void StartOrResume()
        {
            if (model == null) return;
            if (model.Phase == BattlePhase.Paused)
                model.Resume();
            else if (model.Phase == BattlePhase.Ready)
            {
                model.Begin(model.Seed);
                ResetTransientViews();
            }
            RefreshView();
        }

        public void Replay()
        {
            if (model == null) return;
            model.RestartSameSeed();
            ResetTransientViews();
            RefreshView();
        }

        public void FreshBattle()
        {
            if (model == null) return;
            model.Begin(NewSeed());
            ResetTransientViews();
            RefreshView();
        }

        public void TogglePause()
        {
            if (model == null) return;
            if (model.Phase == BattlePhase.Running) model.Pause();
            else if (model.Phase == BattlePhase.Paused) model.Resume();
            selectedNode = -1;
            CancelPointer();
            RefreshView();
        }

        public void ToggleAugment()
        {
            if (model == null) return;
            bool enable = !model.Augmented;
            bool inBattle = model.Phase == BattlePhase.Running || model.Phase == BattlePhase.Paused;
            model.SetAugment(enable);
            if (inBattle) model.Begin(NewSeed());
            else model.Prepare(NewSeed());
            ResetTransientViews();
            RefreshView();
        }

        public void SetReducedMotion(bool value)
        {
            reducedMotion = value;
            if (MotionImages != null && AnimatedMaterials != null && StillMaterials != null)
            {
                int count = Mathf.Min(MotionImages.Length,
                    Mathf.Min(AnimatedMaterials.Length, StillMaterials.Length));
                for (int i = 0; i < count; i++)
                    if (MotionImages[i] != null)
                        MotionImages[i].material = reducedMotion ? StillMaterials[i] : AnimatedMaterials[i];
            }

            // Pooled graphics switch art per event, so they cannot rely on a
            // fixed Image/material pair in MotionImages. Rebind active views.
            if (ShotViews != null)
                foreach (var view in ShotViews)
                    if (view != null && view.MeteorId >= 0) ApplyShotTeam(view, view.LastTeam);
            if (FxViews != null)
                foreach (var view in FxViews)
                    if (view != null && view.Image != null && view.Remaining > 0)
                        view.Image.material = MotionMaterial(view.Image.material);
        }

        private Material MotionMaterial(Material source)
        {
            if (source == null) return null;
            if (AnimatedMaterials == null || StillMaterials == null) return source;
            int count = Mathf.Min(AnimatedMaterials.Length, StillMaterials.Length);
            for (int i = 0; i < count; i++)
                if (AnimatedMaterials[i] == source || StillMaterials[i] == source)
                    return reducedMotion ? StillMaterials[i] : AnimatedMaterials[i];
            return source;
        }

        private void DisableBoardHitTargets()
        {
            if (Nodes != null) foreach (var image in Nodes)
                if (image != null && image != InputSurface) image.raycastTarget = false;
            if (SelectedRings != null) foreach (var image in SelectedRings)
                if (image != null) image.raycastTarget = false;
            if (DimHatches != null) foreach (var image in DimHatches)
                if (image != null) image.raycastTarget = false;
            if (Link != null) Link.raycastTarget = false;
            if (DragLine != null) DragLine.raycastTarget = false;
            if (IsolCore != null)
            {
                var graphic = IsolCore.GetComponent<Graphic>();
                if (graphic != null) graphic.raycastTarget = false;
            }
            if (CurseCore != null)
            {
                var graphic = CurseCore.GetComponent<Graphic>();
                if (graphic != null) graphic.raycastTarget = false;
            }
            if (ShotViews != null) foreach (var view in ShotViews)
            {
                if (view == null) continue;
                if (view.Head != null) view.Head.raycastTarget = false;
                if (view.Trail != null) view.Trail.raycastTarget = false;
            }
            if (FxViews != null) foreach (var view in FxViews)
                if (view != null && view.Image != null) view.Image.raycastTarget = false;
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            if (model == null || model.Phase != BattlePhase.Running || dragging) return;
            Vector2 point;
            if (!ScreenToLogical(eventData, out point)) return;
            int hit = NearestSelectable(point);
            if (hit < 0)
            {
                selectedNode = -1;
                return;
            }

            if (selectedNode >= 0 && selectedNode != hit)
            {
                model.TryConnect(selectedNode, hit);
                selectedNode = -1;
                RefreshView();
                return;
            }

            dragging = true;
            moved = false;
            pressedNode = hit;
            pointerId = eventData.pointerId;
            dragPoint = point;
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (!dragging || eventData.pointerId != pointerId) return;
            Vector2 point;
            if (!ScreenToLogical(eventData, out point)) return;
            dragPoint = point;
            if (pressedNode >= 0 &&
                (point - model.Nodes[pressedNode].Position).sqrMagnitude > DragThresholdSquared)
                moved = true;
            RefreshLine();
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            if (!dragging || eventData.pointerId != pointerId) return;
            Vector2 point;
            bool inBoard = ScreenToLogical(eventData, out point);
            int hit = inBoard ? NearestSelectable(point) : -1;
            if (hit >= 0 && hit != pressedNode)
            {
                model.TryConnect(pressedNode, hit);
                selectedNode = -1;
            }
            else if (inBoard && !moved && CanSelect(pressedNode)) selectedNode = pressedNode;
            else selectedNode = -1;
            CancelPointer();
            RefreshView();
        }

        private void CancelPointer()
        {
            dragging = false;
            moved = false;
            pressedNode = -1;
            pointerId = int.MinValue;
            if (DragLine != null) DragLine.enabled = false;
        }

        private int NearestSelectable(Vector2 point)
        {
            int result = -1;
            float best = PickRadiusSquared;
            for (int i = 0; i < model.Nodes.Count; i++)
            {
                var node = model.Nodes[i];
                if (!CanSelect(node.Id)) continue;
                float d = (node.Position - point).sqrMagnitude;
                if (d < best) { best = d; result = node.Id; }
            }
            return result;
        }

        private bool CanSelect(int id)
        {
            return model != null && id >= 0 && id < model.Nodes.Count &&
                model.Nodes[id].DimUntil <= model.Time;
        }

        private bool ScreenToLogical(PointerEventData eventData, out Vector2 point)
        {
            point = Vector2.zero;
            if (Board == null || Board.rect.width <= 0 || Board.rect.height <= 0) return false;
            Vector2 local;
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                Board, eventData.position, eventData.pressEventCamera, out local)) return false;
            point = new Vector2((local.x - Board.rect.xMin) / Board.rect.width * LogicalWidth,
                (Board.rect.yMax - local.y) / Board.rect.height * LogicalHeight);
            return point.x >= 0 && point.x <= LogicalWidth && point.y >= 0 && point.y <= LogicalHeight;
        }

        private Vector2 ToLocal(Vector2 point)
        {
            if (Board == null) return Vector2.zero;
            return new Vector2(Board.rect.xMin + point.x / LogicalWidth * Board.rect.width,
                Board.rect.yMax - point.y / LogicalHeight * Board.rect.height);
        }

        private void RefreshView()
        {
            if (model == null) return;
            if (Nodes != null)
            {
                for (int i = 0; i < model.Nodes.Count; i++)
                {
                    int id = model.Nodes[i].Id;
                    Vector2 local = ToLocal(model.Nodes[i].Position);
                    if (id < Nodes.Length && Nodes[id] != null)
                        Nodes[id].rectTransform.anchoredPosition = local;
                    if (SelectedRings != null && id < SelectedRings.Length && SelectedRings[id] != null)
                    {
                        SelectedRings[id].rectTransform.anchoredPosition = local;
                        SelectedRings[id].enabled = selectedNode == id && model.Phase == BattlePhase.Running;
                    }
                    if (DimHatches != null && id < DimHatches.Length && DimHatches[id] != null)
                    {
                        DimHatches[id].rectTransform.anchoredPosition = local;
                        DimHatches[id].enabled = model.Nodes[i].DimUntil > model.Time;
                    }
                }
            }
            if (IsolCore != null) IsolCore.anchoredPosition = ToLocal(new Vector2(200, 544));
            if (CurseCore != null) CurseCore.anchoredPosition = ToLocal(new Vector2(200, 76));
            RefreshLine();
            RefreshShots();
            RefreshHud();
            RefreshOverlay();
        }

        private void RefreshLine()
        {
            if (Link != null)
            {
                Link.enabled = model.HasLink &&
                    (model.Phase == BattlePhase.Running || model.Phase == BattlePhase.Paused);
                if (Link.enabled) PlaceLine(Link.rectTransform,
                    model.Nodes[model.LinkA].Position, model.Nodes[model.LinkB].Position);
            }
            if (DragLine != null)
            {
                DragLine.enabled = dragging && moved && pressedNode >= 0 &&
                    model.Phase == BattlePhase.Running;
                if (DragLine.enabled)
                    PlaceLine(DragLine.rectTransform, model.Nodes[pressedNode].Position, dragPoint);
            }
        }

        private void PlaceLine(RectTransform rect, Vector2 a, Vector2 b)
        {
            Vector2 localA = ToLocal(a), localB = ToLocal(b);
            Vector2 delta = localB - localA;
            rect.anchoredPosition = (localA + localB) * 0.5f;
            rect.localRotation = Quaternion.Euler(0, 0, Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg);
            Vector2 size = rect.sizeDelta;
            size.x = delta.magnitude;
            rect.sizeDelta = size;
        }

        private void RefreshShots()
        {
            if (ShotViews == null) return;
            visibleIds.Clear();
            foreach (var meteor in model.Meteors)
            {
                visibleIds.Add(meteor.Id);
                ShotView view;
                if (!shotById.TryGetValue(meteor.Id, out view))
                {
                    view = FindFreeShot();
                    if (view == null)
                    {
                        if (!warnedPool) { Debug.LogWarning("StarRainBattle: projectile view pool exhausted."); warnedPool = true; }
                        continue;
                    }
                    view.MeteorId = meteor.Id;
                    view.LastTeam = (MeteorTeam)(-1);
                    shotById.Add(meteor.Id, view);
                }
                if (view.LastTeam != meteor.Team) ApplyShotTeam(view, meteor.Team);
                Vector2 point = ToLocal(meteor.Position);
                if (view.Head != null)
                {
                    view.Head.enabled = true;
                    view.Head.rectTransform.anchoredPosition = point;
                }
                if (view.Trail != null)
                {
                    Vector2 direction = new Vector2(meteor.Tangent.x, -meteor.Tangent.y);
                    if (direction.sqrMagnitude < 0.0001f)
                        direction = ToLocal(meteor.Position) - ToLocal(meteor.PreviousPosition);
                    if (direction.sqrMagnitude < 0.0001f) direction = Vector2.up;
                    direction.Normalize();
                    view.Trail.enabled = true;
                    // The 170px pre-painted trail ends at the meteor head.
                    view.Trail.rectTransform.anchoredPosition = point - direction * 85f;
                    view.Trail.rectTransform.localRotation = Quaternion.Euler(0, 0,
                        Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg);
                }
            }

            // A disappeared model projectile frees its two pre-existing Images.
            // Iterate pool rather than mutating the dictionary during enumeration.
            foreach (var view in ShotViews)
            {
                if (view == null || view.MeteorId < 0 || visibleIds.Contains(view.MeteorId)) continue;
                shotById.Remove(view.MeteorId);
                view.MeteorId = -1;
                if (view.Head != null) view.Head.enabled = false;
                if (view.Trail != null) view.Trail.enabled = false;
            }
        }

        private ShotView FindFreeShot()
        {
            foreach (var view in ShotViews)
                if (view != null && view.MeteorId < 0) return view;
            return null;
        }

        private void ApplyShotTeam(ShotView view, MeteorTeam team)
        {
            view.LastTeam = team;
            Color tint = team == MeteorTeam.Curse ? new Color(0.91f, 0.55f, 0.78f) :
                team == MeteorTeam.Reflected ? new Color(0.66f, 0.89f, 0.84f) :
                new Color(0.96f, 0.81f, 0.48f);
            if (view.Head != null)
            {
                view.Head.sprite = team == MeteorTeam.Isol ? IsolHeadSprite : CurseHeadSprite;
                view.Head.material = MotionMaterial(team == MeteorTeam.Isol ?
                    IsolHeadMaterial : CurseHeadMaterial);
                view.Head.color = tint;
            }
            if (view.Trail != null)
            {
                view.Trail.sprite = TrailSprite;
                view.Trail.material = MotionMaterial(TrailMaterial);
                view.Trail.color = tint;
            }
        }

        private void RefreshHud()
        {
            if (IsolFill != null) IsolFill.fillAmount = model.IsolHP / (float)StarRainBattleModel.IsolMaxHP;
            if (CurseFill != null) CurseFill.fillAmount = model.CurseHP / (float)StarRainBattleModel.CurseMaxHP;
            if (IsolHpText != null) IsolHpText.text = model.IsolHP.ToString();
            if (CurseHpText != null) CurseHpText.text = model.CurseHP.ToString();
            if (BlockedText != null) BlockedText.text = model.BlockedEnemy.ToString();
            if (MisblockedText != null) MisblockedText.text = model.BlockedFriend.ToString();
            if (AugmentIcon != null)
                AugmentIcon.color = model.Augmented ? new Color(0.66f, 0.89f, 0.84f) : Color.white;
            if (PauseIcon != null)
                PauseIcon.sprite = model.Phase == BattlePhase.Paused ? ResumeSprite : PauseSprite;
            if (PauseButton != null) PauseButton.interactable =
                model.Phase == BattlePhase.Running || model.Phase == BattlePhase.Paused;
        }

        private void RefreshOverlay()
        {
            bool visible = model.Phase != BattlePhase.Running;
            if (Overlay != null && Overlay.activeSelf != visible) Overlay.SetActive(visible);
            if (!visible) return;
            if (OverlayTitle != null)
            {
                switch (model.Phase)
                {
                    case BattlePhase.Ready: OverlayTitle.text = "별을 이어 막아"; break;
                    case BattlePhase.Paused: OverlayTitle.text = "잠시 멈춤"; break;
                    case BattlePhase.Won: OverlayTitle.text = "별빛이 닿았다"; break;
                    case BattlePhase.Lost: OverlayTitle.text = "별이 어두워졌다"; break;
                }
            }
            bool start = model.Phase == BattlePhase.Ready || model.Phase == BattlePhase.Paused;
            bool ended = model.Phase == BattlePhase.Won || model.Phase == BattlePhase.Lost;
            if (StartButton != null && StartButton.gameObject.activeSelf != start)
                StartButton.gameObject.SetActive(start);
            if (ReplayButton != null && ReplayButton.gameObject.activeSelf != ended)
                ReplayButton.gameObject.SetActive(ended);
            if (FreshButton != null && FreshButton.gameObject.activeSelf != ended)
                FreshButton.gameObject.SetActive(ended);
        }

        private void OnBattleEvent(BattleEvent battleEvent)
        {
            switch (battleEvent.Type)
            {
                case BattleEventType.Block:
                case BattleEventType.Misblock:
                case BattleEventType.Link:
                    SpawnFx(battleEvent.Position, BlockFxSprite, BlockFxMaterial,
                        battleEvent.Type == BattleEventType.Misblock ? new Color(0.96f, 0.81f, 0.48f) : Color.white);
                    break;
                case BattleEventType.Reflect:
                    SpawnFx(battleEvent.Position, ReflectFxSprite, ReflectFxMaterial, new Color(0.66f, 0.89f, 0.84f));
                    break;
                case BattleEventType.NodeDim:
                case BattleEventType.NodeHit:
                case BattleEventType.CoreHit:
                    SpawnFx(battleEvent.Position, CoreHitFxSprite, CoreHitFxMaterial,
                        battleEvent.Team == MeteorTeam.Curse ? new Color(0.91f, 0.55f, 0.78f) :
                            new Color(0.96f, 0.81f, 0.48f));
                    break;
                case BattleEventType.Win:
                case BattleEventType.Lose:
                    selectedNode = -1;
                    CancelPointer();
                    break;
            }
        }

        private void SpawnFx(Vector2 logical, Sprite sprite, Material material, Color tint)
        {
            if (FxViews == null || sprite == null) return;
            FxView view = null;
            foreach (var candidate in FxViews)
                if (candidate != null && candidate.Image != null && candidate.Remaining <= 0)
                { view = candidate; break; }
            if (view == null) return;
            view.Image.sprite = sprite;
            view.Image.material = MotionMaterial(material);
            view.Image.color = tint;
            view.Image.rectTransform.anchoredPosition = ToLocal(logical);
            view.Image.enabled = true;
            view.Remaining = FxDuration;
            view.Lifetime = FxDuration;
        }

        private void AdvanceFx(float dt)
        {
            if (FxViews == null) return;
            foreach (var view in FxViews)
            {
                if (view == null || view.Image == null || view.Remaining <= 0) continue;
                view.Remaining = Mathf.Max(0, view.Remaining - dt);
                Color color = view.Image.color;
                color.a = view.Remaining / view.Lifetime;
                view.Image.color = color;
                if (view.Remaining <= 0) view.Image.enabled = false;
            }
        }

        private void ResetTransientViews()
        {
            selectedNode = -1;
            CancelPointer();
            shotById.Clear();
            warnedPool = false;
            if (ShotViews != null)
                foreach (var view in ShotViews)
                {
                    if (view == null) continue;
                    view.MeteorId = -1;
                    if (view.Head != null) view.Head.enabled = false;
                    if (view.Trail != null) view.Trail.enabled = false;
                }
            if (FxViews != null)
                foreach (var view in FxViews)
                {
                    if (view == null) continue;
                    view.Remaining = 0;
                    if (view.Image != null) view.Image.enabled = false;
                }
        }

        private static int NewSeed()
        {
            int seed = unchecked((int)DateTime.UtcNow.Ticks ^ Environment.TickCount);
            return seed == 0 ? 1 : seed;
        }
    }
}
