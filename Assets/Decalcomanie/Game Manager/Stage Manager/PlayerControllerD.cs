using System;
using System.Collections;
using UnityEngine;
using TarodevController;

public class PlayerControllerD : MonoBehaviour
{
    public Action OnKeyObtained;
    public Action OnStarObtained;

    public Action OnCleared;
    public Action OnFellIntoHole;

    public PlayerControllerT tarodevController;

    public SpriteRenderer spriteRenderer;
    public Sprite[] playerSprites; // 0: normal, 1: hmm, 2: x, 3: happy

    [SerializeField] private GameObject findKey;
    [SerializeField] private PlayerAnimator playerAnimator;
    [SerializeField] private float fallDuration = 0.4f;
    [SerializeField] private float fallSpinSpeed = 720f;

    private static readonly WaitForSeconds WaitForControllerEnable = new(0.1f);

    private Vector3 _respawnPosition;
    private bool _isFalling;

    void Awake()
    {
        OnCleared += () => tarodevController.ForceStop();
        OnCleared += () => tarodevController.enabled = false;
    }

    void Start()
    {
        _respawnPosition = transform.position;
        tarodevController.InputEnabled = false;
        StartCoroutine(EnableControllerDelayed());
    }

    private IEnumerator EnableControllerDelayed()
    {
        yield return WaitForControllerEnable;
        tarodevController.InputEnabled = true;
    }

    void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.TryGetComponent<IObtainable>(out var obtainable))
        {
            obtainable.OnObtained();
            if (obtainable is KeyController) OnKeyObtained?.Invoke();
            else if (obtainable is StarController) OnStarObtained?.Invoke();
            return;
        }

        if (collision.TryGetComponent<DoorController>(out var door))
        {
            if (door.is_opened) { OnCleared?.Invoke(); spriteRenderer.sprite = playerSprites[3]; }
            else { findKey.SetActive(true); spriteRenderer.sprite = playerSprites[1]; }
            return;
        }

        if (collision.CompareTag("Hole") && !_isFalling)
            StartCoroutine(FallIntoHole(collision.transform.position));
    }

    private IEnumerator FallIntoHole(Vector3 holePosition)
    {
        _isFalling = true;
        tarodevController.ForceStop();
        tarodevController.enabled = false;
        spriteRenderer.sprite = playerSprites[2];

        if (AudioManager.Instance != null)
            AudioManager.Instance.PlaySFX("fall_in_hole");

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

        transform.position = _respawnPosition;
        transform.localScale = startScale;
        transform.localRotation = Quaternion.identity;
        playerAnimator.enabled = true;
        _isFalling = false;
        tarodevController.enabled = true;
        spriteRenderer.sprite = playerSprites[0];
        OnFellIntoHole?.Invoke();
    }

    void OnTriggerExit2D(Collider2D collision)
    {
        if (collision.gameObject.CompareTag("Door"))
        {
            findKey.SetActive(false);
            spriteRenderer.sprite = playerSprites[0];
        }
    }
}
