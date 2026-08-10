using System.Collections;

using UnityEngine;

[DisallowMultipleComponent]
public sealed class BattleCombatFeedback : MonoBehaviour
{
    private const int PoolSize = 5;
    private static readonly Color AllyAttackColor = new(0.15f, 0.85f, 1f, 1f);
    private static readonly Color EnemyAttackColor = new(1f, 0.2f, 0.16f, 1f);
    private static readonly Color ArrowColor = new(1f, 0.78f, 0.2f, 1f);
    private static readonly Color MagicColor = new(0.65f, 0.2f, 1f, 1f);

    private UnitBase _unit;
    private SpriteRenderer _renderer;
    private Material _effectMaterial;
    private LineRenderer[] _attackEffects;
    private BattleDamagePopup[] _damagePopups;
    private int _nextAttackEffect;
    private int _nextDamagePopup;
    private float _flashUntil;

    public void Initialize(UnitBase unit, SpriteRenderer unitRenderer)
    {
        if (_unit != null) return;

        _unit = unit;
        _renderer = unitRenderer;
        var shader = Resources.Load<Shader>("ArcaneVFX/ArcaneAdditive");
        if (shader == null) return;

        _effectMaterial = new Material(shader)
        {
            name = "Battle Feedback (Runtime)",
            hideFlags = HideFlags.HideAndDontSave
        };
        _effectMaterial.SetFloat("_Intensity", 2f);
        _effectMaterial.SetFloat("_GlowSpread", 1.15f);
        _effectMaterial.mainTexture = Texture2D.whiteTexture;
        CreatePools();
    }

    public void PlayBasicAttack(UnitBase target)
    {
        if (target == null || _attackEffects == null) return;

        var effect = _attackEffects[_nextAttackEffect];
        _nextAttackEffect = (_nextAttackEffect + 1) % _attackEffects.Length;
        StartCoroutine(AnimateAttack(
            effect,
            transform.position,
            target.transform.position,
            ResolveStyle(),
            _unit.Team));
    }

    public void PlayDamage(float damage)
    {
        if (damage <= 0f) return;

        _flashUntil = Time.unscaledTime + 0.11f;
        if (_damagePopups == null) return;

        var popup = _damagePopups[_nextDamagePopup];
        _nextDamagePopup = (_nextDamagePopup + 1) % _damagePopups.Length;
        popup.Play(transform.position, damage, _unit.Team);
    }

    private void LateUpdate()
    {
        if (_renderer == null || Time.unscaledTime >= _flashUntil) return;

        float remaining = Mathf.Clamp01((_flashUntil - Time.unscaledTime) / 0.11f);
        _renderer.color = remaining > 0.55f
            ? Color.white * 1.8f
            : new Color(1f, 0.25f, 0.25f, 1f);
    }

    private void CreatePools()
    {
        _attackEffects = new LineRenderer[PoolSize];
        _damagePopups = new BattleDamagePopup[PoolSize];
        int sortingOrder = _renderer != null ? _renderer.sortingOrder + 4 : 14;

        for (var index = 0; index < PoolSize; index++)
        {
            var attackObject = new GameObject($"Attack Effect {index + 1}");
            var line = attackObject.AddComponent<LineRenderer>();
            line.sharedMaterial = _effectMaterial;
            line.useWorldSpace = true;
            line.positionCount = 2;
            line.numCapVertices = 2;
            line.sortingOrder = sortingOrder;
            line.enabled = false;
            _attackEffects[index] = line;

            var popupObject = new GameObject($"Damage Popup {index + 1}");
            var popup = popupObject.AddComponent<BattleDamagePopup>();
            popup.Initialize(sortingOrder + 1);
            _damagePopups[index] = popup;
        }
    }

    private IEnumerator AnimateAttack(
        LineRenderer line,
        Vector3 start,
        Vector3 end,
        EBattleAttackStyle style,
        EBattleTeam team)
    {
        float duration = style == EBattleAttackStyle.Melee ? 0.14f : 0.24f;
        float width = style == EBattleAttackStyle.Melee ? 0.18f : 0.1f;
        Color color = style switch
        {
            EBattleAttackStyle.Arrow => ArrowColor,
            EBattleAttackStyle.Magic => MagicColor,
            _ => team == EBattleTeam.Ally ? AllyAttackColor : EnemyAttackColor
        };
        float startedAt = Time.unscaledTime;
        line.startWidth = width;
        line.endWidth = width * 0.35f;
        line.startColor = color;
        line.endColor = color;
        line.enabled = true;

        while (Time.unscaledTime - startedAt < duration)
        {
            float progress = Mathf.Clamp01((Time.unscaledTime - startedAt) / duration);
            if (style == EBattleAttackStyle.Melee)
            {
                line.SetPosition(0, Vector3.Lerp(start, end, progress * 0.25f));
                line.SetPosition(1, Vector3.Lerp(start, end, Mathf.Min(1f, progress + 0.4f)));
            }
            else
            {
                float head = Mathf.SmoothStep(0f, 1f, progress);
                float tail = Mathf.Max(0f, head - 0.18f);
                line.SetPosition(0, Vector3.Lerp(start, end, tail));
                line.SetPosition(1, Vector3.Lerp(start, end, head));
            }

            float alpha = 1f - Mathf.Clamp01((progress - 0.65f) / 0.35f);
            line.startColor = new Color(color.r, color.g, color.b, alpha);
            line.endColor = new Color(color.r, color.g, color.b, alpha);
            yield return null;
        }

        line.enabled = false;
    }

    private EBattleAttackStyle ResolveStyle()
    {
        string unitId = _unit switch
        {
            AllyUnit ally => ally.UnitId,
            EnemyUnit enemy => enemy.UnitId,
            _ => string.Empty
        };
        return BattleFeedbackMath.ResolveAttackStyle(_unit.AttackRange, unitId);
    }

    private void OnDisable()
    {
        StopAllCoroutines();
        _flashUntil = 0f;
        if (_attackEffects != null)
        {
            foreach (var effect in _attackEffects)
            {
                if (effect != null) effect.enabled = false;
            }
        }
        if (_damagePopups != null)
        {
            foreach (var popup in _damagePopups)
            {
                if (popup != null) popup.gameObject.SetActive(false);
            }
        }
    }

    private void OnDestroy()
    {
        if (_attackEffects != null)
        {
            foreach (var effect in _attackEffects)
            {
                if (effect != null) Destroy(effect.gameObject);
            }
        }
        if (_damagePopups != null)
        {
            foreach (var popup in _damagePopups)
            {
                if (popup != null) Destroy(popup.gameObject);
            }
        }
        if (_effectMaterial != null) Destroy(_effectMaterial);
    }
}
