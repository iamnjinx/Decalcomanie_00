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
    private readonly List<HoleController> holes = new List<HoleController>();

    public bool ObtainedStar { get; private set; }

    public event Action OnCleared;
    public event Action OnFellIntoHole;
    public event Action OnStarObtained;

    #region Board objects

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

        player.OnKeyObtained += HandleKeyObtained;
        player.OnStarObtained += HandleStarObtained;
        player.OnCleared += HandleCleared;
        player.OnFellIntoHole += HandleFellIntoHole;

        PlaceBoardObjects(board);

        player.Init(); // 위치가 확정된 뒤에 호출해야 리스폰 위치가 올바르게 잡힌다.

        SetInteractablesActive(false);
    }

    private void PlaceBoardObjects(Board board)
    {
        for (int i = 0; i < board.allTiles.Length; i++)
        {
            Vector3 pos = board.GetWorldPosition(i);

            switch (board.allTiles[i].type)
            {
                case TileType.Star: star.transform.position = pos; break;
                case TileType.Key: key.transform.position = pos; break;
                case TileType.End: door.transform.position = pos; break;
                case TileType.Start: player.transform.position = pos; break;
            }
        }
    }

    private void SetInteractablesActive(bool active)
    {
        star.gameObject.SetActive(active);
        key.gameObject.SetActive(active);
        door.gameObject.SetActive(active);
    }

    // Platformer 모드에 진입할 때마다 호출. Paint 모드용 Hole 타일을 지우고 실제 HoleController로 교체한 뒤,
    // Door/Star/Key를 보이게 하고 Player 조작을 활성화한다.
    public void SetPlatformerObjects()
    {
        paintManager.DestroyHoleTiles();
        SpawnHoles(boardManager.CurrentBoard);

        SetInteractablesActive(true);
        player.Activate();
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

        SetInteractablesActive(false);

        foreach (HoleController hole in holes) Destroy(hole.gameObject);
        holes.Clear();
        paintManager.RecreateHoleTiles();

        ObtainedStar = false;
    }

    #endregion

    #region Holes

    // 큰 덩어리부터 차례로 먹여 나가서, 붙어 있는 구멍을 하나의 큰 프리팹으로 합쳐 보여 준다.
    private static readonly HolePattern[] HolePatterns =
    {
        HolePattern.Square2x2,
        HolePattern.Horizontal1x2,
        HolePattern.Vertical2x1,
        HolePattern.Single,
    };

    private void SpawnHoles(Board board)
    {
        var consumed = new HashSet<int>();
        foreach (HolePattern pattern in HolePatterns)
            SpawnHolesOfPattern(board, pattern, consumed);
    }

    private void SpawnHolesOfPattern(Board board, HolePattern pattern, HashSet<int> consumed)
    {
        for (int origin = 0; origin < board.allTiles.Length; origin++)
        {
            if (!Matches(board, origin, pattern, consumed)) continue;

            foreach (Vector2Int cell in pattern.Cells)
                consumed.Add(ToIndex(board, origin, cell));

            HoleController prefab = pattern.UseLongPrefab ? longHolePrefab : holePrefab;
            HoleController hole = Instantiate(prefab, PlatformerObjectParent);
            hole.transform.position = board.GetWorldPosition(origin) + (Vector3)pattern.CenterOffset * board.tileSpacing;
            if (!Mathf.Approximately(pattern.ScaleMultiplier, 1f)) hole.transform.localScale *= pattern.ScaleMultiplier;
            if (!Mathf.Approximately(pattern.ZRotation, 0f)) hole.transform.Rotate(0f, 0f, pattern.ZRotation);
            holes.Add(hole);
        }
    }

    // 기준 칸이 패턴 모양대로 전부 Hole이고, 아직 다른 패턴에 먹히지 않았는지 확인한다.
    private static bool Matches(Board board, int origin, HolePattern pattern, HashSet<int> consumed)
    {
        int size = board.size;
        int originX = origin % size;
        int originY = origin / size;

        foreach (Vector2Int cell in pattern.Cells)
        {
            int x = originX + cell.x;
            int y = originY + cell.y;
            if (x < 0 || x >= size || y < 0 || y >= size) return false;

            int index = x + y * size;
            if (consumed.Contains(index)) return false;
            if (board.allTiles[index].type != TileType.Hole) return false;
        }

        return true;
    }

    private static int ToIndex(Board board, int origin, Vector2Int cell)
    {
        return origin + cell.x + cell.y * board.size;
    }

    /// 붙어 있는 Hole 타일 묶음을 하나의 프리팹으로 표현하기 위한 배치 규칙.
    private readonly struct HolePattern
    {
        public readonly Vector2Int[] Cells;   // 기준 칸으로부터의 상대 좌표
        public readonly Vector2 CenterOffset; // 묶음의 중심으로 옮기기 위한 보정 (칸 단위)
        public readonly float ZRotation;
        public readonly float ScaleMultiplier;
        public readonly bool UseLongPrefab;

        private HolePattern(Vector2Int[] cells, Vector2 centerOffset, float zRotation, float scaleMultiplier, bool useLongPrefab)
        {
            Cells = cells;
            CenterOffset = centerOffset;
            ZRotation = zRotation;
            ScaleMultiplier = scaleMultiplier;
            UseLongPrefab = useLongPrefab;
        }

        public static HolePattern Square2x2 => new HolePattern(
            new[] { new Vector2Int(0, 0), new Vector2Int(1, 0), new Vector2Int(0, 1), new Vector2Int(1, 1) },
            new Vector2(0.5f, 0.5f), 0f, 2f, false);

        public static HolePattern Horizontal1x2 => new HolePattern(
            new[] { new Vector2Int(0, 0), new Vector2Int(1, 0) },
            new Vector2(0.5f, 0f), 0f, 1f, true);

        public static HolePattern Vertical2x1 => new HolePattern(
            new[] { new Vector2Int(0, 0), new Vector2Int(0, 1) },
            new Vector2(0f, 0.5f), 90f, 1f, true);

        public static HolePattern Single => new HolePattern(
            new[] { new Vector2Int(0, 0) },
            Vector2.zero, 0f, 1f, false);
    }

    #endregion

    #region Player event handlers

    private void HandleKeyObtained() => door.OpenDoor();

    private void HandleStarObtained()
    {
        ObtainedStar = true;
        OnStarObtained?.Invoke();
    }

    private void HandleCleared() => OnCleared?.Invoke();

    private void HandleFellIntoHole() => OnFellIntoHole?.Invoke();

    #endregion
}
