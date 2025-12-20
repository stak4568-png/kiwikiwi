using UnityEngine;

/// <summary>
/// 튜토리얼 단계 진행 조건
/// </summary>
public enum TutorialProgressCondition
{
    None,               // 즉시 진행
    ClickAnywhere,      // 아무 곳이나 클릭
    ClickTarget,        // 특정 오브젝트 클릭
    PlayCard,           // 카드 플레이
    AttackEnemy,        // 적 공격
    EndTurn,            // 턴 종료
    UseMana,            // 마나 사용
    Delay               // 시간 지연
}

/// <summary>
/// 튜토리얼 단계
/// </summary>
[System.Serializable]
public class TutorialStep
{
    [Header("기본 정보")]
    public string stepId;
    public string title;

    [TextArea(3, 6)]
    public string message;

    [Header("화자 (선택)")]
    public string speakerName;
    public Sprite speakerPortrait;

    [Header("위치")]
    public TutorialPopupPosition popupPosition = TutorialPopupPosition.Center;
    public Vector2 customOffset;

    [Header("하이라이트")]
    [Tooltip("하이라이트할 오브젝트 이름들")]
    public string[] highlightTargets;

    [Header("진행 조건")]
    public TutorialProgressCondition progressCondition = TutorialProgressCondition.ClickAnywhere;
    
    [Tooltip("ClickTarget 조건 시 대상 이름")]
    public string targetObjectName;
    
    [Tooltip("Delay 조건 시 자동 진행 시간")]
    public float autoProgressDelay = 3f;

    [Header("딜레이")]
    public float delayBefore = 0f;
    public float delayAfter = 0.3f;

    [Header("특수 동작")]
    public bool pauseGame = false;
    public bool blockInput = true;
    public bool showArrow = false;
}

public enum TutorialPopupPosition
{
    Center,
    Top,
    Bottom,
    Left,
    Right,
    TopLeft,
    TopRight,
    BottomLeft,
    BottomRight,
    NearTarget
}

/// <summary>
/// 튜토리얼 데이터
/// </summary>
[CreateAssetMenu(fileName = "Tutorial", menuName = "Kiwi Card Game/Tutorial Data")]
public class TutorialData : ScriptableObject
{
    [Header("튜토리얼 정보")]
    public string tutorialId;
    public string tutorialName;

    [Header("단계들")]
    public TutorialStep[] steps;

    [Header("설정")]
    public bool canSkip = true;
    public bool showProgressBar = true;
}

