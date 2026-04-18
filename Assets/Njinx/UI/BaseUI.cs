using System.Collections;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using DG.Tweening;

namespace Njinx.UI
{
    [RequireComponent(typeof(CanvasGroup))]
    public class BaseUI : Selectable
    {
        [SerializeField] bool startHidden = false;
        [SerializeField] bool is_blockRaycast = true;
        public CanvasGroup canvasGroup;

        public bool is_shown = true;

        protected override void Awake()
        {
            base.Awake();

            canvasGroup = GetComponent<CanvasGroup>();
        }

        protected override void Start()
        {
            base.Start();

            is_shown = true;
            //canvasGroup.blocksRaycasts = is_blockRaycast;
            if (startHidden) HideUI();
        }

        public async void SetUI(bool show, float delay = 0f)
        {
            if (show) await ShowUI(delay);
            else await HideUI(delay);
        }

        public virtual void ShowUI()
        {
            if(is_shown) return;

            canvasGroup.alpha = 1f;
            SetInteractable(true);
            is_shown = true;
        }

        public virtual void HideUI()
        {
            if(!is_shown) return;

            canvasGroup.alpha = 0f;
            SetInteractable(false);
            is_shown = false;
        }

        public async virtual UniTask ShowUI(float delay = 0)
        {
            if(is_shown) return;

            await canvasGroup.DOFade(1f, delay).AsyncWaitForCompletion();

            SetInteractable(true);
            is_shown = true;
        }

        public async virtual UniTask HideUI(float delay = 0)
        {
            if(!is_shown) return;

            await canvasGroup.DOFade(0f, delay).AsyncWaitForCompletion();

            SetInteractable(false);
            is_shown = false;
        }

        public void SetInteractable(bool is_interactable)
        {
            canvasGroup.interactable = is_interactable;
            if(is_blockRaycast) canvasGroup.blocksRaycasts = is_interactable;
            //canvasGroup.alpha = is_interactable ? 1f : 0f;
        }
    }
}
