using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlatformerManager : MonoBehaviour
{
    [SerializeField] BoardManager boardManager;

    [SerializeField] Transform PlatformerObjectParent;
    [SerializeField] GameObject OrgamiObj;

    [SerializeField] private Obtainables starPrefab;
    [SerializeField] private Obtainables keyPrefab;
    [SerializeField] private HoleController holePrefab;
    [SerializeField] private DoorController doorPrefab;
    [SerializeField] private PlayerControllerD playerPrefab;
    [SerializeField] private PaintedController tilePrefab;


    private Obtainables star;
    private Obtainables key;
    private DoorController door;
    private PlayerControllerD player;

    public bool obtainedStar = false;

    public Action OnCleared;

    public void SetPlatformerObjects(Board board)
    {
        OrgamiObj.SetActive(true);
        InstantiateObjects();

        for(int i = 0; i < board.allTiles.Length; i++)
        {
            if(board.allTiles[i].type == TileType.Paint || board.allTiles[i].type == TileType.Paint_Dec || board.allTiles[i].type == TileType.Fixed)
            {
                PaintedController tile = Instantiate(tilePrefab, PlatformerObjectParent);

                tile.transform.position = GetTilePosition(i);
                tile.SetPaintSprite(board.allTiles[i], board, i);
            }
            else if(board.allTiles[i].type == TileType.Star)
            {
                star.transform.position = GetTilePosition(i);
            }
            else if(board.allTiles[i].type == TileType.Key)
            {
                key.transform.position = GetTilePosition(i);
            }
            else if(board.allTiles[i].type == TileType.End)
            {
                door.transform.position = GetTilePosition(i);
            }
            else if(board.allTiles[i].type == TileType.Start)
            {
                player.transform.position = GetTilePosition(i);
            }
            else if(board.allTiles[i].type == TileType.Hole)
            {
                HoleController hole = Instantiate(holePrefab, PlatformerObjectParent);
                hole.transform.position = GetTilePosition(i);
            }
        }
    }

    private void InstantiateObjects()
    {
        star = Instantiate(starPrefab, PlatformerObjectParent);
        key = Instantiate(keyPrefab, PlatformerObjectParent);
        door = Instantiate(doorPrefab, PlatformerObjectParent);
        player = Instantiate(playerPrefab, PlatformerObjectParent);

        player.OnKeyObtained += () => OnKeyObtained();
        player.OnStarObtained += () => OnStarObtained();
        player.OnCleared += () => OnCleared?.Invoke();
    }

    public void ResetObjects()
    {
        foreach(Transform child in PlatformerObjectParent)
        {
            Destroy(child.gameObject);
        }

        star = null;
        key = null;
        door = null;
        player = null;

        obtainedStar = false;
        OnCleared = null;

        OrgamiObj.SetActive(false);
    }

    private Vector3 GetTilePosition(int index)
    {
        return new Vector3(index % boardManager.CurBoard.size, index / boardManager.CurBoard.size, 0) * 2.5f;
    }

    private void OnKeyObtained()
    {
        door.OpenDoor();
    }

    private void OnStarObtained()
    {
        obtainedStar = true;
    }
}
