using System;
using Njinx.UI;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

// 스테이지의 소모성 아이템(연필/지우개) 하나에 대응하는 버튼. PaintManager가 Instantiate/소모/복구를 관리한다.
public class ItemButton : ButtonUI
{
    [SerializeField] private PaintManager.PaintMode itemMode;
    public PaintManager.PaintMode ItemMode => itemMode;

    [SerializeField] private float selectedScaleMultiplier = 1.15f;
    [SerializeField] private float selectedScaleDuration = 0.15f;

    [Header("Sprite")]
    private Image iconImage;
    [SerializeField] private Sprite normalSprite;
    [SerializeField] private Sprite selectedSprite;

    public Action<ItemButton> onItemButtonClicked;

    private Vector3 baseScale;

    protected override void Awake()
    {
        base.Awake();
        iconImage = GetComponent<Image>();
        baseScale = transform.localScale;
        OnSingleClick += () => onItemButtonClicked?.Invoke(this);
    }

    public void SetItemMode(PaintManager.PaintMode mode)
    {
        itemMode = mode;
    }

    // instant: Reset/소모 직후처럼 애니메이션 없이 즉시 크기를 맞춰야 할 때 사용.
    public void SetSelected(bool selected, bool instant = false)
    {
        transform.DOKill();
        Vector3 target = selected ? baseScale * selectedScaleMultiplier : baseScale;

        if (instant) transform.localScale = target;
        else transform.DOScale(target, selectedScaleDuration).SetEase(Ease.OutQuad);

        if (iconImage != null)
        {
            Sprite sprite = selected ? selectedSprite : normalSprite;
            if (sprite != null) iconImage.sprite = sprite;
        }
    }
}
