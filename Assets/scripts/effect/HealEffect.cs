using UnityEngine;

[CreateAssetMenu(fileName = "NewHealEffect", menuName = "TCG/Effects/Heal")]
public class HealEffect : CardEffect
{
    [Header("회복 설정")]
    public int healAmount = 1;
    public bool canOverheal = false;    // 최대 체력 초과 회복 가능?

    public override void Execute(EffectContext context)
    {
        switch (targetType)
        {
            case EffectTarget.Self:
                if (context.sourceCard != null) HealCard(context.sourceCard);
                break;

            case EffectTarget.SingleAlly:
                if (context.targetCard != null) HealCard(context.targetCard);
                break;

            case EffectTarget.AllAllies:
                HealAllInZone(ZoneType.PlayerField);
                break;

            case EffectTarget.PlayerHero:
                if (HeroPortrait.playerHero != null)
                {
                    HeroPortrait.playerHero.Heal(healAmount);
                    Debug.Log($"[효과] {effectName}: 플레이어 체력 {healAmount} 회복!");
                }
                break;
        }
    }

    void HealCard(CardDisplay card)
    {
        // ★ 수정 포인트: data.IsCharacter() 체크 ★
        if (card.data != null && card.data.IsCharacter())
        {
            int maxHealth = card.data.hp; // CardData의 기본 체력
            int newHealth = card.currentHp + healAmount; // CardDisplay의 현재 체력

            if (!canOverheal && newHealth > maxHealth)
                newHealth = maxHealth;

            card.currentHp = newHealth;

            // ★ 수정 포인트: UpdateVisual() 호출 ★
            card.UpdateVisual();

            Debug.Log($"[효과] {effectName}: {card.data.title} 체력 {healAmount} 회복! (현재: {card.currentHp})");
        }
    }

    void HealAllInZone(ZoneType zone)
    {
        DropZone[] zones = GameObject.FindObjectsByType<DropZone>(FindObjectsSortMode.None);
        foreach (var dz in zones)
        {
            if (dz.zoneType == zone)
            {
                CardDisplay[] cards = dz.GetComponentsInChildren<CardDisplay>();
                foreach (var card in cards)
                {
                    HealCard(card);
                }
                break;
            }
        }
    }
}