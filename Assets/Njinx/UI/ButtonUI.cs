using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Njinx.UI
{
    public class ButtonUI : BaseUI, IPointerClickHandler
    {
        public bool set_doubleclick = false;

        [Header("ButtonUI")]
        public Action OnSingleClick;
        public Action OnDoubleClick;

        private float doubleClickThreshold = 0.05f;
        private Coroutine clickRoutine;

        public void OnPointerClick(PointerEventData eventData)
        {
            if (eventData.button != PointerEventData.InputButton.Left) return;

            if (!set_doubleclick)
            {
                OnClick();
                return;
            }

            if (eventData.clickCount == 2)
            {
                if (clickRoutine != null)
                {
                    StopCoroutine(clickRoutine);
                    clickRoutine = null;
                }
                OnDoubleClick?.Invoke();
            }
            else if (eventData.clickCount == 1)
            {
                clickRoutine = StartCoroutine(SingleClickDelay());
            }
        }

        IEnumerator SingleClickDelay()
        {
            yield return new WaitForSeconds(doubleClickThreshold);
            OnSingleClick?.Invoke();
            clickRoutine = null;
        }
    
        public virtual void OnClick()
        {
            OnSingleClick?.Invoke();
        }
    }
}
