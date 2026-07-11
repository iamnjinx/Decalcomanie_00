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
    [SerializeField] private HoleController longHolePrefab; // 2칸짜리(가로 기본, 세로는 90도 회전해서 사용)
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

        // 1x2 (가로로 두 칸)
        for (int i = 0; i < board.allTiles.Length; i++)
        {
            if (consumed.Contains(i) || board.allTiles[i].type != TileType.Hole) continue;

            int x = i % size;
            int right = i + 1;

            bool is1x2 = x < size - 1
                && board.allTiles[right].type == TileType.Hole
                && !consumed.Contains(right);

            if (is1x2)
            {
                consumed.Add(i); consumed.Add(right);
                HoleController hole = Instantiate(longHolePrefab, PlatformerObjectParent);
                hole.transform.position = board.GetWorldPosition(i) + new Vector3(1.25f, 0f, 0f);
            }
        }

        // 2x1 (세로로 두 칸)
        for (int i = 0; i < board.allTiles.Length; i++)
        {
            if (consumed.Contains(i) || board.allTiles[i].type != TileType.Hole) continue;

            int y = i / size;
            int up = i + size;

            bool is2x1 = y < size - 1
                && board.allTiles[up].type == TileType.Hole
                && !consumed.Contains(up);

            if (is2x1)
            {
                consumed.Add(i); consumed.Add(up);
                HoleController hole = Instantiate(longHolePrefab, PlatformerObjectParent);
                hole.transform.position = board.GetWorldPosition(i) + new Vector3(0f, 1.25f, 0f);
                hole.transform.Rotate(0f, 0f, 90f);
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
