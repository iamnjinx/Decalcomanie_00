using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class EditorTileDroppable : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
{
    private const int CurtainObjectIndex = 6;

    private int _id;
    public TileType tileType;
    //Fixed, Hole, Start, End, Star, Key, Curtain
    // 0~5는 tileType과 1:1로 대응하지만(TileType.Fixed - 3 = 0), 6번 Curtain은 tileType과 무관한 별개의 상태다.
    public GameObject[] tileObjects = new GameObject[7];

    //[SerializeField] private CanvasGroup highlightGroup;

    public void Init(int id)
    {
        _id = id;
    }

    public void HandleDrop(EditorTileDraggable draggable)
    {
        tileType = draggable.tileType;
        EditorManager.Instance.SetBoardTile(_id, tileType);

        switch (tileType)
        {
            case TileType.Fixed:
                tileObjects[0].SetActive(true);
                break;
            case TileType.Hole:
                tileObjects[1].SetActive(true);
                break;
            case TileType.Start:
                tileObjects[2].SetActive(true);
                break;
            case TileType.End:
                tileObjects[3].SetActive(true);
                break;
            case TileType.Star:
                tileObjects[4].SetActive(true);
                break;
            case TileType.Key:
                tileObjects[5].SetActive(true);
                break;
        }
    }

    // 좌클릭: 커튼(칠할 수 없는 영역) 토글 / 우클릭: 놓인 타일 제거
    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData.button == PointerEventData.InputButton.Right)
        {
            ResetDroppable();
            return;
        }

        if (eventData.button != PointerEventData.InputButton.Left) return;

        SetCurtain(!IsCurtain);
    }

    public bool IsCurtain => CurtainObject != null && CurtainObject.activeSelf;

    private GameObject CurtainObject =>
        tileObjects != null && tileObjects.Length > CurtainObjectIndex ? tileObjects[CurtainObjectIndex] : null;

    public void SetCurtain(bool is_curtain)
    {
        GameObject curtain = CurtainObject;
        if (curtain == null)
        {
            Debug.LogWarning($"[EditorTileDroppable] tileObjects[{CurtainObjectIndex}](Curtain)가 할당되지 않았습니다.");
            return;
        }

        curtain.SetActive(is_curtain);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {

    }

    public void OnPointerExit(PointerEventData eventData)
    {
        
    }

    public void RestoreType(TileType type)
    {
        if (type == TileType.Empty) return;
        tileType = type;
        tileObjects[(int)type - 3].SetActive(true);
    }

    // clearCurtain: 보드 전체 초기화처럼 커튼 상태까지 지워야 할 때만 true.
    // (같은 종류의 타일을 다른 칸에 놓아 기존 칸을 비우는 경우에는 커튼을 유지한다)
    public void ResetDroppable(bool clearCurtain = false)
    {
        if (clearCurtain && IsCurtain)
            SetCurtain(false);

        if (tileType == TileType.Empty) return;

        tileObjects[(int)tileType-3].SetActive(false);
        tileType = default;
        EditorManager.Instance.SetBoardTile(_id, tileType);
    }
}
