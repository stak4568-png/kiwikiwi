using UnityEngine;

[CreateAssetMenu(fileName = "NewDamageEffect", menuName = "TCG/Effects/Damage")]
public class DamageEffect : CardEffect
{
    [Header("데미지 설정")]
    public int damageAmount = 1;
    public bool canTargetHero = false;

    public override void Execute(EffectContext context)
    {
        // 대상에 따라 데미지 적용
        switch (targetType)
        {
            case EffectTarget.SingleEnemy:
            case EffectTarget.SingleAlly:
                // 영웅 타겟팅 우선 처리
                if (context.targetHero != null && canTargetHero)
                {
                    context.targetHero.TakeDamage(damageAmount);
                    Debug.Log($"[효과] {effectName}: {context.targetHero.heroData.title}에게 {damageAmount} 데미지!");
                }
                // 카드 타겟팅
                else if (context.targetCard != null)
                {
                    context.targetCard.TakeDamage(damageAmount);
                    // ★ 수정 포인트: cardData.cardName -> data.title ★
                    Debug.Log($"[효과] {effectName}: {context.targetCard.data.title}에게 {damageAmount} 데미지!");
                }
                break;

            case EffectTarget.AllEnemies:
                DamageAllInZone(ZoneType.EnemyField);
                break;

            case EffectTarget.AllAllies:
                DamageAllInZone(ZoneType.PlayerField);
                break;

            case EffectTarget.EnemyHero:
                if (canTargetHero && HeroPortrait.enemyHero != null)
                {
                    HeroPortrait.enemyHero.TakeDamage(damageAmount);
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
        // 씬에서 DropZone을 찾음
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
                    // ★ 수정 포인트: cardData.cardName -> data.title ★
                    Debug.Log($"[효과] {effectName}: {cards[randomIndex].data.title}에게 {damageAmount} 데미지!");
                    cards[randomIndex].TakeDamage(damageAmount);
                }
                break;
            }
        }
    }
}