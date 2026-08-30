using System;
using System.Collections;
using System.Collections.Generic;
using Njinx.UI;
using UnityEngine;

public class PaintButton : ButtonUI
{
    [SerializeField] private int paintButtonID;

    public Action<int> onPaintButtonClicked;

    private MouseBlockUI mouseBlockUI;

    protected override void Awake()
    {
        base.Awake();
        OnSingleClick += () => OnPaintButtonClicked();
        mouseBlockUI = GetComponent<MouseBlockUI>();
    }

    public void SetPaintButtonID(int id)
    {
        paintButtonID = id;
    }

    public void SetBlocked(bool is_blocked)
    {
        if (mouseBlockUI != null) mouseBlockUI.enabled = is_blocked;
        SetInteractable(!is_blocked);
    }

    public void OnPaintButtonClicked()
    {
        onPaintButtonClicked?.Invoke(paintButtonID);
    }
}
