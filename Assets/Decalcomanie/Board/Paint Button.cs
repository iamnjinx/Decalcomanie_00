using System;
using System.Collections;
using System.Collections.Generic;
using Njinx.UI;
using UnityEngine;

public class PaintButton : ButtonUI
{
    [SerializeField] private int paintButtonID;

    public Action<int> onPaintButtonClicked;

    protected override void Awake()
    {
        base.Awake();
        OnSingleClick += () => OnPaintButtonClicked();
    }

    public void SetPaintButtonID(int id)
    {
        paintButtonID = id;
    }

    public void OnPaintButtonClicked()
    {
        onPaintButtonClicked?.Invoke(paintButtonID);
    }
}
