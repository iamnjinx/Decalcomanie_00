using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PaintedController : MonoBehaviour
{
    [SerializeField] List<Sprite> paintSprites;
    [SerializeField] List<Sprite> decPaintSprites;

    [SerializeField] Sprite fixedTileSprite;

    public void SetPaintSprite(Tile tile, Board board, int tileIndex)
    {
        int color = tile.paint_color_id;
        SpriteRenderer sr = GetComponent<SpriteRenderer>();

        if(tile.type == TileType.Fixed)
        {
            sr.sprite = fixedTileSprite;
            return;
        }
        
        sr.sprite = tile.type == TileType.Paint_Dec ? decPaintSprites[color] : paintSprites[color];

        // tile이 어떤 quadrant에 있는지에 따라 flipX, flipY 설정
        sr.flipX = board.Quadrant2.Contains(tileIndex) || board.Quadrant3.Contains(tileIndex);
        sr.flipY = board.Quadrant3.Contains(tileIndex) || board.Quadrant4.Contains(tileIndex);
    }
}
