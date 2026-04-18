using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TileController : MonoBehaviour
{
    public int TileID;
    public Tile Tile;
    [SerializeField] TileUI tileUI;

    public Action<int> OnTilePainted;

    public void SetTile(Tile tile, int tileID)
    {
        Tile = tile;
        TileID = tileID;

        tile.OnTileTypeChanged += UpdateTile;

        UpdateTile();
    }

    public void PaintTile()
    {
        if(Tile.type != TileType.Empty)
        {
            return;
        }

        OnTilePainted?.Invoke(TileID);
        ChangeTileType(1);
    }

    public void ChangeTileType(int tileTypeID)
    {
        TileType targetType = (TileType)tileTypeID;
        Tile.ChangeTileType(targetType);
        //UpdateTile();
    }

    public void SetPaintFlip(bool flipX, bool flipY)
    {
        tileUI.SetPaintFlip(flipX, flipY);
    }

    public void ShowTile()
    {
        tileUI.ShowTile();
    }

    public void HideTile()
    {
        tileUI.HideTile();
    }

    public void UpdateTile()
    {
        tileUI.UpdateTileUI((int)Tile.type, Tile.paint_color_id);
    }
}
