using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class BoardManager : MonoBehaviour
{
    public Board CurrentBoard;

    public void CreateBoard(BoardData boardData)
    {
        CurrentBoard = new Board(boardData);
    }

    public void CreateBoard(TextAsset boardDataTextAsset)
    {
        CreateBoard(BoardData.FromJson(boardDataTextAsset));
    }
}

[Serializable]
public class Board
{
    public int size;
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
        allTiles[boardData.StarPoint].ChangeTileType(TileType.Star);
        allTiles[boardData.KeyPoint].ChangeTileType(TileType.Key);

        foreach (int fixedPoint in boardData.FixedPoints)
        {
            allTiles[fixedPoint].ChangeTileType(TileType.Fixed);
        }

        foreach (int holePoint in boardData.HolePoints)
        {
            allTiles[holePoint].ChangeTileType(TileType.Hole);
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

    public Vector3 GetWorldPosition(int index, float tileSpacing = 2.5f)
    {
        return new Vector3(index % size, index / size, 0) * tileSpacing;
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

    [Header("Init Points")]
    public int StartPoint = -1;
    public int EndPoint = -1;
    public int StarPoint = -1;
    public int KeyPoint = -1;

    public List<int> FixedPoints = new List<int>();
    public List<int> HolePoints = new List<int>();

    [Header("Achivements")]
    public int minMoves = 99;

    public BoardData(int size, Vector2 startPointV, Vector2 endPointV, Vector2 starPointV, Vector2 keyPointV, List<Vector2> fixedPointVs, List<Vector2> holePointVs)
    {
        Size = size;
        StartPoint = GetIndex(startPointV, size);
        EndPoint = GetIndex(endPointV, size);
        StarPoint = GetIndex(starPointV, size);
        KeyPoint = GetIndex(keyPointV, size);
        FixedPoints = fixedPointVs.Select(v => GetIndex(v, size)).ToList();
        HolePoints = holePointVs.Select(v => GetIndex(v, size)).ToList();
    }

    // Deserializes a stage JSON TextAsset (e.g. stage_01.json) into a BoardData.
    // Assign the TextAsset in the Inspector or load it via Resources.Load<TextAsset>("Stage/stage_01").
    public static BoardData FromJson(TextAsset jsonAsset)
    {
        StageJson stageJson = JsonUtility.FromJson<StageJson>(jsonAsset.text);
        BoardData boardData = new BoardData(
            stageJson.Size,
            stageJson.StartPoint,
            stageJson.EndPoint,
            stageJson.StarPoint,
            stageJson.KeyPoint,
            stageJson.FixedPoints,
            stageJson.HolePoints
        );
        boardData.minMoves = stageJson.MinMoves;
        return boardData;
    }

    private int GetIndex(Vector2 point, int size)
    {
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
    Empty, Paint, Paint_Dec, Fixed, Hole, Start, End, Star, Key
}
