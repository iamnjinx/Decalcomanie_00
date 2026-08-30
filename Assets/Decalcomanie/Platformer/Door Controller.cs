using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DoorController : MonoBehaviour
{
    [SerializeField] SpriteRenderer spriteRenderer;
    public bool is_opened = false;

    [SerializeField] private Sprite openedDoorSprite;

    private Sprite closedDoorSprite;

    void Awake()
    {
        closedDoorSprite = spriteRenderer.sprite;
    }

    public void OpenDoor()
    {
        is_opened = true;

        spriteRenderer.sprite = openedDoorSprite;
    }

    public void ResetDoor()
    {
        is_opened = false;

        spriteRenderer.sprite = closedDoorSprite;
    }
}
