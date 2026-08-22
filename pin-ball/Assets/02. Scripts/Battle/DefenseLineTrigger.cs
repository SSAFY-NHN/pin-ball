using UnityEngine;

public class DefenseLineTrigger : MonoBehaviour
{
    [SerializeField] private EBattleTeam defenseTeam;
    [SerializeField] private SpriteRenderer bodyRenderer;
    [SerializeField] private Transform healthFill;

    public EBattleTeam DefenseTeam => defenseTeam;
    public float DisplayedHealthRatio { get; private set; } = 1f;

    private Vector3 healthFillFullScale;
    private bool healthFillScaleInitialized;
    private Color bodyColor;
    private bool bodyColorInitialized;

    public void SetHealth(int currentHp, int maximumHp)
    {
        DisplayedHealthRatio = maximumHp <= 0
            ? 0f
            : Mathf.Clamp01((float)currentHp / maximumHp);
        if (healthFill == null) return;
        if (!healthFillScaleInitialized)
        {
            healthFillFullScale = healthFill.localScale;
            healthFillScaleInitialized = true;
        }

        healthFill.localScale = new Vector3(
            healthFillFullScale.x * DisplayedHealthRatio,
            healthFillFullScale.y,
            healthFillFullScale.z);
    }

    public void PlayHit()
    {
        if (bodyRenderer == null) return;
        if (!bodyColorInitialized)
        {
            bodyColor = bodyRenderer.color;
            bodyColorInitialized = true;
        }

        bodyRenderer.color = Color.white;
        CancelInvoke(nameof(RestoreBodyColor));
        Invoke(nameof(RestoreBodyColor), 0.08f);
    }

    private void RestoreBodyColor()
    {
        if (bodyRenderer != null) bodyRenderer.color = bodyColor;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        UnitBase unit = other.GetComponentInParent<UnitBase>();
        if (unit == null || unit.Team == defenseTeam) return;

        unit.ReachDefenseLine(defenseTeam);
    }
}
