using UnityEngine;

[CreateAssetMenu(fileName = "New Hero", menuName = "Kiwi Card Game/Hero Data")]
public class HeroData : ScriptableObject
{
    [Header("기본 정보")]
    public string heroId;
    public string heroName;
    [TextArea(2, 4)]
    public string description;

    [Header("아트워크")]
    public Sprite portrait;           // 영웅 초상화
    public Sprite portraitDamaged;    // 피해 입었을 때 초상화
    public Sprite portraitSeduced;    // 유혹 상태 초상화 (플레이어용)
    public Sprite seduceEventArt;     // ★ 유혹 패널용 대형 일러스트

    [Header("기본 스탯")]
    public int maxHealth = 30;
    public int startingArmor = 0;

    [Header("영웅 능력")]
    public HeroPowerData heroPower;

    [Header("적 영웅 전용 - 유혹 공격")]
    public bool canSeduceAttack = false;
    public int seducePower = 5;
    [TextArea(2, 3)]
    public string seduceDescription;

    [Header("특수 이벤트")]
    // ★ 이 줄이 빠져서 에러가 났던 것입니다.
    public ClimaxEventData climaxEvent;

    [Header("시작 무기")]
    public WeaponData startingWeapon;
}


/// <summary>
/// 영웅 능력 데이터
/// </summary>
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
    public int usesPerTurn = 1;           // 턴당 사용 횟수
    public bool requiresTarget = false;   // 타겟 지정 필요 여부
    public EffectTarget targetType = EffectTarget.None;

    [Header("효과")]
    public HeroPowerType powerType;
    public int effectValue = 2;           // 효과 수치 (데미지, 힐, 버프량 등)

    [Header("특수 효과 (선택)")]
    public CardEffect[] additionalEffects;  // 추가 효과들
}

/// <summary>
/// 영웅 능력 종류
/// </summary>
public enum HeroPowerType
{
    // 공격형
    DealDamage,           // 적에게 데미지
    DealDamageAll,        // 모든 적에게 데미지

    // 방어형
    GainArmor,            // 방어력 획득
    HealSelf,             // 자가 회복
    HealTarget,           // 대상 회복

    // 버프형
    BuffAttack,           // 아군 공격력 증가
    BuffHealth,           // 아군 체력 증가
    GiveKeyword,          // 아군에게 키워드 부여

    // 소환형
    SummonMinion,         // 하수인 소환

    // 드로우형
    DrawCard,             // 카드 드로우

    // 자원형
    GainMana,             // 마나 획득
    GainFocus,            // 집중력 획득

    // 유혹형 (적 영웅 전용)
    SeduceAttack,         // 유혹 공격
    ReduceLust,           // 흥분도 감소 (플레이어 전용)

    // 무기형
    EquipWeapon,          // 무기 장착
    BuffWeapon,           // 무기 강화

    // 특수
    Custom                // 커스텀 효과 (additionalEffects 사용)
}
