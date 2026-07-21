using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using Njinx.UI;
using UnityEngine;

public class CreditManager : MonoBehaviour
{
    [SerializeField] BaseUI curtainUI;

    [SerializeField] Transform creditUI;
    [SerializeField] float targetYPos;
    [SerializeField] float moveSpeed = 50f;
    [SerializeField] float fastMoveSpeed = 200f;

    RectTransform creditRect;
    bool isMoving;

    async void Start()
    {
        creditRect = creditUI.GetComponent<RectTransform>();

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

        if (isMoving)
        {
            float speed = Input.GetKey(KeyCode.Space) ? fastMoveSpeed : moveSpeed;
            Vector2 pos = creditRect.anchoredPosition;
            pos.y = Mathf.MoveTowards(pos.y, targetYPos, speed * Time.deltaTime);
            creditRect.anchoredPosition = pos;
        }
    }

    private async UniTask MoveCreditUI()
    {
        isMoving = true;

        while (Mathf.Abs(creditRect.anchoredPosition.y - targetYPos) > 0.01f)
        {
            await UniTask.Yield();
        }

        isMoving = false;
    }
}
