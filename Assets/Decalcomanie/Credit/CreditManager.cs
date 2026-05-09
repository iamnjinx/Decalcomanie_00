using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using Njinx.UI;
using UnityEngine;

public class CreditManager : MonoBehaviour
{
    [SerializeField] BaseUI curtainUI;

    [SerializeField] Transform creditUI;
    [SerializeField] float targetYPos;
    async void Start()
    {
        await curtainUI.HideUI(0.5f);

        await MoveCreditUI();

        await curtainUI.ShowUI(0.5f);

        GameManager.Instance.LoadTitleScene();
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape) || Input.GetMouseButtonDown(0))
        {
            GameManager.Instance.LoadTitleScene();
        }  
    }

    private async UniTask MoveCreditUI()
    {
        creditUI.GetComponent<RectTransform>().DOAnchorPosY(targetYPos, 10f).SetEase(Ease.Linear);
        await UniTask.Delay(120000);
    }
}
