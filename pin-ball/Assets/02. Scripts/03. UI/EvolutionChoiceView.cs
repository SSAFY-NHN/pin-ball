using System;

using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class EvolutionChoiceView : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI nameText;
    [SerializeField] private TextMeshProUGUI roleText;
    [SerializeField] private TextMeshProUGUI statsText;
    [SerializeField] private TextMeshProUGUI skillText;
    [SerializeField] private Button selectButton;

    private string _unitId;
    private Action<string> _onSelected;

    private void Awake()
    {
        if (selectButton != null)
        {
            selectButton.onClick.AddListener(OnSelectButtonClicked);
        }
    }

    public bool Bind(
        AllyUnitData data,
        Action<string> onSelected)
    {
        if (data == null ||
            nameText == null ||
            roleText == null ||
            statsText == null ||
            skillText == null ||
            selectButton == null)
        {
            Debug.LogError("[EvolutionChoiceView] UI 참조가 설정되지 않았습니다.");
            return false;
        }

        _unitId = data.id;
        _onSelected = onSelected;
        nameText.text = data.name;
        roleText.text = data.role;
        statsText.text =
            $"HP {data.health}  ATK {data.attack}  DEF {data.defense}\n" +
            $"공속 {data.attackSpeed:0.##}  사거리 {data.attackRange:0.##}";
        skillText.text = data.skill == null
            ? "스킬 없음"
            : $"{data.skill.name}\n{data.skill.description}";
        selectButton.interactable = true;
        return true;
    }

    private void OnSelectButtonClicked()
    {
        if (string.IsNullOrEmpty(_unitId)) return;
        _onSelected?.Invoke(_unitId);
    }

    private void OnDestroy()
    {
        if (selectButton != null)
        {
            selectButton.onClick.RemoveListener(OnSelectButtonClicked);
        }
    }
}
