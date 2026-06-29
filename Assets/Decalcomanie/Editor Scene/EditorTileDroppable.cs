using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class EditorTileDroppable : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
{
    private int _id;
    public TileType tileType;
    //Fixed, Hole, Start, End, Star, Key
    public GameObject[] tileObjects = new GameObject[6];

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

    public void OnPointerClick(PointerEventData eventData)
    {
        Debug.Log($"Tile {_id} clicked");
        ResetDroppable();
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

    public void ResetDroppable()
    {
        if (tileType == TileType.Empty) return;
        
        tileObjects[(int)tileType-3].SetActive(false);
        tileType = default;
        EditorManager.Instance.SetBoardTile(_id, tileType);
    }
}
