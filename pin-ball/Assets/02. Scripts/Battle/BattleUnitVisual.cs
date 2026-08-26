using System;
using System.Collections.Generic;

using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(SpriteRenderer))]
public sealed class BattleUnitVisual : MonoBehaviour
{
    [Serializable]
    private sealed class UnitAnimationProfile
    {
        [SerializeField] private string unitId;
        [SerializeField] private Sprite[] idleFrames;
        [SerializeField] private Sprite[] moveFrames;
        [SerializeField] private Sprite[] attackFrames;
        [SerializeField] private Sprite[] skillFrames;
        [SerializeField, Min(1f)] private float moveFramesPerSecond = 10f;
        [SerializeField, Min(1f)] private float attackFramesPerSecond = 10f;
        [SerializeField] private bool sourceFacesRight;

        public string UnitId => unitId;
        public Sprite[] IdleFrames => idleFrames ?? Array.Empty<Sprite>();
        public Sprite[] MoveFrames => moveFrames ?? Array.Empty<Sprite>();
        public Sprite[] AttackFrames => attackFrames ?? Array.Empty<Sprite>();
        public Sprite[] SkillFrames => skillFrames ?? Array.Empty<Sprite>();
        public bool SourceFacesRight => sourceFacesRight;

        public float GetFramesPerSecond(EBattleUnitState state)
        {
            return state == EBattleUnitState.Attacking
                ? attackFramesPerSecond
                : moveFramesPerSecond;
        }

#if UNITY_EDITOR
        public void Configure(
            string id,
            Sprite[] idle,
            Sprite[] moving,
            Sprite[] attacking,
            float movingFramesPerSecond,
            float attackingFramesPerSecond,
            bool spritesFaceRight)
        {
            unitId = id;
            idleFrames = idle;
            moveFrames = moving;
            attackFrames = attacking;
            moveFramesPerSecond = Mathf.Max(1f, movingFramesPerSecond);
            attackFramesPerSecond = Mathf.Max(1f, attackingFramesPerSecond);
            sourceFacesRight = spritesFaceRight;
        }
#endif
    }

    [SerializeField] private Sprite[] idleFrames;
    [SerializeField] private Sprite[] moveFrames;
    [SerializeField] private Sprite[] attackFrames;
    [SerializeField, Min(1f)] private float framesPerSecond = 10f;
    [SerializeField] private bool sourceFacesRight;
    [SerializeField] private UnitAnimationProfile[] unitAnimations =
        Array.Empty<UnitAnimationProfile>();

    private UnitBase _unit;
    private SpriteRenderer _spriteRenderer;
    private UnitAnimationProfile _activeProfile;
    private EBattleUnitState _previousState;
    private Vector3 _previousPosition;
    private float _stateStartedAt;
    private float _skillStartedAt;
    private float _skillAnimationUntil;

    private void Awake()
    {
        _unit = GetComponent<UnitBase>();
        _spriteRenderer = GetComponent<SpriteRenderer>();
        _previousPosition = transform.position;
        _previousState = _unit != null ? _unit.State : EBattleUnitState.Idle;
        _stateStartedAt = Time.time;
        _skillAnimationUntil = 0f;

        if (_spriteRenderer != null)
        {
            _spriteRenderer.sortingOrder = 10;
        }
    }

    public void SetUnitId(string unitId)
    {
        _activeProfile = null;

        if (!string.IsNullOrEmpty(unitId) && unitAnimations != null)
        {
            foreach (var profile in unitAnimations)
            {
                if (profile == null || profile.UnitId != unitId) continue;

                _activeProfile = profile;
                break;
            }
        }

        _previousState = _unit != null
            ? _unit.State
            : EBattleUnitState.Idle;
        _stateStartedAt = Time.time;

        var initialFrames = GetFrames(_previousState);
        if (_spriteRenderer != null && initialFrames.Length > 0)
        {
            _spriteRenderer.sprite = initialFrames[0];
        }
    }

    public void ResetFacing()
    {
        _previousPosition = transform.position;

        _unit ??= GetComponent<UnitBase>();
        _spriteRenderer ??= GetComponent<SpriteRenderer>();

        if (_unit == null || _spriteRenderer == null)
        {
            return;
        }

        UpdateFacing();
    }

    public void PlaySkillAnimation()
    {
        _spriteRenderer ??= GetComponent<SpriteRenderer>();
        if (_activeProfile == null || _activeProfile.SkillFrames.Length == 0)
        {
            return;
        }

        _skillStartedAt = Time.time;
        _skillAnimationUntil = Time.time +
            (_activeProfile.SkillFrames.Length / 10f);
        if (_spriteRenderer != null)
        {
            _spriteRenderer.sprite = _activeProfile.SkillFrames[0];
        }
    }

    private void LateUpdate()
    {
        if (_unit == null || _spriteRenderer == null)
        {
            return;
        }

        var state = _unit.State;
        if (state != _previousState)
        {
            _previousState = state;
            _stateStartedAt = Time.time;
        }

        var isPlayingSkill = Time.time < _skillAnimationUntil;
        var frames = isPlayingSkill
            ? _activeProfile.SkillFrames
            : GetFrames(state);
        if (frames.Length > 0)
        {
            var elapsed = Mathf.Max(0f, Time.time -
                (isPlayingSkill ? _skillStartedAt : _stateStartedAt));
            var animationSpeed = isPlayingSkill
                ? 10f
                : _activeProfile != null
                ? _activeProfile.GetFramesPerSecond(state)
                : framesPerSecond;
            var frameIndex = Mathf.FloorToInt(elapsed * animationSpeed) % frames.Length;
            _spriteRenderer.sprite = frames[frameIndex];
        }

        UpdateFacing();
    }

    private Sprite[] GetFrames(EBattleUnitState state)
    {
        var activeIdleFrames = _activeProfile != null
            ? _activeProfile.IdleFrames
            : idleFrames ?? Array.Empty<Sprite>();
        var activeMoveFrames = _activeProfile != null
            ? _activeProfile.MoveFrames
            : moveFrames ?? Array.Empty<Sprite>();
        var activeAttackFrames = _activeProfile != null
            ? _activeProfile.AttackFrames
            : attackFrames ?? Array.Empty<Sprite>();

        if (state == EBattleUnitState.Moving && activeMoveFrames.Length > 0)
        {
            return activeMoveFrames;
        }

        if (state == EBattleUnitState.Attacking && activeAttackFrames.Length > 0)
        {
            return activeAttackFrames;
        }

        return activeIdleFrames;
    }

    private void UpdateFacing()
    {
        var movement = transform.position - _previousPosition;
        _previousPosition = transform.position;

        var facesRight = _unit.Team == EBattleTeam.Enemy;
        if (Mathf.Abs(movement.x) > 0.0001f)
        {
            facesRight = movement.x > 0f;
        }

        var activeSourceFacesRight = _activeProfile != null
            ? _activeProfile.SourceFacesRight
            : sourceFacesRight;
        _spriteRenderer.flipX = activeSourceFacesRight != facesRight;
    }

#if UNITY_EDITOR
    public void Configure(
        Sprite[] idle,
        Sprite[] moving,
        Sprite[] attacking,
        float animationFramesPerSecond,
        bool spritesFaceRight)
    {
        idleFrames = idle;
        moveFrames = moving;
        attackFrames = attacking;
        framesPerSecond = Mathf.Max(1f, animationFramesPerSecond);
        sourceFacesRight = spritesFaceRight;
    }

    public void ConfigureUnitAnimation(
        string unitId,
        Sprite[] idle,
        Sprite[] moving,
        Sprite[] attacking,
        float movingFramesPerSecond,
        float attackingFramesPerSecond,
        bool spritesFaceRight)
    {
        var profiles = unitAnimations != null
            ? new List<UnitAnimationProfile>(unitAnimations)
            : new List<UnitAnimationProfile>();
        var profile = profiles.Find(candidate =>
            candidate != null && candidate.UnitId == unitId);

        if (profile == null)
        {
            profile = new UnitAnimationProfile();
            profiles.Add(profile);
        }

        profile.Configure(
            unitId,
            idle,
            moving,
            attacking,
            movingFramesPerSecond,
            attackingFramesPerSecond,
            spritesFaceRight);
        unitAnimations = profiles.ToArray();
    }
#endif
}
