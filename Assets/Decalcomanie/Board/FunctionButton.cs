using System.Collections;
using System.Collections.Generic;
using Njinx.UI;
using UnityEngine;
using DG.Tweening;

public class FunctionButton : ButtonUI
{
    [SerializeField] private float highlightScale = 1.1f;
    [SerializeField] private float pulseDuration = 0.5f;

    private Vector3 _originalScale;

    protected override void Awake()
    {
        base.Awake();
        _originalScale = transform.localScale;
    }

    public void OnHighlighted()
    {
        transform.DOKill();
        transform.DOScale(_originalScale * highlightScale, pulseDuration)
            .SetLoops(-1, LoopType.Yoyo)
            .SetEase(Ease.InOutSine);
    }

    public void OnUnhighlighted()
    {
        transform.DOKill();
        transform.DOScale(_originalScale, pulseDuration).SetEase(Ease.InOutSine);
    }
}
