using System.Collections.Generic;

using UnityEngine;

public sealed class PinballGoalController
{
    private readonly List<PinballGoal> _goals = new();
    private readonly Dictionary<PinballGoal, BattleUnitSpawnData>
        _initialUnitData = new();

    private int _selectedGoalIndex;
    private int _pendingSwapGoalIndex = -1;
    private int _swapCount;
    private int _remainingSwapCount;
    private float _focusedPocketBonus;
    private float _otherPocketPenalty;

    public PinballGoal SelectedGoal
    {
        get
        {
            if (_goals.Count <= 0)
            {
                return null;
            }

            int goalIndex = Mathf.Clamp(
                _selectedGoalIndex,
                0,
                _goals.Count - 1);
            return _goals[goalIndex];
        }
    }

    public void Register(PinballGoal goal)
    {
        if (goal == null || _goals.Contains(goal))
        {
            return;
        }

        _goals.Add(goal);
        _initialUnitData[goal] = CopyUnitData(goal.UnitData);
        _goals.Sort((left, right) =>
            left.transform.position.x.CompareTo(
                right.transform.position.x));
        RefreshGoalWidths();
    }

    public void Unregister(PinballGoal goal)
    {
        _goals.Remove(goal);
        _initialUnitData.Remove(goal);
        _selectedGoalIndex = Mathf.Clamp(
            _selectedGoalIndex,
            0,
            Mathf.Max(0, _goals.Count - 1));
    }

    public bool Select(PinballGoal goal)
    {
        int goalIndex = _goals.IndexOf(goal);
        if (goalIndex < 0)
        {
            return false;
        }

        _selectedGoalIndex = goalIndex;
        RefreshGoalWidths();
        return true;
    }

    public void SelectSwap(PinballGoal goal, bool hasActiveBalls)
    {
        int goalIndex = _goals.IndexOf(goal);
        if (goalIndex < 0 ||
            _remainingSwapCount <= 0 ||
            hasActiveBalls)
        {
            return;
        }

        if (_pendingSwapGoalIndex < 0)
        {
            _pendingSwapGoalIndex = goalIndex;
            return;
        }

        if (_pendingSwapGoalIndex != goalIndex)
        {
            var firstGoal = _goals[_pendingSwapGoalIndex];
            var firstData = firstGoal.UnitData;
            firstGoal.SetUnitData(goal.UnitData);
            goal.SetUnitData(firstData);
            _remainingSwapCount--;
        }

        _pendingSwapGoalIndex = -1;
    }

    public void SetFocusedPocket(float bonus, float penalty)
    {
        _focusedPocketBonus = bonus;
        _otherPocketPenalty = penalty;
        RefreshGoalWidths();
    }

    public void SetSwapCount(int swapCount)
    {
        _swapCount = swapCount;
        _remainingSwapCount = swapCount;
    }

    public void ResetForPreparation()
    {
        _remainingSwapCount = _swapCount;
        _pendingSwapGoalIndex = -1;
        RefreshGoalWidths();
    }

    public void ResetForNewRun()
    {
        _selectedGoalIndex = 0;
        _pendingSwapGoalIndex = -1;
        _swapCount = 0;
        _remainingSwapCount = 0;
        _focusedPocketBonus = 0f;
        _otherPocketPenalty = 0f;

        foreach (var goal in _goals)
        {
            if (goal != null &&
                _initialUnitData.TryGetValue(goal, out var initialData))
            {
                goal.SetUnitData(CopyUnitData(initialData));
            }
        }

        RefreshGoalWidths();
    }

    private static BattleUnitSpawnData CopyUnitData(
        BattleUnitSpawnData source)
    {
        return source == null
            ? new BattleUnitSpawnData()
            : new BattleUnitSpawnData
            {
                UnitId = source.UnitId,
                Level = source.Level,
                Modifier = source.Modifier
            };
    }

    private void RefreshGoalWidths()
    {
        for (var i = 0; i < _goals.Count; i++)
        {
            var multiplier = 1f;
            if (_focusedPocketBonus > 0f)
            {
                multiplier += i == _selectedGoalIndex
                    ? _focusedPocketBonus
                    : -_otherPocketPenalty;
            }

            _goals[i].SetWidthMultiplier(
                Mathf.Max(0.1f, multiplier),
                GetMaximumGoalWidth(i));
        }
    }

    private float GetMaximumGoalWidth(int goalIndex)
    {
        var goal = _goals[goalIndex];
        var nearestDistance = float.MaxValue;

        if (goalIndex > 0)
        {
            nearestDistance = Mathf.Min(
                nearestDistance,
                Mathf.Abs(
                    goal.transform.position.x -
                    _goals[goalIndex - 1].transform.position.x));
        }

        if (goalIndex + 1 < _goals.Count)
        {
            nearestDistance = Mathf.Min(
                nearestDistance,
                Mathf.Abs(
                    goal.transform.position.x -
                    _goals[goalIndex + 1].transform.position.x));
        }

        return nearestDistance;
    }
}
