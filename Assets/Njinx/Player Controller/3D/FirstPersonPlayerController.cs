using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class FirstPersonPlayerController : PlayerController
{
    public Transform orientation;
    private CharacterController characterController;

    private Vector3 currentVelocity;
    private float verticalVelocity;

    [Header("이동 반응성")]
    public float acceleration = 60f;
    public float deceleration = 70f;

    [Header("접지")]
    public float groundStickForce = -8f;

    void Awake()
    {
        characterController = GetComponent<CharacterController>();
    }

    protected override void Update()
    {
        base.Update();
    }

    public override void Move()
    {
        // 접지 처리
        if (characterController.isGrounded && verticalVelocity < 0f)
            verticalVelocity = groundStickForce;

        // 중력 적용
        verticalVelocity += Physics.gravity.y * Time.deltaTime;

        // 입력 방향 계산
        Vector3 inputDir = orientation.forward * moveInput.y + orientation.right * moveInput.x;

        // 목표 속도
        Vector3 targetVelocity = inputDir.normalized * moveSpeed;

        // 입력 유무에 따라 가속/감속 전환
        float factor = (inputDir.sqrMagnitude > 0.01f) ? acceleration : deceleration;
        currentVelocity = Vector3.MoveTowards(currentVelocity, targetVelocity, factor * Time.deltaTime);

        // 최종 이동
        Vector3 finalVelocity = currentVelocity + Vector3.up * verticalVelocity;
        characterController.Move(finalVelocity * Time.deltaTime);
    }
}