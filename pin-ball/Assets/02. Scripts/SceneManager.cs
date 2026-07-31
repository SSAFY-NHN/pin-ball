using System;

using UnityEngine;

public enum SceneName
{
    Developer,
    Title,
    Game,
    Empty
}

public sealed class SceneManager : AppService
{
    [SerializeField] private BlackBlur blackScreen;
    [SerializeField] private SoundManager soundManager;

    [SerializeField] private float transitionDuration = 2f;

    private bool isTransitioning;

    public void Load(SceneName sceneName)
    {
        if (isTransitioning) return;

        isTransitioning = true;

        soundManager.FadeOutBGM(transitionDuration);

        blackScreen.FadeInOut(
            transitionDuration,
            () => OnScreenCovered(sceneName));
    }

    private void OnScreenCovered(SceneName sceneName)
    {
        UnityEngine.SceneManagement.SceneManager.LoadScene(GetSceneName(sceneName));

        PlaySceneBgm(sceneName);

        isTransitioning = false;
    }

    private static string GetSceneName(SceneName sceneName)
    {
        return sceneName switch
        {
            SceneName.Developer => "Developer",
            SceneName.Title => "Title",
            SceneName.Game => "Game",
            SceneName.Empty => "Empty",

            _ => throw new ArgumentOutOfRangeException(
                nameof(sceneName),
                sceneName,
                null)
        };
    }

    private void PlaySceneBgm(SceneName sceneName)
    {
        string bgmKey = sceneName switch
        {
            SceneName.Title => "Title",
            SceneName.Game => "InGame",
            _ => null
        };

        if (!string.IsNullOrEmpty(bgmKey))
        {
            soundManager.PlayBGM(bgmKey);
        }
    }
}