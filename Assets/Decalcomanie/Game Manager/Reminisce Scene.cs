using System.Collections;
using System.Collections.Generic;
using Njinx.UI;
using UnityEngine;
using UnityEngine.UI;

public class ReminisceScene : MonoBehaviour
{
    [SerializeField] BaseUI[] reminisceImages;
    [SerializeField] BaseUI fadeUI;

    [SerializeField] private float displayDuration = 3f;

    [SerializeField] ReminisceType reminisceType;

    void Start()
    {
        StartCoroutine(PlayReminisce());
    }

    private IEnumerator PlayReminisce()
    {
        foreach (var img in reminisceImages)
        {
            img.ShowUI();
            yield return new WaitForSeconds(displayDuration);
        }

        LoadFadeScene();
    }

    public async void LoadFadeScene()
    {
        await fadeUI.ShowUI(.5f);

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
