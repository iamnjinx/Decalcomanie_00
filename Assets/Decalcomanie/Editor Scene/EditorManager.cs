using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;

public class EditorManager : MonoBehaviour
{
    static EditorManager _instance;

    // UI 참조와 뷰 로직은 전부 EditorUI가 갖는다.
    [SerializeField] private EditorUI editorUI;

    [SerializeField] private EditorTileDroppable droppablePrefab;

    private List<EditorTileDroppable> droppableTiles = new List<EditorTileDroppable>();

    private TileType[] editorBoard = new TileType[64];

    public static TileType[] SavedBoard;
    // 커튼(칠할 수 없는 영역)은 TileType과 별개라서 따로 넘긴다. SavedBoard와 같은 인덱스다.
    public static bool[] SavedCurtains;
    // EditorPlayScene에서 배경 material에 물릴 인덱스. GameData.chapterBackgroundSprites의 인덱스다.
    public static int SavedBackgroundId;

    // EditorPlayScene에서 쓸 아이템 개수. Paint UI가 항목 하나당 버튼 하나를 만든다.
    public static int SavedPencilCount;
    public static int SavedEraserCount;

    // 스테이지 JSON의 AvailableItems에 들어가는 이름. Paint UI의 ParseItemMode와 같은 문자열이어야 한다.
    public const string PencilItemName = "pencil";
    public const string EraserItemName = "eraser";

    // 버튼은 종류당 하나뿐이고 개수만 텍스트로 표시되지만, 난이도 상 상한을 둔다.
    [SerializeField] private int maxItemCount = 9;

    private int pencilCount;
    private int eraserCount;

    // 커튼 프리셋이 반대쪽 대각선에 깔리면 이 값을 켜서 뒤집는다.
    [SerializeField] private bool flipCurtainPreset;

    private const int MinBoardSize = 2;
    private const int MaxBoardSize = 16;

    private int boardSize = 8;

    private int backgroundImageId = 0;

    void Start()
    {
        // SavedBoard는 크기를 따로 들고 있지 않으므로 길이에서 boardSize를 역산한다.
        if (SavedBoard != null)
            boardSize = Mathf.RoundToInt(Mathf.Sqrt(SavedBoard.Length));

        // EditorPlayScene에 갔다가 돌아와도 고르던 아이템 개수를 유지한다.
        pencilCount = Mathf.Clamp(SavedPencilCount, 0, maxItemCount);
        eraserCount = Mathf.Clamp(SavedEraserCount, 0, maxItemCount);

        ChangeBoardSize(boardSize);

        if (SavedBoard != null && SavedBoard.Length == editorBoard.Length)
        {
            SavedBoard.CopyTo(editorBoard, 0);
            for (int i = 0; i < droppableTiles.Count; i++)
                droppableTiles[i].RestoreType(SavedBoard[i]);
        }

        if (SavedCurtains != null && SavedCurtains.Length == droppableTiles.Count)
        {
            for (int i = 0; i < droppableTiles.Count; i++)
                droppableTiles[i].SetCurtain(SavedCurtains[i]);
        }

        if (editorUI != null)
        {
            editorUI.OnResetClicked += ResetBoard;
            editorUI.OnSwitchClicked += Switch;
            editorUI.OnSaveClicked += Save;
            editorUI.OnIncreaseSizeClicked += IncreaseBoardSize;
            editorUI.OnDecreaseSizeClicked += DecreaseBoardSize;
            editorUI.OnPresetClicked += SetCurtainAsPreset;
            editorUI.OnBackgroundClicked += ChangeBackgroundImage;
            editorUI.OnQuitClicked += Quit;
            editorUI.OnPencilCountDelta += ChangePencilCount;
            editorUI.OnEraserCountDelta += ChangeEraserCount;

            editorUI.SetItemCountTexts(pencilCount, eraserCount);
        }

        // EditorPlayScene에 갔다가 돌아와도 고르던 배경을 유지한다.
        SetBackgroundImage(SavedBackgroundId);
    }

    public static EditorManager Instance
    {
        get
        {
            if (_instance == null)
                Debug.LogWarning("[EditorManager] Instance is null. Make sure it exists in the scene.");
            return _instance;
        }
    }

    void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }
        _instance = this;
    }

    void OnDestroy()
    {
        if (editorUI != null)
        {
            editorUI.OnResetClicked -= ResetBoard;
            editorUI.OnSwitchClicked -= Switch;
            editorUI.OnSaveClicked -= Save;
            editorUI.OnIncreaseSizeClicked -= IncreaseBoardSize;
            editorUI.OnDecreaseSizeClicked -= DecreaseBoardSize;
            editorUI.OnPresetClicked -= SetCurtainAsPreset;
            editorUI.OnBackgroundClicked -= ChangeBackgroundImage;
            editorUI.OnQuitClicked -= Quit;
            editorUI.OnPencilCountDelta -= ChangePencilCount;
            editorUI.OnEraserCountDelta -= ChangeEraserCount;
        }

        if (_instance == this)
            _instance = null;
    }

    private void Quit()
    {
        SavedBoard = null;
        SavedCurtains = null;
        SavedPencilCount = 0;
        SavedEraserCount = 0;
        Application.Quit();
    }

    #region Items

    public int PencilCount => pencilCount;
    public int EraserCount => eraserCount;

    private void ChangePencilCount(int delta) => SetItemCounts(pencilCount + delta, eraserCount);
    private void ChangeEraserCount(int delta) => SetItemCounts(pencilCount, eraserCount + delta);

    public void SetItemCounts(int pencil, int eraser)
    {
        pencilCount = Mathf.Clamp(pencil, 0, maxItemCount);
        eraserCount = Mathf.Clamp(eraser, 0, maxItemCount);

        if (editorUI != null)
            editorUI.SetItemCountTexts(pencilCount, eraserCount);
    }

    // Paint UI는 항목 하나당 버튼 하나를 만들기 때문에 개수만큼 같은 이름을 반복해 넣는다.
    private static List<string> BuildAvailableItems(int pencil, int eraser)
    {
        var items = new List<string>();
        for (int i = 0; i < pencil; i++) items.Add(PencilItemName);
        for (int i = 0; i < eraser; i++) items.Add(EraserItemName);
        return items;
    }

    // EditorPlayScene의 StageManager가 보드를 만들 때 쓴다.
    public static List<string> BuildSavedAvailableItems()
        => BuildAvailableItems(SavedPencilCount, SavedEraserCount);

    #endregion

    public void SetBoardTile(int id, TileType tileType)
    {
        editorBoard[id] = tileType;

        // 만약 tiletype이 Start, End, Star, Key 중 하나이며, 이미 존재한다면 기존 tiledroppable을 reset한다.
        if (tileType == TileType.Start || tileType == TileType.End || tileType == TileType.Star || tileType == TileType.Key)
        {
            for (int i = 0; i < droppableTiles.Count; i++)
            {
                if (i != id && droppableTiles[i].tileType == tileType)
                {
                    droppableTiles[i].ResetDroppable();
                    break;
                }
            }
        }
    }

    private void CreateDroppableTiles()
    {
        ClearDroppableTiles();

        editorBoard = new TileType[boardSize * boardSize];

        Transform parent = editorUI != null ? editorUI.DroppableParent : null;
        if (parent == null) return;

        for (int i = 0; i < boardSize * boardSize; i++)
        {
            var droppable = Instantiate(droppablePrefab, parent);
            droppable.Init(i);
            droppableTiles.Add(droppable);
        }
    }

    private void ClearDroppableTiles()
    {
        for (int i = 0; i < droppableTiles.Count; i++)
        {
            if (droppableTiles[i] != null)
                Destroy(droppableTiles[i].gameObject);
        }
        droppableTiles.Clear();
    }

    public void ResetBoard()
    {
        droppableTiles.ForEach(tile => tile.ResetDroppable(true));
    }

    private void Save()
    {
        int size = boardSize;
        var save = new StageJsonSave { Size = size };
        var fixedPoints = new List<Vector2>();
        var holePoints = new List<Vector2>();
        var blockedPoints = new List<Vector2>();

        for (int i = 0; i < editorBoard.Length; i++)
        {
            var xy = new Vector2(i % size + 1, i / size + 1);

            // 커튼은 TileType과 별개의 상태라 droppable에서 직접 읽는다.
            if (i < droppableTiles.Count && droppableTiles[i].IsCurtain)
                blockedPoints.Add(xy);

            switch (editorBoard[i])
            {
                case TileType.Start:  save.StartPoint = xy; break;
                case TileType.End:    save.EndPoint   = xy; break;
                case TileType.Star:   save.StarPoint  = xy; break;
                case TileType.Key:    save.KeyPoint   = xy; break;
                case TileType.Fixed:  fixedPoints.Add(xy); break;
                case TileType.Hole:   holePoints.Add(xy);  break;
            }
        }

        save.FixedPoints = fixedPoints;
        save.HolePoints  = holePoints;
        // 커튼이 하나도 없으면 빈 목록으로 저장되고, Board가 기본 규칙(Quadrant2/Quadrant4 차단)으로 폴백한다.
        save.BlockedPoints = blockedPoints;
        save.AvailableItems = BuildAvailableItems(pencilCount, eraserCount);

        string json = JsonUtility.ToJson(save, true);
        string timestamp = System.DateTime.Now.ToString("yyyyMMdd_HHmmss");
        string path = Path.Combine(Application.persistentDataPath, $"stage_{timestamp}.json");
        File.WriteAllText(path, json);
        Debug.Log($"[EditorManager] Saved: {path}");
    }

    private void Switch()
    {
        // 만약 현재 board에 Start, End, Star, Key 중 하나라도 존재하지 않는다면 return
        if (!System.Array.Exists(editorBoard, tile => tile == TileType.Start) ||
            !System.Array.Exists(editorBoard, tile => tile == TileType.End) ||
            !System.Array.Exists(editorBoard, tile => tile == TileType.Star) ||
            !System.Array.Exists(editorBoard, tile => tile == TileType.Key))
        {
            return;
        }

        SavedBoard = (TileType[])editorBoard.Clone();

        SavedCurtains = new bool[droppableTiles.Count];
        for (int i = 0; i < droppableTiles.Count; i++)
            SavedCurtains[i] = droppableTiles[i].IsCurtain;

        SavedBackgroundId = backgroundImageId;

        SavedPencilCount = pencilCount;
        SavedEraserCount = eraserCount;

        SceneManager.LoadScene("EditorPlayScene");
    }

    public void IncreaseBoardSize()
    {
        ChangeBoardSize(boardSize+2);
    }

    public void DecreaseBoardSize()
    {
        ChangeBoardSize(boardSize-2);
    }

    public void ChangeBoardSize(int size)
    {
        ResetBoard();

        size = Mathf.Clamp(size, MinBoardSize, MaxBoardSize);
        boardSize = size % 2 == 0 ? size : size + 1;

        // 그리드 셀 크기가 먼저 정해져야 타일이 올바른 크기로 생성된다.
        if (editorUI != null)
            editorUI.ApplyBoardLayout(boardSize);

        CreateDroppableTiles();
    }

    // 버튼(presetButton1)에 바로 물리기 위한 무인자 버전.
    public void SetCurtainAsPreset() => SetCurtainAsPreset(1);

    public void SetCurtainAsPreset(int presetID)
    {
        // 프리셋을 덮어쓰기 전에 기존 타일과 커튼을 모두 비운다.
        ResetBoard();

        switch (presetID)
        {
            default:
                SetQuadrantCurtain();
                break;
        }
    }

    // Board Manager의 기본 차단 규칙(Quadrant2 / Quadrant4 차단)을 그대로 편집기에 적용한다.
    // 인덱스는 index = x + boardSize * y 이고, x와 y가 둘 다 절반 이상(Q1)이거나
    // 둘 다 절반 미만(Q3)이면 칠할 수 있는 영역, 나머지 대각선 쪽(Q2 / Q4)이 커튼 대상이다.
    private void SetQuadrantCurtain()
    {
        int half = boardSize / 2;

        for (int i = 0; i < droppableTiles.Count; i++)
        {
            int x = i % boardSize;
            int y = i / boardSize;

            bool isBlocked = (x < half) != (y < half);
            if (flipCurtainPreset) isBlocked = !isBlocked;

            droppableTiles[i].SetCurtain(isBlocked);
        }
    }

    // 배경 목록(GameData.chapterBackgroundSprites). GameManager가 없으면 null이 나올 수 있다.
    public static List<Sprite> BackgroundSprites
    {
        get
        {
            var gm = GameManager.Instance;
            if (gm == null || gm.GameData == null) return null;
            return gm.GameData.chapterBackgroundSprites;
        }
    }

    public static int BackgroundCount
    {
        get
        {
            var list = BackgroundSprites;
            return list == null ? 0 : list.Count;
        }
    }

    public int BackgroundImageId => backgroundImageId;

    // 0 -> 1 -> ... -> Count-1 -> 0 순으로 순환한다.
    public void ChangeBackgroundImage()
    {
        SetBackgroundImage(backgroundImageId + 1);
    }

    public void SetBackgroundImage(int id)
    {
        int count = BackgroundCount;
        if (count <= 0)
        {
            backgroundImageId = 0;
            return;
        }

        // 음수로 들어와도 안전하게 [0, count) 안으로 접는다.
        backgroundImageId = ((id % count) + count) % count;

        if (editorUI != null)
            editorUI.SetBackgroundPreview(BackgroundSprites[backgroundImageId]);
    }
}

[System.Serializable]
public class StageJsonSave
{
    public int Size;
    public Vector2 StartPoint;
    public Vector2 EndPoint;
    public Vector2 StarPoint;
    public Vector2 KeyPoint;
    public List<Vector2> FixedPoints = new List<Vector2>();
    public List<Vector2> HolePoints  = new List<Vector2>();
    public List<Vector2> BlockedPoints = new List<Vector2>(); // 비워 두면 기본 규칙(Quadrant2/Quadrant4 차단)이 적용된다.
    public int MinMoves = 0;
    // 개수만큼 이름을 반복해 넣는다. 예: 연필 2 + 지우개 1 -> ["pencil", "pencil", "eraser"]
    public List<string> AvailableItems = new List<string>();
}
