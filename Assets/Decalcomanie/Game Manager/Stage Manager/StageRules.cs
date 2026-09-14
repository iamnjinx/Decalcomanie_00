using UnityEngine;

/// <summary>
/// 스테이지 인덱스만으로 결정되는 초반부 특수 규칙(디자인상 고정값)을 한곳에 모아 둡니다.
/// StageManager / StageUI / HintManager가 같은 숫자를 각자 들고 있다가 어긋나는 것을 막기 위한 단일 출처입니다.
/// </summary>
public static class StageRules
{
    private const int PlatformerOnlyBelowStage = 2; // 이 인덱스 미만은 페인트 없이 플랫포머만
    private const int EarlyStageMaxIndex = 3;       // 이 인덱스 이하는 별/최소이동 자동 달성
    private const int GuideStageCount = 3;          // 이 인덱스 미만만 가이드 이미지 사용 (1-4부터는 불필요)
    private const int GuidePaintStageIndex = 2;     // 1-3은 Paint에서 노출, 1-1~1-2는 Platformer에서 노출

    private const int SwitchUnlockStage = 3;        // 1-4부터 Switch 버튼 등장
    private const int ResetUnlockStage = 4;         // 1-5부터 Reset 버튼 등장
    private const int FoldUnlockStage = 4;          // 1-5부터 접기 버튼 등장
    private const int UsedTileCountUnlockStage = 4; // 1-5부터 현재 색칠 수 UI 등장
    private const int HintUnlockStage = 5;          // 1-6부터 힌트 버튼 등장

    public const int SwitchTutorialStageIndex = 2;  // 1-3: 특정 타일 클릭 시 Switch 조기 활성화
    public const int FoldTutorialStageIndex = 3;    // 1-4: 타일 클릭 시 접기 조기 활성화, 접기 시 Reset 조기 활성화

    // BoardData.FromJson이 사방 1칸을 벽으로 두르기 때문에, 원본 스테이지 JSON 좌표(4,1)에서 (+1,+1) 밀린 값입니다.
    public static readonly Vector2Int SwitchTutorialTileCoord = new Vector2Int(5, 2);

    public static bool IsPlatformerOnly(int stageIndex) => stageIndex < PlatformerOnlyBelowStage;
    public static bool IsEarlyStage(int stageIndex) => stageIndex <= EarlyStageMaxIndex;
    public static bool HasGuideImage(int stageIndex) => stageIndex < GuideStageCount;
    public static bool IsGuideShownInPaint(int stageIndex) => stageIndex == GuidePaintStageIndex;

    public static bool IsSwitchUnlocked(int stageIndex) => stageIndex >= SwitchUnlockStage;
    public static bool IsResetUnlocked(int stageIndex) => stageIndex >= ResetUnlockStage;
    public static bool IsFoldUnlocked(int stageIndex) => stageIndex >= FoldUnlockStage;
    public static bool IsUsedTileCountShown(int stageIndex) => stageIndex >= UsedTileCountUnlockStage;
    public static bool IsHintAvailable(int stageIndex) => stageIndex >= HintUnlockStage;

    public static bool IsSwitchTutorialStage(int stageIndex) => stageIndex == SwitchTutorialStageIndex;
    public static bool IsFoldTutorialStage(int stageIndex) => stageIndex == FoldTutorialStageIndex;
}
