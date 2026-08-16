using UnityEngine;

internal sealed class GameRunController
{
    public void PrepareForSceneLoad()
    {
        if (App.TryGet<ItemManager>(out var itemManager))
        {
            itemManager.ResetRunState();
        }
    }

    public void InitializeLoadedScene()
    {
        if (!App.TryGet<UnitManager>(out var unitManager) ||
            !App.TryGet<BattleManager>(out var battleManager) ||
            !App.TryGet<PinballManager>(out var pinballManager))
        {
            Debug.LogError(
                "[GameRunController] Game run services are missing.");
            return;
        }

        unitManager.InitializeNewRun();
        battleManager.InitializeNewRun();
        pinballManager.InitializeNewRun();
    }
}
