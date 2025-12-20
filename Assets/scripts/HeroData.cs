using UnityEngine;

[CreateAssetMenu(fileName = "New Hero", menuName = "Kiwi Card Game/Hero Data")]
public class HeroData : ScriptableObject
{
    [Header("1. Display")]
    public string title;             // 영웅 이름
    public Sprite art_full;          // 기본 초상화
    public Sprite art_seduced;       // 유혹 상태 초상화 (플레이어용)
    public Sprite seduce_event_art;  // 유혹 패널용 대형 일러스트

    [Header("2. Stats")]
    public int maxHealth = 30;
    public int startingArmor = 0;
    public WeaponData startingWeapon;

    [Header("3. 영웅 능력")]
    public HeroPowerData heroPower;

    [Header("4. 적 영웅 전용 - 유혹 공격")]
    public bool canSeduceAttack = true;
    public int seducePower = 5;
    public int mana_defense = 2;
    [TextArea(2, 3)]
    public string seduceDescription;

    [Header("5. 특수 이벤트")]
    [Tooltip("흥분도 100% 달성 시 실행될 클라이맥스 데이터")]
    public ClimaxEventData climax_data; // GameManager에서 이 이름을 참조합니다.
}

// ==========================================
// 영웅 능력 데이터 구조 (기존 코드 유지)
// ==========================================
[CreateAssetMenu(fileName = "New Hero Power", menuName = "Kiwi Card Game/Hero Power")]
public class HeroPowerData : ScriptableObject
{
    [Header("기본 정보")]
    public string powerName;
    public Sprite icon;
    [TextArea(2, 4)]
    public string description;

    [Header("비용")]
    public int manaCost = 2;
    public int focusCost = 0;

    [Header("사용 제한")]
    public int usesPerTurn = 1;
    public bool requiresTarget = false;
    public EffectTarget targetType = EffectTarget.None;

    [Header("효과")]
    public HeroPowerType powerType;
    public int effectValue = 2;

    [Header("특수 효과 (선택)")]
    public CardEffect[] additionalEffects;
}

public enum HeroPowerType
{
    DealDamage,
    DealDamageAll,
    GainArmor,
    HealSelf,
    HealTarget,
    BuffAttack,
    BuffHealth,
    GiveKeyword,
    SummonMinion,
    DrawCard,
    GainMana,
    GainFocus,
    SeduceAttack,
    ReduceLust,
    EquipWeapon,
    BuffWeapon,
    Custom
}