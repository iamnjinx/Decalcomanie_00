using UnityEngine;
using UnityEngine.Events;

public class Button3D : MonoBehaviour, IClickable, IDraggable
{
    [Header("Draggable Settings")]
    [SerializeField] private bool _isDraggable = false;
    [SerializeField] private DragAxis _dragAxis = DragAxis.Free;
    [SerializeField] private float _minLimit = float.NegativeInfinity;
    [SerializeField] private float _maxLimit = float.PositiveInfinity;

    public bool IsDraggable { get => _isDraggable; set => _isDraggable = value; }
    public DragAxis DragAxis { get => _dragAxis; set => _dragAxis = value; }
    public float MinLimit { get => _minLimit; set => _minLimit = value; }
    public float MaxLimit { get => _maxLimit; set => _maxLimit = value; }

    public bool is_enabled = true;

    [Header("Events")]

    public UnityEvent onClick;
    public UnityEvent onDragStart;
    public UnityEvent<Vector3> onDrag;
    public UnityEvent onDragEnd;

    public virtual void OnClick() { if (!is_enabled) return; onClick?.Invoke(); Debug.Log($"{gameObject.name} Clicked"); }
    public virtual void OnDragStart() { if (!is_enabled || !IsDraggable) return; onDragStart?.Invoke(); }
    public virtual void OnDrag(Vector3 worldPos) { if (!is_enabled || !IsDraggable) return; onDrag?.Invoke(worldPos); }
    public virtual void OnDragEnd() { if (!is_enabled || !IsDraggable) return; onDragEnd?.Invoke(); }
}
