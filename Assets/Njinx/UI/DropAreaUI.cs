using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;

namespace Njinx.UI
{
    public class DropAreaUI<T> : BaseUI, IDropHandler
    {
        public T value;
        public UnityEvent<T> onDropUnityEvent;
        public System.Action<T> onDropCallback;

        public void OnDrop(PointerEventData eventData)
        {
            if (eventData.pointerDrag == null) return;

            Debug.Log($"{eventData.pointerDrag.gameObject.name} dropped on {gameObject.name}");

            IObjDraggable<T> draggable = null;
            foreach (var mb in eventData.pointerDrag.GetComponents<MonoBehaviour>())
            {
                if (mb is IObjDraggable<T> d) { draggable = d; break; }
            }

            if (draggable == null)
            {
                Debug.Log($"Dropped object does not implement IObjDraggable<{typeof(T).Name}>");
                return;
            }

            onDropCallback?.Invoke(value);
            onDropUnityEvent?.Invoke(value);
        }
    }
}