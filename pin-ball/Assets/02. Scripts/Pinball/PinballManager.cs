using System;
using System.Collections.Generic;

using UnityEngine;

public class PinballManager : AppService
{
    public event Action<EPinballState> OnStateChanged;

    [Header("Economy")]
    [SerializeField, Min(0)] private int launchCost = 2;
    
    [SerializeField] private List<Pinball> pooledBalls = new();

    private readonly Queue<Pinball> _availableBalls = new();
    private readonly HashSet<Pinball> _activeBalls = new();

    private BattleManager _battleManager;
    private UnitManager _unitManager;

    private void Start()
    {
        _battleManager = App.Get<BattleManager>();
        _unitManager = App.Get<UnitManager>();

        PrepareBallPool();
    }
    
    private void PrepareBallPool()
    {
        _availableBalls.Clear();
        _activeBalls.Clear();

        foreach (var ball in pooledBalls)
        {
            if (ball == null) continue;
            
            _availableBalls.Enqueue(ball);
        }
    }

    public void LaunchBall(Vector2 position)
    {
        if (!_battleManager.TrySpendGold(launchCost)) return;

        if (_availableBalls.Count <= 0) return;

        var ball = _availableBalls.Dequeue();
        ball.Activate(position);
        _activeBalls.Add(ball);
        OnStateChanged?.Invoke(EPinballState.Launched);
    }

    public void OnGoalBall(Pinball ball)
    {
        _unitManager.SpawnAlly(ball.AllyData);

        ReleaseBall(ball);
    }

    public void ReleaseBall(Pinball ball)
    {
        if (ball == null || !_activeBalls.Remove(ball)) return;
        
        ball.Deactivate();
        _availableBalls.Enqueue(ball);
        
        if (_activeBalls.Count <= 0)
        {
            OnStateChanged?.Invoke(EPinballState.Idle);
        }
    }
}
