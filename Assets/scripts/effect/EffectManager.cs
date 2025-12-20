using UnityEngine;
using System.Collections.Generic;

public class EffectManager : MonoBehaviour
{
    public static EffectManager instance;

    private bool isWaitingForTarget = false;
    private CardEffect pendingEffect;
    private EffectContext pendingContext;

    void Awake()
    {
        if (instance == null) instance = this;
        else Destroy(gameObject);
    }

    /// <summary>
    /// 특정 타이밍에 해당하는 카드의 모든 효과 발동
    /// </summary>
    public void TriggerEffects(CardDisplay card, EffectTiming timing, CardDisplay target = null)
    {
        // ★ 수정 포인트: cardData -> data ★
        if (card == null || card.data == null) return;

        // CardData 에 정의된 효과 리스트를 순회
        // (CardData.cs에 public List<CardEffect> effects 변수가 있어야 함)
        if (card.data.effects != null)
        {
            foreach (CardEffect effect in card.data.effects)
            {
                if (effect != null && effect.timing == timing)
                {
                    ExecuteEffect(effect, card, target);
                }
            }
        }
    }

    public void ExecuteEffect(CardEffect effect, CardDisplay source, CardDisplay target = null)
    {
        if (effect == null) return;

        EffectContext context = new EffectContext(source, effect.timing);
        context.targetCard = target;

        // 타겟 선택이 필요한지 체크
        if (NeedsTargetSelection(effect.targetType) && target == null)
        {
            StartTargetSelection(effect, context);
            return;
        }

        effect.Execute(context);
    }

    bool NeedsTargetSelection(EffectTarget targetType)
    {
        return targetType == EffectTarget.SingleEnemy || targetType == EffectTarget.SingleAlly;
    }

    void StartTargetSelection(CardEffect effect, EffectContext context)
    {
        isWaitingForTarget = true;
        pendingEffect = effect;
        pendingContext = context;
        Debug.Log($"[타겟 선택] {effect.effectName}의 대상을 선택하세요.");
    }

    public void OnTargetSelected(CardDisplay target)
    {
        if (!isWaitingForTarget || pendingEffect == null) return;
        pendingContext.targetCard = target;
        pendingEffect.Execute(pendingContext);
        CancelTargetSelection();
    }

    public void OnHeroTargetSelected(HeroPortrait targetHero)
    {
        if (!isWaitingForTarget || pendingEffect == null) return;
        pendingContext.targetHero = targetHero;
        pendingEffect.Execute(pendingContext);
        CancelTargetSelection();
    }

    public void CancelTargetSelection()
    {
        isWaitingForTarget = false;
        pendingEffect = null;
        pendingContext = null;
    }

    public bool IsWaitingForTarget() => isWaitingForTarget;

    // 전역 타이밍 트리거 (턴 시작/종료 등)
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