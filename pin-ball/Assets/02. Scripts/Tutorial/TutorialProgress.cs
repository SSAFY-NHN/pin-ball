public enum TutorialStep
{
    GoalExplanation,
    MagnetExplanation,
    FirstLaunch,
    SecondLaunch,
    Merge,
    BuyPersonalPotion,
    Items,
    StartWave,
    Complete
}

public sealed class TutorialProgress
{
    public TutorialStep Step { get; private set; } = TutorialStep.GoalExplanation;

    public void ContinueFromMessage()
    {
        if (Step == TutorialStep.GoalExplanation) Step = TutorialStep.MagnetExplanation;
        else if (Step == TutorialStep.MagnetExplanation) Step = TutorialStep.FirstLaunch;
        else if (Step == TutorialStep.Merge) Step = TutorialStep.BuyPersonalPotion;
    }

    public void NotifyGoalReached()
    {
        if (Step == TutorialStep.FirstLaunch) Step = TutorialStep.SecondLaunch;
        else if (Step == TutorialStep.SecondLaunch) Step = TutorialStep.Merge;
    }

    public void NotifyPersonalPotionPurchased()
    {
        if (Step == TutorialStep.BuyPersonalPotion) Step = TutorialStep.Items;
    }

    public void NotifyItemsOpened()
    {
        if (Step == TutorialStep.Items) Step = TutorialStep.StartWave;
    }

    public void NotifyWaveStarted()
    {
        if (Step == TutorialStep.StartWave) Step = TutorialStep.Complete;
    }
}
