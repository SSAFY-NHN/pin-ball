using System.Collections.Generic;

using UnityEngine;

public class WaveBattleController : MonoBehaviour
{
    [Header("Flow")]
    [SerializeField] private bool autoStartAfterPreparation = true;
    [SerializeField, Min(0f)] private float preparationTime = 8f;

    [Header("Player")]
    [SerializeField, Min(1)] private int playerMaxHp = 20;
    [SerializeField, Min(0)] private int startingGold = 0;

    [Header("Placement Input (Temporary)")]
    [SerializeField] private List<BattleUnitSpawnData> placedUnitList = new()
    {
        new BattleUnitSpawnData
        {
            UnitId = "DefaultAlly",
            SpawnPosition = new Vector2(-4f, 0f),
            BaseStats = new BattleActorStats
            {
                MaxHp = 24f,
                AttackDamage = 5f,
                AttackRate = 1.2f,
                AttackRange = 1.6f,
                MoveSpeed = 3.2f
            },
            Modifier = new BattleUnitModifier
            {
                MergeTier = 1,
                MergeAttackBonusPerTier = 0.2f,
                MergeHpBonusPerTier = 0.25f,
                EquipmentAttackBonus = 2f,
                EquipmentHpBonus = 4f
            }
        }
    };

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
                    SpawnPosition = new Vector2(4.8f, 1f),
                    Stats = new BattleActorStats
                    {
                        MaxHp = 18f,
                        AttackDamage = 3f,
                        AttackRate = 1f,
                        AttackRange = 1.2f,
                        MoveSpeed = 2f
                    },
                    DefenseDamage = 1
                },
                new BattleEnemySpawnData
                {
                    EnemyId = "Enemy B",
                    SpawnPosition = new Vector2(5.2f, -1f),
                    Stats = new BattleActorStats
                    {
                        MaxHp = 20f,
                        AttackDamage = 3f,
                        AttackRate = 1f,
                        AttackRange = 1.2f,
                        MoveSpeed = 2f
                    },
                    DefenseDamage = 1
                }
            }
        }
    };

    [Header("Battlefield")]
    [SerializeField] private Vector2 defensePointPosition = new(-7f, 0f);

    private readonly List<BattleActor> _activeAllies = new();
    private readonly List<BattleActor> _activeEnemies = new();
    
    private BattleStatusPanel _statusPanel;
    private Transform _defensePoint;

    private bool _isWaveActive;
    private bool _isDefeated;
    private bool _isPreparing;
    private int _currentWaveIndex;
    private int _playerHp;
    private int _gold;
    private float _prepareRemainSeconds;

    private void Awake()
    {
        _currentWaveIndex = -1;
        _playerHp = playerMaxHp;
        _gold = startingGold;
        _prepareRemainSeconds = preparationTime;

        EnsureDefensePoint();
        EnsureStatusPanel();
    }

    private void Start()
    {
        _statusPanel.BindStart(OnClickStartWave);
        _statusPanel.SetPlayerHp(_playerHp, playerMaxHp);
        _statusPanel.SetGold(_gold);
        _statusPanel.SetWaveInfo(_currentWaveIndex, waveList.Count, 0);
        _statusPanel.SetResult("준비 중. 버튼으로 즉시 시작 가능");
        _statusPanel.SetStartButtonVisible(true);

        BeginPreparation();
    }

    private void Update()
    {
        CleanupDestroyedActors();

        if (_isDefeated)
        {
            return;
        }

        if (_isPreparing)
        {
            _prepareRemainSeconds -= Time.deltaTime;
            _statusPanel.SetPreparation(_prepareRemainSeconds);

            if (autoStartAfterPreparation && _prepareRemainSeconds <= 0f)
            {
                StartNextWave();
            }
        }

        if (_isWaveActive && _activeEnemies.Count == 0)
        {
            CompleteWave();
        }

        _statusPanel.SetPlayerHp(_playerHp, playerMaxHp);
        _statusPanel.SetGold(_gold);
        _statusPanel.SetWaveInfo(_currentWaveIndex, waveList.Count, _activeEnemies.Count);
    }

    public BattleActor FindClosestAliveEnemy(Vector3 fromPosition, float maxDistance)
    {
        return FindClosest(fromPosition, maxDistance, _activeEnemies);
    }

    public BattleActor FindClosestAliveAlly(Vector3 fromPosition, float maxDistance)
    {
        return FindClosest(fromPosition, maxDistance, _activeAllies);
    }

    public void NotifyActorDied(BattleActor actor)
    {
        if (actor == null)
        {
            return;
        }

        if (actor.Team == EBattleTeam.Ally)
        {
            _activeAllies.Remove(actor);
        }
        else
        {
            _activeEnemies.Remove(actor);
        }
    }

    public void NotifyEnemyReachedDefense(BattleActor enemy)
    {
        if (enemy == null || !enemy.IsAlive || enemy.Team != EBattleTeam.Enemy)
        {
            return;
        }

        ApplyDefenseDamage(enemy.DefenseDamage);
    }

    private void BeginPreparation()
    {
        _isPreparing = true;
        _prepareRemainSeconds = preparationTime;
        _statusPanel.SetPreparation(_prepareRemainSeconds);
    }

    private void OnClickStartWave()
    {
        StartNextWave();
    }

    private void StartNextWave()
    {
        if (_isDefeated || _isWaveActive)
        {
            return;
        }

        if (_currentWaveIndex + 1 >= waveList.Count)
        {
            _statusPanel.SetResult("모든 웨이브 완료");
            _statusPanel.SetStartButtonVisible(false);
            _isPreparing = false;
            return;
        }

        _isPreparing = false;
        _statusPanel.SetPreparation(0f);
        _statusPanel.SetStartButtonVisible(false);

        _currentWaveIndex++;
        _isWaveActive = true;

        ClearAllActors();
        SpawnPlacedAllies();
        SpawnEnemies(waveList[_currentWaveIndex]);

        if (_activeEnemies.Count == 0)
        {
            CompleteWave();
            return;
        }

        _statusPanel.SetResult($"{waveList[_currentWaveIndex].WaveName} 시작");
    }

    private void CompleteWave()
    {
        if (!_isWaveActive || _isDefeated)
        {
            return;
        }

        _isWaveActive = false;

        var wave = waveList[_currentWaveIndex];
        _gold += Mathf.Max(0, wave.WaveClearGoldReward);
        _statusPanel.SetResult(
            $"웨이브 승리! +{wave.WaveClearGoldReward} Gold (임시)");
        _statusPanel.SetStartButtonVisible(true);

        if (_currentWaveIndex + 1 >= waveList.Count)
        {
            _statusPanel.SetResult(
                $"최종 웨이브 승리! 총 Gold: {_gold}");
            _statusPanel.SetStartButtonVisible(false);
            return;
        }

        BeginPreparation();
    }

    private void ApplyDefenseDamage(int damage)
    {
        _playerHp = Mathf.Max(0, _playerHp - Mathf.Max(1, damage));
        _statusPanel.SetResult(
            $"방어 지점 피해! Player HP {_playerHp}/{playerMaxHp}");

        if (_playerHp <= 0)
        {
            HandleDefeat();
        }
    }

    private void HandleDefeat()
    {
        if (_isDefeated)
        {
            return;
        }

        _isDefeated = true;
        _isWaveActive = false;
        _isPreparing = false;
        _statusPanel.SetStartButtonVisible(false);
        _statusPanel.SetResult("패배: 플레이어 체력이 0이 되었습니다.");
    }

    private void SpawnPlacedAllies()
    {
        foreach (var data in placedUnitList)
        {
            if (data == null)
            {
                continue;
            }

            if (!TryBuildUnitStats(data, out var finalStats))
            {
                Debug.LogWarning($"[WaveBattleController] Invalid unit data skipped: {data.UnitId}");
                continue;
            }

            var actor = SpawnActor(
                data.Prefab,
                data.SpawnPosition,
                EBattleTeam.Ally,
                data.UnitId,
                finalStats,
                1);

            if (actor != null)
            {
                _activeAllies.Add(actor);
            }
        }
    }

    private void SpawnEnemies(BattleWaveData wave)
    {
        if (wave == null || wave.Enemies == null)
        {
            return;
        }

        foreach (var enemy in wave.Enemies)
        {
            if (enemy == null || !IsValidStats(enemy.Stats))
            {
                Debug.LogWarning("[WaveBattleController] Invalid enemy data skipped.");
                continue;
            }

            var actor = SpawnActor(
                enemy.Prefab,
                enemy.SpawnPosition,
                EBattleTeam.Enemy,
                enemy.EnemyId,
                enemy.Stats,
                enemy.DefenseDamage);

            if (actor != null)
            {
                _activeEnemies.Add(actor);
            }
        }
    }

    private BattleActor SpawnActor(
        GameObject prefab,
        Vector2 position,
        EBattleTeam team,
        string actorName,
        BattleActorStats stats,
        int defenseDamage)
    {
        GameObject actorObject;
        if (prefab != null)
        {
            actorObject = Instantiate(prefab, position, Quaternion.identity);
        }
        else
        {
            actorObject = GameObject.CreatePrimitive(PrimitiveType.Quad);
            var primitiveCollider = actorObject.GetComponent<Collider>();
            if (primitiveCollider != null)
            {
                Destroy(primitiveCollider);
            }

            actorObject.transform.position = position;
        }

        actorObject.name = $"{team}_{actorName}";
        actorObject.transform.localScale = new Vector3(0.8f, 0.8f, 1f);

        var actor = actorObject.GetComponent<BattleActor>();
        if (actor == null)
        {
            actor = actorObject.AddComponent<BattleActor>();
        }

        actor.Initialize(this, team, stats, defenseDamage, _defensePoint);
        return actor;
    }

    private static BattleActor FindClosest(
        Vector3 fromPosition,
        float maxDistance,
        List<BattleActor> candidates)
    {
        BattleActor best = null;
        var bestDistance = maxDistance;

        foreach (var candidate in candidates)
        {
            if (candidate == null || !candidate.IsAlive)
            {
                continue;
            }

            var distance = Vector2.Distance(fromPosition, candidate.transform.position);
            if (distance > bestDistance)
            {
                continue;
            }

            bestDistance = distance;
            best = candidate;
        }

        return best;
    }

    private bool TryBuildUnitStats(BattleUnitSpawnData data, out BattleActorStats finalStats)
    {
        finalStats = data.BaseStats;
        if (!IsValidStats(finalStats))
        {
            return false;
        }

        // TODO: 합성/장착 수치는 JSON 데이터(유닛/장비 테이블)에서 계산하도록 교체
        var attackMultiplier = 1f + (data.Modifier.MergeTier * data.Modifier.MergeAttackBonusPerTier);
        var hpMultiplier = 1f + (data.Modifier.MergeTier * data.Modifier.MergeHpBonusPerTier);

        finalStats.AttackDamage = (finalStats.AttackDamage * attackMultiplier) + data.Modifier.EquipmentAttackBonus;
        finalStats.MaxHp = (finalStats.MaxHp * hpMultiplier) + data.Modifier.EquipmentHpBonus;

        return IsValidStats(finalStats);
    }

    private static bool IsValidStats(BattleActorStats stats)
    {
        return stats.MaxHp > 0f
               && stats.AttackDamage >= 0f
               && stats.AttackRate > 0f
               && stats.AttackRange > 0f
               && stats.MoveSpeed >= 0f;
    }

    private void CleanupDestroyedActors()
    {
        _activeAllies.RemoveAll(actor => actor == null || !actor.IsAlive);
        _activeEnemies.RemoveAll(actor => actor == null || !actor.IsAlive);
    }

    private void ClearAllActors()
    {
        foreach (var ally in _activeAllies)
        {
            if (ally != null)
            {
                ally.ForceRemove();
            }
        }

        foreach (var enemy in _activeEnemies)
        {
            if (enemy != null)
            {
                enemy.ForceRemove();
            }
        }

        _activeAllies.Clear();
        _activeEnemies.Clear();
    }

    private void EnsureDefensePoint()
    {
        var pointObject = new GameObject("DefensePoint");
        pointObject.transform.position = defensePointPosition;
        _defensePoint = pointObject.transform;
    }

    private void EnsureStatusPanel()
    {
        _statusPanel = Object.FindFirstObjectByType<BattleStatusPanel>();
        if (_statusPanel != null)
        {
            return;
        }

        var panelObject = new GameObject("BattleStatusPanel");
        _statusPanel = panelObject.AddComponent<BattleStatusPanel>();
    }
}

public static class WaveBattleBootstrap
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void EnsureControllerInGameScene()
    {
        var activeScene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        var sceneName = activeScene.name;

        var isGameScene =
            sceneName == "Game" ||
            sceneName == "02. Game" ||
            sceneName.EndsWith("Game");

        if (!isGameScene)
        {
            return;
        }

        if (Object.FindFirstObjectByType<WaveBattleController>() != null)
        {
            return;
        }

        var controllerObject = new GameObject("WaveBattleController");
        controllerObject.AddComponent<WaveBattleController>();
    }
}