using System;
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
    public Action OnFellIntoHole;

    public void SetPlatformerObjects(Board board)
    {   
        OrgamiObj.SetActive(true);
        InstantiateObjects();

        for(int i = 0; i < board.allTiles.Length; i++)
        {
            Tile tile = board.allTiles[i];
            Vector3 pos = board.GetWorldPosition(i);

            if(tile.IsPainted || tile.type == TileType.Fixed)
            {
                PaintedController pc = Instantiate(tilePrefab, PlatformerObjectParent);
                pc.transform.position = pos;
                pc.SetPaintSprite(tile, board, i);
            }
            else if(tile.type == TileType.Star)  star.transform.position = pos;
            else if(tile.type == TileType.Key)   key.transform.position = pos;
            else if(tile.type == TileType.End)   door.transform.position = pos;
            else if(tile.type == TileType.Start) player.transform.position = pos;
        }

        SpawnHoles(board);
    }

    private void SpawnHoles(Board board)
    {
        int size = board.size;
        var consumed = new HashSet<int>();

        for (int i = 0; i < board.allTiles.Length; i++)
        {
            if (consumed.Contains(i) || board.allTiles[i].type != TileType.Hole) continue;

            int x = i % size, y = i / size;
            int right = i + 1, up = i + size, diagUR = i + size + 1;

            bool is2x2 = x < size - 1 && y < size - 1
                && board.allTiles[right].type == TileType.Hole
                && board.allTiles[up].type == TileType.Hole
                && board.allTiles[diagUR].type == TileType.Hole
                && !consumed.Contains(right) && !consumed.Contains(up) && !consumed.Contains(diagUR);

            if (is2x2)
            {
                consumed.Add(i); consumed.Add(right); consumed.Add(up); consumed.Add(diagUR);
                HoleController hole = Instantiate(holePrefab, PlatformerObjectParent);
                hole.transform.position = board.GetWorldPosition(i) + new Vector3(1.25f, 1.25f, 0f);
                hole.transform.localScale *= 2f;
            }
        }

        for (int i = 0; i < board.allTiles.Length; i++)
        {
            if (consumed.Contains(i) || board.allTiles[i].type != TileType.Hole) continue;
            HoleController hole = Instantiate(holePrefab, PlatformerObjectParent);
            hole.transform.position = board.GetWorldPosition(i);
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
        player.OnFellIntoHole += () => OnFellIntoHole?.Invoke();
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
        OnFellIntoHole = null;

        OrgamiObj.SetActive(false);
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
