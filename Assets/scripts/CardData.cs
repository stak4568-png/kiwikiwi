using System.Collections.Generic;
using UnityEngine;

// --- [���� Enum ����] ---
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

// ======================================================
// 2. ī�� ������ Ŭ����
// ======================================================

[CreateAssetMenu(fileName = "card", menuName = "TcgEngine/CardData", order = 5)]
public class CardData : ScriptableObject
{
    public string id;

    [Header("Display")]
    public string title;
    public Sprite art_full;          // ���� �Ϸ���Ʈ
    public Sprite art_censored;      // �˿��� �Ϸ���Ʈ
    public Sprite art_board;

    [Header("Stats")]
    public CardType type;
    public int mana;
    public int attack;
    public int hp;
    public int lust_attack;          // ��Ȥ ���ݷ�
    public int mana_defense;   // �� ���� �ʿ��� ������

    [Header("Keywords")]
    public List<Keyword> keywords = new List<Keyword>();

    // Ű���� Ȯ�ο� ���� �Լ�
    public bool HasKeyword(Keyword kw) => keywords != null && keywords.Contains(kw);

    [Header("Special Events")]
    public ClimaxEventData climax_data; // 클라이맥스 데이터
    public Sprite seduce_event_art;    // 유혹 이벤트 전용 아트
    public TemptationData temptation_data; // 응시 유혹 데이터

    [Header("Card Text")]
    [TextArea(3, 5)]
    public string text;              // �ر� �� ����
    [TextArea(3, 5)]
    public string text_censored;     // �ر� �� ����

    [Header("FX & Audio")]
    public GameObject death_fx;
    public AudioClip spawn_audio;

    // �� �߰��� �κ�: ȿ�� �ý��� ���� ��
    [Header("Abilities & Effects")]
    [Tooltip("�� ī�尡 ���� Ư�� ȿ�� ����Ʈ (ScriptableObject)")]
    public List<CardEffect> effects = new List<CardEffect>();

    // --- ���� �Լ� ---
    public bool IsCharacter() => type == CardType.Character;
    public bool IsHero() => type == CardType.Hero;
    public bool IsSpell() => type == CardType.Spell;
}