using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameSceneInitiator : SceneInitiator
{
    string[] bgmNames = { "Game Theme 1", "Game Theme 2", "Game Theme 3", "Game Theme 4", "Game Theme 5"};
    override protected void Start()
    {
        base.Start();
        int bgmIndex = GameManager.Instance.CurrentStageIndex / 10;
        if (bgmIndex >= 0 && bgmIndex < bgmNames.Length && !string.IsNullOrEmpty(bgmNames[bgmIndex]) && AudioManager.Instance != null)
            AudioManager.Instance.PlayBGM(bgmNames[bgmIndex]);
    }
}
