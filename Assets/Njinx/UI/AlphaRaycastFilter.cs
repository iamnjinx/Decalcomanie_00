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

        // 로컬 좌표로 변환
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            m_image.rectTransform, screenPoint, eventCamera, out Vector2 localPoint);

        // 스프라이트 픽셀 좌표로 변환
        var rect = m_image.rectTransform.rect;
        var spriteRect = m_sprite.textureRect;

        float u = (localPoint.x - rect.x) / rect.width;
        float v = (localPoint.y - rect.y) / rect.height;

        int tx = (int)(spriteRect.x + u * spriteRect.width);
        int ty = (int)(spriteRect.y + v * spriteRect.height);

        // 알파값 체크
        float alpha = m_sprite.texture.GetPixel(tx, ty).a;
        return alpha >= alphaThreshold;
    }
}