using System;
using System.Collections.Generic;

using UnityEngine;

//소유: 웨이브 인덱스, 플레이어 HP, 골드, EWaveState
//책임: 시작/종료 결정, 보상 지급, 패배 판정, UI 이벤트 발행
//금지: 유닛 탐색/이동/공격, Instantiate 직접 처리
public class BattleManager : AppService
{
    [Header("Player")]
    [SerializeField, Min(1)] public int playerMaxHp = 20;
    [SerializeField, Min(0)] public int startingGold;
    
    public BattleWaveData CurrentWave => waveList[_currentWaveIndex];
    
    public event Action<EWaveState> OnStateChanged;
    public event Action<int> OnWaveChanged;
    public event Action<int> OnHpChanged;
    public event Action<int> OnGoldChanged;

    private UnitManager _unitManager;
    
    private EWaveState _state;
    private int _currentWaveIndex;
    private int _playerHp;
    private int _gold;
    
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
                    EnemyId = "Enemy A",
                    Stats = new BattleUnitStats
                    {
                        MaxHp = 18f,
                        AttackDamage = 3f,
                        AttackRate = 1f,
                        AttackRange = 1.2f,
                        MoveSpeed = 2f
                    }
                },
                new BattleEnemySpawnData
                {
                    EnemyId = "Enemy B",
                    Stats = new BattleUnitStats
                    {
                        MaxHp = 20f,
                        AttackDamage = 3f,
                        AttackRate = 1f,
                        AttackRange = 1.2f,
                        MoveSpeed = 2f
                    }
                }
            }
        }
    };

    protected override void Awake()
    {
        base.Awake();

        _state = EWaveState.Pending;
        _currentWaveIndex = 0;
        _playerHp = playerMaxHp;
        _gold = startingGold;
    }

    private void Start()
    {
        _unitManager = App.Get<UnitManager>();
        
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

    private void DefeatWave()
    {
        if (_state is not EWaveState.Active) return;
        
        _playerHp = Mathf.Max(0, _playerHp - Mathf.Max(1, 10)); // TODO: 데미지 데이터와 연결
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
}