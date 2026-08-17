using System.Collections.Generic;

public sealed class PinballAutoCycleController
{
    private readonly Dictionary<Pinball, float> _readyTimes = new();

    public void Schedule(Pinball ball, float readyAt)
    {
        if (ball == null || _readyTimes.ContainsKey(ball)) return;
        _readyTimes.Add(ball, readyAt);
    }

    public bool TryTakeReady(float currentTime, out Pinball ball)
    {
        ball = null;
        foreach (var reservation in _readyTimes)
        {
            if (currentTime < reservation.Value) continue;
            ball = reservation.Key;
            break;
        }

        return ball != null && _readyTimes.Remove(ball);
    }

    public void Reset()
    {
        _readyTimes.Clear();
    }
}
