using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FlipShadow : MonoBehaviour
{
    public RectTransform tile;
    public CanvasGroup shadowGroup;

    void Update()
    {
        // localEulerAngles는 짐벌락 근처에서 축이 뒤섞여 불안정하게 분해되므로,
        // 쿼터니언 각도 차이(0도 기준)를 직접 사용한다.
        float angleFromFlat = Quaternion.Angle(tile.localRotation, Quaternion.identity);
        // 0도/180도일 때 1, 90도일 때 0에 가까워지게
        float visibility = Mathf.Abs(Mathf.Cos(angleFromFlat * Mathf.Deg2Rad));
        shadowGroup.alpha = 0.4f * visibility;
    }
}