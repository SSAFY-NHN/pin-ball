using UnityEngine;

public class EvolutionPanel : UIBase
{
    [SerializeField] private EvolutionChoiceView firstChoice;
    [SerializeField] private EvolutionChoiceView secondChoice;

    private UnitManager _unitManager;

    public override void Initialize(UIManager manager)
    {
        base.Initialize(manager);

        if (firstChoice == null || secondChoice == null)
        {
            Debug.LogError("[EvolutionPanel] 선택 카드가 설정되지 않았습니다.");
            enabled = false;
            return;
        }

        _unitManager = App.Get<UnitManager>();
        _unitManager.OnEvolutionRequested += OnEvolutionRequested;
        gameObject.SetActive(false);
    }

    private void OnEvolutionRequested(
        AllyUnitData first,
        AllyUnitData second)
    {
        bool firstReady = firstChoice.Bind(first, OnChoiceSelected);
        bool secondReady = secondChoice.Bind(second, OnChoiceSelected);
        if (!firstReady || !secondReady) return;

        gameObject.SetActive(true);
        transform.SetAsLastSibling();
    }

    private void OnChoiceSelected(string unitId)
    {
        if (_unitManager != null &&
            _unitManager.ChooseEvolution(unitId))
        {
            gameObject.SetActive(false);
        }
    }

    private void OnDestroy()
    {
        if (_unitManager != null)
        {
            _unitManager.OnEvolutionRequested -= OnEvolutionRequested;
        }
    }
}
