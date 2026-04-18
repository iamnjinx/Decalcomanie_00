using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class TopViewController : BasePlayerController2D
{
    [Header("회전")]
    [SerializeField] private bool enableMouseRotation = true;

    private Camera mainCamera;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.gravityScale = 0f;
        rb.freezeRotation = true;
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;

        mainCamera = Camera.main;
    }

    protected override void Update()
    {
        base.Update();

        if (enableMouseRotation)
        {
            RotateTowardsMouse();
        }
    }
    
    void RotateTowardsMouse()
    {
        Vector3 mouseWorldPos = mainCamera.ScreenToWorldPoint(Input.mousePosition);
        Vector2 direction = (mouseWorldPos - transform.position).normalized;

        float targetAngle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        float smoothAngle = Mathf.LerpAngle(
            rb.rotation,
            targetAngle,
            rotationSpeed * Time.deltaTime
        );

        rb.rotation = smoothAngle;
    }

    public void SetMouseRotation(bool enabled)
    {
        enableMouseRotation = enabled;
    }

    public void ToggleMouseRotation()
    {
        enableMouseRotation = !enableMouseRotation;
    }
}
