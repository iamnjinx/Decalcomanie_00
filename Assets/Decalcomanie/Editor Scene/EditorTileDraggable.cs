using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.EventSystems;

[RequireComponent(typeof(CanvasGroup))]
public class EditorTileDraggable : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    public TileType tileType;

    private RectTransform rectTransform;
    private CanvasGroup canvasGroup;
    private Canvas canvas;

    private Vector3 originalScale;
    private Vector2 originalAnchoredPosition;
    private int originalSiblingIndex;

    private Tween scaleTween;

    void Awake()
    {
        rectTransform = (RectTransform)transform;
        canvas = GetComponentInParent<Canvas>();
        canvasGroup = GetComponent<CanvasGroup>();
    }

    void Start()
    {
        originalScale = rectTransform.localScale;
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        originalAnchoredPosition = rectTransform.anchoredPosition;
        originalSiblingIndex = rectTransform.GetSiblingIndex();

        rectTransform.SetAsLastSibling();
        canvasGroup.blocksRaycasts = false;
    }

    public void OnDrag(PointerEventData eventData)
    {
        float scaleFactor = canvas != null ? canvas.scaleFactor : 1f;
        ChangeScale(Vector3.one, 0.1f);
        rectTransform.anchoredPosition += eventData.delta / scaleFactor;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        EditorTileDroppable droppable = FindDroppableUnderPointer(eventData);
        if (droppable != null)
            droppable.HandleDrop(this);

        rectTransform.anchoredPosition = originalAnchoredPosition;
        rectTransform.SetSiblingIndex(originalSiblingIndex);
        ChangeScale(originalScale, 0);
        canvasGroup.blocksRaycasts = true;
    }

    private EditorTileDroppable FindDroppableUnderPointer(PointerEventData eventData)
    {
        List<RaycastResult> raycastResults = new();
        EventSystem.current.RaycastAll(eventData, raycastResults);

        foreach (RaycastResult result in raycastResults)
        {
            EditorTileDroppable droppable = result.gameObject.GetComponentInParent<EditorTileDroppable>();
            if (droppable != null)
                return droppable;
        }

        return null;
    }

    private void ChangeScale(Vector3 targetScale, float duration)
    {
        if (scaleTween != null && scaleTween.IsActive())
            scaleTween.Kill();
        scaleTween = rectTransform.DOScale(targetScale, duration);
    }
}
