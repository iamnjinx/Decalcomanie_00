using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class BoardManager : MonoBehaviour
{
    public Board CurrentBoard;

    [SerializeField] private Camera boardCamera;
    [SerializeField] private float cameraPaddingRatio = 0.174f; // 보드 절반 크기 대비 여백 비율. 에디터에서 눈으로 맞추는 값.

    public void CreateBoard(BoardData boardData)
    {
        CurrentBoard = new Board(boardData);
        FitCameraToBoard();
    }

    public void CreateBoard(TextAsset boardDataTextAsset)
    {
        CreateBoard(BoardData.FromJson(boardDataTextAsset));
    }

    private void FitCameraToBoard()
    {
        if (boardCamera == null) return;

        int half = CurrentBoard.playableSize / 2;
        float boardHalfExtent = half * CurrentBoard.tileSpacing;
        float halfExtent = boardHalfExtent * (1f + cameraPaddingRatio);

        float halfFovRad = boardCamera.fieldOfView * Mathf.Deg2Rad * 0.5f;
        float distanceForHeight = halfExtent / Mathf.Tan(halfFovRad);
        float distanceForWidth = halfExtent / (boardCamera.aspect * Mathf.Tan(halfFovRad));
        float distance = Mathf.Max(distanceForHeight, distanceForWidth);

        Vector3 pos = boardCamera.transform.position;
        boardCamera.transform.position = new Vector3(0f, pos.y, -distance);
    }
}

[Serializable]
public class Board
{
    public int size;
    public int playableSize; // 테두리 벽을 뺀, 실제로 플레이 가능한 영역 크기 (카메라 프레이밍 기준)
    public float tileSpacing = 2.5f;
    public List<int> Quadrant1;
    public List<int> Quadrant2;
    public List<int> Quadrant3;
    public List<int> Quadrant4;

    public Tile[] allTiles;

    public BoardData BoardData {get ; private set;}

    public Board(BoardData boardData)
    {
        BoardData = boardData;
        size = boardData.Size;
        playableSize = boardData.PlayableSize > 0 ? boardData.PlayableSize : boardData.Size;

        if(size % 2 != 0)
        {
            Debug.LogError("Size must be even");
            return;
        }

        SetArrays(boardData.Size);

        SetInitialPoints(boardData);

        SetQuadrants();
    }

    private void SetArrays(int size)
    {
        Quadrant1 = new List<int>();
        Quadrant2 = new List<int>();
        Quadrant3 = new List<int>();
        Quadrant4 = new List<int>();

        allTiles = new Tile[size * size];
    }

    private void SetInitialPoints(BoardData boardData)
    {
        for(int i = 0; i < allTiles.Length; i++)
        {
            allTiles[i] = new Tile();
        }

        allTiles[boardData.StartPoint].ChangeTileType(TileType.Start);
        allTiles[boardData.EndPoint].ChangeTileType(TileType.End);
        if (boardData.StarPoint != -1) allTiles[boardData.StarPoint].ChangeTileType(TileType.Star);
        if (boardData.KeyPoint != -1) allTiles[boardData.KeyPoint].ChangeTileType(TileType.Key);

        foreach (int fixedPoint in boardData.FixedPoints)
        {
            allTiles[fixedPoint].ChangeTileType(TileType.Fixed);
        }

        foreach (int holePoint in boardData.HolePoints)
        {
            allTiles[holePoint].ChangeTileType(TileType.Hole);
        }

        foreach (int wallPoint in boardData.WallPoints)
        {
            allTiles[wallPoint].ChangeTileType(TileType.Wall);
        }
    }

    private void SetQuadrants()
    {
        for(int y1 = size/2; y1 < size; y1++)
        {
            for(int x1 = size/2; x1 < size; x1++)
            {
                Quadrant1.Add(x1 + size * y1);
            }
            for(int x2 = size/2 - 1; x2 >= 0; x2--)
            {
                Quadrant2.Add(x2 + size * y1);
            }
        }

        for(int y2 = size/2 - 1; y2 >= 0; y2--)
        {
            for(int x1 = size/2; x1 < size; x1++)
            {
                Quadrant4.Add(x1 + size * y2);
            }
            for(int x2 = size/2 - 1; x2 >= 0; x2--)
            {
                Quadrant3.Add(x2 + size * y2);
            }
        }
    }

    public List<int> FoldVertical()
    {
        List<int> paintedTiles = new List<int>();
        MirrorPaint(Quadrant1, Quadrant4, paintedTiles);
        MirrorPaint(Quadrant2, Quadrant3, paintedTiles);
        MirrorPaint(Quadrant3, Quadrant2, paintedTiles);
        MirrorPaint(Quadrant4, Quadrant1, paintedTiles);
        return paintedTiles;
    }

    public List<int> FoldHorizontal()
    {
        List<int> paintedTiles = new();
        MirrorPaint(Quadrant1, Quadrant2, paintedTiles);
        MirrorPaint(Quadrant2, Quadrant1, paintedTiles);
        MirrorPaint(Quadrant3, Quadrant4, paintedTiles);
        MirrorPaint(Quadrant4, Quadrant3, paintedTiles);
        return paintedTiles;
    }

    public Vector3 GetWorldPosition(int index)
    {
        int half = size / 2;
        float x = (index % size - half + 0.5f) * tileSpacing;
        float y = (index / size - half + 0.5f) * tileSpacing;
        return new Vector3(x, y, 0);
    }

    // 테두리 벽을 제외한, 실제로 플레이 가능한 영역의 타일 인덱스를 반환합니다.
    public List<int> GetPlayableTileIndices()
    {
        int margin = (size - playableSize) / 2;
        List<int> indices = new List<int>(playableSize * playableSize);
        for (int y = margin; y < size - margin; y++)
        {
            for (int x = margin; x < size - margin; x++)
            {
                indices.Add(x + size * y);
            }
        }
        return indices;
    }

    // 스테이지 JSON과 동일한 1-based (x, y) 좌표계를 flat tile index로 변환합니다.
    public static int CoordToIndex(int x, int y, int size)
    {
        return (x - 1) + size * (y - 1);
    }

    public (bool flipX, bool flipY) GetFlips(int index)
    {
        return (
            Quadrant2.Contains(index) || Quadrant3.Contains(index),
            Quadrant3.Contains(index) || Quadrant4.Contains(index)
        );
    }

    private void MirrorPaint(List<int> source, List<int> target, List<int> paintedTiles)
    {
        for(int i = 0; i < source.Count; i++)
        {
            int tileIndex = source[i];
            if(!allTiles[tileIndex].IsPainted) continue;
            if(!allTiles[target[i]].IsEmpty) continue;
            if(paintedTiles.Contains(tileIndex)) continue;

            allTiles[target[i]].ChangeTileType(TileType.Paint_Dec, allTiles[tileIndex].paint_color_id);
            paintedTiles.Add(target[i]);
        }
    }
}

[Serializable]
public class BoardData
{
    public int Size;
    public int PlayableSize; // 테두리 벽을 뺀 원래 크기. 0이면 Size와 동일하게 취급.

    [Header("Init Points")]
    public int StartPoint = -1;
    public int EndPoint = -1;
    public int StarPoint = -1;
    public int KeyPoint = -1;

    public List<int> FixedPoints = new List<int>();
    public List<int> HolePoints = new List<int>();
    public List<int> WallPoints = new List<int>(); // 테두리 벽 전용 (TileType.Wall)

    [Header("Achivements")]
    public int minMoves = 99;

    public BoardData(int size, Vector2 startPointV, Vector2 endPointV, Vector2 starPointV, Vector2 keyPointV, List<Vector2> fixedPointVs, List<Vector2> holePointVs, List<Vector2> wallPointVs = null)
    {
        Size = size;
        PlayableSize = size;
        StartPoint = GetIndex(startPointV, size);
        EndPoint = GetIndex(endPointV, size);
        StarPoint = GetIndex(starPointV, size);
        KeyPoint = GetIndex(keyPointV, size);
        FixedPoints = fixedPointVs.Select(v => GetIndex(v, size)).ToList();
        HolePoints = holePointVs.Select(v => GetIndex(v, size)).ToList();
        WallPoints = (wallPointVs ?? new List<Vector2>()).Select(v => GetIndex(v, size)).ToList();
    }

    // Deserializes a stage JSON TextAsset (e.g. stage_01.json) into a BoardData.
    // Assign the TextAsset in the Inspector or load it via Resources.Load<TextAsset>("Stage/stage_01").
    // 원본 보드를 그대로 쓰지 않고, 사방 1칸을 Wall로 둘러싼 형태로 로드합니다.
    // 카메라 프레이밍은 벽을 뺀 원래 크기(PlayableSize) 기준으로 유지됩니다.
    public static BoardData FromJson(TextAsset jsonAsset)
    {
        StageJson stageJson = JsonUtility.FromJson<StageJson>(jsonAsset.text);

        int playableSize = stageJson.Size;
        int paddedSize = playableSize + 2;

        // x==0 또는 y==0은 "설정 안 됨" 의미이므로 그대로 유지합니다.
        Vector2 Shift(Vector2 p) => (p.x == 0 || p.y == 0) ? p : new Vector2(p.x + 1, p.y + 1);

        List<Vector2> fixedPoints = stageJson.FixedPoints.Select(Shift).ToList();
        List<Vector2> holePoints = stageJson.HolePoints.Select(Shift).ToList();
        List<Vector2> wallPoints = new List<Vector2>();

        for (int i = 1; i <= paddedSize; i++)
        {
            wallPoints.Add(new Vector2(1, i));
            wallPoints.Add(new Vector2(paddedSize, i));
            wallPoints.Add(new Vector2(i, 1));
            wallPoints.Add(new Vector2(i, paddedSize));
        }

        BoardData boardData = new BoardData(
            paddedSize,
            Shift(stageJson.StartPoint),
            Shift(stageJson.EndPoint),
            Shift(stageJson.StarPoint),
            Shift(stageJson.KeyPoint),
            fixedPoints,
            holePoints,
            wallPoints
        );
        boardData.minMoves = stageJson.MinMoves;
        boardData.PlayableSize = playableSize;
        return boardData;
    }

    private int GetIndex(Vector2 point, int size)
    {
        if ((int)point.x == 0 || (int)point.y == 0) return -1;
        return (int)point.x-1 + size * ((int)point.y-1);
    }

    [Serializable]
    private class StageJson
    {
        public int Size;
        public Vector2 StartPoint;
        public Vector2 EndPoint;
        public Vector2 StarPoint;
        public Vector2 KeyPoint;
        public List<Vector2> FixedPoints;
        public List<Vector2> HolePoints;
        public int MinMoves;
    }
}

[Serializable]
public class Tile
{
    public TileType type;
    public int paint_color_id = -1;
    public bool flipX = false;
    public bool flipY = false;

    public bool IsPainted => type == TileType.Paint || type == TileType.Paint_Dec;
    public bool IsEmpty => type == TileType.Empty;

    public Action OnTileTypeChanged;

    public Tile(TileType type = TileType.Empty)
    {
        this.type = type;
    }

    public void ChangeTileType(TileType newType, int paintColorID = -1)
    {
        type = newType;

        if(newType != TileType.Paint && newType != TileType.Paint_Dec)
        {
            paint_color_id = -1;
            OnTileTypeChanged?.Invoke();
            return;
        }

        if(paintColorID == -1)
        {
            paint_color_id = UnityEngine.Random.Range(0, 5);   
        }
        else
        {
            paint_color_id = paintColorID;
        }

        OnTileTypeChanged?.Invoke();
    }
}

public enum TileType
{
    Empty, Paint, Paint_Dec, Fixed, Hole, Start, End, Star, Key, Wall
}
