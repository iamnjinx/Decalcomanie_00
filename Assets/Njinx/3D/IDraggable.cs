using UnityEngine;

public enum DragAxis { Free, Horizontal, Vertical }

public interface IDraggable
{
    bool IsDraggable { get; set; }
    DragAxis DragAxis { get; set; }
    float MinLimit { get; set; }
    float MaxLimit { get; set; }

    void OnDragStart();
    void OnDrag(Vector3 worldPos);
    void OnDragEnd();
}
