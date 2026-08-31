using System;
using System.Collections.Generic;
using UnityEngine;

public class PlatformerManager : MonoBehaviour
{
    [SerializeField] BoardManager boardManager;
    [SerializeField] private PaintManager paintManager;

    [SerializeField] Transform PlatformerObjectParent;

    [SerializeField] private Obtainables starPrefab;
    [SerializeField] private Obtainables keyPrefab;
    [SerializeField] private HoleController holePrefab;
    [SerializeField] private HoleController longHolePrefab; // 2칸짜리(가로 기본, 세로는 90도 회전해서 사용)
    [SerializeField] private DoorController doorPrefab;
    [SerializeField] private PlayerControllerD playerPrefab;


    private Obtainables star;
    private Obtainables key;
    private DoorController door;
    private PlayerControllerD player;
    private List<HoleController> holes = new List<HoleController>();

    public bool obtainedStar = false;

    public Action OnCleared;
    public Action OnFellIntoHole;
    public Action OnStarObtained;

    // 스테이지 로드 시 1회만 호출. Door/Key/Star/Player는 Platformer 모드에서만 보여야 하므로
    // 모드 전환마다 새로 만들지 않고 여기서 한 번만 만들어 유지하되, 생성 직후에는 숨겨 둔다.
    // Hole은 Paint 모드에서는 PaintManager가 만든 타일 스프라이트로 대신 보여주고,
    // 실제 HoleController는 Platformer 모드에 진입할 때만 만든다.
    public void CreateBoardObjects(Board board)
    {
        star = Instantiate(starPrefab, PlatformerObjectParent);
        key = Instantiate(keyPrefab, PlatformerObjectParent);
        door = Instantiate(doorPrefab, PlatformerObjectParent);
        player = Instantiate(playerPrefab, PlatformerObjectParent);

        player.OnKeyObtained += () => OnKeyObtained();
        player.OnStarObtained += HandleStarObtained;
        player.OnCleared += () => OnCleared?.Invoke();
        player.OnFellIntoHole += () => OnFellIntoHole?.Invoke();

        for(int i = 0; i < board.allTiles.Length; i++)
        {
            Tile tile = board.allTiles[i];
            Vector3 pos = board.GetWorldPosition(i);

            if(tile.type == TileType.Star)       star.transform.position = pos;
            else if(tile.type == TileType.Key)   key.transform.position = pos;
            else if(tile.type == TileType.End)   door.transform.position = pos;
            else if(tile.type == TileType.Start) player.transform.position = pos;
        }

        player.Init(); // 위치가 확정된 뒤에 호출해야 리스폰 위치가 올바르게 잡힌다.

        star.gameObject.SetActive(false);
        key.gameObject.SetActive(false);
        door.gameObject.SetActive(false);
    }

    // Platformer 모드에 진입할 때마다 호출. Paint 모드용 Hole 타일을 지우고 실제 HoleController로 교체한 뒤,
    // Door/Star/Key를 보이게 하고 Player 조작을 활성화한다.
    public void SetPlatformerObjects()
    {
        paintManager.DestroyHoleTiles();
        SpawnHoles(boardManager.CurrentBoard);

        star.gameObject.SetActive(true);
        key.gameObject.SetActive(true);
        door.gameObject.SetActive(true);
        player.Activate();
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
                holes.Add(hole);
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
                holes.Add(hole);
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
                holes.Add(hole);
            }
        }

        for (int i = 0; i < board.allTiles.Length; i++)
        {
            if (consumed.Contains(i) || board.allTiles[i].type != TileType.Hole) continue;
            HoleController hole = Instantiate(holePrefab, PlatformerObjectParent);
            hole.transform.position = board.GetWorldPosition(i);
            holes.Add(hole);
        }
    }

    // Paint 모드로 돌아올 때 호출. Door/Star/Key/Player는 문이 열렸든 뭔가 획득했든
    // 플레이어가 어디까지 갔든 상관없이 처음 상태로 되돌리고 화면에서 숨기며, 실제 HoleController는 지운 뒤
    // Paint 모드용 Hole 타일 오브젝트를 다시 만든다.
    public void ResetObjects()
    {
        door.ResetDoor();
        star.ResetObtainable();
        key.ResetObtainable();
        player.Freeze();

        star.gameObject.SetActive(false);
        key.gameObject.SetActive(false);
        door.gameObject.SetActive(false);

        foreach (HoleController hole in holes) Destroy(hole.gameObject);
        holes.Clear();
        paintManager.RecreateHoleTiles();

        obtainedStar = false;
        OnCleared = null;
        OnFellIntoHole = null;
    }

    private void OnKeyObtained()
    {
        door.OpenDoor();
    }

    private void HandleStarObtained()
    {
        obtainedStar = true;
        OnStarObtained?.Invoke();
    }
}
