using System;
using Njinx.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

// 스테이지의 소모성 아이템(연필/지우개) 버튼. 종류당 하나씩 씬에 미리 배치해 두고 재사용하며,
// 남은 개수는 countText로 표시한다. Instantiate/Destroy하지 않고, 개수 관리는 PaintManager가 맡는다.
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

    [Header("Count")]
    [SerializeField] private TextMeshProUGUI countText;
    [SerializeField] private float disabledAlpha = 0.4f; // 남은 개수가 0일 때의 투명도

    public Action<ItemButton> onItemButtonClicked;

    public int Count { get; private set; }
    public bool HasRemaining => Count > 0;

    private Vector3 baseScale;
    private bool initialized;

    protected override void Awake()
    {
        base.Awake();
        EnsureInitialized();
        OnSingleClick += () => onItemButtonClicked?.Invoke(this);
    }

    // 스테이지 로드 순서에 따라 SetCount/SetSelected가 Awake보다 먼저 불릴 수 있어 초기화를 따로 빼 둔다.
    // (개수가 0인 아이템은 GameObject가 꺼진 채로 세팅되기 때문에 Awake가 아예 돌지 않을 수도 있다.)
    private void EnsureInitialized()
    {
        if (initialized) return;
        initialized = true;

        iconImage = GetComponent<Image>();
        baseScale = transform.localScale;
        if (canvasGroup == null) canvasGroup = GetComponent<CanvasGroup>();
    }

    public void SetItemMode(PaintManager.PaintMode mode)
    {
        itemMode = mode;
    }

    // 남은 개수를 표시하고, 0이 되면 반투명 + 클릭 불가로 만든다.
    public void SetCount(int count)
    {
        EnsureInitialized();

        Count = Mathf.Max(0, count);
        if (countText != null) countText.text = Count.ToString();

        if (canvasGroup != null) canvasGroup.alpha = HasRemaining ? 1f : disabledAlpha;
        SetInteractable(HasRemaining);
    }

    // instant: Reset/소모 직후처럼 애니메이션 없이 즉시 크기를 맞춰야 할 때 사용.
    public void SetSelected(bool selected, bool instant = false)
    {
        EnsureInitialized();

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
