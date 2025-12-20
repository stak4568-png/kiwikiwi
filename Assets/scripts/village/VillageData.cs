using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 마을 데이터
/// 해당 층 마을의 시설 및 설정
/// </summary>
[CreateAssetMenu(fileName = "Village", menuName = "Kiwi Card Game/Tower/Village Data")]
public class VillageData : ScriptableObject
{
    [Header("기본 정보")]
    public string villageName;
    
    [TextArea(2, 4)]
    public string description;

    [Header("비주얼")]
    public Sprite backgroundImage;
    public AudioClip backgroundMusic;

    [Header("시설")]
    public List<FacilityType> availableFacilities = new List<FacilityType>();

    [Header("여관")]
    public int innCost = 50;
    public int innHealAmount = 10;
    public int innHealPercent = 30;     // 최대 체력의 %

    [Header("상점")]
    public List<ShopItem> shopItems = new List<ShopItem>();
    public int shopRefreshCost = 50;

    [Header("교회")]
    public int churchPurificationCost = 100;
    public bool churchCanRemoveFetish = true;

    [Header("특수 NPC")]
    public List<VillageNPC> npcs = new List<VillageNPC>();

    [Header("이벤트")]
    public StoryData villageEnterEvent;
    public List<StoryData> randomEvents = new List<StoryData>();
}

/// <summary>
/// 상점 아이템
/// </summary>
[System.Serializable]
public class ShopItem
{
    public ShopItemType itemType;
    public CardData cardData;           // 카드인 경우
    public string itemId;               // 아이템인 경우
    public int cost;
    public int stock = 1;
    public bool isSoldOut = false;
}

public enum ShopItemType
{
    Card,
    CardRemoval,        // 카드 제거
    MaxHPUp,            // 최대 체력 증가
    Heal,               // 즉시 회복
    RandomCard,         // 랜덤 카드
    Relic               // 유물
}

/// <summary>
/// 마을 NPC
/// </summary>
[System.Serializable]
public class VillageNPC
{
    public string npcName;
    public Sprite portrait;
    public StoryData dialogueEvent;
    public bool isOneTime = false;
    public string requiredFlag;         // 이 플래그가 있어야 나타남
}

