using System.Collections.Generic;
using System.IO;
using Njinx.UI;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class EditorManager : MonoBehaviour
{
    [SerializeField] private ButtonUI quitButton;
    static EditorManager _instance;

    [SerializeField] private EditorTileDroppable droppablePrefab;
    [SerializeField] private Transform droppableParent;

    private List<EditorTileDroppable> droppableTiles = new List<EditorTileDroppable>();

    private TileType[] editorBoard = new TileType[64];

    public static TileType[] SavedBoard;
    // 커튼(칠할 수 없는 영역)은 TileType과 별개라서 따로 넘긴다. SavedBoard와 같은 인덱스다.
    public static bool[] SavedCurtains;
    // EditorPlayScene에서 배경 material에 물릴 인덱스. GameData.chapterBackgroundSprites의 인덱스다.
    public static int SavedBackgroundId;

    public ButtonUI ResetButton;
    public ButtonUI SwitchButton;
    public ButtonUI SaveButton;

    public ButtonUI presetButton1;

    [Header("Adjust Board Size")]
    public VerticalLayoutGroup horizontalLineParent;
    public VerticalLayoutGroup verticalLineParent;
    public GameObject linePrefab;
    public ButtonUI increaseSizeButton;
    public ButtonUI decreaseSizeButton;
    public TextMeshProUGUI curSizeText;

    // 보드(Tile Grid)의 가로/세로 픽셀 크기와 line prefab의 두께.
    // 라인 간격 = boardPixelSize / boardSize - lineThickness (6:127, 8:94, 10:74)
    [SerializeField] private float boardPixelSize = 800f;
    [SerializeField] private float lineThickness = 6f;

    // 커튼 프리셋이 반대쪽 대각선에 깔리면 이 값을 켜서 뒤집는다.
    [SerializeField] private bool flipCurtainPreset;

    private const int MinBoardSize = 2;
    private const int MaxBoardSize = 16;

    private int boardSize = 8;

    [Header("Background")]
    // 배경 목록은 GameData.chapterBackgroundSprites를 그대로 쓴다. 개수는 하드코딩하지 않는다.
    public ButtonUI backgroundButton;
    // 선택한 배경을 에디터에서 확인하기 위한 미리보기(선택 사항).
    [SerializeField] private Image backgroundPreviewImage;

    private int backgroundImageId = 0;

    void Start()
    {
        // SavedBoard는 크기를 따로 들고 있지 않으므로 길이에서 boardSize를 역산한다.
        if (SavedBoard != null)
            boardSize = Mathf.RoundToInt(Mathf.Sqrt(SavedBoard.Length));

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

        ResetButton.OnSingleClick += ResetBoard;
        SwitchButton.OnSingleClick += Switch;
        SaveButton.OnSingleClick += Save;
        increaseSizeButton.OnSingleClick += IncreaseBoardSize;
        decreaseSizeButton.OnSingleClick += DecreaseBoardSize;
        presetButton1.OnSingleClick += SetCurtainAsPreset;

        // 배경 버튼은 씬에 아직 없을 수 있으므로 null을 허용한다.
        if (backgroundButton != null)
            backgroundButton.OnSingleClick += ChangeBackgroundImage;

        // EditorPlayScene에 갔다가 돌아와도 고르던 배경을 유지한다.
        SetBackgroundImage(SavedBackgroundId);

        quitButton.OnSingleClick += () =>
        {
            SavedBoard = null;
            SavedCurtains = null;
            Application.Quit();
        };
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
        if (_instance == this)
            _instance = null;
    }

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

        // 보드 전체 크기는 고정이므로 타일 한 칸의 크기를 boardSize에 맞춰 줄인다.
        var grid = droppableParent.GetComponent<GridLayoutGroup>();
        if (grid != null)
        {
            float cellSize = boardPixelSize / boardSize;
            grid.cellSize = new Vector2(cellSize, cellSize);
        }

        editorBoard = new TileType[boardSize * boardSize];

        for (int i = 0; i < boardSize * boardSize; i++)
        {
            var droppable = Instantiate(droppablePrefab, droppableParent);
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

    private void CreateBoardLines()
    {
        // 칸 하나의 간격에서 라인 두께를 뺀 값이 라인 사이 간격이 된다.
        float spacing = boardPixelSize / boardSize - lineThickness;
        CreateLines(horizontalLineParent, spacing);
        CreateLines(verticalLineParent, spacing);
    }

    private void CreateLines(VerticalLayoutGroup lineParent, float spacing)
    {
        if (lineParent == null || linePrefab == null) return;

        Transform parent = lineParent.transform;
        for (int i = parent.childCount - 1; i >= 0; i--)
            Destroy(parent.GetChild(i).gameObject);

        lineParent.spacing = spacing;

        // 칸이 boardSize개면 라인은 양쪽 끝을 포함해 boardSize + 1개다.
        for (int i = 0; i <= boardSize; i++)
            Instantiate(linePrefab, parent);
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

        CreateDroppableTiles();
        CreateBoardLines();

        if (curSizeText != null)
            curSizeText.text = boardSize.ToString();
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

        if (backgroundPreviewImage != null)
            backgroundPreviewImage.sprite = BackgroundSprites[backgroundImageId];
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
}
