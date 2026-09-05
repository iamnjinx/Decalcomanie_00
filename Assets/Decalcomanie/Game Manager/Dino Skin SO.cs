using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "DinoSkinSO", menuName = "ScriptableObjects/DinoSkinSO", order = 1)]
public class DinoSkinSO : ScriptableObject
{
    public Sprite[] dinoSkins;
    public int[] unlockThresholds;
}
