using UnityEngine;

[CreateAssetMenu(fileName = "NewHealEffect", menuName = "TCG/Effects/Heal")]
public class HealEffect : CardEffect
{
    [Header("회복 설정")]
    public int healAmount = 1;
    public bool canOverheal = false;    // 최대 체력 초과 회복 가능?

    public override void Execute(EffectContext context)
    {
        if (context.isCancelled) return;

        switch (targetType)
        {
            case EffectTarget.Self:
                if (context.sourceCard != null)
                {
                    HealCard(context.sourceCard);
                }
                break;

            case EffectTarget.SingleAlly:
                if (context.targetCard != null)
                {
                    HealCard(context.targetCard);
                }
                break;

            case EffectTarget.AllAllies:
                HealAllInZone(ZoneType.PlayerField);
                break;

            case EffectTarget.PlayerHero:
                // 수정됨: GameManager의 변수 대신 HeroPortrait의 Heal 메서드 사용
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
        if (card.cardData is MonsterCardData monster)
        {
            int maxHealth = monster.health;
            int newHealth = card.currentHealth + healAmount;

            if (!canOverheal && newHealth > maxHealth)
                newHealth = maxHealth;

            card.currentHealth = newHealth;
            card.UpdateCardUI();
            Debug.Log($"[효과] {effectName}: {card.cardData.cardName} 체력 {healAmount} 회복! (현재: {card.currentHealth})");
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

    public override string GetDescription()
    {
        string targetStr = targetType switch
        {
            EffectTarget.Self => "자신의",
            EffectTarget.SingleAlly => "아군 하나의",
            EffectTarget.AllAllies => "모든 아군의",
            EffectTarget.PlayerHero => "영웅의",
            _ => ""
        };
        return $"{targetStr} 체력을 {healAmount} 회복합니다.";
    }
}