using System;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.UI;

internal sealed class SoundButtonClickController
{
    private readonly Action _playClickSound;
    private readonly List<Button> _buttons = new();

    public SoundButtonClickController(Action playClickSound)
    {
        _playClickSound = playClickSound;
    }

    public void RefreshSceneButtons()
    {
        Clear();

        var buttons = UnityEngine.Object.FindObjectsByType<Button>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None);
        foreach (var button in buttons)
        {
            if (button == null) continue;

            button.onClick.AddListener(PlayClickSound);
            _buttons.Add(button);
        }
    }

    public void Clear()
    {
        foreach (var button in _buttons)
        {
            if (button != null)
            {
                button.onClick.RemoveListener(PlayClickSound);
            }
        }

        _buttons.Clear();
    }

    private void PlayClickSound()
    {
        _playClickSound();
    }
}
