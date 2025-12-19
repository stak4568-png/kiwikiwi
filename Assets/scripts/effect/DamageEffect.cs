// DamageEffect.cs
// 데미지를 주는 기본 효과

using UnityEngine;

[CreateAssetMenu(fileName = "NewDamageEffect", menuName = "TCG/Effects/Damage")]
public class DamageEffect : CardEffect
{
    [Header("데미지 설정")]
    public int damageAmount = 1;
    public bool canTargetHero = false;  // 영웅도 타겟 가능?


    public override void Execute(EffectContext context)
    {
        if (context.isCancelled) return;

        // 대상에 따라 데미지 적용
        switch (targetType)
        {
            case EffectTarget.SingleEnemy:
            case EffectTarget.SingleAlly:
                if (context.targetCard != null)
                {
                    context.targetCard.TakeDamage(damageAmount);
                    Debug.Log($"[효과] {effectName}: {context.targetCard.cardData.cardName}에게 {damageAmount} 데미지!");
                }
                break;

            case EffectTarget.AllEnemies:
                DamageAllInZone(ZoneType.EnemyField);
                break;

            case EffectTarget.AllAllies:
                DamageAllInZone(ZoneType.PlayerField);
                break;

            case EffectTarget.EnemyHero:
                if (canTargetHero)
                {
                    GameManager.instance.DamageEnemyHero(damageAmount);
                    Debug.Log($"[효과] {effectName}: 적 영웅에게 {damageAmount} 데미지!");
                }
                break;

            case EffectTarget.RandomEnemy:
                DamageRandomInZone(ZoneType.EnemyField);
                break;
        }
    }

    void DamageAllInZone(ZoneType zone)
    {
        // DropZone 찾아서 모든 카드에 데미지
        DropZone[] zones = GameObject.FindObjectsByType<DropZone>(FindObjectsSortMode.None);
        foreach (var dz in zones)
        {
            if (dz.zoneType == zone)
            {
                CardDisplay[] cards = dz.GetComponentsInChildren<CardDisplay>();
                foreach (var card in cards)
                {
                    card.TakeDamage(damageAmount);
                }
                Debug.Log($"[효과] {effectName}: {zone}의 모든 카드에 {damageAmount} 데미지!");
                break;
            }
        }
    }

    void DamageRandomInZone(ZoneType zone)
    {
        DropZone[] zones = GameObject.FindObjectsByType<DropZone>(FindObjectsSortMode.None);
        foreach (var dz in zones)
        {
            if (dz.zoneType == zone)
            {
                CardDisplay[] cards = dz.GetComponentsInChildren<CardDisplay>();
                if (cards.Length > 0)
                {
                    int randomIndex = Random.Range(0, cards.Length);
                    cards[randomIndex].TakeDamage(damageAmount);
                    Debug.Log($"[효과] {effectName}: {cards[randomIndex].cardData.cardName}에게 {damageAmount} 데미지!");
                }
                break;
            }
        }
    }

    public override string GetDescription()
    {
        string targetStr = targetType switch
        {
            EffectTarget.SingleEnemy => "적 하나에게",
            EffectTarget.AllEnemies => "모든 적에게",
            EffectTarget.RandomEnemy => "무작위 적에게",
            EffectTarget.EnemyHero => "적 영웅에게",
            _ => "대상에게"
        };
        return $"{targetStr} {damageAmount}의 피해를 줍니다.";
    }
}
