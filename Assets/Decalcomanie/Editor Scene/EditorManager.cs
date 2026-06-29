using System.Collections.Generic;
using System.IO;
using Njinx.UI;
using UnityEngine;
using UnityEngine.SceneManagement;

public class EditorManager : MonoBehaviour
{
    [SerializeField] private ButtonUI quitButton;
    static EditorManager _instance;

    [SerializeField] private EditorTileDroppable droppablePrefab;
    [SerializeField] private Transform droppableParent;

    private List<EditorTileDroppable> droppableTiles = new List<EditorTileDroppable>();

    private TileType[] editorBoard = new TileType[64];

    public static TileType[] SavedBoard;

    public ButtonUI ResetButton;
    public ButtonUI SwitchButton;
    public ButtonUI SaveButton;


    void Start()
    {
        for (int i = 0; i < 64; i++)
        {
            var droppable = Instantiate(droppablePrefab, droppableParent);
            droppable.Init(i);
            droppableTiles.Add(droppable);
        }

        if (SavedBoard != null)
        {
            SavedBoard.CopyTo(editorBoard, 0);
            for (int i = 0; i < droppableTiles.Count; i++)
                droppableTiles[i].RestoreType(SavedBoard[i]);
        }

        ResetButton.OnSingleClick += ResetBoard;
        SwitchButton.OnSingleClick += Switch;
        SaveButton.OnSingleClick += Save;

        quitButton.OnSingleClick += () =>
        {
            SavedBoard = null;
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

    public void ResetBoard()
    {
        droppableTiles.ForEach(tile => tile.ResetDroppable());
    }

    private void Save()
    {
        const int size = 8;
        var save = new StageJsonSave { Size = size };
        var fixedPoints = new List<Vector2>();
        var holePoints = new List<Vector2>();

        for (int i = 0; i < editorBoard.Length; i++)
        {
            var xy = new Vector2(i % size + 1, i / size + 1);
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
        SceneManager.LoadScene("EditorPlayScene");
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
    public int MinMoves = 0;
}
