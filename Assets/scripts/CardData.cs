using UnityEngine;

// --- 1. 공통 Enum 정의 (클래스 밖으로 뺍니다) ---
public enum ZoneType { Hand, PlayerField, EnemyField }
public enum CardElement { None, Water, Fire, Wind, Earth, Electric, Dark, Light }

// --- 2. 카드 종류 정의 (필요시 사용) ---
public enum CardType
{
    Monster, Spell, Trap, Field, Location, Equipment
}

// --- 3. 기본 추상 클래스 ---
public abstract class CardData : ScriptableObject
{
    [Header("1. 공통 정보")]
    public string id;
    public string cardName;
    public CardType cardType; // 카드 종류 식별용
    public CardElement element;
    public int manaCost;

    [Header("2. 시선 시스템용 데이터")]
    public Sprite originalArt;
    public Sprite censoredArt;

    [TextArea(3, 5)]
    public string description;          // 해금 후 설명
    [TextArea(3, 5)]
    public string censoredDescription;  // 해금 전 설명
}