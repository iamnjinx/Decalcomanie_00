using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class HintSpace3 : HintSpace
{
    [SerializeField] private Image hintImage;
    [SerializeField] private Sprite unlockedHintSprite;

    public void SetHintImage(Sprite hintSprite)
    {
        hintImage.sprite = hintSprite;
    }
}
