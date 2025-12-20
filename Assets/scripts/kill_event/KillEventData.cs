using UnityEngine;

/// <summary>
/// 처치 이벤트 데이터
/// 여성 캐릭터가 남성 하수인을 처치할 때 발동하는 미니 이벤트
/// </summary>
[CreateAssetMenu(fileName = "KillEvent", menuName = "Kiwi Card Game/Kill Event")]
public class KillEventData : ScriptableObject
{
    [Header("발동 조건")]
    [Tooltip("처치자의 성별 (기본: 여성)")]
    public GenderType killerGender = GenderType.Female;
    
    [Tooltip("피해자의 성별 (기본: 남성)")]
    public GenderType victimGender = GenderType.Male;

    [Header("연결 대상 (선택)")]
    [Tooltip("특정 카드 전용 이벤트 (null이면 범용)")]
    public CardData specificCard;

    [Header("연출")]
    [Tooltip("이벤트 일러스트")]
    public Sprite eventIllustration;
    
    [TextArea(2, 4)]
    [Tooltip("짧은 텍스트 (선택)")]
    public string eventText;
    
    [Tooltip("표시 시간 (초)")]
    public float displayDuration = 1.5f;
    
    [Tooltip("팝업 페이드 인/아웃 시간")]
    public float fadeDuration = 0.2f;

    [Header("효과")]
    [Tooltip("EP 상승량")]
    public int lustGain = 5;
    
    [Tooltip("true면 VN 스타일 상세 이벤트")]
    public bool showFullEvent = false;
    
    [Tooltip("상세 이벤트 페이지들 (showFullEvent가 true일 때)")]
    public KillEventPage[] detailedPages;
}

/// <summary>
/// 상세 처치 이벤트의 한 페이지
/// </summary>
[System.Serializable]
public class KillEventPage
{
    public Sprite illustration;
    
    [TextArea(2, 4)]
    public string dialogue;
    
    public string speakerName;
}

