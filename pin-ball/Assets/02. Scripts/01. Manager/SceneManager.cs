using System;

using UnityEngine;

public sealed class SceneManager : AppService
{
    [SerializeField] private BlackBlur blackScreen;
    [SerializeField] private SoundManager soundManager;

    [SerializeField] private float transitionDuration = 2f;

    private bool _isTransitioning;
    private ESceneName _activeScene;

    public void Load(ESceneName eSceneName)
    {
        if (_isTransitioning) return;

        _isTransitioning = true;

        soundManager.FadeOutBGM(transitionDuration);

        blackScreen.FadeInOut(
            transitionDuration,
            () => OnScreenCovered(eSceneName));
    }

    private void OnScreenCovered(ESceneName eSceneName)
    {
        UnityEngine.SceneManagement.SceneManager.LoadScene(GetSceneName(eSceneName));

        PlaySceneBgm(eSceneName);

        _isTransitioning = false;
    }

    private string GetSceneName(ESceneName eSceneName)
    {
        return eSceneName switch
        {
            ESceneName.Developer => "Developer",
            ESceneName.Title => "Title",
            ESceneName.Game => "Game",
            ESceneName.Empty => "Empty",

            _ => throw new ArgumentOutOfRangeException(
                nameof(eSceneName),
                eSceneName,
                null)
        };
    }

    private string GetActiveScene()
    {
        return _activeScene switch
        {
            ESceneName.Developer => "Developer",
            ESceneName.Title => "Title",
            ESceneName.Game => "Game",
            ESceneName.Empty => "Empty",

            _ => throw new ArgumentOutOfRangeException(
                nameof(_activeScene),
                _activeScene,
                null)
        };
    }

    private void PlaySceneBgm(ESceneName eSceneName)
    {
        string bgmKey = eSceneName switch
        {
            ESceneName.Title => "Title",
            ESceneName.Game => "InGame",
            _ => null
        };

        if (!string.IsNullOrEmpty(bgmKey))
        {
            soundManager.PlayBGM(bgmKey);
        }
    }
}