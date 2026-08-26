#if UNITY_EDITOR
using NUnit.Framework;

public sealed class PinballManualInputTests
{
    [Test]
    public void LauncherManualInput_RemainsDisabled()
    {
        Assert.That(PinballLauncherController.ManualInputEnabled, Is.False);
    }
}
#endif
