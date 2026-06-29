using System.Collections;
using System.Collections.Generic;
using Njinx.UI;
using UnityEngine;

public class SceneInitiator : MonoBehaviour
{
    [SerializeField] private string bgmName;

    void Start()
    {
        if (!string.IsNullOrEmpty(bgmName) && AudioManager.Instance != null)
            AudioManager.Instance.PlayBGM(bgmName);
    }
}
