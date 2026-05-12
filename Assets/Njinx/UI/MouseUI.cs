using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(RectTransform))]
[RequireComponent(typeof(Image))]
[RequireComponent(typeof(Canvas))]
public class MouseUI : MonoBehaviour
{
    private RectTransform rectTransform;
    private Canvas rootCanvas;
    private Image imageComponent;

    [SerializeField] private Vector2 offset;
    [SerializeField] private List<Sprite> mouseSprites;

    [SerializeField] bool isBlocked;

    void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        imageComponent = GetComponent<Image>();

        // 부모 쪽 Canvas를 좌표 변환용으로 사용
        rootCanvas = transform.parent.GetComponentInParent<Canvas>().rootCanvas;

        // 이 오브젝트 전용 Canvas로 격리 — localPosition 변경 시 부모 Canvas 리빌드 방지
        Canvas selfCanvas = GetComponent<Canvas>();
        selfCanvas.overrideSorting = true;
        selfCanvas.sortingOrder = 999;
    }

    void Start()
    {
        Cursor.visible = false; // 시스템 커서 숨기기
    }

    void Update()
    {
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            rootCanvas.transform as RectTransform,
            Input.mousePosition,
            rootCanvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : rootCanvas.worldCamera,
            out Vector2 localPoint
        );

        rectTransform.localPosition = localPoint + offset;

        if (isBlocked)
        {
            SetMouseState(MouseState.Blocked);
        }
        else
        {
            if (Input.GetMouseButtonDown(0))
            {
                SetMouseState(MouseState.Click);
            }
            else if (Input.GetMouseButtonUp(0))
            {
                SetMouseState(MouseState.Default);
            }
        }
    }

    private void SetMouseState(MouseState state)
    {
        switch (state)
        {
            case MouseState.Default:
                imageComponent.sprite = mouseSprites[0];
                break;
            case MouseState.Click:
                imageComponent.sprite = mouseSprites[1];
                break;
            case MouseState.Blocked:
                imageComponent.sprite = mouseSprites[2];
                break;
        }
    }

    public void SetBlocked(bool isBlocked)
    {
        this.isBlocked = isBlocked;
        if (!isBlocked) SetMouseState(MouseState.Default);
    }
}

public enum MouseState
{
    Default,
    Click,
    Blocked
}
