using System;
using System.Collections;
using UnityEngine;

//소유: 웨이브 인덱스, 플레이어 HP, 골드, EWaveState
//책임: 시작/종료 결정, 보상 지급, 패배 판정, UI 이벤트 발행
//금지: 유닛 탐색/이동/공격, Instantiate 직접 처리
public class BattleManager : AppService, IItemEventListener
{
    [Header("Player")]
    [SerializeField, Min(1)] public int playerMaxHp = 20;
    [SerializeField, Min(0f)] private float waveResolutionDuration = 2f;

    public BattleWaveData CurrentWave => _runState?.CurrentWave;
    public int CurrentWaveNumber => _runState?.CurrentWaveNumber ?? 1;
    public int TotalWaveCount => _runState?.TotalWaveCount ?? 0;
    public int PlayerHp => _runState?.PlayerHp ?? playerMaxHp;
    public int Gold => _economy?.Gold ?? 0;
    public bool IsInitialized { get; private set; }
    public EWaveState State => _runState?.State ?? EWaveState.Pending;
    public bool IsPreparationPhase => State == EWaveState.Pending;
    public bool CanUsePreparationActions =>
        IsPreparationPhase && !_isPreparationLocked;
    public bool HasValidCurrentWave => _runState?.HasValidCurrentWave ?? false;
    
    public event Action<EWaveState> OnStateChanged;
    public event Action OnInitialized;
    public event Action<int> OnWaveChanged;
    public event Action<int> OnHpChanged;
    public event Action<int> OnGoldChanged;
    public event Action<string> OnActionRejected;
    public event Action<bool> OnPreparationAvailabilityChanged;
    public event Action<EWaveResolutionResult, int> OnWaveResolutionStarted;

    private UnitManager _unitManager;
    private BattleRunState _runState;
    private BattleEconomy _economy = new(0);
    private int _barrierDamageReduction;
    private int _minimumBarrierDamage = 1;
    private bool _isPreparationLocked;
    private readonly WaveResolutionState _waveResolution = new();
    private Coroutine _waveResolutionCoroutine;

    private void Start()
    {
        _unitManager = App.Get<UnitManager>();
        _unitManager.OnBattleRosterChanged += OnBattleRosterChanged;

        var titleData = App.Get<TitleData>();
        _runState = new BattleRunState(
            titleData.BattleWaves,
            titleData.HasValidBattleRun,
            playerMaxHp);
        _economy = new BattleEconomy(
            titleData.BattleRunCommon?.StartingGold ?? 0);

        if (!titleData.HasValidBattleRun)
        {
            Debug.LogError(
                "[BattleManager] Battle wave data is invalid. " +
                "Wave start is disabled.");
        }

        App.Get<ItemManager>().Subscribe(EItem.BarrierReinforcement, this);

        IsInitialized = true;
        OnInitialized?.Invoke();
        OnStateChanged?.Invoke(_runState.State);
        OnHpChanged?.Invoke(_runState.PlayerHp);
        OnGoldChanged?.Invoke(_economy.Gold);
        OnWaveChanged?.Invoke(_runState.CurrentWaveIndex);
    }

    private void OnBattleRosterChanged()
    {
        if (State == EWaveState.Active &&
            BattleResolutionPolicy.TryDetectWipe(
                _unitManager.RemainingAllyCount,
                _unitManager.RemainingEnemyCount,
                out EWaveResolutionResult result))
        {
            BeginWaveResolution(result);
        }
    }
    
    public bool TryStartWave()
    {
        if (!CanUsePreparationActions)
        {
            RejectAction("전투 준비 단계에서만 웨이브를 시작할 수 있습니다.");
            return false;
        }

        if (!HasValidCurrentWave)
        {
            RejectAction("시작할 웨이브 데이터가 없습니다.");
            return false;
        }

        if (_unitManager == null || _unitManager.DeployedAllyCount <= 0)
        {
            RejectAction("아군 유닛을 한 명 이상 준비해야 합니다.");
            return false;
        }

        if (!_unitManager.CanStartWaveWithCurrentRoster)
        {
            RejectAction("배치 아군은 5마리까지 웨이브에 참가할 수 있습니다.");
            return false;
        }

        ChangeState(EWaveState.Active);
        SoundManager.PlaySFXIfAvailable(SoundName.WaveStart);
        return true;
    }

    public void StartWave()
    {
        TryStartWave();
    }

    public bool TrySpendGold(int amount)
    {
        int previousGold = _economy.Gold;
        if (!_economy.TrySpend(amount)) return false;
        if (_economy.Gold != previousGold)
        {
            OnGoldChanged?.Invoke(_economy.Gold);
        }
        return true;
    }

    public bool TrySpendPreparationGold(int amount)
    {
        if (!CanUsePreparationActions) return false;
        return TrySpendGold(amount);
    }

    public void SetPreparationLock(bool isLocked)
    {
        if (_isPreparationLocked == isLocked) return;

        _isPreparationLocked = isLocked;
        OnPreparationAvailabilityChanged?.Invoke(
            CanUsePreparationActions);
    }

    public void AddGold(int amount)
    {
        if (!_economy.Add(amount)) return;
        OnGoldChanged?.Invoke(_economy.Gold);
    }

    public void OnItemEvent(Item item)
    {
        _barrierDamageReduction = Mathf.RoundToInt(item.Value1);
        _minimumBarrierDamage = Mathf.Max(1, Mathf.RoundToInt(item.Value2));
    }

    private void BeginWaveResolution(EWaveResolutionResult result)
    {
        if (State != EWaveState.Active ||
            !_waveResolution.TryBegin(
                result,
                CurrentWaveNumber,
                Time.time,
                waveResolutionDuration)) return;

        BattleWaveData wave = CurrentWave;
        bool isFinalWave =
            _runState.CurrentWaveIndex + 1 >= _runState.TotalWaveCount;
        ChangeState(EWaveState.Resolving);

        if (result == EWaveResolutionResult.Cleared)
        {
            if (wave != null)
            {
                AddGold(isFinalWave
                    ? wave.FinalClearGoldReward
                    : wave.WaveClearGoldReward);
            }
        }
        else
        {
            int damage = BarrierDamageCalculator.Calculate(
                _unitManager.CalculateRemainingBreachDamage(),
                _barrierDamageReduction,
                _minimumBarrierDamage);
            _runState.ApplyPlayerDamage(damage);
            OnHpChanged?.Invoke(_runState.PlayerHp);
            if (_runState.PlayerHp > 0 && wave != null)
            {
                AddGold(wave.RetryGoldReward);
            }
        }

        SoundManager.PlaySFXIfAvailable(
            result == EWaveResolutionResult.Cleared
                ? SoundName.WaveWin
                : SoundName.WaveFailed);
        OnWaveResolutionStarted?.Invoke(result, CurrentWaveNumber);
        _waveResolutionCoroutine = StartCoroutine(WaitForWaveResolution());
    }

    private IEnumerator WaitForWaveResolution()
    {
        yield return new WaitForSeconds(waveResolutionDuration);
        _waveResolutionCoroutine = null;
        FinishWaveResolution();
    }

    private void FinishWaveResolution()
    {
        if (State != EWaveState.Resolving ||
            !_waveResolution.IsPending) return;

        EWaveResolutionResult result = _waveResolution.Result;
        bool isFinalWave =
            _runState.CurrentWaveIndex + 1 >= _runState.TotalWaveCount;
        bool hasValidWave = CurrentWave != null;
        _unitManager.ResolveWaveResult();
        _waveResolution.Clear();

        if (!hasValidWave)
        {
            ChangeState(EWaveState.Defeat);
            return;
        }

        EWaveState nextState = BattleResolutionPolicy.ResolveNextState(
            result,
            isFinalWave,
            _runState.PlayerHp);
        if (result == EWaveResolutionResult.Cleared &&
            nextState == EWaveState.Pending)
        {
            _runState.AdvanceWave();
            OnWaveChanged?.Invoke(_runState.CurrentWaveIndex);
        }

        ChangeState(nextState);
    }

    private void ChangeState(EWaveState nextState)
    {
        if (!_runState.ChangeState(nextState)) return;
        OnStateChanged?.Invoke(_runState.State);
    }

    private void RejectAction(string message)
    {
        Debug.LogWarning($"[BattleManager] {message}");
        OnActionRejected?.Invoke(message);
    }

    protected override void OnDestroy()
    {
        if (_waveResolutionCoroutine != null)
        {
            StopCoroutine(_waveResolutionCoroutine);
            _waveResolutionCoroutine = null;
        }

        if (_unitManager != null)
        {
            _unitManager.OnBattleRosterChanged -= OnBattleRosterChanged;
        }

        if (App.TryGet<ItemManager>(out var itemManager))
        {
            itemManager.Unsubscribe(EItem.BarrierReinforcement, this);
        }

        base.OnDestroy();
    }
}
