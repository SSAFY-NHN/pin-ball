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
    private AllyDragLineageHighlight _lineageHighlight;
    private Camera _dragCamera;
    private Vector3 _dragStartPosition;
    private bool _isDragging;
    private bool _isMergeReserved;

    public void SetData(string unitId, int level, AllySkillData skill, AllyCommonData common, UnitManager unitManager = null, UnitSkillRegistry registry = null)
    {
        UnitId = unitId;
        Level = level;
        _skill = skill;
        _unitManager = unitManager;
        _skillController = new AllySkillController(registry ?? UnitSkillRegistry.CreateDefault());
        _skillController.Initialize(common, skill, MaxMana);
        _attackEffectPlayer ??= GetComponent<UnitAttackEffectPlayer>();
        _lineageHighlight ??= GetComponent<AllyDragLineageHighlight>();
        if (_lineageHighlight == null)
        {
            _lineageHighlight = gameObject.AddComponent<AllyDragLineageHighlight>();
        }
        _dragCamera = Camera.main;
        _isDragging = false;
        _isMergeReserved = false;
        GetComponent<BattleUnitVisual>()?.SetUnitId(unitId);
        ResetMana();
    }

    public void ResetMana() => _skillController?.Reset(MaxMana);

    public void SetMergeReserved(bool reserved)
    {
        if (reserved) _unitManager?.EndAllyDragHighlight();
        _isMergeReserved = reserved;
        _isDragging = false;
        gameObject.SetActive(!reserved);
    }

    protected override void Tick()
    {
        if (!TryKeepOrAcquireTarget()) { _state = EBattleUnitState.Idle; ClearTarget(); return; }
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
    }

    protected override void OnBasicAttackHit(UnitBase target)
    {
        _attackEffectPlayer?.Play(UnitId, target);
        _skillController?.GainFromBasicAttack(MaxMana);
    }
    protected override void OnDamaged() => _skillController?.GainFromDamage(Time.time, MaxMana);

    private UnitSkillContext CreateSkillContext() => new(this, _currentTarget, _unitManager.TargetFinder);

    private void OnMouseDown()
    {
        if (!Input.GetMouseButton(0) || _isMergeReserved || _unitManager == null || !_unitManager.CanDragAlly(this)) return;
        _dragStartPosition = transform.position;
        _isDragging = true;
        _unitManager.BeginAllyDragHighlight(this);
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
        _unitManager.EndAllyDragHighlight();
        if ((EventSystem.current != null && EventSystem.current.IsPointerOverGameObject()) || !_unitManager.IsValidAllyPlacement(this, transform.position))
        { transform.position = _dragStartPosition; return; }
        AllyUnit target = null;
        foreach (var candidate in Physics2D.OverlapPointAll(transform.position))
        {
            var ally = candidate.GetComponentInParent<AllyUnit>();
            if (ally != null && ally != this) { target = ally; break; }
        }
        if (target != null) { _unitManager.TryMergeAllies(this, target, _dragStartPosition); return; }
        _unitManager.SaveAllyPreparationPosition(this);
    }

    private float GetPlacementPadding()
    {
        var collider = GetComponentInChildren<Collider2D>();
        return collider == null ? 0f : Mathf.Max(collider.bounds.extents.x, collider.bounds.extents.y);
    }

    public void SetLineageHighlighted(bool highlighted)
    {
        _lineageHighlight?.SetHighlighted(highlighted);
    }

    private void OnDisable()
    {
        if (_isDragging) _unitManager?.EndAllyDragHighlight();
        _isDragging = false;
        _lineageHighlight?.SetHighlighted(false);
    }
}
