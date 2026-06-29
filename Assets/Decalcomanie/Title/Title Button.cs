using Njinx.UI;
using UnityEngine;
using UnityEngine.EventSystems;
using DG.Tweening;

public class TitleButton : ButtonUI
{
    [SerializeField] private float hoverScale = 1.1f;
    [SerializeField] private float scaleDuration = 0.15f;

    private Vector3 _originalScale;

    protected override void Awake()
    {
        base.Awake();
        _originalScale = transform.localScale;
    }

    public override void OnPointerEnter(PointerEventData eventData)
    {
        base.OnPointerEnter(eventData);
        transform.DOScale(_originalScale * hoverScale, scaleDuration);
    }

    public override void OnPointerExit(PointerEventData eventData)
    {
        base.OnPointerExit(eventData);
        transform.DOScale(_originalScale, scaleDuration);
    }

    public override void OnPointerDown(PointerEventData eventData)
    {
        base.OnPointerDown(eventData);
        if (AudioManager.Instance != null)
            AudioManager.Instance.PlaySFX("button_click");
    }

}
