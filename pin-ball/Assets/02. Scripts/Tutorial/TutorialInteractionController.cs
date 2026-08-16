using UnityEngine;
using UnityEngine.UI;

public sealed class TutorialInteractionController
{
    private static readonly Vector2 FocusPadding = new(24f, 20f);

    private readonly Button _shopButton;
    private readonly Button _itemsButton;
    private readonly Button _waveStartButton;
    private readonly TutorialFocusIndicator _focusIndicator;

    public TutorialInteractionController(
        Button shopButton,
        Button itemsButton,
        Button waveStartButton,
        TutorialFocusIndicator focusIndicator)
    {
        _shopButton = shopButton;
        _itemsButton = itemsButton;
        _waveStartButton = waveStartButton;
        _focusIndicator = focusIndicator;
    }

    public void Show(
        Button allowedButton,
        Transform focusTarget,
        bool blockInput)
    {
        SetOnlyAllowedButton(allowedButton);
        _focusIndicator?.SetInputBlocked(blockInput);

        if (focusTarget == null) _focusIndicator?.Hide();
        else _focusIndicator?.Focus(focusTarget, FocusPadding);
    }

    public void Clear()
    {
        _focusIndicator?.Hide();
        _focusIndicator?.SetInputBlocked(false);
    }

    private void SetOnlyAllowedButton(Button allowedButton)
    {
        if (_shopButton != null)
        {
            _shopButton.interactable = allowedButton == _shopButton;
        }
        if (_itemsButton != null)
        {
            _itemsButton.interactable = allowedButton == _itemsButton;
        }
        if (_waveStartButton != null)
        {
            _waveStartButton.interactable =
                allowedButton == _waveStartButton;
        }
    }
}
