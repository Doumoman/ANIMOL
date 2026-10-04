using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace ANIMOL.AnimalUiV2
{
    public sealed class AnimalUiPresenter : MonoBehaviour
    {
        public AnimalCatalog Catalog;
        public AnimalUiScreenView View;
        public AnimalUiBackendBehaviour Backend;
        public AnimalUiMode PreviewMode;
        public bool OpenReadonlyPreviewOnStart = true;
        public event Action<AnimalUiCommitResult> CampaignAccepted;
        public event Action<AnimalUiCommitResult> MultiplayerAccepted;
        public event Action<AnimalUiMode, AnimalLoadout> Cancelled;
        public event Action<AnimalUiCommitResult> UpgradeAccepted;
        public event Action CampaignRefreshRequested;
        public bool IsCommitting => _pending || _uncertain;
        public bool IsMultiplayerModalOpen => _mode == AnimalUiMode.MultiplayerAnimalSelect && _modalVisible;
        public AnimalLoadout Draft => _draft.Clone();
        public string InspectedAnimalId => _animalId;

        private AnimalUiMode _mode;
        private AnimalRole _role;
        private string _animalId;
        private StageSelectionRequest _stage;
        private MultiplayerSelectionRequest _multiplayer;
        private AnimalLoadout _draft = new AnimalLoadout();
        private AnimalLoadout _original = new AnimalLoadout();
        private AnimalUiSnapshot _snapshot = new AnimalUiSnapshot();
        private UpgradeQuote _activeQuote;
        private UpgradeQuote _passiveQuote;
        private readonly List<AnimalCardView> _cards = new List<AnimalCardView>();
        private CancellationTokenSource _reads;
        private int _readGeneration;
        private int _epoch;
        private bool _opened, _reading, _pending, _uncertain, _modalVisible, _snapshotValid, _selectionCompleted;
        private string _status;
        private string _multiplayerUncertainReason;
        private Action _modalAction;
        private UpgradeCommitRequest _upgradeRequest;
        private SelectionCommitRequest _selectionRequest;
        private AnimalUiMode _commitMode;

        private void Awake()
        {
            View.Back.onClick.AddListener(Back);
            View.Refresh.onClick.AddListener(RequestRefresh);
            View.Primary.onClick.AddListener(Primary);
            View.ActiveTrack.UpgradeButton.onClick.AddListener(() => ConfirmUpgrade(UpgradeTrack.Active));
            View.PassiveTrack.UpgradeButton.onClick.AddListener(() => ConfirmUpgrade(UpgradeTrack.Passive));
            View.ActiveTrack.DetailsButton.onClick.AddListener(() => ShowTrackDetails(UpgradeTrack.Active));
            View.PassiveTrack.DetailsButton.onClick.AddListener(() => ShowTrackDetails(UpgradeTrack.Passive));
            if (View.SelectionDetailsButton != null) View.SelectionDetailsButton.onClick.AddListener(ShowSelectionDetails);
            View.ModalCancel.onClick.AddListener(CloseModal);
            View.ModalConfirm.onClick.AddListener(() => { if (!_pending) _modalAction?.Invoke(); });
            for (int i = 0; i < 3; i++)
            {
                var role = (AnimalRole)i;
                View.RoleTabs[i].onClick.AddListener(() => ChangeRole(role));
                View.Slots[i].Button.onClick.AddListener(() => ChangeRole(role));
            }
        }
        private void Start()
        {
            if (_opened || !OpenReadonlyPreviewOnStart) return;
            if (PreviewMode == AnimalUiMode.CharacterUpgrade) OpenUpgrade("Rabbit");
            else if (PreviewMode == AnimalUiMode.StageAnimalSelect) OpenCampaign(AnimalStagePhase2DemoHost.CreateReadonlyFixture());
            else OpenMultiplayer(AnimalMultiplayerPhase3DemoHost.CreateReadonlyFixture());
        }
        private void OnEnable() { if (_uncertain && View != null) ShowRetryModal(); }
        private void OnDisable()
        {
            _epoch++; CancelReads();
            if (_pending) { _pending = false; _uncertain = true; }
        }
        private void OnDestroy() => CancelReads();
        public void SetBackend(AnimalUiBackendBehaviour backend)
        {
            if (IsCommitting) throw new InvalidOperationException("처리 결과 확인 중에는 backend를 교체할 수 없습니다.");
            if (_mode == AnimalUiMode.MultiplayerAnimalSelect && _modalVisible)
                throw new InvalidOperationException("멀티플레이 확인 창을 닫은 뒤 backend를 교체해주세요.");
            CancelReads(); Backend = backend;
            if (_opened && isActiveAndEnabled) Refresh();
        }
        public bool OpenUpgrade(string animalId)
        {
            if (!BeginOpen(AnimalUiMode.CharacterUpgrade)) return false;
            _stage = null; _multiplayer = null; _animalId = Catalog.Find(animalId)?.Id ?? Catalog.Animals[0].Id;
            _role = Catalog.Find(_animalId).Role; CompleteOpen(); return true;
        }
        public bool OpenCampaign(StageSelectionRequest request)
        {
            if (!BeginOpen(AnimalUiMode.StageAnimalSelect)) return false;
            _stage = (request ?? new StageSelectionRequest()).Clone(); _multiplayer = null;
            _draft = _stage.Policy == CampaignSelectionPolicy.Fixed ? _stage.FixedLoadout.Clone() : _stage.InitialLoadout.Clone();
            _original = _stage.InitialLoadout.Clone(); SelectInitialRole(); CompleteOpen(); return true;
        }
        public bool OpenMultiplayer(MultiplayerSelectionRequest request)
        {
            if (!BeginOpen(AnimalUiMode.MultiplayerAnimalSelect)) return false;
            _multiplayer = (request ?? new MultiplayerSelectionRequest()).Clone(); _stage = null;
            _draft = _multiplayer.InitialLoadout.Clone(); _original = _draft.Clone(); SelectInitialRole(); CompleteOpen(); return true;
        }
        private bool BeginOpen(AnimalUiMode mode)
        {
            if (IsCommitting || IsMultiplayerModalOpen) return false;
            string error = null;
            if (Catalog == null || !Catalog.IsValid(out error))
            { Debug.LogError(Catalog == null ? "AnimalCatalog가 연결되지 않았습니다." : error, this); return false; }
            CancelReads(); _epoch++; _opened = true; _mode = mode; _status = null;
            _selectionCompleted = false;
            _snapshot = new AnimalUiSnapshot(); _snapshotValid = false; _activeQuote = _passiveQuote = null;
            _upgradeRequest = null; _selectionRequest = null; _modalVisible = false;
            _multiplayerUncertainReason = null;
            View.Modal.SetActive(false); gameObject.SetActive(true); return true;
        }
        private SelectionRequirements Requirements => _mode == AnimalUiMode.StageAnimalSelect ? _stage?.Requirements : _multiplayer?.Requirements;
        private void SelectInitialRole()
        {
            _role = Requirements?.RequiredRoles != null && Requirements.RequiredRoles.Length > 0 ? Requirements.RequiredRoles[0] : AnimalRole.Ground;
            if (!Enum.IsDefined(typeof(AnimalRole), _role)) _role = AnimalRole.Ground;
            _animalId = Catalog.Find(_draft.Get(_role))?.Id ?? Catalog.ForRole(_role).First().Id;
        }
        private void CompleteOpen()
        {
            View.SetMode(_mode); BuildRoster(); Render(); Refresh();
        }
        private void CancelReads()
        {
            _readGeneration++; _reads?.Cancel(); _reads?.Dispose(); _reads = null; _reading = false;
        }
        public void Refresh() => RefreshWithStatus(null);
        private void RequestRefresh()
        {
            if (_reading || IsCommitting || _modalVisible) return;
            if (_mode == AnimalUiMode.StageAnimalSelect && CampaignRefreshRequested != null) CampaignRefreshRequested.Invoke();
            else Refresh();
        }
        private async void RefreshWithStatus(string outcome)
        {
            if (!_opened || !isActiveAndEnabled || IsCommitting ||
                (_mode == AnimalUiMode.MultiplayerAnimalSelect && _modalVisible)) return;
            CancelReads(); int generation = _readGeneration, epoch = _epoch;
            _reads = new CancellationTokenSource(); var token = _reads.Token;
            _activeQuote = _passiveQuote = null;
            _snapshotValid = false;
            if (_mode == AnimalUiMode.MultiplayerAnimalSelect) _snapshot = new AnimalUiSnapshot();
            if (Backend == null)
            { _snapshot = new AnimalUiSnapshot(); _status = "데이터 연결 전 · 15종 일러스트 열람"; Render(); return; }
            _reading = true; _status = "최신 정보를 확인하고 있습니다."; Render();
            var backend = Backend;
            try
            {
                var snapshot = await backend.ReadSnapshotAsync(new AnimalUiReadRequest
                { Mode = _mode, Stage = _stage?.Clone(), Multiplayer = _multiplayer?.Clone() }, token);
                if (!IsCurrent(generation, epoch, token)) return;
                if (_mode == AnimalUiMode.MultiplayerAnimalSelect &&
                    (!AnimalUiRules.ValidateMultiplayerContext(Catalog, _multiplayer, out var contextError) ||
                     !AnimalUiRules.ValidateMultiplayerSnapshotContext(snapshot, _multiplayer, out contextError)))
                    throw new InvalidOperationException(contextError);
                _snapshot = snapshot ?? throw new InvalidOperationException("동물 스냅샷이 비어 있습니다.");
                _snapshotValid = true;
                if (_mode == AnimalUiMode.CharacterUpgrade)
                {
                    string id = _animalId;
                    var active = await backend.QuoteUpgradeAsync(id, UpgradeTrack.Active, token);
                    if (!IsCurrent(generation, epoch, token) || id != _animalId) return;
                    var passive = await backend.QuoteUpgradeAsync(id, UpgradeTrack.Passive, token);
                    if (!IsCurrent(generation, epoch, token) || id != _animalId) return;
                    _activeQuote = MatchingQuote(active, id, UpgradeTrack.Active);
                    _passiveQuote = MatchingQuote(passive, id, UpgradeTrack.Passive);
                }
                _status = outcome;
            }
            catch (OperationCanceledException)
            {
                if (!IsCurrent(generation, epoch, token)) return;
                _status = "정보 조회가 취소되었습니다. 새로고침해주세요.";
                _snapshotValid = false; _activeQuote = _passiveQuote = null;
            }
            catch (Exception exception)
            {
                if (!IsCurrent(generation, epoch, token)) return;
                _status = "정보를 불러오지 못했습니다. 새로고침해주세요. " + exception.Message;
                _activeQuote = _passiveQuote = null;
            }
            if (!IsCurrent(generation, epoch, token)) return;
            _reading = false; Render();
        }
        private bool IsCurrent(int generation, int epoch, CancellationToken token) =>
            generation == _readGeneration && epoch == _epoch && !token.IsCancellationRequested && isActiveAndEnabled;
        private static UpgradeQuote MatchingQuote(UpgradeQuote quote, string id, UpgradeTrack track) =>
            quote != null && quote.AnimalId == id && quote.Track == track &&
            quote.Costs != null && quote.Costs.All(cost => cost != null && cost.Amount >= 0 && !string.IsNullOrWhiteSpace(cost.CurrencyId)) ? quote : null;
        private void ChangeRole(AnimalRole role)
        {
            if (IsCommitting || _modalVisible) return;
            _role = role; _animalId = Catalog.Find(_draft.Get(role))?.Id ?? Catalog.ForRole(role).First().Id;
            BuildRoster(); if (_mode == AnimalUiMode.CharacterUpgrade) Refresh(); else Render();
        }
        private void BuildRoster()
        {
            // Operational prefabs may author these cards ahead of time. Reuse them across tabs/re-entry.
            if (_cards.Count == 0) _cards.AddRange(View.RosterContent.GetComponentsInChildren<AnimalCardView>(true));
            var animals = Catalog.ForRole(_role).ToList();
            while (_cards.Count < animals.Count)
                _cards.Add(Instantiate(View.CardTemplate, View.RosterContent));
            for (int i = 0; i < _cards.Count; i++)
            {
                _cards[i].gameObject.SetActive(i < animals.Count);
                if (i < animals.Count) _cards[i].Bind(animals[i], "", false, false, ChooseAnimal);
            }
        }
        private void ChooseAnimal(string id)
        {
            if (IsCommitting || _modalVisible) return;
            var animal = Catalog.Find(id); if (animal == null) return;
            _animalId = id;
            if (_mode == AnimalUiMode.CharacterUpgrade) { Refresh(); return; }
            if (_mode == AnimalUiMode.StageAnimalSelect && _stage.Policy == CampaignSelectionPolicy.Fixed)
                _status = _stage.FixedLoadout.Get(animal.Role) == id ? "지정 동물 정보 열람 · 편성 유지" : "다른 동물 정보 열람 · 지정 편성 유지";
            else if (!_reading && _snapshotValid && AnimalUiRules.IsSelectable(animal, _snapshot, _mode, _stage, _multiplayer, out _))
            { _draft.Set(animal.Role, id); _status = null; _selectionRequest = null; }
            else
            { AnimalUiRules.IsSelectable(animal, _snapshot, _mode, _stage, _multiplayer, out var reason); _status = _snapshotValid && !_reading ? reason : "정보 열람 · 최신 사용 권한 연결 전"; }
            Render();
        }
        private string Availability(AnimalDefinition animal)
        {
            if (animal == null) return "선택 전";
            if (_reading) return "정보 조회 중";
            if (_mode == AnimalUiMode.CharacterUpgrade)
            {
                var progress = _snapshot.Find(animal.Id);
                if (progress == null) return "데이터 연결 전";
                if (!progress.Implemented) return string.IsNullOrEmpty(progress.AvailabilityMessage) ? "플레이 준비 중" : progress.AvailabilityMessage;
                if (!progress.Unlocked) return string.IsNullOrEmpty(progress.AvailabilityMessage) ? "잠김" : progress.AvailabilityMessage;
                return "성장 정보 연결됨";
            }
            if (_mode == AnimalUiMode.StageAnimalSelect && _stage?.Policy == CampaignSelectionPolicy.Fixed &&
                _stage.FixedLoadout?.Get(animal.Role) != animal.Id) return "열람 · 이 스테이지의 지정 동물이 아닙니다.";
            return AnimalUiRules.IsSelectable(animal, _snapshot, _mode, _stage, _multiplayer, out var reason) ?
                (_mode == AnimalUiMode.StageAnimalSelect && _stage.Policy == CampaignSelectionPolicy.Fixed ? "지정 동물" : "선택 가능") : reason;
        }
        private void Render()
        {
            if (!_opened || View == null) return;
            bool upgrade = _mode == AnimalUiMode.CharacterUpgrade;
            View.Title.text = upgrade ? "동물 강화" : _mode == AnimalUiMode.StageAnimalSelect ? "스테이지 동물 확인" : "멀티플레이 동물 선택";
            View.Subtitle.text = upgrade ? "15종 동물 · 액티브 / 패시브" : _mode == AnimalUiMode.StageAnimalSelect ? (string.IsNullOrWhiteSpace(_stage?.DisplayName) ? "스테이지 연결 전" : _stage.DisplayName) : CompactMultiplayerName(_multiplayer?.DisplayName);
            var animal = Catalog.Find(_animalId); var progress = _snapshot.Find(_animalId);
            View.HeroPortrait.sprite = animal?.Portrait; View.HeroPortrait.preserveAspect = true; View.HeroPortrait.useSpriteMesh = false;
            View.HeroName.text = animal?.DisplayName ?? "동물 선택";
            View.HeroRole.text = animal == null ? "" : AnimalUiRules.RoleName(animal.Role) + " 동물";
            View.HeroDescription.text = Backend == null ? "일러스트 미리보기 · 플레이 데이터 연결 전" : upgrade ? "액티브와 패시브를 각각 강화합니다." :
                _mode == AnimalUiMode.StageAnimalSelect && _stage.Policy == CampaignSelectionPolicy.Fixed ? "지정 편성을 유지하며 동물 정보를 열람합니다." : "카드를 누르면 해당 역할의 초안이 바뀝니다.";
            if (!upgrade)
                View.HeroDescription.text = "액티브: " + SelectionEffect(progress?.ActiveDescription) + "\n패시브: " + SelectionEffect(progress?.PassiveDescription);
            View.HeroState.text = Availability(animal);
            View.Balances.text = "코인 " + Number(_snapshot.CoinBalance) + "  ·  " + (animal?.DisplayName ?? "동물") + " 숙련도 " + Number(progress?.MasteryBalance);
            for (int i = 0; i < 3; i++)
            {
                var role = (AnimalRole)i; var selected = Catalog.Find(_draft.Get(role));
                bool required = Requirements?.Requires(role) == true;
                View.Slots[i].gameObject.SetActive(!upgrade && required);
                View.Slots[i].Bind(selected, _mode == AnimalUiMode.StageAnimalSelect && _stage.Policy == CampaignSelectionPolicy.Fixed ? "고정 · " + AnimalUiRules.RoleName(role) : AnimalUiRules.RoleName(role), selected != null, false, null);
                if (_mode == AnimalUiMode.StageAnimalSelect && selected == null) View.Slots[i].Name.text = "-- · 설정 대기";
                View.Slots[i].Button.interactable = !IsCommitting && !_modalVisible;
                View.RoleLabels[i].text = (_role == role ? "● " : "") + AnimalUiRules.RoleName(role);
                View.RoleTabs[i].interactable = !IsCommitting && !_modalVisible;
            }
            var animals = Catalog.ForRole(_role).ToList();
            for (int i = 0; i < _cards.Count && i < animals.Count; i++)
            {
                var definition = animals[i]; var state = _snapshot.Find(definition.Id);
                bool selectable = AnimalUiRules.IsSelectable(definition, _snapshot, _mode, _stage, _multiplayer, out _);
                bool fixedStage = _mode == AnimalUiMode.StageAnimalSelect && _stage.Policy == CampaignSelectionPolicy.Fixed;
                string badge = upgrade ? (_animalId == definition.Id ? "열람 중" : "") : fixedStage ?
                    (_draft.Get(_role) == definition.Id ? "지정 동물" : _animalId == definition.Id ? "열람 중" : "열람") :
                    _draft.Get(_role) == definition.Id ? (_animalId == definition.Id ? "선택됨 · 열람" : "선택됨") :
                    _animalId == definition.Id ? "열람 중" : selectable && _snapshotValid && !_reading ? "선택 가능" : "열람 가능";
                _cards[i].Bind(definition, badge, upgrade ? _animalId == definition.Id : _draft.Get(_role) == definition.Id,
                    state != null && !state.Unlocked && !state.CanUseInContext, ChooseAnimal);
                _cards[i].Button.interactable = !IsCommitting && !_modalVisible;
            }
            RenderTrack(View.ActiveTrack, _activeQuote, progress, UpgradeTrack.Active);
            RenderTrack(View.PassiveTrack, _passiveQuote, progress, UpgradeTrack.Passive);
            View.SelectionInfo.text = _mode == AnimalUiMode.StageAnimalSelect ?
                (_stage.Policy == CampaignSelectionPolicy.Fixed ? "지정 편성 · 동물 변경 불가" : _stage.Policy == CampaignSelectionPolicy.AllowedPool ? "스테이지 허용 동물 안에서 선택" : _stage.Policy == CampaignSelectionPolicy.Free ? "사용 가능한 동물을 역할별로 선택" : "스테이지 정책 연결 전") :
                "지상·특수·공중 편성 · 플레이어 간 동물 중복 허용";
            bool valid = !upgrade && CanSubmitSelection(out _);
            CanSubmitSelection(out var invalidReason);
            if (View.SelectionDetailsButton != null) View.SelectionDetailsButton.interactable = !_reading && !IsCommitting && !_modalVisible;
            View.Status.text = _pending ? "요청 처리 중 · 잠시 기다려주세요." : _uncertain ? "결과 확인이 필요합니다. 동일 요청으로 다시 확인해주세요." :
                _status ?? (Backend == null ? "데이터 연결 전 · 일러스트 열람" : upgrade ? "강화 전 비용과 효과를 확인해주세요." : valid ? "선택을 완료할 수 있습니다." : invalidReason);
            View.Primary.interactable = Backend != null && !_reading && !IsCommitting && !_modalVisible && !_selectionCompleted && (upgrade || valid);
            View.Refresh.interactable = Backend != null && !_reading && !IsCommitting && !_modalVisible;
            View.Back.interactable = !IsCommitting;
            View.BodyInteraction.interactable = !IsCommitting && !_modalVisible;
            View.ModalCancel.interactable = !IsCommitting;
            View.ModalConfirm.interactable = !_pending;
        }
        private void RenderTrack(AnimalUpgradeTrackView view, UpgradeQuote quote, AnimalProgress progress, UpgradeTrack track)
        {
            view.Title.text = track == UpgradeTrack.Active ? "액티브 강화 · 상세" : "패시브 강화 · 상세";
            int knownLevel = track == UpgradeTrack.Active ? progress?.ActiveLevel ?? -1 : progress?.PassiveLevel ?? -1;
            int level = quote != null && quote.CurrentLevel >= 0 ? quote.CurrentLevel : knownLevel;
            view.Level.text = level < 0 ? "Lv. --" : "Lv. " + level + (quote?.State == UpgradeQuoteState.Maximum ? " · MAX" : quote != null && quote.NextLevel >= 0 ? " → " + quote.NextLevel : "");
            string effect = quote?.CurrentEffect ?? (track == UpgradeTrack.Active ? progress?.ActiveDescription : progress?.PassiveDescription);
            view.Effect.text = _reading ? "강화 정보 조회 중" : "현재: " + CompactEffect(effect);
            if (quote?.State != UpgradeQuoteState.Maximum && !_reading) view.Effect.text += "\n다음: " + CompactEffect(quote?.NextEffect);
            view.Cost.text = quote == null ? "비용 -- · 설정 대기" : quote.State == UpgradeQuoteState.Maximum ? "최대 강화 완료" :
                quote.Costs.Count > 0 || quote.IsFree ? CompactCosts(quote) : "비용 -- · 설정 대기";
            view.ButtonLabel.text = quote?.State == UpgradeQuoteState.Maximum ? "MAX" : quote?.State == UpgradeQuoteState.InsufficientFunds ? "재화 부족" : "강화";
            view.UpgradeButton.interactable = CanUpgrade(quote) && !_reading && !IsCommitting && !_modalVisible;
            view.DetailsButton.interactable = !IsCommitting && !_modalVisible;
        }
        private bool CanUpgrade(UpgradeQuote quote) => AnimalUiRules.CanPurchaseUpgrade(Catalog, _snapshot, quote, Backend != null, _snapshotValid, out _);
        private static string DisplayEffect(string effect) => string.IsNullOrWhiteSpace(effect) ? "-- · 설정 대기" : effect;
        private static string CompactMultiplayerName(string value)
        {
            string text = string.IsNullOrWhiteSpace(value) ? "대기방 진입 전 편성" : value.Replace('\n', ' ').Replace('\r', ' ');
            var elements = new System.Globalization.StringInfo(text);
            if (elements.LengthInTextElements <= 24) return text;
            // Keep the host value intact. The full name is available even in read-only details.
            return text.Contains("<") ? "모드 이름 · 동물 정보에서 확인" : elements.SubstringByTextElements(0, 18) + "… (동물 정보)";
        }
        private static string SelectionEffect(string effect)
        {
            string text = DisplayEffect(effect);
            return text.Contains("\n") || new System.Globalization.StringInfo(text).LengthInTextElements > 20
                ? "전체 효과는 동물 정보에서 확인" : text;
        }
        private static string CompactEffect(string effect)
        {
            string text = DisplayEffect(effect).Replace('\n', ' ').Replace('\r', ' ');
            var elements = new System.Globalization.StringInfo(text);
            if (elements.LengthInTextElements <= 40) return text;
            // Do not cut rich-text tags. Full, unmodified content is retained in both modals.
            return text.Contains("<") ? "전체 효과는 상세에서 확인" : elements.SubstringByTextElements(0, 40) + "… (상세)";
        }
        private static string CompactCosts(UpgradeQuote quote)
        {
            string text = FormatCosts(quote);
            return text.Length <= 42 && !text.Contains("\n") ? text : "비용 " + quote.Costs.Count + "종 · 상세 확인";
        }
        private static string Number(long? value) => value.HasValue ? value.Value.ToString("N0") : "--";
        private static string FormatCosts(UpgradeQuote quote) => quote.IsFree && quote.Costs.Count == 0 ? "무료 (설정된 비용)" :
            string.Join(" · ", quote.Costs.Select(cost => (cost.DisplayName ?? cost.CurrencyId) + " " + cost.Amount.ToString("N0")));
        private void Primary()
        {
            if (_reading || IsCommitting || _modalVisible || Backend == null || _selectionCompleted) return;
            if (_mode == AnimalUiMode.CharacterUpgrade) { Refresh(); return; }
            if (!CanSubmitSelection(out var reason)) { _status = reason; Render(); return; }
            ShowModal(_mode == AnimalUiMode.StageAnimalSelect ? "이 지정 편성으로 시작할까요?" : "이 편성으로 입장할까요?",
                (_mode == AnimalUiMode.StageAnimalSelect ? (_stage.DisplayName ?? _stage.StageId) : (_multiplayer.DisplayName ?? _multiplayer.ModeId)) +
                "\n\n" + LoadoutSummary(_draft), "확정", () => CommitSelection(false));
        }
        private bool CanSubmitSelection(out string reason)
        {
            if (_mode == AnimalUiMode.StageAnimalSelect)
                return AnimalUiRules.CanConfirmFixedStage(Catalog, _snapshot, _draft, _stage, Backend != null, _snapshotValid, out reason);
            return AnimalUiRules.CanConfirmMultiplayer(Catalog, _snapshot, _draft, _multiplayer, Backend != null, _snapshotValid, out reason);
        }
        private void ShowSelectionDetails()
        {
            if (_mode == AnimalUiMode.CharacterUpgrade || _reading || IsCommitting || _modalVisible) return;
            var animal = Catalog.Find(_animalId); if (animal == null) return;
            var progress = _snapshot.Find(_animalId);
            string marker = _mode == AnimalUiMode.StageAnimalSelect ?
                (_stage?.FixedLoadout?.Get(animal.Role) == animal.Id ? "스테이지 지정 동물" : "정보 열람 · 지정 편성 유지") :
                (_draft.Get(animal.Role) == animal.Id ? "현재 선택한 동물 · 정보 열람" : "정보 열람 · 현재 선택과 별개");
            string modeName = _mode == AnimalUiMode.MultiplayerAnimalSelect ? (_multiplayer?.DisplayName ?? "모드 설정 대기") + "\n\n" : "";
            ShowModal(animal.DisplayName + " · 동물 정보", modeName + marker + "\n" + AnimalUiRules.RoleName(animal.Role) + " 동물\n" +
                Availability(animal) + "\n\n액티브 · Lv. " + (progress == null || progress.ActiveLevel < 0 ? "--" : progress.ActiveLevel.ToString()) +
                "\n" + DisplayEffect(progress?.ActiveDescription) + "\n\n패시브 · Lv. " +
                (progress == null || progress.PassiveLevel < 0 ? "--" : progress.PassiveLevel.ToString()) +
                "\n" + DisplayEffect(progress?.PassiveDescription), "닫기", CloseModal);
            View.ModalCancelLabel.text = "돌아가기";
        }
        private string LoadoutSummary(AnimalLoadout loadout) => string.Join("\n", (Requirements?.RequiredRoles ?? Array.Empty<AnimalRole>())
            .Select(role => AnimalUiRules.RoleName(role) + " · " + (Catalog.Find(loadout.Get(role))?.DisplayName ?? "선택 전")));
        private void ConfirmUpgrade(UpgradeTrack track)
        {
            var quote = track == UpgradeTrack.Active ? _activeQuote : _passiveQuote;
            if (!CanUpgrade(quote) || _reading || IsCommitting || _modalVisible) return;
            ShowModal("강화를 진행할까요?", Catalog.Find(_animalId).DisplayName + " · " + (track == UpgradeTrack.Active ? "액티브" : "패시브") +
                "\nLv. " + quote.CurrentLevel + " → " + quote.NextLevel + "\n현재: " + quote.CurrentEffect +
                "\n다음: " + quote.NextEffect + "\n비용: " + FormatCosts(quote), "강화", () => CommitUpgrade(quote, false));
        }
        private void ShowTrackDetails(UpgradeTrack track)
        {
            if (IsCommitting || _modalVisible) return;
            var quote = track == UpgradeTrack.Active ? _activeQuote : _passiveQuote;
            var progress = _snapshot.Find(_animalId);
            var description = track == UpgradeTrack.Active ? progress?.ActiveDescription : progress?.PassiveDescription;
            string body = "현재 효과\n" + DisplayEffect(quote?.CurrentEffect ?? description) +
                "\n\n다음 효과\n" + (quote?.State == UpgradeQuoteState.Maximum ? "최대 강화 완료" : DisplayEffect(quote?.NextEffect)) + "\n\n비용\n" +
                (quote == null || (!quote.IsFree && quote.Costs.Count == 0) ? "설정 대기" : FormatCosts(quote)) +
                "\n\n" + (quote?.Message ?? "서버 또는 기존 성장 데이터에 연결된 정보를 표시합니다.");
            ShowModal((track == UpgradeTrack.Active ? "액티브" : "패시브") + " 강화 상세", body, "닫기", CloseModal);
            View.ModalCancelLabel.text = "돌아가기";
        }
        private void ShowModal(string title, string body, string confirm, Action action)
        {
            _modalVisible = true; _modalAction = action; View.ModalTitle.text = title; View.ModalBody.text = body;
            if (View.ModalBodyScroll != null) View.ModalBodyScroll.verticalNormalizedPosition = 1;
            View.ModalConfirmLabel.text = confirm; View.ModalCancelLabel.text = "취소"; View.Modal.SetActive(true); Render();
            var scroll = View.ModalBody.GetComponentInParent<UnityEngine.UI.ScrollRect>();
            if (scroll != null)
            {
                Canvas.ForceUpdateCanvases(); scroll.StopMovement(); scroll.verticalNormalizedPosition = 1;
            }
        }
        private void ShowRetryModal() => ShowModal("처리 결과 확인", "응답을 확인하지 못했습니다.\n같은 요청 ID로 결과를 다시 확인합니다.\n확인 전에는 편성과 비용을 바꾸지 않습니다." +
            (_commitMode == AnimalUiMode.MultiplayerAnimalSelect && !string.IsNullOrWhiteSpace(_multiplayerUncertainReason) ? "\n\n" + _multiplayerUncertainReason : ""), "결과 다시 확인", () =>
        {
            if (_commitMode == AnimalUiMode.CharacterUpgrade) CommitUpgrade(null, true); else CommitSelection(true);
        });
        private void CloseModal()
        {
            if (IsCommitting) return;
            _modalVisible = false; _modalAction = null; View.Modal.SetActive(false); Render();
        }
        private async void CommitUpgrade(UpgradeQuote quote, bool retry)
        {
            if (_pending || Backend == null || (!retry && (IsCommitting || !CanUpgrade(quote)))) return;
            if (!retry) _upgradeRequest = new UpgradeCommitRequest { ActionId = Guid.NewGuid().ToString("N"), AnimalId = quote.AnimalId,
                Track = quote.Track, QuoteToken = quote.QuoteToken, ExpectedCurrentLevel = quote.CurrentLevel };
            if (_upgradeRequest == null) return;
            _commitMode = AnimalUiMode.CharacterUpgrade; await ExecuteCommit(() => Backend.TryUpgradeAsync(_upgradeRequest, CancellationToken.None));
        }
        private async void CommitSelection(bool retry)
        {
            if (_pending || Backend == null || _selectionCompleted || (!retry && IsCommitting)) return;
            if (!retry)
            {
                if (!CanSubmitSelection(out var reason)) { _status = reason; CloseModal(); return; }
                var normalized = AnimalUiRules.NormalizeLoadout(_draft, Requirements);
                _selectionRequest = new SelectionCommitRequest { ActionId = Guid.NewGuid().ToString("N"), ContextId = _mode == AnimalUiMode.StageAnimalSelect ? _stage.StageId : _multiplayer.ModeId,
                    SnapshotRevision = _snapshot.Revision, PolicyRevision = Requirements.PolicyRevision, Loadout = normalized,
                    EntryIntent = _mode == AnimalUiMode.MultiplayerAnimalSelect ? _multiplayer.EntryIntent : null };
            }
            if (_selectionRequest == null) return;
            _commitMode = _mode;
            if (_mode == AnimalUiMode.StageAnimalSelect) await ExecuteCommit(() => Backend.SubmitCampaignAsync(_stage.Clone(), _selectionRequest.Clone(), CancellationToken.None));
            else await ExecuteCommit(() => Backend.SubmitMultiplayerAsync(_multiplayer.Clone(), _selectionRequest.Clone(), CancellationToken.None));
        }
        private async Task ExecuteCommit(Func<Task<AnimalUiCommitResult>> operation)
        {
            int epoch = _epoch; _pending = true; _uncertain = false; Render();
            AnimalUiCommitResult result;
            try { result = await operation(); }
            catch (Exception exception) { result = new AnimalUiCommitResult { Status = CommitStatus.Unknown, Message = exception.Message }; }
            if (epoch != _epoch || !isActiveAndEnabled) return;
            _pending = false;
            if (result == null || result.Status == CommitStatus.Unknown ||
                (_commitMode == AnimalUiMode.MultiplayerAnimalSelect && !Enum.IsDefined(typeof(CommitStatus), result.Status)))
            {
                if (_commitMode == AnimalUiMode.MultiplayerAnimalSelect) _multiplayerUncertainReason = result?.Message;
                _uncertain = true; ShowRetryModal(); return;
            }
            if (result.Status == CommitStatus.Accepted && _commitMode == AnimalUiMode.StageAnimalSelect &&
                !AnimalUiRules.IsAcceptedFixedStageResult(Catalog, _snapshot, _stage, _selectionRequest, result, out _))
            { _uncertain = true; ShowRetryModal(); return; }
            if (result.Status == CommitStatus.Accepted && _commitMode == AnimalUiMode.MultiplayerAnimalSelect &&
                !AnimalUiRules.IsAcceptedMultiplayerResult(Catalog, _snapshot, _multiplayer, _selectionRequest, result, out var multiplayerReason))
            { _multiplayerUncertainReason = multiplayerReason; _uncertain = true; ShowRetryModal(); return; }
            _uncertain = false; _modalVisible = false; View.Modal.SetActive(false);
            _multiplayerUncertainReason = null;
            if (result.Snapshot != null && (_commitMode != AnimalUiMode.MultiplayerAnimalSelect ||
                AnimalUiRules.ValidateMultiplayerSnapshotContext(result.Snapshot, _multiplayer, out _))) _snapshot = result.Snapshot;
            _status = result.Message ?? (result.Status == CommitStatus.Accepted ? "요청이 승인되었습니다." : "요청을 완료하지 못했습니다.");
            if (result.Status != CommitStatus.Accepted && _commitMode != AnimalUiMode.CharacterUpgrade)
            {
                _snapshotValid = false;
                _status += _commitMode == AnimalUiMode.StageAnimalSelect ? " 최신 스테이지 사용 권한을 새로고침해주세요." : " 최신 모드 사용 권한을 새로고침해주세요.";
            }
            _upgradeRequest = null; _selectionRequest = null; Render();
            if (result.Status == CommitStatus.Accepted)
            {
                if (_commitMode == AnimalUiMode.CharacterUpgrade) { UpgradeAccepted?.Invoke(result); Refresh(); }
                else
                {
                    _selectionCompleted = true; _draft = result.AcceptedLoadout.Clone(); _original = _draft.Clone(); Render();
                    if (_commitMode == AnimalUiMode.StageAnimalSelect) CampaignAccepted?.Invoke(result);
                    else MultiplayerAccepted?.Invoke(result);
                }
            }
            else
            {
                _activeQuote = _passiveQuote = null; Render();
                // A definitive refusal ends this request. Re-read before another confirmation,
                // retaining the service's refusal reason after the refresh.
                if (_commitMode == AnimalUiMode.CharacterUpgrade) RefreshWithStatus(_status);
            }
        }
        public void Back()
        {
            if (IsCommitting) return;
            if (_modalVisible) { CloseModal(); return; }
            CancelReads(); _draft = _original.Clone(); Cancelled?.Invoke(_mode, _original.Clone()); gameObject.SetActive(false);
        }
    }
}
