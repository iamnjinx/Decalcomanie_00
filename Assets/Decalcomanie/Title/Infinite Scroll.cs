using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class InfiniteScroll : MonoBehaviour
{
     [Header("설정")]
    [SerializeField] bool moveLeft = true;

    [SerializeField] List<InfiniteImages> items = new List<InfiniteImages>();

    void Update()
    {
        float dir = moveLeft ? -1f : 1f;

        for (int i = 0; i < items.Count; i++)
        {
            float delta = items[i].speed * Time.deltaTime * dir;

            Vector2 pos = items[i].imageRT.anchoredPosition;
            pos.x += delta;

            if (moveLeft)
            {
                // 왼쪽 경계를 넘으면 → 오른쪽 끝으로
                if (pos.x < items[i].leftBound)
                {
                    pos.x = items[i].rightBound;
                }
            }

            items[i].imageRT.anchoredPosition = pos;
        }
    }
}

[Serializable]
public class InfiniteImages
{
    public RectTransform imageRT;
    public float leftBound;
    public float rightBound;
    public float speed = 200f;
}
