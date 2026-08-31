using System;
using System.Collections.Generic;
using UnityEngine;

public class PaintedController : MonoBehaviour
{
    [SerializeField] List<Sprite> paintSprites;
    [SerializeField] private List<Sprite> decPaintSprites;
    [SerializeField] private Sprite fixedTileSprite;
    [SerializeField] private Sprite wallTileSprite;
    [SerializeField] private Sprite holeSprite;

    public int TileID { get; private set; }
    public Tile Tile { get; private set; }

    public event Action<int> OnTilePainted;

    private SpriteRenderer sr;
    private bool flipX;
    private bool flipY;

    private void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
    }

    public void Init(Tile tile, int tileID, Board board)
    {
        Tile = tile;
        TileID = tileID;
        (flipX, flipY) = board.GetFlips(tileID);

        tile.OnTileTypeChanged += Refresh;

        Refresh();
    }

    private void OnDestroy()
    {
        if (Tile != null) Tile.OnTileTypeChanged -= Refresh;
    }

    public void PaintTile()
    {
        if (Tile.type != TileType.Empty) return;

        OnTilePainted?.Invoke(TileID);
        ChangeTileType(1);
    }

    public void ChangeTileType(int tileTypeID)
    {
        Tile.ChangeTileType((TileType)tileTypeID);
    }

    public void Refresh()
    {
        switch (Tile.type)
        {
            case TileType.Empty:
                sr.enabled = false;
                return;
            case TileType.Paint:
            case TileType.Paint_Dec:
                sr.enabled = true;
                sr.sprite = Tile.type == TileType.Paint_Dec ? decPaintSprites[Tile.paint_color_id] : paintSprites[Tile.paint_color_id];
                sr.flipX = flipX;
                sr.flipY = flipY;
                return;
            case TileType.Fixed:
                sr.enabled = true;
                sr.sprite = fixedTileSprite;
                return;
            case TileType.Wall:
                sr.enabled = true;
                sr.sprite = wallTileSprite;
                return;
            case TileType.Hole:
                sr.enabled = true;
                sr.sprite = holeSprite;
                return;
            default:
                sr.enabled = false;
                return;
        }
    }
}
