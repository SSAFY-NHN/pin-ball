using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class UnitUpgradeCard : MonoBehaviour
{
    [SerializeField] private string rootUnitId;
    [SerializeField] private TextMeshProUGUI displayText;
    [SerializeField] private Button levelUpButton;

    public string RootUnitId => rootUnitId;

    private BattleManager battleManager;

    public void Initialize(BattleManager manager)
    {
        battleManager = manager;
        levelUpButton?.onClick.AddListener(LevelUp);
    }

    public void Refresh(
        BattleManager manager,
        UnitManager unitManager,
        TitleData titleData)
    {
        if (manager == null || unitManager == null) return;

        int level = manager.GetAllyJobLevel(rootUnitId);
        bool owned = unitManager.GetOwnedAllyCount(rootUnitId) > 0;
        string unitName = rootUnitId;
        string stats = string.Empty;
        if (titleData != null &&
            titleData.TryGetAllyUnit(rootUnitId, out AllyUnitData data))
        {
            unitName = data.name;
            BattleUnitStats current = data.CreateStats(
                level,
                titleData.AllyCommon.classLevel);
            int nextLevel = Mathf.Min(
                AllyProgressionController.MaximumLevel,
                level + 1);
            BattleUnitStats next = data.CreateStats(
                nextLevel,
                titleData.AllyCommon.classLevel);
            stats = $"HP {current.MaxHp:0}→{next.MaxHp:0}  " +
                    $"ATK {current.AttackDamage:0}→{next.AttackDamage:0}\n" +
                    $"DEF {current.Defense:0}→{next.Defense:0}  " +
                    $"AS {current.AttackRate:0.##}→{next.AttackRate:0.##}";
        }

        string state = !owned
            ? "유닛 보유 필요"
            : level >= AllyProgressionController.MaximumLevel
                ? "최대 레벨 · 두 번째 직업 해금"
                : $"{manager.GetAllyJobLevelUpCost(rootUnitId)}G";
        string unlock = level < 5
            ? "Lv.5 첫 번째 직업 해금"
            : level < AllyProgressionController.MaximumLevel
                ? "Lv.10 두 번째 직업 해금"
                : "모든 직업 해금";

        UiRefreshUtility.SetTextIfChanged(
            displayText,
            $"{unitName}  Lv.{level}\n{stats}\n{unlock}\n{state}");
        if (levelUpButton != null)
        {
            levelUpButton.interactable = manager.CanLevelUpAllyJob(rootUnitId);
        }
    }

    private void LevelUp()
    {
        battleManager?.TryLevelUpAllyJob(rootUnitId);
    }

    private void OnDestroy()
    {
        levelUpButton?.onClick.RemoveListener(LevelUp);
    }
}
