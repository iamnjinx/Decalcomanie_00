using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class HintButton : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
{
    public Image HintButtonImage;
    public Sprite[] HintButtonSprites; // 0: idle, 1. pressed

    public event Action OnPressed;
    public event Action OnReleased;

    public void SetHintButtonState(bool isPressed)
    {
        if (isPressed)
        {
            HintButtonImage.sprite = HintButtonSprites[1];
        }
        else
        {
            HintButtonImage.sprite = HintButtonSprites[0];
        }
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        SetHintButtonState(true);
        OnPressed?.Invoke();
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        SetHintButtonState(false);
        OnReleased?.Invoke();
    }

}
