// WeaponData.cs
// 무기 데이터 ScriptableObject - 영웅이 장착하는 무기 정의

using UnityEngine;

[CreateAssetMenu(fileName = "New Weapon", menuName = "Kiwi Card Game/Weapon Data")]
public class WeaponData : ScriptableObject
{
    [Header("기본 정보")]
    public string weaponId;
    public string weaponName;
    [TextArea(2, 4)]
    public string description;

    [Header("아트워크")]
    public Sprite weaponIcon;
    public Sprite weaponGlow;  // 공격 가능할 때 이펙트 (선택)

    [Header("스탯")]
    public int attack = 2;        // 공격력
    public int durability = 2;    // 내구도 (공격 횟수)

    [Header("비용")]
    public int manaCost = 2;

    [Header("특수 효과")]
    public WeaponEffect weaponEffect = WeaponEffect.None;
    public int effectValue = 0;

    [Header("키워드")]
    public bool hasWindfury = false;   // 바람의 격노 (2회 공격)
    public bool hasLifesteal = false;  // 생명력 흡수
    public bool hasPoisonous = false;  // 독성 (즉사)

    [Header("추가 효과 (선택)")]
    public CardEffect[] onEquipEffects;   // 장착 시 효과
    public CardEffect[] onAttackEffects;  // 공격 시 효과
    public CardEffect[] onBreakEffects;   // 파괴 시 효과
}

/// <summary>
/// 무기 특수 효과 종류
/// </summary>
public enum WeaponEffect
{
    None,                    // 효과 없음

    // 공격 관련
    Cleave,                  // 인접 적에게도 데미지
    IgnoreTaunt,             // 도발 무시
    IgnoreArmor,             // 방어력 무시
    BonusDamageToMinions,    // 하수인에게 추가 데미지
    BonusDamageToHeroes,     // 영웅에게 추가 데미지

    // 방어 관련
    DamageImmune,            // 공격 시 피해 면역
    ReduceDamageTaken,       // 받는 피해 감소

    // 특수
    FreezeOnHit,             // 공격 대상 빙결
    DrawOnKill,              // 처치 시 드로우
    GainArmorOnHit,          // 공격 시 방어력 획득
    HealOnHit,               // 공격 시 회복

    // 유혹 관련 (성인 테마)
    SeduceOnHit,             // 공격 시 유혹 데미지 추가
    ReduceLustOnKill         // 처치 시 흥분도 감소
}

/// <summary>
/// 런타임 무기 상태 (HeroPortrait에서 사용)
/// </summary>
[System.Serializable]
public class WeaponState
{
    public WeaponData weaponData;
    public int currentAttack;
    public int currentDurability;
    public int attacksThisTurn;

    public WeaponState(WeaponData data)
    {
        weaponData = data;
        currentAttack = data.attack;
        currentDurability = data.durability;
        attacksThisTurn = 0;
    }

    /// <summary>
    /// 무기 공격 가능 여부
    /// </summary>
    public bool CanAttack()
    {
        if (currentDurability <= 0) return false;

        int maxAttacks = weaponData.hasWindfury ? 2 : 1;
        return attacksThisTurn < maxAttacks;
    }

    /// <summary>
    /// 무기로 공격 수행
    /// </summary>
    public void UseWeapon()
    {
        attacksThisTurn++;
        currentDurability--;
    }

    /// <summary>
    /// 턴 시작 시 초기화
    /// </summary>
    public void OnTurnStart()
    {
        attacksThisTurn = 0;
    }

    /// <summary>
    /// 무기 파괴 여부
    /// </summary>
    public bool IsBroken()
    {
        return currentDurability <= 0;
    }

    /// <summary>
    /// 무기 강화
    /// </summary>
    public void BuffWeapon(int attackBuff, int durabilityBuff)
    {
        currentAttack += attackBuff;
        currentDurability += durabilityBuff;
    }
}
