using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TileUI : MonoBehaviour
{
    [SerializeField] List<GameObject> tileTypeObjects;

    [SerializeField] List<Sprite> paint_Tile_Sprites;
    [SerializeField] List<Sprite> paint_dec_Tile_Sprites;

    [SerializeField] SpriteRenderer paint_SpriteRenderer;

    [SerializeField] List<Animator> tileTypeAnimators;

    private int lastTileTypeID = 0;

    public void UpdateTileUI(int tileTypeID, int paintColorID = -1)
    {
        if(lastTileTypeID != 0)
        {
            tileTypeObjects[lastTileTypeID].SetActive(false);
        }

        if(tileTypeID == 0)
        {
            return;
        }
        
        tileTypeObjects[tileTypeID].SetActive(true);
        lastTileTypeID = tileTypeID;

        if(tileTypeID == 1)
        {
            paint_SpriteRenderer.sprite = paint_Tile_Sprites[paintColorID];
        }
        else if(tileTypeID == 2)
        {
            paint_SpriteRenderer.sprite = paint_dec_Tile_Sprites[paintColorID];
        }
    }

    public void SetPaintFlip(bool flipX, bool flipY)
    {
        paint_SpriteRenderer.flipX = flipX;
        paint_SpriteRenderer.flipY = flipY;
    }

    public void ShowTile()
    {
        gameObject.SetActive(true);
        SetTileTypeAnimatorsEnabled(true);
    }

    public void HideTile()
    {
        gameObject.SetActive(false);
        SetTileTypeAnimatorsEnabled(false);
    }

    private void SetTileTypeAnimatorsEnabled(bool isEnabled)
    {
        foreach (Animator animator in tileTypeAnimators)
        {
            if (animator != null)
                animator.enabled = isEnabled;
        }
    }
}
