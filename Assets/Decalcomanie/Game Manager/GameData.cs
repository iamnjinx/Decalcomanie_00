using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "GameData", menuName = "ScriptableObjects/GameData", order = 1)]
public class GameData: ScriptableObject
{
    public List<Color>  chapterColors;
    public List<Color> chapterColors2;
}
