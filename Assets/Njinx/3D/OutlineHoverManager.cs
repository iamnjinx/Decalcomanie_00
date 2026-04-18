using UnityEngine;
using System.Collections.Generic;

public class OutlineHoverManager : MonoBehaviour
{
    [SerializeField] private LayerMask selectableLayer;
    [SerializeField] private int outlineLayer = 6;

    private Camera _cam;
    private readonly Dictionary<GameObject, int> _highlighted = new(); // 원래 레이어 저장

    void Start()
    {
        _cam = Camera.main;
    }

    private GameObject _dragTarget;
    private IDraggable _dragClickable;
    private float _dragDistance;
    private bool _isDragging;
    private Vector3 _dragOffset;
    private Vector3 _dragStartPos;

    void Update()
    {
        if (_isDragging)
        {
            HandleDrag();
            return;
        }

        HandleHoverAndClick();
    }

    // ===== 드래그 =====

    private void HandleDrag()
    {
        if (Input.GetMouseButton(0))
            UpdateDragPosition();

        if (Input.GetMouseButtonUp(0))
            EndDrag();
    }

    private void UpdateDragPosition()
    {
        Vector3 targetPos = GetMouseWorldPosition(_dragDistance) + _dragOffset;

        if (_dragClickable != null)
        {
            switch (_dragClickable.DragAxis)
            {
                case DragAxis.Horizontal:
                    targetPos.y = _dragStartPos.y;
                    targetPos.z = _dragStartPos.z;
                    targetPos.x = Mathf.Clamp(targetPos.x, _dragClickable.MinLimit, _dragClickable.MaxLimit);
                    break;
                case DragAxis.Vertical:
                    targetPos.x = _dragStartPos.x;
                    targetPos.z = _dragStartPos.z;
                    targetPos.y = Mathf.Clamp(targetPos.y, _dragClickable.MinLimit, _dragClickable.MaxLimit);
                    break;
            }
        }

        _dragTarget.transform.position = targetPos;
        if (_dragClickable != null) _dragClickable.OnDrag(targetPos);
    }

    private void StartDrag(GameObject target)
    {
        _dragTarget = target;
        _dragClickable = target.GetComponent<IDraggable>();
        _dragStartPos = target.transform.position;
        _dragDistance = Vector3.Distance(_cam.transform.position, target.transform.position);
        _dragOffset = target.transform.position - GetMouseWorldPosition(_dragDistance);
        _isDragging = true;

        _dragClickable?.OnDragStart();
        if (target.TryGetComponent(out IClickable clickable)) clickable.OnClick();
    }

    private void EndDrag()
    {
        _isDragging = false;
        if (_dragClickable != null) _dragClickable.OnDragEnd();
        _dragTarget = null;
        _dragClickable = null;
    }

    // ===== 호버 & 클릭 =====

    private void HandleHoverAndClick()
    {
        int combinedMask = selectableLayer | (1 << outlineLayer);
        Ray ray = _cam.ScreenPointToRay(Input.mousePosition);

        if (Physics.Raycast(ray, out RaycastHit hit, 100f, combinedMask))
        {
            GameObject target = hit.collider.gameObject;

            if (Input.GetMouseButtonDown(0))
                TryClick(target);

            UpdateHighlight(target);
        }
        else
        {
            ClearAll();
        }
    }

    private void TryClick(GameObject target)
    {
        var draggable = target.GetComponent<IDraggable>();
        if (draggable != null && draggable.IsDraggable)
        {
            StartDrag(target);
            return;
        }

        if (target.TryGetComponent(out IClickable clickable)) clickable.OnClick();
    }

    private void UpdateHighlight(GameObject target)
    {
        if (!_highlighted.ContainsKey(target))
        {
            ClearAll();
            _highlighted[target] = target.layer;
            SetLayer(target, outlineLayer);
        }
    }

    // ===== 유틸 =====

    private Vector3 GetMouseWorldPosition(float distance)
    {
        return _cam.ScreenToWorldPoint(
            new Vector3(Input.mousePosition.x, Input.mousePosition.y, distance)
        );
    }

    public void AddHighlight(GameObject obj)
    {
        if (!_highlighted.ContainsKey(obj))
        {
            _highlighted[obj] = obj.layer;
            SetLayer(obj, outlineLayer);
        }
    }

    public void ClearAll()
    {
        foreach (var kvp in _highlighted)
        {
            if (kvp.Key != null)
                SetLayer(kvp.Key, kvp.Value); // 저장해둔 원래 레이어로 복원
        }
        _highlighted.Clear();
    }

    private void SetLayer(GameObject obj, int layer)
    {
        obj.layer = layer;
        foreach (Transform child in obj.GetComponentsInChildren<Transform>())
            child.gameObject.layer = layer;
    }
}