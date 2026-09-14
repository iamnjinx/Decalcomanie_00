/// <summary>
/// 스테이지 진행 모드. Paint(색칠) → Platformer(플레이) → End(클리어) 순으로 오갑니다.
/// </summary>
public enum GameState
{
    Paint, Platformer, End
}

/// <summary>
/// 한 스테이지에서 달성한 목표 3종. 클리어 시점에 한 번 계산되어 저장/UI로 흘러갑니다.
/// </summary>
public readonly struct Achievements
{
    public bool IsCleared { get; }
    public bool ObtainedStar { get; }
    public bool MinMoves { get; }

    /// 아직 아무것도 달성하지 않은 상태.
    public static Achievements None => default;

    public bool IsAllAchieved => IsCleared && ObtainedStar && MinMoves;

    public Achievements(bool isCleared, bool obtainedStar, bool minMoves)
    {
        IsCleared = isCleared;
        ObtainedStar = obtainedStar;
        MinMoves = minMoves;
    }
}
