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

    public DinoSkinSO dinoSkinSO;
    public SpriteRenderer dinoSkinRenderer;

    [SerializeField] private GameObject findKey;
    [SerializeField] private PlayerAnimator playerAnimator;
    [SerializeField] private float fallDuration = 0.4f;
    [SerializeField] private float fallSpinSpeed = 720f;

    private static readonly WaitForSeconds WaitForControllerEnable = new(0.1f);

    private Vector3 _respawnPosition;
    private Vector3 _initialScale;
    private bool _isFalling;

    void Awake()
    {
        OnCleared += () => tarodevController.ForceStop();
        OnCleared += () => tarodevController.enabled = false;
    }

    // PlatformerManager가 보드 생성 시 위치를 잡아준 직후 1회 호출한다.
    public void Init()
    {
        _respawnPosition = transform.position;
        _initialScale = transform.localScale;
        Freeze();
    }

    // Paint 모드로 돌아올 때 호출. 위치/모습을 처음 상태로 되돌리고, 조작을 멈춘 채 화면에서 숨긴다.
    // Paint 모드에서는 플레이어 스프라이트를 그대로 보여줄 수 없어서(다른 표현이 필요) 일단 안 보이게 처리한다.
    public void Freeze()
    {
        StopAllCoroutines();
        _isFalling = false;

        transform.position = _respawnPosition;
        transform.localScale = _initialScale;
        transform.localRotation = Quaternion.identity;

        spriteRenderer.sprite = playerSprites[0];
        findKey.SetActive(false);
        playerAnimator.enabled = true;

        tarodevController.ForceStop();
        tarodevController.InputEnabled = false;
        tarodevController.enabled = false;

        gameObject.SetActive(false);
    }

    // Platformer 모드에 진입할 때 호출. 다시 보이게 하고, 잠깐의 딜레이 후 조작이 가능해진다.
    public void Activate()
    {
        gameObject.SetActive(true);

        SetDinoSkin(GameProgressData.Load().skinID);

        tarodevController.enabled = true;
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
            AudioManager.Instance.PlaySFX(UnityEngine.Random.value < 0.05f ? "fall_in_hole_2" : "fall_in_hole");

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

    public void SetDinoSkin(int skinID)
    {
        if (skinID == -1)
        {
            dinoSkinRenderer.enabled = false;
            return;
        }

        if (dinoSkinSO != null && dinoSkinSO.dinoSkins != null && skinID >= 0 && skinID < dinoSkinSO.dinoSkins.Length)
        {
            dinoSkinRenderer.sprite = dinoSkinSO.dinoSkins[skinID];
            dinoSkinRenderer.enabled = true;
        }
    }
}
