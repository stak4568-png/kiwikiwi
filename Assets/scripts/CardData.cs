using System.Collections.Generic;
using UnityEngine;

// --- 공용 Enum 정의 ---
public enum ZoneType { Hand, PlayerField, EnemyField }
public enum CardElement { None, Water, Fire, Wind, Earth, Electric, Dark, Light }
public enum CardType { None = 0, Hero = 5, Character = 10, Spell = 20, Artifact = 30, Equipment = 50 }
public enum CardAvailability { Unlisted = 0, Collectible = 10, AlwaysAvailable = 20 }
public enum Keyword { None, Taunt, Charge, Stealth, Divine, Lifesteal, Poison, Windfury, Reborn, Seduce, Immune }

public enum EffectTiming
{
    None, OnSummon, OnDeath, OnRelease, OnAttack, OnDamaged,
    OnTurnStart, OnTurnEnd, OnEnemyTurnStart, OnEnemyTurnEnd, Manual
}

public enum EffectTarget
{
    None, Self, SingleEnemy, SingleAlly, AllEnemies,
    AllAllies, EnemyHero, PlayerHero, RandomEnemy, Adjacent
}

public enum EffectCategory
{
    Damage, Heal, Buff, Debuff, Draw, Summon, Destroy, Control, Special
}

// 성별 타입 (처치 이벤트용)
public enum GenderType
{
    None,       // 성별 없음
    Male,       // 남성
    Female,     // 여성
    Futanari,   // 후타나리
    Monster     // 몬스터 (비인간)
}

// ======================================================
// 카드 데이터 클래스
// ======================================================

[CreateAssetMenu(fileName = "card", menuName = "TcgEngine/CardData", order = 5)]
public class CardData : ScriptableObject
{
    public string id;

    [Header("Display")]
    public string title;
    public Sprite art_full;          // 전체 일러스트
    public Sprite art_censored;      // 검열된 일러스트
    public Sprite art_board;

    [Header("Stats")]
    public CardType type;
    public int mana;
    public int attack;
    public int hp;
    public int lust_attack;          // 유혹 공격력
    public int mana_defense;         // 방어에 필요한 마나

    [Header("성별 및 속성")]
    public GenderType gender = GenderType.None;  // 성별 (처치 이벤트용)

    [Header("유혹 속성 (페티시 시스템)")]
    [Tooltip("이 카드의 유혹 공격 속성")]
    public FetishType seduceFetishType = FetishType.None;
    
    [Tooltip("페티시 약점 시 추가 EP 상승량")]
    public int bonusLustOnFetish = 10;

    [Header("Keywords")]
    public List<Keyword> keywords = new List<Keyword>();

    // 키워드 확인용 함수
    public bool HasKeyword(Keyword kw) => keywords != null && keywords.Contains(kw);

    [Header("Special Events")]
    public ClimaxEventData climax_data;      // 클라이맥스 데이터
    public Sprite seduce_event_art;          // 유혹 이벤트 전용 아트
    public TemptationData temptation_data;   // 응시 유혹 데이터

    [Header("Card Text")]
    [TextArea(3, 5)]
    public string text;              // 해금 후 텍스트
    [TextArea(3, 5)]
    public string text_censored;     // 해금 전 텍스트

    [Header("FX & Audio")]
    public GameObject death_fx;
    public AudioClip spawn_audio;

    [Header("Abilities & Effects")]
    [Tooltip("이 카드가 가진 특수 효과 리스트")]
    public List<CardEffect> effects = new List<CardEffect>();

    // --- 헬퍼 함수 ---
    public bool IsCharacter() => type == CardType.Character;
    public bool IsHero() => type == CardType.Hero;
    public bool IsSpell() => type == CardType.Spell;
}
