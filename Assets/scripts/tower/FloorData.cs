using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 층 데이터
/// 해당 층의 노드 구성 및 설정
/// </summary>
[CreateAssetMenu(fileName = "Floor", menuName = "Kiwi Card Game/Tower/Floor Data")]
public class FloorData : ScriptableObject
{
    [Header("기본 정보")]
    public int floorNumber;
    public string floorName;
    
    [TextArea(2, 4)]
    public string description;

    [Header("비주얼")]
    public Sprite backgroundImage;
    public Color themeColor = Color.white;
    public AudioClip backgroundMusic;

    [Header("노드 구성")]
    public List<NodeData> nodes = new List<NodeData>();

    [Header("보스")]
    public HeroData bossData;
    public string bossIntroEventId;

    [Header("마을")]
    public VillageData villageData;

    [Header("난이도")]
    [Range(1, 10)]
    public int difficultyLevel = 1;
    
    [Tooltip("일반 전투 적 풀")]
    public List<HeroData> normalEnemyPool = new List<HeroData>();
    
    [Tooltip("엘리트 전투 적 풀")]
    public List<HeroData> eliteEnemyPool = new List<HeroData>();
}

/// <summary>
/// 노드 데이터
/// 맵 상의 하나의 노드 (전투, 이벤트, 상점 등)
/// </summary>
[System.Serializable]
public class NodeData
{
    [Header("기본 정보")]
    public string nodeId;
    public string displayName;
    public NodeType nodeType = NodeType.Battle;

    [Header("위치")]
    public Vector2 mapPosition;         // 맵 UI에서의 위치
    public bool isStartNode = false;
    public bool isBossNode = false;

    [Header("연결")]
    [Tooltip("이 노드에서 이동 가능한 노드 인덱스들")]
    public List<int> connectedNodeIndices = new List<int>();

    [Header("전투 노드")]
    public HeroData enemyData;
    public List<CardData> additionalEnemyCards = new List<CardData>();

    [Header("이벤트 노드")]
    public StoryData eventData;

    [Header("보상")]
    public NodeReward reward;

    [Header("비주얼")]
    public Sprite nodeIcon;
}

public enum NodeType
{
    Battle,         // 일반 전투
    EliteBattle,    // 엘리트 전투
    Boss,           // 보스 전투
    Event,          // 랜덤 이벤트
    Rest,           // 휴식처
    Shop,           // 상점
    Treasure,       // 보물
    Village,        // 마을
    Unknown         // 미확인 (안개)
}

/// <summary>
/// 노드 보상
/// </summary>
[System.Serializable]
public class NodeReward
{
    [Header("카드")]
    public List<CardData> guaranteedCards = new List<CardData>();
    public int randomCardCount = 0;
    public CardType randomCardType = CardType.None;

    [Header("골드")]
    public int goldMin = 0;
    public int goldMax = 0;

    [Header("회복")]
    public int healAmount = 0;
    public int epReduce = 0;

    [Header("특수")]
    public bool unlockGallery = false;
    public string galleryUnlockId;
}

