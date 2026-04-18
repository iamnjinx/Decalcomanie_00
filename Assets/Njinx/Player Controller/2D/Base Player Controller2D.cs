using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class BasePlayerController2D : PlayerController
{
    protected Rigidbody2D rb;
    
    public override void Move()
    {
        rb.velocity = moveInput * moveSpeed;
    }
}
