using System;
using System.Collections.Generic;

using UnityEngine;

//소유: 웨이브 인덱스, 플레이어 HP, 골드, EWaveState
//책임: 시작/종료 결정, 보상 지급, 패배 판정, UI 이벤트 발행
//금지: 유닛 탐색/이동/공격, Instantiate 직접 처리
public class BattleManager : AppService, IItemEventListener
{
    [Header("Player")]
    [SerializeField, Min(1)] public int playerMaxHp = 20;
    [SerializeField, Min(0)] public int startingGold;
    
    public BattleWaveData CurrentWave => waveList[_currentWaveIndex];
    public int CurrentWaveNumber => _currentWaveIndex + 1;
    public int Gold => _gold;
    
    public event Action<EWaveState> OnStateChanged;
    public event Action<int> OnWaveChanged;
    public event Action<int> OnHpChanged;
    public event Action<int> OnGoldChanged;

    private UnitManager _unitManager;
    
    private EWaveState _state;
    private int _currentWaveIndex;
    private int _playerHp;
    private int _gold;
    private int _barrierDamageReduction;
    private int _minimumBarrierDamage = 1;
    
    [Header("Wave Input (Temporary)")]
    [SerializeField] private List<BattleWaveData> waveList = new()
    {
        new BattleWaveData
        {
            WaveName = "Wave 1",
            WaveClearGoldReward = 10,
            Enemies = new List<BattleEnemySpawnData>
            {
                new BattleEnemySpawnData
                {
                    EnemyId = "goblin",
                    Count = 2
                },
                new BattleEnemySpawnData
                {
                    EnemyId = "wolf",
                    Count = 1
                }
            }
        }
    };

    protected override void Awake()
    {
        base.Awake();

        NormalizeLegacyWaveData();
        _state = EWaveState.Pending;
        _currentWaveIndex = 0;
        _playerHp = playerMaxHp;
        _gold = startingGold;
    }

    private void NormalizeLegacyWaveData()
    {
        if (waveList == null) return;

        foreach (var wave in waveList)
        {
            if (wave?.Enemies == null) continue;

            foreach (var enemy in wave.Enemies)
            {
                if (enemy == null) continue;

                if (enemy.EnemyId == "Enemy A")
                {
                    enemy.EnemyId = "goblin";
                }
                else if (enemy.EnemyId == "Enemy B")
                {
                    enemy.EnemyId = "wolf";
                }

                enemy.Count = Mathf.Max(1, enemy.Count);
            }
        }
    }

    private void Start()
    {
        _unitManager = App.Get<UnitManager>();

        App.Get<ItemManager>().Subscribe(EItem.BarrierReinforcement, this);

        OnStateChanged?.Invoke(_state);
        OnHpChanged?.Invoke(_playerHp);
        OnGoldChanged?.Invoke(_gold);
        OnWaveChanged?.Invoke(_currentWaveIndex);
    }

    private void Update()
    {
        if (_state is not EWaveState.Active) return;

        if (_unitManager.RemainingEnemyCount <= 0)
        {
            CompleteWave();
        }
        else if (_unitManager.RemainingAllyCount <= 0)
        {
            DefeatWave();
        }
    }
    
    public void StartWave()
    {
        if (_state is not EWaveState.Pending) return;
        
        ChangeState(EWaveState.Active);
    }

    public bool TrySpendGold(int amount)
    {
        var clampedAmount = Mathf.Max(0, amount);
        if (clampedAmount <= 0) return true;

        if (_gold < clampedAmount) return false;

        _gold -= clampedAmount;
        OnGoldChanged?.Invoke(_gold);
        return true;
    }

    public void AddGold(int amount)
    {
        if (amount <= 0) return;

        _gold += amount;
        OnGoldChanged?.Invoke(_gold);
    }

    public void OnItemEvent(Item item)
    {
        _barrierDamageReduction = Mathf.RoundToInt(item.Value1);
        _minimumBarrierDamage = Mathf.Max(1, Mathf.RoundToInt(item.Value2));
    }

    private void DefeatWave()
    {
        if (_state is not EWaveState.Active) return;
        
        int breachDamage = _unitManager.CalculateRemainingBreachDamage();
        var damage = Mathf.Max(
            _minimumBarrierDamage,
            breachDamage - _barrierDamageReduction);
        _playerHp = Mathf.Max(0, _playerHp - damage);
        OnHpChanged?.Invoke(_playerHp);

        ChangeState(_playerHp <= 0 ? EWaveState.Defeat : EWaveState.Pending);
    }

    private void CompleteWave()
    {
        if (_state is not EWaveState.Active) return;

        if (_currentWaveIndex + 1 >= waveList.Count)
        {
            ChangeState(EWaveState.Victory);
        }
        else
        {
            var wave = waveList[_currentWaveIndex];
            
            _gold += Mathf.Max(0, wave.WaveClearGoldReward);
            OnGoldChanged?.Invoke(_gold);
            
            _currentWaveIndex++;
            OnWaveChanged?.Invoke(_currentWaveIndex);
            
            ChangeState(EWaveState.Pending);
        }
    }

    private void ChangeState(EWaveState nextState)
    {
        if (_state == nextState)
        {
            return;
        }

        _state = nextState;
        OnStateChanged?.Invoke(_state);
    }

    protected override void OnDestroy()
    {
        if (App.TryGet<ItemManager>(out var itemManager))
        {
            itemManager.Unsubscribe(EItem.BarrierReinforcement, this);
        }

        base.OnDestroy();
    }
}
