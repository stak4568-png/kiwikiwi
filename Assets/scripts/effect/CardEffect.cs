// CardEffect.cs
// 모든 카드 효과의 기본 추상 클래스

using UnityEngine;

/// <summary>
/// 모든 카드 효과의 기본 클래스
/// ScriptableObject로 만들어 재사용 가능한 효과 에셋 생성
/// </summary>
public abstract class CardEffect : ScriptableObject
{
    [Header("효과 기본 정보")]
    public string effectName;           // 효과 이름 (예: "화염구")
    [TextArea(2, 4)]
    public string effectDescription;    // 효과 설명
    public Sprite effectIcon;           // 효과 아이콘 (선택)

    [Header("발동 조건")]
    public EffectTiming timing = EffectTiming.OnSummon;  // 언제 발동?
    public EffectTarget targetType = EffectTarget.None;  // 누구에게?
    public EffectCategory category = EffectCategory.Special; // 분류

    [Header("비용 (선택)")]
    public int manaCost = 0;            // 추가 마나 비용
    public int focusCost = 0;           // 추가 집중력 비용

    /// <summary>
    /// 효과 실행 - 자식 클래스에서 구현
    /// </summary>
    /// <param name="context">효과 실행에 필요한 모든 정보</param>
    public abstract void Execute(EffectContext context);

    /// <summary>
    /// 효과 발동 가능 여부 체크 (오버라이드 가능)
    /// </summary>
    public virtual bool CanExecute(EffectContext context)
    {
        // 기본: 비용 체크
        if (manaCost > 0 && GameManager.instance.currentMana < manaCost)
            return false;
        if (focusCost > 0 && GameManager.instance.currentFocus < focusCost)
            return false;

        return true;
    }

    /// <summary>
    /// 효과 설명 텍스트 생성 (동적으로 값 포함)
    /// </summary>
    public virtual string GetDescription()
    {
        return effectDescription;
    }
}

/// <summary>
/// 효과 실행 시 전달되는 컨텍스트 정보
/// </summary>
[System.Serializable]
public class EffectContext
{
    public CardDisplay sourceCard;      // 효과를 발동한 카드
    public CardDisplay targetCard;      // 대상 카드 (있을 경우)
    public HeroPortrait sourceHero;     // 효과를 발동한 영웅 (있을 경우)
    public HeroPortrait targetHero;     // 대상 영웅 (있을 경우)
    public EffectTiming currentTiming;  // 현재 타이밍
    public int damageAmount;            // 데미지량 (OnDamaged 등에서 사용)
    public bool isCancelled;            // 효과 취소 여부

    public EffectContext(CardDisplay source, EffectTiming timing)
    {
        sourceCard = source;
        currentTiming = timing;
        isCancelled = false;
    }

    public EffectContext(HeroPortrait source, EffectTiming timing)
    {
        sourceHero = source;
        currentTiming = timing;
        isCancelled = false;
    }
}
