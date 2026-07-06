using UnityEngine;
using UnityEngine.UI;

public class AlphaRaycastFilter : MonoBehaviour, ICanvasRaycastFilter
{
    private Image m_image;
    private Sprite m_sprite;

    [Range(0, 1)]
    public float alphaThreshold = 0.1f;

    void Awake()
    {
        m_image = GetComponent<Image>();
    }

    public bool IsRaycastLocationValid(Vector2 screenPoint, Camera eventCamera)
    {
        m_sprite = m_image.sprite;
        if (m_sprite == null) return true;
        if (!m_sprite.texture.isReadable) return true;

        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            m_image.rectTransform, screenPoint, eventCamera, out Vector2 localPoint);

        var rect = m_image.rectTransform.rect;

        float u = Mathf.Clamp01((localPoint.x - rect.x) / rect.width);
        float v = Mathf.Clamp01((localPoint.y - rect.y) / rect.height);

        // sprite.rect(=textureRect)는 알파 트리밍된 좁은 영역이라 RectTransform 전체 비율을
        // 여기에 다시 매핑하면 항상 트리밍 박스 안쪽(대부분 불투명)만 샘플링하게 된다.
        // 이 스프라이트는 아틀라스로 패킹되지 않고 텍스처 전체를 그대로 쓰므로 텍스처 크기 기준으로 샘플링한다.
        int tx = Mathf.Clamp((int)(u * m_sprite.texture.width), 0, m_sprite.texture.width - 1);
        int ty = Mathf.Clamp((int)(v * m_sprite.texture.height), 0, m_sprite.texture.height - 1);

        float alpha = m_sprite.texture.GetPixel(tx, ty).a;
        return alpha >= alphaThreshold;
    }
}