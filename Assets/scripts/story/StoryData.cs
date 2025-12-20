using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 스토리 이벤트 데이터
/// VN 스타일의 대화 및 선택지 시스템
/// </summary>
[CreateAssetMenu(fileName = "StoryEvent", menuName = "Kiwi Card Game/Story Event")]
public class StoryData : ScriptableObject
{
    [Header("기본 정보")]
    public string eventId;
    public string eventTitle;
    
    [TextArea(2, 4)]
    public string eventDescription;

    [Header("발동 조건")]
    public StoryTriggerType triggerType = StoryTriggerType.Manual;
    public int triggerFloor = 0;                    // 특정 층에서 발동
    public string triggerAfterEventId;              // 특정 이벤트 후 발동
    public bool repeatable = false;                 // 반복 가능 여부

    [Header("씬 구성")]
    public StoryScene[] scenes;

    [Header("결과")]
    public StoryReward[] rewards;

    [Header("비주얼")]
    public Sprite backgroundImage;
    public AudioClip backgroundMusic;
}

public enum StoryTriggerType
{
    Manual,         // 수동 발동
    OnFloorEnter,   // 특정 층 진입 시
    OnBattleWin,    // 전투 승리 시
    OnBattleLose,   // 전투 패배 시
    OnItemUse,      // 아이템 사용 시
    AfterEvent,     // 특정 이벤트 후
    Random          // 랜덤 발생
}

/// <summary>
/// 스토리 씬 (대화 시퀀스)
/// </summary>
[System.Serializable]
public class StoryScene
{
    [Header("기본")]
    public string sceneId;

    [Header("대사")]
    public StoryDialogue[] dialogues;

    [Header("선택지 (있을 경우)")]
    public StoryChoice[] choices;

    [Header("배경")]
    public Sprite backgroundOverride;   // null이면 기본 배경 사용

    [Header("자동 진행")]
    public bool autoProgress = false;
    public float autoProgressDelay = 3f;
}

/// <summary>
/// 대화 한 줄
/// </summary>
[System.Serializable]
public class StoryDialogue
{
    [Header("화자")]
    public string speakerName;
    public Sprite speakerPortrait;
    public CharacterPosition portraitPosition = CharacterPosition.Left;

    [Header("대사")]
    [TextArea(2, 5)]
    public string text;

    [Header("연출")]
    public DialogueEffect effect = DialogueEffect.None;
    public float effectDuration = 0.3f;

    [Header("음성 (선택)")]
    public AudioClip voiceLine;
}

public enum CharacterPosition
{
    Left,
    Center,
    Right
}

public enum DialogueEffect
{
    None,
    FadeIn,
    FadeOut,
    Shake,
    Flash
}

/// <summary>
/// 선택지
/// </summary>
[System.Serializable]
public class StoryChoice
{
    [Header("선택지 텍스트")]
    public string choiceText;

    [Header("조건 (선택)")]
    public ChoiceConditionType conditionType = ChoiceConditionType.None;
    public string conditionValue;
    public bool hideIfConditionFails = false;

    [Header("결과")]
    public string nextSceneId;              // 이동할 씬 ID (빈 값이면 다음 씬)
    public StoryReward[] choiceRewards;     // 이 선택지의 보상

    [Header("플래그")]
    public string setFlag;                  // 설정할 플래그
    public int flagValue = 1;
}

public enum ChoiceConditionType
{
    None,
    HasCard,        // 특정 카드 보유
    HasFetish,      // 특정 페티시 보유
    HasFlag,        // 특정 플래그 보유
    EPAbove,        // EP가 특정 값 이상
    FloorAbove      // 특정 층 이상
}

/// <summary>
/// 스토리 보상
/// </summary>
[System.Serializable]
public class StoryReward
{
    public RewardType type;
    public string rewardId;         // 카드 ID, 페티시 타입 등
    public int amount = 1;

    [TextArea(1, 2)]
    public string description;
}

public enum RewardType
{
    Card,           // 카드 획득
    RemoveCard,     // 카드 제거
    Gold,           // 골드
    Heal,           // 체력 회복
    MaxHPUp,        // 최대 체력 증가
    EPReduce,       // EP 감소
    EPIncrease,     // EP 증가
    Fetish,         // 페티시 획득
    FetishRemove,   // 페티시 제거
    Flag,           // 플래그 설정
    UnlockGallery   // 갤러리 해금
}

