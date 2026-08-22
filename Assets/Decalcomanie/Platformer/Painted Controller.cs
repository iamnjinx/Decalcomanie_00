using System.Collections.Generic;
using UnityEngine;

public class PaintedController : MonoBehaviour
{
    [SerializeField] List<Sprite> paintSprites;
    [SerializeField] private List<Sprite> decPaintSprites;
    [SerializeField] private Sprite fixedTileSprite;
    [SerializeField] private Sprite wallTileSprite;

    public void SetPaintSprite(Tile tile, Board board, int tileIndex)
    {
        SpriteRenderer sr = GetComponent<SpriteRenderer>();

        if(tile.type == TileType.Fixed)
        {
            sr.sprite = fixedTileSprite;
            return;
        }

        if(tile.type == TileType.Wall)
        {
            sr.sprite = wallTileSprite;
            return;
        }

        sr.sprite = tile.type == TileType.Paint_Dec ? decPaintSprites[tile.paint_color_id] : paintSprites[tile.paint_color_id];

        var (flipX, flipY) = board.GetFlips(tileIndex);
        sr.flipX = flipX;
        sr.flipY = flipY;
    }
}
