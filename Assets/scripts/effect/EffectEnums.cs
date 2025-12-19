// EffectEnums.cs
// 효과 시스템에서 사용하는 열거형 정의

/// <summary>
/// 효과가 발동되는 타이밍
/// </summary>
public enum EffectTiming
{
    None,           // 타이밍 없음 (패시브 스탯 변경 등)
    OnSummon,       // 소환 시
    OnDeath,        // 파괴 시
    OnRelease,      // 릴리스(희생) 시 - OnDeath 이전에 발동
    OnAttack,       // 공격 선언 시
    OnDamaged,      // 피해를 받을 때
    OnTurnStart,    // 내 턴 시작 시
    OnTurnEnd,      // 내 턴 종료 시
    OnEnemyTurnStart, // 적 턴 시작 시
    OnEnemyTurnEnd,   // 적 턴 종료 시
    OnDraw,         // 카드를 뽑을 때
    OnDiscard,      // 카드를 버릴 때
    OnHeal,         // 회복 시
    OnSpellCast,    // 주문 시전 시
    Manual          // 수동 발동 (클릭 등)
}

/// <summary>
/// 효과의 대상
/// </summary>
public enum EffectTarget
{
    None,           // 대상 없음
    Self,           // 자기 자신
    SingleEnemy,    // 적 하나 선택
    SingleAlly,     // 아군 하나 선택
    AllEnemies,     // 모든 적
    AllAllies,      // 모든 아군
    AllCards,       // 모든 카드
    RandomEnemy,    // 무작위 적 하나
    RandomAlly,     // 무작위 아군 하나
    EnemyHero,      // 적 영웅
    PlayerHero,     // 플레이어 영웅
    Adjacent        // 인접한 카드
}

/// <summary>
/// 효과의 종류 (UI 표시, 분류용)
/// </summary>
public enum EffectCategory
{
    Damage,         // 데미지 관련
    Heal,           // 회복 관련
    Buff,           // 버프 (스탯 증가)
    Debuff,         // 디버프 (스탯 감소)
    Draw,           // 드로우 관련
    Summon,         // 소환 관련
    Destroy,        // 파괴 관련
    Control,        // 제어 (도발, 침묵 등)
    Special         // 특수 효과
}
