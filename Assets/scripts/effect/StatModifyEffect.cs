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
        // ★ 수정 포인트: currentAttack, currentHp (변수명 변경 반영) ★
        card.currentAttack += attackModifier;
        card.currentHp += healthModifier;

        // 최소값 보정
        if (card.currentAttack < 0) card.currentAttack = 0;
        if (card.currentHp < 0) card.currentHp = 0;

        // ★ 수정 포인트: UpdateCardUI -> UpdateVisual (함수명 변경 반영) ★
        card.UpdateVisual();

        string buffStr = "";
        if (attackModifier != 0) buffStr += $"공격력 {(attackModifier > 0 ? "+" : "")}{attackModifier} ";
        if (healthModifier != 0) buffStr += $"체력 {(healthModifier > 0 ? "+" : "")}{healthModifier}";

        // ★ 수정 포인트: cardData.cardName -> data.title ★
        Debug.Log($"[효과] {effectName}: {card.data.title} {buffStr}");

        // 체력이 0 이하면 파괴 처리 (CardDisplay에 TakeDamage 로직이 있다면 활용 가능)
        if (card.currentHp <= 0)
        {
            Destroy(card.gameObject);
        }
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
}