using System;
using System.Linq;
using System.Collections.Generic;

using UnityEngine;

public class UIManager : AppService
{
    public bool IsOnlyDefaultPanelsInStack =>
        _uiStack.Count > 0 &&
        _uiStack.All(panel => panel.IsDefaultPanel);

    public UIBase TopPanel => _uiStack.Peek();
    
    private Dictionary<Type, UIBase> _uiDictionary;
    private Stack<UIBase> _uiStack;

    protected override void Awake()
    {
        base.Awake();

        var uiPanels = GetComponentsInChildren<UIBase>(true);

        _uiDictionary = new(uiPanels.Length);
        _uiStack = new(uiPanels.Length);

        _uiDictionary = uiPanels.ToDictionary(p => p.GetType(), p => p);
    }

    private void Start()
    {
        foreach (var panel in _uiDictionary.Values)
        {
            try
            {
                panel.gameObject.SetActive(true);
                
                panel.Initialize(this); 

                if (!panel.IsManagedByStack)
                {
                    continue;
                }
                
                if (panel.IsDefaultPanel)
                {
                    _uiStack.Push(panel);
                }
                else
                {
                    panel.gameObject.SetActive(false);
                }
            }
            catch (Exception error)
            { Debug.LogError($"ERROR: {error.Message}\n{error.StackTrace}"); }
        }
    }
    
    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            Back();
        }
    }

    #region Get Panel
    public T GetPanel<T>() where T : UIBase
    {
        if (_uiDictionary.TryGetValue(typeof(T), out var panel))
        {
            return panel as T;
        }

        Debug.LogError($"Panel of type {typeof(T)} not found.");
        return null;
    }
    #endregion

    #region Manage Stack
    public void PushPanel(UIBase panel)
    {
        if (_uiStack.Count > 0 && _uiStack.Peek() == panel) return;
        
        foreach (var peek in _uiStack)
        {
            peek.Hide();
        }
     
        panel.Show();
        _uiStack.Push(panel);
    }
    
    public void PopPanel(UIBase panel)
    {
        if (_uiStack.Count == 0 || _uiStack.Peek() != panel) return;

        _uiStack.Pop();
        panel.Hide();

        if (_uiStack.Count > 0)
        {
            if (_uiStack.Peek().IsDefaultPanel)
            {
                foreach (var peek in _uiStack)
                {
                    peek.Show();
                }
            }
            else
            {
                var newPanel = _uiStack.Peek();
                newPanel.Show();
            }
        }
    }

    public void PopAllPanels()
    {
        while (_uiStack.Count > 0 && !_uiStack.Peek().IsDefaultPanel)
        {
            var top = _uiStack.Peek();
            top.ClosePanel();
        }
    }

    private void Back()
    {
        if (_uiStack.Count == 0) return;
        if (_uiStack.Peek().IsDefaultPanel) return;
        
        PopPanel(_uiStack.Peek());
    }
    #endregion
}
