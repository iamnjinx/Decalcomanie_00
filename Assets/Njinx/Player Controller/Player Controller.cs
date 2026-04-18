using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class PlayerController : MonoBehaviour
{
    public bool can_move = true;

    [Header("이동")]
    [SerializeField] protected float moveSpeed = 7f;

    [Header("회전")]
    [SerializeField] protected float rotationSpeed = 15f;

    protected Vector2 moveInput;

    protected virtual void Update()
    {
        SetMoveInput();
    }

    void SetMoveInput()
    {
        float h = Input.GetAxisRaw("Horizontal");
        float v = Input.GetAxisRaw("Vertical");
        moveInput = new Vector2(h, v).normalized;
    }

    void FixedUpdate()
    {
        if (!can_move) return;
        Move();
    }

    public abstract void Move();
}
