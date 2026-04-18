using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TarodevController;

public class PlayerControllerD : MonoBehaviour
{
    public Action OnKeyObtained;
    public Action OnStarObtained;

    public Action OnCleared;

    public PlayerControllerT tarodevController;

    [SerializeField] private GameObject findKey;
    [SerializeField] private PlayerAnimator playerAnimator;
    [SerializeField] private float fallDuration = 0.4f;
    [SerializeField] private float fallSpinSpeed = 720f;

    private Vector3 _spawnPosition;
    private bool _isFalling;

    void Awake()
    {
        OnCleared += () => tarodevController.ForceStop();
        OnCleared += () => tarodevController.enabled = false;
    }

    void Start()
    {
        _spawnPosition = transform.position;
    }

    void OnTriggerEnter2D(Collider2D collision)
    {
        
        if (collision.gameObject.CompareTag("Obtainables"))
        {
            IObtainable obtainable = collision.gameObject.GetComponent<IObtainable>();
            if (obtainable != null)
            {
                obtainable.OnObtained();

                if (obtainable is KeyController)
                {
                    OnKeyObtained?.Invoke();
                }
                else if (obtainable is StarController)
                {
                    OnStarObtained?.Invoke();
                }
            }
        }
        else if (collision.gameObject.CompareTag("Door"))
        {
            DoorController door = collision.gameObject.GetComponent<DoorController>();
            if (door != null)
            {
                if (door.is_opened)
                {
                    // 클리어
                    OnCleared?.Invoke();
                }
                else
                {
                    findKey.SetActive(true);
                }
            }
        }
        else if (collision.gameObject.CompareTag("Hole"))
        {
            // 구멍에 빠지면 구멍에 빨려들어간 후, 리셋
            if (!_isFalling)
                StartCoroutine(FallIntoHole(collision.transform.position));
        }
    }

    private IEnumerator FallIntoHole(Vector3 holePosition)
    {
        _isFalling = true;
        tarodevController.ForceStop();
        tarodevController.enabled = false;

        Vector3 startPos = transform.position;
        Vector3 startScale = transform.localScale;
        float elapsed = 0f;

        playerAnimator.enabled = false;

        while (elapsed < fallDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / fallDuration;
            transform.position = Vector3.Lerp(startPos, holePosition, t);
            transform.localScale = Vector3.Lerp(startScale, Vector3.zero, t);
            transform.Rotate(0f, 0f, fallSpinSpeed * Time.deltaTime);
            yield return null;
        }

        transform.position = _spawnPosition;
        transform.localScale = startScale;
        transform.localRotation = Quaternion.identity;
        playerAnimator.enabled = true;
        _isFalling = false;
        tarodevController.enabled = true;
    }

    void OnTriggerExit2D(Collider2D collision)
    {
        if (collision.gameObject.CompareTag("Door"))
        {
            findKey.SetActive(false);
        }
    }
}
