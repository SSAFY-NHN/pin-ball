using System.Collections.Generic;

public enum EPinballReleaseType
{
    None,
    Permanent,
    Clone
}

public sealed class PinballBallPool
{
    private readonly List<Pinball> _permanentBalls = new();
    private readonly List<Pinball> _cloneBalls = new();
    private readonly Queue<Pinball> _availablePermanentBalls = new();
    private readonly Queue<Pinball> _availableCloneBalls = new();
    private readonly HashSet<Pinball> _activePermanentBalls = new();
    private readonly HashSet<Pinball> _activeCloneBalls = new();

    public bool HasActiveBalls =>
        _activePermanentBalls.Count > 0 || _activeCloneBalls.Count > 0;
    public IReadOnlyCollection<Pinball> ActiveBalls => _activePermanentBalls;

    public PinballBallPool(IEnumerable<Pinball> permanentBalls)
        : this(permanentBalls, null)
    {
    }

    public PinballBallPool(
        IEnumerable<Pinball> permanentBalls,
        IEnumerable<Pinball> cloneBalls)
    {
        AddUnique(permanentBalls, _permanentBalls);
        AddUnique(cloneBalls, _cloneBalls);
        ResetForNewRun();
    }

    public void ResetForNewRun()
    {
        _activePermanentBalls.Clear();
        _activeCloneBalls.Clear();
        _availablePermanentBalls.Clear();
        _availableCloneBalls.Clear();
        ResetCollection(_permanentBalls, _availablePermanentBalls);
        ResetCollection(_cloneBalls, _availableCloneBalls);
    }

    public bool TryAcquirePermanent(out Pinball ball)
    {
        return TryAcquire(_availablePermanentBalls, _activePermanentBalls, out ball);
    }

    public bool TryAcquireClone(out Pinball ball)
    {
        return TryAcquire(_availableCloneBalls, _activeCloneBalls, out ball);
    }

    public bool TryReactivatePermanent(Pinball ball)
    {
        if (ball == null ||
            !_permanentBalls.Contains(ball) ||
            _activePermanentBalls.Contains(ball) ||
            _availablePermanentBalls.Contains(ball))
        {
            return false;
        }

        _activePermanentBalls.Add(ball);
        return true;
    }

    public EPinballReleaseType Release(Pinball ball)
    {
        if (ball == null) return EPinballReleaseType.None;

        if (_activePermanentBalls.Remove(ball))
        {
            ball.Deactivate();
            return EPinballReleaseType.Permanent;
        }

        if (_activeCloneBalls.Remove(ball))
        {
            ball.Deactivate();
            _availableCloneBalls.Enqueue(ball);
            return EPinballReleaseType.Clone;
        }

        return EPinballReleaseType.None;
    }

    private static bool TryAcquire(
        Queue<Pinball> available,
        HashSet<Pinball> active,
        out Pinball ball)
    {
        ball = null;
        if (available.Count <= 0) return false;
        ball = available.Dequeue();
        active.Add(ball);
        return true;
    }

    private static void AddUnique(
        IEnumerable<Pinball> source,
        List<Pinball> destination)
    {
        if (source == null) return;
        foreach (Pinball ball in source)
        {
            if (ball != null && !destination.Contains(ball)) destination.Add(ball);
        }
    }

    private static void ResetCollection(
        IEnumerable<Pinball> balls,
        Queue<Pinball> available)
    {
        foreach (Pinball ball in balls)
        {
            if (ball == null) continue;
            ball.Deactivate();
            available.Enqueue(ball);
        }
    }
}
