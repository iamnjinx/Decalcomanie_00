using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "GameData", menuName = "ScriptableObjects/GameData", order = 1)]
public class GameData: ScriptableObject
{
    public List<Color>  chapterColors;
    public List<Color> chapterColors2;

    public List<Sprite> chapterBackgroundSprites;

    public List<Sprite> chapterDecoL;
    public List<Sprite> chapterDecoR;

    public List<Sprite> guideSprites;

    public List<Sprite> achievementSprites;
}
