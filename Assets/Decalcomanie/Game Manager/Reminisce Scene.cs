using System.Collections;
using Cysharp.Threading.Tasks;
using Njinx.UI;
using UnityEngine;

public class ReminisceScene : MonoBehaviour
{
    [SerializeField] BaseUI[] reminisceImages;
    [SerializeField] BaseUI fadeUI;

    [SerializeField] private float displayDuration = 3f;

    [SerializeField] ReminisceType reminisceType;

    [SerializeField] private float skipHoldDuration = 1f;

    private Coroutine playRoutine;
    private float spaceHoldTime;
    private bool skipped;

    void Start()
    {
        playRoutine = StartCoroutine(PlayReminisce());
    }

    void Update()
    {
        if (skipped) return;

        if (Input.GetKey(KeyCode.Space))
        {
            spaceHoldTime += Time.deltaTime;
            if (spaceHoldTime >= skipHoldDuration)
                Skip();
        }
        else
        {
            spaceHoldTime = 0f;
        }
    }

    private void Skip()
    {
        skipped = true;

        if (playRoutine != null)
        {
            StopCoroutine(playRoutine);
            playRoutine = null;
        }

        foreach (var img in reminisceImages)
            img.HideUI(1f).Forget();

        LoadFadeScene();
    }

    private IEnumerator PlayReminisce()
    {
        foreach (var img in reminisceImages)
        {
            img.ShowUI(1f).Forget();
            yield return new WaitForSeconds(displayDuration);
            img.HideUI(1f).Forget();
        }

        LoadFadeScene();
    }

    public async void LoadFadeScene()
    {
        await fadeUI.ShowUI(.5f);

        await UniTask.Delay(1000);

        if (reminisceType == ReminisceType.Intro)
            GameManager.Instance.LoadStage(0);
        else if (reminisceType == ReminisceType.Outro)
            GameManager.Instance.LoadCreditScene();
    }
}

public enum ReminisceType
{
    Intro, Outro
}
