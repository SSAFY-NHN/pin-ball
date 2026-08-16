using System;

using UnityEngine;

public sealed class SceneManager : AppService
{
    [SerializeField] private BlackBlur blackScreen;
    [SerializeField] private SoundManager soundManager;

    [SerializeField] private float transitionDuration = 2f;

    private bool _isTransitioning;
    private ESceneName _activeScene;
    private readonly GameRunController _gameRunController = new();

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
        if (eSceneName == ESceneName.Game)
        {
            _gameRunController.PrepareForSceneLoad();
        }

        UnityEngine.SceneManagement.SceneManager.LoadScene(GetSceneName(eSceneName));

        if (eSceneName == ESceneName.Game)
        {
            _gameRunController.InitializeLoadedScene();
        }

        PlaySceneBgm(eSceneName);

        _isTransitioning = false;
    }

    private string GetSceneName(ESceneName eSceneName)
    {
        return eSceneName switch
        {
            ESceneName.Developer => "00. Developer",
            ESceneName.Title => "01. Title",
            ESceneName.Game => "02. Game",
            ESceneName.Empty => "Empty",

            _ => throw new ArgumentOutOfRangeException(
                nameof(eSceneName),
                eSceneName,
                null)
        };
    }

    private void PlaySceneBgm(ESceneName eSceneName)
    {
        string bgmKey = eSceneName switch
        {
            ESceneName.Title => SoundName.MainBgm,
            ESceneName.Game => SoundName.MainBgm,
            _ => null
        };

        if (!string.IsNullOrEmpty(bgmKey))
        {
            soundManager.PlayBGM(bgmKey);
        }
    }
}
