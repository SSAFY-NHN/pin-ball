using System.Collections.Generic;

public sealed class PinballBallPool
{
    private readonly Queue<Pinball> _availableBalls = new();
    private readonly HashSet<Pinball> _activeBalls = new();

    public Pinball LoadedBall { get; private set; }
    public bool HasAvailableBall =>
        LoadedBall != null || _availableBalls.Count > 0;
    public bool HasActiveBalls => _activeBalls.Count > 0;
    public IReadOnlyCollection<Pinball> ActiveBalls => _activeBalls;

    public PinballBallPool(IEnumerable<Pinball> pooledBalls)
    {
        foreach (var ball in pooledBalls)
        {
            if (ball != null)
            {
                _availableBalls.Enqueue(ball);
            }
        }
    }

    public bool TryLoadNext(out Pinball ball)
    {
        ball = null;
        if (LoadedBall != null || _availableBalls.Count <= 0)
        {
            return false;
        }

        LoadedBall = _availableBalls.Dequeue();
        ball = LoadedBall;
        return true;
    }

    public bool TryLaunchLoaded(out Pinball ball)
    {
        ball = LoadedBall;
        if (ball == null)
        {
            return false;
        }

        LoadedBall = null;
        _activeBalls.Add(ball);
        return true;
    }

    public bool TryAcquireActive(out Pinball ball)
    {
        ball = null;
        if (_availableBalls.Count <= 0)
        {
            return false;
        }

        ball = _availableBalls.Dequeue();
        _activeBalls.Add(ball);
        return true;
    }

    public bool Release(Pinball ball, out bool hasNoActiveBalls)
    {
        hasNoActiveBalls = false;
        if (ball == null || !_activeBalls.Remove(ball))
        {
            return false;
        }

        ball.Deactivate();
        _availableBalls.Enqueue(ball);
        hasNoActiveBalls = _activeBalls.Count <= 0;
        return true;
    }
}
