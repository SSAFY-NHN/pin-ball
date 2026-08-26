using NUnit.Framework;

using TMPro;
using UnityEngine;

public sealed class WebGlUiRefreshTests
{
    private GameObject host;

    [SetUp]
    public void SetUp()
    {
        host = new GameObject("WebGL UI Refresh Test");
    }

    [TearDown]
    public void TearDown()
    {
        Object.DestroyImmediate(host);
    }

    [Test]
    public void SetTextIfChanged_IdenticalValueSkipsAssignment()
    {
        var text = host.AddComponent<TextMeshProUGUI>();
        text.text = "사용 가능";

        bool changed = UiRefreshUtility.SetTextIfChanged(
            text,
            "사용 가능");

        Assert.That(changed, Is.False);
        Assert.That(text.text, Is.EqualTo("사용 가능"));
    }

    [Test]
    public void SetTextIfChanged_DifferentValueAppliesAssignment()
    {
        var text = host.AddComponent<TextMeshProUGUI>();
        text.text = "1";

        bool changed = UiRefreshUtility.SetTextIfChanged(text, "0");

        Assert.That(changed, Is.True);
        Assert.That(text.text, Is.EqualTo("0"));
    }

    [Test]
    public void SetActiveIfChanged_OnlyReportsRealStateChange()
    {
        host.SetActive(true);

        Assert.That(
            UiRefreshUtility.SetActiveIfChanged(host, true),
            Is.False);
        Assert.That(
            UiRefreshUtility.SetActiveIfChanged(host, false),
            Is.True);
        Assert.That(host.activeSelf, Is.False);
    }

    [TestCase(0f, 0)]
    [TestCase(59.1f, 59)]
    [TestCase(59.9f, 59)]
    [TestCase(60f, 60)]
    [TestCase(89.9f, 89)]
    [TestCase(90f, 90)]
    public void GetAssaultCountdownUpdateKey_ChangesAtWholeSecondBoundary(
        float elapsedTime,
        int expected)
    {
        Assert.That(
            StatusPanel.GetAssaultCountdownUpdateKey(elapsedTime),
            Is.EqualTo(expected));
    }
}
