using UnityEngine;
using UnityEngine.EventSystems;

public class AllyUnit : UnitBase
{
    public override EBattleTeam Team => EBattleTeam.Ally;
    protected override Color IdleColor => Color.white;
    public string UnitId { get; private set; }
    public int Level { get; private set; }
    public float CurrentMana => _skillController?.CurrentMana ?? 0f;
    public AllySkillData Skill => _skill;
    protected override string BasicAttackSoundName =>
        SoundName.GetAttack(UnitId);

    private UnitManager _unitManager;
    private AllySkillData _skill;
    private AllySkillController _skillController;
    private UnitAttackEffectPlayer _attackEffectPlayer;
    private readonly AllyBasicAttackController _basicAttackController = new();
    private Camera _dragCamera;
    private Vector3 _dragStartPosition;
    private bool _isDragging;

    public void SetData(string unitId, int level, AllySkillData skill, AllyCommonData common, UnitManager unitManager = null, UnitSkillRegistry registry = null)
    {
        UnitId = unitId;
        Level = level;
        _skill = skill;
        _unitManager = unitManager;
        _skillController = new AllySkillController(registry ?? UnitSkillRegistry.CreateDefault());
        _skillController.Initialize(common, skill, MaxMana);
        _attackEffectPlayer ??= GetComponent<UnitAttackEffectPlayer>();
        _dragCamera = Camera.main;
        _isDragging = false;
        GetComponent<BattleUnitVisual>()?.SetUnitId(unitId);
        ResetMana();
    }

    public void ResetMana() => _skillController?.Reset(MaxMana);

    protected override void Tick()
    {
        if (TryKeepOrAcquireTarget())
        {
            LeaveDefenseLine();
            if (_skillController != null && _skillController.CanCast(MaxMana))
            {
                _state = EBattleUnitState.Attacking;
                if (_skillController.TryCast(
                        CreateSkillContext(),
                        MaxMana,
                        Debug.LogWarning))
                {
                    SoundManager.PlaySFXIfAvailable(BasicAttackSoundName);
                    PlaySkillFeedback(_skill?.id, _currentTarget);
                }
                return;
            }

            MoveOrAttackTarget();
            return;
        }

        if (TryMoveOrAttackDefenseLine(_unitManager)) return;
        _state = EBattleUnitState.Idle;
        ClearTarget();
    }

    protected override void OnBasicAttackHit(UnitBase target)
    {
        _attackEffectPlayer?.Play(UnitId, target);
        _basicAttackController.ApplySecondaryHits(
            UnitId,
            this,
            target,
            GetBasicAttackDamage(target),
            _unitManager?.TargetFinder,
            secondaryTarget => _attackEffectPlayer?.Play(UnitId, secondaryTarget));
        _skillController?.GainFromBasicAttack(MaxMana);
    }

    protected override float GetBasicAttackArmorIgnoreRatio(UnitBase target)
    {
        return _basicAttackController.GetArmorIgnoreRatio(UnitId, target);
    }
    protected override void OnDamaged() => _skillController?.GainFromDamage(Time.time, MaxMana);

    private UnitSkillContext CreateSkillContext() => new(this, _currentTarget, _unitManager.TargetFinder);

    private void OnMouseDown()
    {
        if (!Input.GetMouseButton(0) || _unitManager == null || !_unitManager.CanDragAlly(this)) return;
        _dragStartPosition = transform.position;
        _isDragging = true;
    }

    private void OnMouseOver()
    {
        if (!_isDragging && Input.GetMouseButtonDown(1)) _unitManager?.RequestAllyDetail(this);
    }

    private void OnMouseDrag()
    {
        if (!_isDragging) return;
        if (_dragCamera == null) _dragCamera = Camera.main;
        if (_dragCamera == null) return;
        var position = _dragCamera.ScreenToWorldPoint(Input.mousePosition);
        position.z = _dragStartPosition.z;
        if (_unitManager.BattleArea != null) position = _unitManager.BattleArea.ClampAllyPlacement(position, GetPlacementPadding());
        transform.position = position;
    }

    private void OnMouseUp()
    {
        if (!_isDragging) return;
        _isDragging = false;
        if ((EventSystem.current != null && EventSystem.current.IsPointerOverGameObject()) || !_unitManager.IsValidAllyPlacement(this, transform.position))
        { transform.position = _dragStartPosition; return; }
        _unitManager.SaveAllyPreparationPosition(this);
    }

    private float GetPlacementPadding()
    {
        var collider = GetComponentInChildren<Collider2D>();
        return collider == null ? 0f : Mathf.Max(collider.bounds.extents.x, collider.bounds.extents.y);
    }

    private void OnDisable()
    {
        _isDragging = false;
    }
}
