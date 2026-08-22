using System.Text;
using TMPro;
using UnityEngine;

public sealed class PrototypeMetricsDisplayController : MonoBehaviour
{
    [SerializeField] private PrototypeMetricsController metricsController;
    [SerializeField] private TextMeshProUGUI metricsText;
    [SerializeField] private bool showMetrics = true;
    [SerializeField, Min(0.5f)] private float refreshInterval = 0.75f;

    private readonly StringBuilder builder = new(320);
    private float nextRefreshAt;

    private void Start()
    {
        ApplyVisibility();
        Refresh();
    }

    private void Update()
    {
        if (!showMetrics || Time.unscaledTime < nextRefreshAt) return;
        Refresh();
    }

    private void ApplyVisibility()
    {
        if (metricsText != null) metricsText.gameObject.SetActive(showMetrics);
    }

    private void Refresh()
    {
        nextRefreshAt = Time.unscaledTime + Mathf.Max(0.5f, refreshInterval);
        if (metricsController == null || metricsText == null) return;

        float sinceLastJackpot = metricsController.LastJackpotAt < 0f
            ? -1f
            : metricsController.RunElapsed - metricsController.LastJackpotAt;
        builder.Clear();
        builder.Append("PROTOTYPE METRICS | RUN ")
            .Append(PrototypeMetricsController.FormatTime(metricsController.RunElapsed))
            .Append('\n');
        builder.Append("BALL ").Append(metricsController.BallsPerMinute.ToString("0.0"))
            .Append("/min | HIT ").Append(metricsController.AverageHitsPerBall.ToString("0.0"))
            .Append("/ball | CLONE ").Append(metricsController.ActiveCloneCount)
            .Append('\n');
        builder.Append("GOLD ").Append(metricsController.NormalGoldPerMinute.ToString("0"))
            .Append("/min | JACKPOT ").Append(metricsController.JackpotGoldPerMinute.ToString("0"))
            .Append("/min\n");
        builder.Append("JACKPOT ").Append(metricsController.JackpotCount)
            .Append(" | AVG ").Append(metricsController.AverageJackpotReward.ToString("0"))
            .Append("G | LAST ").Append(PrototypeMetricsController.FormatTime(sinceLastJackpot))
            .Append(" | GAP ").Append(PrototypeMetricsController.FormatTime(metricsController.AverageJackpotInterval))
            .Append('\n');
        builder.Append("WAVE ").Append(metricsController.CurrentWave);
        if (metricsController.IsBossStage) builder.Append(" BOSS");
        builder.Append(" | TIME ").Append(metricsController.CurrentWaveElapsed.ToString("0.0"))
            .Append("s | RETRY ").Append(metricsController.CurrentWaveRetryCount)
            .Append(" | DMG ").Append(metricsController.LastDefenseLineDamage)
            .Append('\n');
        builder.Append("BOSS REACHED ")
            .Append(PrototypeMetricsController.FormatTime(metricsController.FirstStageTenReachedAt))
            .Append(" | CLEARED ")
            .Append(PrototypeMetricsController.FormatTime(metricsController.FirstBossDefeatedAt))
            .Append('\n');
        builder.Append("BUY ").Append(metricsController.RecentPurchase);
        metricsText.text = builder.ToString();
    }
}
