using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FlipShadow : MonoBehaviour
{
    public RectTransform tile;
    public CanvasGroup shadowGroup;

    void Update()
    {
        // X축 회전 기준
        float xRot = tile.localEulerAngles.x;
        // 0도/180도일 때 1, 90도일 때 0에 가까워지게
        float visibility = Mathf.Abs(Mathf.Cos(xRot * Mathf.Deg2Rad));
        shadowGroup.alpha = 0.4f * visibility;
    }
}