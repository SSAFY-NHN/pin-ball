public readonly struct WaveButtonState
{
    public bool ShowStartButton { get; }
    public bool EnableStartButton { get; }
    public bool EnableLaunchButton { get; }
    public int LaunchCost { get; }
    public bool CanAffordLaunch { get; }

    public WaveButtonState(
        bool showStartButton,
        bool enableStartButton,
        bool enableLaunchButton,
        int launchCost,
        bool canAffordLaunch)
    {
        ShowStartButton = showStartButton;
        EnableStartButton = enableStartButton;
        EnableLaunchButton = enableLaunchButton;
        LaunchCost = launchCost;
        CanAffordLaunch = canAffordLaunch;
    }
}

public sealed class WaveButtonStateController
{
    public WaveButtonState Calculate(
        bool isPreparation,
        bool canUsePreparation,
        EPinballState pinballState,
        bool hasAvailableBall,
        int currentGold,
        int launchCost)
    {
        bool canAffordLaunch = currentGold >= launchCost;
        return new WaveButtonState(
            isPreparation,
            canUsePreparation && pinballState == EPinballState.Idle,
            IsLaunchAvailable(
                canUsePreparation,
                pinballState,
                hasAvailableBall,
                canAffordLaunch),
            launchCost,
            canAffordLaunch);
    }

    public static bool IsLaunchAvailable(
        bool canUsePreparation,
        EPinballState pinballState,
        bool hasAvailableBall,
        bool canAffordLaunch)
    {
        return canUsePreparation &&
               pinballState == EPinballState.Idle &&
               hasAvailableBall &&
               canAffordLaunch;
    }
}
