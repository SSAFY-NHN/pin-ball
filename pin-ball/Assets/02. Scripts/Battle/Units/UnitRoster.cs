using System.Collections.Generic;

public sealed class UnitRoster
{
    private readonly List<AllyUnit> _ownedAllies = new();
    private readonly List<UnitBase> _activeAllies = new();
    private readonly List<UnitBase> _activeEnemies = new();

    public IReadOnlyList<AllyUnit> OwnedAllies => _ownedAllies;
    public IReadOnlyList<UnitBase> ActiveAllies => _activeAllies;
    public IReadOnlyList<UnitBase> ActiveEnemies => _activeEnemies;
    public int OwnedAllyCount => _ownedAllies.Count;
    public int ActiveAllyCount => _activeAllies.Count;
    public int ActiveEnemyCount => _activeEnemies.Count;

    public bool AddOwnedAlly(AllyUnit ally)
    {
        if (ally == null) return false;

        bool changed = false;
        if (!_ownedAllies.Contains(ally))
        {
            _ownedAllies.Add(ally);
            changed = true;
        }

        if (!_activeAllies.Contains(ally))
        {
            _activeAllies.Add(ally);
            changed = true;
        }

        return changed;
    }

    public bool AddActiveAlly(AllyUnit ally)
    {
        if (ally == null || _activeAllies.Contains(ally)) return false;

        _activeAllies.Add(ally);
        return true;
    }

    public bool RemoveActiveAlly(AllyUnit ally)
    {
        return ally != null && _activeAllies.Remove(ally);
    }

    public bool AddEnemy(UnitBase enemy)
    {
        if (enemy == null || _activeEnemies.Contains(enemy)) return false;

        _activeEnemies.Add(enemy);
        return true;
    }

    public bool NotifyUnitDied(UnitBase unit)
    {
        if (unit == null) return false;

        return unit.Team == EBattleTeam.Ally
            ? _activeAllies.Remove(unit)
            : _activeEnemies.Remove(unit);
    }

    public bool RemoveUnit(UnitBase unit)
    {
        if (unit == null) return false;

        bool changed = _activeAllies.Remove(unit);
        changed = _activeEnemies.Remove(unit) || changed;
        if (unit is AllyUnit ally)
        {
            changed = _ownedAllies.Remove(ally) || changed;
        }

        return changed;
    }

    public void CleanupDestroyedUnits()
    {
        _activeAllies.RemoveAll(unit => unit == null || !unit.IsAlive);
        _activeEnemies.RemoveAll(unit => unit == null || !unit.IsAlive);
    }

    public UnitBase[] DrainEnemies()
    {
        var snapshot = _activeEnemies.ToArray();
        _activeEnemies.Clear();
        return snapshot;
    }

    public void ClearActiveAllies()
    {
        _activeAllies.Clear();
    }
}
