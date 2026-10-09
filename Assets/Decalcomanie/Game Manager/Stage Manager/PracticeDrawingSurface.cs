using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// 연습 모드에서 마우스 좌클릭 드래그로 선을 그리는 UI 그래픽입니다.
/// 획은 로컬 좌표의 점 목록으로 들고 있다가, OnPopulateMesh에서 두께 있는 선 메쉬로 만듭니다.
/// 선 색은 Graphic의 Color 필드를 그대로 씁니다.
/// </summary>
[RequireComponent(typeof(CanvasRenderer))]
public class PracticeDrawingSurface : MaskableGraphic,
    IInitializePotentialDragHandler, IPointerDownHandler, IDragHandler, IPointerUpHandler
{
    [SerializeField] private float lineWidth = 8f;
    [SerializeField] private float minPointDistance = 3f; // 이보다 가까운 점은 버려서 정점 수를 아낍니다.
    [SerializeField] private int capSegments = 8;         // 점마다 둥근 캡을 그려 선 이음새를 메웁니다.

    private readonly List<List<Vector2>> strokes = new List<List<Vector2>>();
    private List<Vector2> currentStroke;
    private int activePointerId;

    // 컴포넌트를 끄면 화면에서 가려지고 클릭도 받지 않지만, 그린 획은 남아 있다가 다시 켜면 그대로 보입니다.
    protected override void OnDisable()
    {
        base.OnDisable();
        EndStroke();
    }

    #region Public

    public void EndStroke()
    {
        currentStroke = null;
    }

    public void Clear()
    {
        strokes.Clear();
        currentStroke = null;
        SetVerticesDirty();
    }

    #endregion

    #region Pointer

    // 드래그 임계값 때문에 첫 구간이 끊겨 보이지 않도록, 누르자마자 드래그로 받습니다.
    public void OnInitializePotentialDrag(PointerEventData eventData)
    {
        eventData.useDragThreshold = false;
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (eventData.button != PointerEventData.InputButton.Left) return;
        if (currentStroke != null) return; // 이미 다른 포인터로 그리는 중
        if (!TryGetLocalPoint(eventData, out Vector2 point)) return;

        activePointerId = eventData.pointerId;
        currentStroke = new List<Vector2> { point };
        strokes.Add(currentStroke);
        SetVerticesDirty();
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (currentStroke == null || eventData.pointerId != activePointerId) return;
        if (!TryGetLocalPoint(eventData, out Vector2 point)) return;

        Vector2 last = currentStroke[currentStroke.Count - 1];
        if ((point - last).sqrMagnitude < minPointDistance * minPointDistance) return;

        currentStroke.Add(point);
        SetVerticesDirty();
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (eventData.pointerId != activePointerId) return;
        EndStroke();
    }

    private bool TryGetLocalPoint(PointerEventData eventData, out Vector2 point)
    {
        return RectTransformUtility.ScreenPointToLocalPointInRectangle(
            rectTransform, eventData.position, eventData.pressEventCamera, out point);
    }

    #endregion

    #region Mesh

    protected override void OnPopulateMesh(VertexHelper vh)
    {
        vh.Clear();
        float halfWidth = lineWidth * 0.5f;

        foreach (List<Vector2> stroke in strokes)
        {
            for (int i = 0; i < stroke.Count; i++)
            {
                AddCap(vh, stroke[i], halfWidth);
                if (i > 0) AddSegment(vh, stroke[i - 1], stroke[i], halfWidth);
            }
        }
    }

    private void AddSegment(VertexHelper vh, Vector2 from, Vector2 to, float halfWidth)
    {
        Vector2 dir = to - from;
        Vector2 normal = new Vector2(-dir.y, dir.x).normalized * halfWidth;

        int start = vh.currentVertCount;
        vh.AddVert(from - normal, color, Vector2.zero);
        vh.AddVert(from + normal, color, Vector2.zero);
        vh.AddVert(to + normal, color, Vector2.zero);
        vh.AddVert(to - normal, color, Vector2.zero);
        vh.AddTriangle(start, start + 1, start + 2);
        vh.AddTriangle(start, start + 2, start + 3);
    }

    private void AddCap(VertexHelper vh, Vector2 center, float radius)
    {
        int start = vh.currentVertCount;
        vh.AddVert(center, color, Vector2.zero);

        for (int i = 0; i < capSegments; i++)
        {
            float angle = i * Mathf.PI * 2f / capSegments;
            vh.AddVert(center + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius, color, Vector2.zero);
        }

        for (int i = 0; i < capSegments; i++)
            vh.AddTriangle(start, start + 1 + i, start + 1 + (i + 1) % capSegments);
    }

    #endregion
}
