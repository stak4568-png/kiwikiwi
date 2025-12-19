// StatModifyEffect.cs
// 공격력/체력을 변경하는 버프/디버프 효과

using UnityEngine;

[CreateAssetMenu(fileName = "NewStatModifyEffect", menuName = "TCG/Effects/Stat Modify")]
public class StatModifyEffect : CardEffect
{
    [Header("스탯 변경 설정")]
    public int attackModifier = 0;      // +면 버프, -면 디버프
    public int healthModifier = 0;      // +면 버프, -면 디버프
    public bool isPermanent = true;     // 영구적 변경 여부


    public override void Execute(EffectContext context)
    {
        if (context.isCancelled) return;

        switch (targetType)
        {
            case EffectTarget.Self:
                if (context.sourceCard != null)
                    ModifyStats(context.sourceCard);
                break;

            case EffectTarget.SingleEnemy:
            case EffectTarget.SingleAlly:
                if (context.targetCard != null)
                    ModifyStats(context.targetCard);
                break;

            case EffectTarget.AllAllies:
                ModifyAllInZone(ZoneType.PlayerField);
                break;

            case EffectTarget.AllEnemies:
                ModifyAllInZone(ZoneType.EnemyField);
                break;
        }
    }

    void ModifyStats(CardDisplay card)
    {
        card.currentAttack += attackModifier;
        card.currentHealth += healthModifier;

        // 최소값 보정
        if (card.currentAttack < 0) card.currentAttack = 0;
        if (card.currentHealth <= 0)
        {
            card.currentHealth = 0;
            // 체력 0 이하면 파괴 (Die 호출은 TakeDamage에서 처리)
        }

        card.UpdateCardUI();

        string buffStr = "";
        if (attackModifier != 0) buffStr += $"공격력 {(attackModifier > 0 ? "+" : "")}{attackModifier} ";
        if (healthModifier != 0) buffStr += $"체력 {(healthModifier > 0 ? "+" : "")}{healthModifier}";

        Debug.Log($"[효과] {effectName}: {card.cardData.cardName} {buffStr}");
    }

    void ModifyAllInZone(ZoneType zone)
    {
        DropZone[] zones = GameObject.FindObjectsByType<DropZone>(FindObjectsSortMode.None);
        foreach (var dz in zones)
        {
            if (dz.zoneType == zone)
            {
                CardDisplay[] cards = dz.GetComponentsInChildren<CardDisplay>();
                foreach (var card in cards)
                {
                    ModifyStats(card);
                }
                break;
            }
        }
    }

    public override string GetDescription()
    {
        string result = "";
        if (attackModifier != 0)
            result += $"공격력 {(attackModifier > 0 ? "+" : "")}{attackModifier} ";
        if (healthModifier != 0)
            result += $"체력 {(healthModifier > 0 ? "+" : "")}{healthModifier}";
        return result.Trim();
    }
}
