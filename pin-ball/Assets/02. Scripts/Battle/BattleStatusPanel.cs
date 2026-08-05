using System;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.UI;

public class BattleStatusPanel : UIBase
{
    private Text _waveText;
    private Text _enemyText;
    private Text _playerHpText;
    private Text _goldText;
    private Text _prepText;
    private Text _resultText;
    private Button _startButton;

    public void BindStart(Action onStartClicked)
    {
        EnsureBuilt();

        _startButton.onClick.RemoveAllListeners();
        _startButton.onClick.AddListener(() => onStartClicked?.Invoke());
    }

    public void SetWaveInfo(int currentWaveIndex, int totalWaveCount, int remainingEnemies)
    {
        EnsureBuilt();
        var current = Mathf.Clamp(currentWaveIndex + 1, 0, Mathf.Max(0, totalWaveCount));
        _waveText.text = $"Wave: {current}/{totalWaveCount}";
        _enemyText.text = $"Remaining Enemies: {Mathf.Max(0, remainingEnemies)}";
    }

    public void SetPlayerHp(int hp, int maxHp)
    {
        EnsureBuilt();
        _playerHpText.text = $"Player HP: {Mathf.Max(0, hp)}/{Mathf.Max(1, maxHp)}";
    }

    public void SetGold(int gold)
    {
        EnsureBuilt();
        _goldText.text = $"Gold: {Mathf.Max(0, gold)}";
    }

    public void SetPreparation(float remainSeconds)
    {
        EnsureBuilt();

        if (remainSeconds > 0f)
        {
            _prepText.text = $"Preparation: {Mathf.CeilToInt(remainSeconds)}s";
        }
        else
        {
            _prepText.text = "Preparation: done";
        }
    }

    public void SetResult(string message)
    {
        EnsureBuilt();
        _resultText.text = message;
    }

    public void SetStartButtonVisible(bool visible)
    {
        EnsureBuilt();
        _startButton.gameObject.SetActive(visible);
    }

    private void EnsureBuilt()
    {
        if (_startButton != null)
        {
            return;
        }

        var canvas = FindObjectOfType<Canvas>();
        if (canvas == null)
        {
            var canvasObject = new GameObject("BattleCanvas");
            canvas = canvasObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasObject.AddComponent<CanvasScaler>();
            canvasObject.AddComponent<GraphicRaycaster>();
        }

        transform.SetParent(canvas.transform, false);

        var rootRect = gameObject.GetComponent<RectTransform>();
        if (rootRect == null)
        {
            rootRect = gameObject.AddComponent<RectTransform>();
        }

        rootRect.anchorMin = new Vector2(0f, 1f);
        rootRect.anchorMax = new Vector2(0f, 1f);
        rootRect.pivot = new Vector2(0f, 1f);
        rootRect.anchoredPosition = new Vector2(20f, -20f);
        rootRect.sizeDelta = new Vector2(420f, 260f);

        var backgroundImage = gameObject.GetComponent<Image>();
        if (backgroundImage == null)
        {
            backgroundImage = gameObject.AddComponent<Image>();
        }

        backgroundImage.color = new Color(0f, 0f, 0f, 0.5f);

        var labels = new List<string>
        {
            "Wave: 0/0",
            "Remaining Enemies: 0",
            "Player HP: 0/0",
            "Gold: 0",
            "Preparation: -",
            "Status: -"
        };

        var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        var y = -16f;

        _waveText = CreateText("WaveText", labels[0], y, font);
        y -= 34f;
        _enemyText = CreateText("EnemyText", labels[1], y, font);
        y -= 34f;
        _playerHpText = CreateText("PlayerHpText", labels[2], y, font);
        y -= 34f;
        _goldText = CreateText("GoldText", labels[3], y, font);
        y -= 34f;
        _prepText = CreateText("PrepText", labels[4], y, font);
        y -= 34f;
        _resultText = CreateText("ResultText", labels[5], y, font);

        var buttonObject = new GameObject("StartWaveButton");
        buttonObject.transform.SetParent(transform, false);

        var buttonRect = buttonObject.AddComponent<RectTransform>();
        buttonRect.anchorMin = new Vector2(1f, 0f);
        buttonRect.anchorMax = new Vector2(1f, 0f);
        buttonRect.pivot = new Vector2(1f, 0f);
        buttonRect.anchoredPosition = new Vector2(-16f, 16f);
        buttonRect.sizeDelta = new Vector2(160f, 48f);

        var buttonImage = buttonObject.AddComponent<Image>();
        buttonImage.color = new Color(0.2f, 0.6f, 0.2f, 0.95f);

        _startButton = buttonObject.AddComponent<Button>();

        var buttonLabel = CreateChildText(buttonObject.transform, "Start Wave", font);
        buttonLabel.alignment = TextAnchor.MiddleCenter;
        buttonLabel.rectTransform.anchorMin = Vector2.zero;
        buttonLabel.rectTransform.anchorMax = Vector2.one;
        buttonLabel.rectTransform.offsetMin = Vector2.zero;
        buttonLabel.rectTransform.offsetMax = Vector2.zero;
    }

    private Text CreateText(string name, string text, float y, Font font)
    {
        var obj = new GameObject(name);
        obj.transform.SetParent(transform, false);

        var rect = obj.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(0f, 1f);
        rect.pivot = new Vector2(0f, 1f);
        rect.anchoredPosition = new Vector2(16f, y);
        rect.sizeDelta = new Vector2(360f, 28f);

        var label = obj.AddComponent<Text>();
        label.font = font;
        label.fontSize = 20;
        label.color = Color.white;
        label.alignment = TextAnchor.MiddleLeft;
        label.text = text;

        return label;
    }

    private static Text CreateChildText(Transform parent, string text, Font font)
    {
        var labelObject = new GameObject("Label");
        labelObject.transform.SetParent(parent, false);

        var labelRect = labelObject.AddComponent<RectTransform>();
        labelRect.sizeDelta = Vector2.zero;

        var label = labelObject.AddComponent<Text>();
        label.font = font;
        label.fontSize = 22;
        label.color = Color.white;
        label.text = text;

        return label;
    }
}
