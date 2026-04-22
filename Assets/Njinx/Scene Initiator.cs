using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SceneInitiator : MonoBehaviour
{
    [SerializeField] private string bgmName;

    void Start()
    {
        AudioManager.Instance.PlayBGM(bgmName);
    }
}
