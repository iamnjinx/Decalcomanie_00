using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "HintData", menuName = "ScriptableObjects/HintData", order = 1)]
public class HintData : ScriptableObject
{
    public List<HintElement> hintElements = new List<HintElement>();
}

[System.Serializable]
public class HintElement
{
    public HintPos Hint1Pos;
    public int Hint1Num;

    public HintPos Hint2Pos;
    public Sprite Hint2Sprite;

    public Sprite Hint3Sprite;
}

public enum HintPos
{
    UR,LL
}