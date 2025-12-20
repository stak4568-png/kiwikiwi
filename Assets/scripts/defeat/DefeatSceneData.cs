using UnityEngine;

/// <summary>
/// 패배씬 데이터
/// 패배 유형, 적, 상태에 따라 다른 씬 표시
/// </summary>
[CreateAssetMenu(fileName = "DefeatScene", menuName = "Kiwi Card Game/Defeat Scene")]
public class DefeatSceneData : ScriptableObject
{
    [Header("기본 정보")]
    public string sceneId;
    public string sceneTitle;

    [Header("발동 조건")]
    public DefeatType defeatType = DefeatType.Any;
    
    [Tooltip("특정 적에게 패배 시 (null이면 범용)")]
    public HeroData specificEnemy;
    
    [Tooltip("특정 페티시 상태에서만 발동")]
    public FetishType requiredFetish = FetishType.None;
    
    [Tooltip("최소 EP (0이면 무시)")]
    [Range(0, 100)]
    public int minimumEP = 0;
    
    [Tooltip("우선순위 (높을수록 우선)")]
    public int priority = 0;

    [Header("씬 페이지")]
    public DefeatPage[] pages;

    [Header("결과")]
    [Tooltip("획득하는 페티시")]
    public FetishType acquiredFetish = FetishType.None;
    
    [Tooltip("갤러리 해금 ID")]
    public string galleryUnlockId;

    [Header("분기")]
    [Tooltip("씬 종료 후 이어지는 이벤트 (선택)")]
    public StoryData followUpEvent;
}

public enum DefeatType
{
    Any,            // 모든 패배
    HPZero,         // HP 0 패배
    Climax,         // 절정 패배 (EP 100%)
    Surrender,      // 항복 패배
    Masturbation    // 자위 항복 패배
}

/// <summary>
/// 패배씬 페이지
/// </summary>
[System.Serializable]
public class DefeatPage
{
    [Header("비주얼")]
    public Sprite illustration;
    public Sprite characterPortrait;

    [Header("텍스트")]
    public string speakerName;
    
    [TextArea(3, 6)]
    public string dialogue;

    [Header("연출")]
    public DefeatPageEffect effect = DefeatPageEffect.None;
    
    [Tooltip("화면 틴트 색상")]
    public Color screenTint = Color.white;

    [Header("오디오")]
    public AudioClip voiceLine;
    public AudioClip soundEffect;
}

public enum DefeatPageEffect
{
    None,
    FadeIn,
    FadeOut,
    WhiteFade,
    HeartBeat,
    Shake,
    SlowZoom
}

