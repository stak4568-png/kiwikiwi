// EffectManager.cs
// 효과 실행을 중앙에서 관리하는 매니저

using UnityEngine;
using System.Collections.Generic;

public class EffectManager : MonoBehaviour
{
    public static EffectManager instance;

    // 타겟 선택 대기 상태
    private bool isWaitingForTarget = false;
    private CardEffect pendingEffect;
    private EffectContext pendingContext;

    void Awake()
    {
        if (instance == null) instance = this;
        else Destroy(gameObject);
    }

    /// <summary>
    /// 특정 타이밍에 해당하는 모든 효과 발동
    /// </summary>
    public void TriggerEffects(CardDisplay card, EffectTiming timing, CardDisplay target = null)
    {
        if (card == null || card.cardData == null) return;

        // MonsterCardData의 효과 리스트 가져오기
        if (card.cardData is MonsterCardData monster && monster.effects != null)
        {
            foreach (CardEffect effect in monster.effects)
            {
                if (effect != null && effect.timing == timing)
                {
                    ExecuteEffect(effect, card, target);
                }
            }
        }
    }

    /// <summary>
    /// 단일 효과 실행
    /// </summary>
    public void ExecuteEffect(CardEffect effect, CardDisplay source, CardDisplay target = null)
    {
        if (effect == null) return;

        EffectContext context = new EffectContext(source, effect.timing);
        context.targetCard = target;

        // 발동 가능 체크
        if (!effect.CanExecute(context))
        {
            Debug.Log($"[효과 불발] {effect.effectName}: 조건 미충족");
            return;
        }

        // 타겟 선택이 필요한 효과인지 체크
        if (NeedsTargetSelection(effect.targetType) && target == null)
        {
            // 타겟 선택 모드 진입
            StartTargetSelection(effect, context);
            return;
        }

        // 비용 소모
        if (effect.manaCost > 0)
            GameManager.instance.TrySpendMana(effect.manaCost);
        if (effect.focusCost > 0)
            GameManager.instance.TryUseFocus();

        // 효과 실행
        effect.Execute(context);
    }

    /// <summary>
    /// 타겟 선택이 필요한 효과 타입인지 확인
    /// </summary>
    bool NeedsTargetSelection(EffectTarget targetType)
    {
        return targetType == EffectTarget.SingleEnemy ||
               targetType == EffectTarget.SingleAlly;
    }

    /// <summary>
    /// 타겟 선택 모드 시작
    /// </summary>
    void StartTargetSelection(CardEffect effect, EffectContext context)
    {
        isWaitingForTarget = true;
        pendingEffect = effect;
        pendingContext = context;
        Debug.Log($"[타겟 선택] {effect.effectName}의 대상을 선택하세요.");
        // TODO: UI에서 선택 가능한 카드 하이라이트
    }

    /// <summary>
    /// 타겟 선택 완료 시 호출
    /// </summary>
    public void OnTargetSelected(CardDisplay target)
    {
        if (!isWaitingForTarget || pendingEffect == null) return;

        pendingContext.targetCard = target;

        // 유효한 타겟인지 체크
        if (IsValidTarget(pendingEffect.targetType, target))
        {
            pendingEffect.Execute(pendingContext);
        }
        else
        {
            Debug.Log("[타겟 선택] 유효하지 않은 대상입니다.");
        }

        CancelTargetSelection();
    }

    /// <summary>
    /// 타겟 선택 취소
    /// </summary>
    public void CancelTargetSelection()
    {
        isWaitingForTarget = false;
        pendingEffect = null;
        pendingContext = null;
    }

    /// <summary>
    /// 유효한 타겟인지 확인
    /// </summary>
    bool IsValidTarget(EffectTarget targetType, CardDisplay target)
    {
        if (target == null) return false;

        DropZone zone = target.transform.parent?.GetComponent<DropZone>();
        if (zone == null) return false;

        return targetType switch
        {
            EffectTarget.SingleEnemy => zone.zoneType == ZoneType.EnemyField,
            EffectTarget.SingleAlly => zone.zoneType == ZoneType.PlayerField,
            _ => true
        };
    }

    /// <summary>
    /// 현재 타겟 선택 대기 중인지 확인
    /// </summary>
    public bool IsWaitingForTarget() => isWaitingForTarget;

    // === 전역 이벤트 트리거 (턴 시작/종료 등) ===

    /// <summary>
    /// 필드의 모든 카드에 타이밍 이벤트 발동
    /// </summary>
    public void TriggerGlobalTiming(EffectTiming timing, ZoneType zone)
    {
        DropZone[] zones = FindObjectsByType<DropZone>(FindObjectsSortMode.None);
        foreach (var dz in zones)
        {
            if (dz.zoneType == zone)
            {
                CardDisplay[] cards = dz.GetComponentsInChildren<CardDisplay>();
                foreach (var card in cards)
                {
                    TriggerEffects(card, timing);
                }
            }
        }
    }
}
