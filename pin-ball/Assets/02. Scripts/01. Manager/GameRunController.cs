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

}
