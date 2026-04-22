using System.Collections;
using System.Collections.Generic;
using Njinx.UI;
using UnityEngine;
using UnityEngine.UI;

public class ReminisceScene : MonoBehaviour
{
    [SerializeField] BaseUI[] reminisceImages;

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

        if (reminisceType == ReminisceType.Intro)
            GameManager.Instance.LoadTutorialScene();
        else if (reminisceType == ReminisceType.Outro)
            GameManager.Instance.LoadTitleScene();
    }
}

public enum ReminisceType
{
    Intro, Outro
}
