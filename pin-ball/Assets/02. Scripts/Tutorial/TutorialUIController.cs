using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class TutorialUIController
{
    private readonly GameObject _overlay;
    private readonly TextMeshProUGUI _messageText;
    private readonly Button _continueButton;

    public TutorialUIController(
        GameObject overlay,
        TextMeshProUGUI messageText,
        Button continueButton)
    {
        _overlay = overlay;
        _messageText = messageText;
        _continueButton = continueButton;
    }

    public void Show(string message, bool showContinue)
    {
        if (_overlay != null) _overlay.SetActive(true);
        if (_messageText != null) _messageText.text = message;
        if (_continueButton != null)
        {
            _continueButton.gameObject.SetActive(showContinue);
        }
    }

    public void Hide()
    {
        if (_overlay != null) _overlay.SetActive(false);
    }
}
