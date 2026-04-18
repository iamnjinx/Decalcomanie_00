using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;

namespace Njinx.UI
{
    public class DraggableUI<T> : BaseUI, IBeginDragHandler, IDragHandler, IEndDragHandler, IObjDraggable<T>
    {
        protected T objSO;

        private RectTransform rectTransform;
        private Canvas canvas;  

        private Vector3 initPos;
        private Transform originalParent;
        private Vector2 pointerOffset;

        public System.Action<T> onBeginDragCallback;
        public System.Action<T> onEndDragCallback;

        public UnityEvent<T> onBeginDragUnityEvent;
        public UnityEvent<T> onEndDragUnityEvent;

        public T GetObjSO() => objSO;

        protected override void Awake()
        {
            base.Awake();

            rectTransform = GetComponent<RectTransform>();
            canvas        = GetComponentInParent<Canvas>();
        }

        public virtual void OnBeginDrag(PointerEventData eventData)
        {
            if(!interactable) return;

            onBeginDragCallback?.Invoke(objSO);
            onBeginDragUnityEvent?.Invoke(objSO);

            initPos = rectTransform.localPosition;

            originalParent = transform.parent;
            transform.SetParent(canvas.transform);  // 최상단으로 빼주면 드래그시 위로 뜸

            canvasGroup.blocksRaycasts = false;     // 드래그 중에는 뒤에 있는 DropZone이 ray를 받게

            // 드래그 시작 시 마우스 위치를 캔버스 로컬 좌표로 변환
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                canvas.transform as RectTransform,
                eventData.position,
                canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera,
                out var localPoint);

            pointerOffset = (Vector2)rectTransform.localPosition - localPoint;
        }

        public virtual void OnDrag(PointerEventData eventData)
        {
            if(!interactable) return;

            // 마우스/터치 위치로 따라가게
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                canvas.transform as RectTransform,
                eventData.position,
                canvas.worldCamera,
                out var localPoint);

            rectTransform.anchoredPosition = localPoint + pointerOffset;
        }

        public virtual void OnEndDrag(PointerEventData eventData)
        {   
            if(!interactable) return;
            
            canvasGroup.blocksRaycasts = true;
            canvasGroup.alpha = 1f;

            if (transform.parent == canvas.transform)
            {
                transform.SetParent(originalParent);
                rectTransform.localPosition = initPos;
                onEndDragCallback?.Invoke(objSO);
                onEndDragUnityEvent?.Invoke(objSO);
            }
        }
    }

    public interface IObjDraggable<out T>
    {
        T GetObjSO();
    }
}
