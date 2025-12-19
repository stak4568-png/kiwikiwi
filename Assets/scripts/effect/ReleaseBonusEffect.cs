// ReleaseBonusEffect.cs
// 릴리스 시 추가 보너스를 제공하는 효과

using UnityEngine;

[CreateAssetMenu(fileName = "ReleaseBonusEffect", menuName = "TCG/Effects/Release Bonus")]
public class ReleaseBonusEffect : CardEffect
{
    [Header("릴리스 보너스 설정")]
    [Tooltip("추가로 회복할 마나")]
    public int bonusMana = 1;

    [Tooltip("추가로 회복할 집중력")]
    public int bonusFocus = 0;

    [Tooltip("추가로 뽑을 카드")]
    public int bonusDraw = 0;

    [Tooltip("적 영웅에게 줄 데미지")]
    public int damageToEnemy = 0;

    [Tooltip("플레이어 체력 회복")]
    public int healPlayer = 0;

    public override void Execute(EffectContext context)
    {
        if (context.sourceCard == null) return;

        Debug.Log($"[릴리스 보너스] {context.sourceCard.cardData.cardName}의 릴리스 효과 발동!");

        // 추가 마나 회복
        if (bonusMana > 0 && GameManager.instance != null)
        {
            GameManager.instance.GainMana(bonusMana);
            Debug.Log($"  → 추가 마나 +{bonusMana}");
        }

        // 추가 집중력 회복
        if (bonusFocus > 0 && GameManager.instance != null)
        {
            GameManager.instance.GainFocus(bonusFocus);
            Debug.Log($"  → 추가 집중력 +{bonusFocus}");
        }

        // 추가 드로우
        if (bonusDraw > 0 && DeckManager.instance != null)
        {
            for (int i = 0; i < bonusDraw; i++)
            {
                DeckManager.instance.DrawCard();
            }
            Debug.Log($"  → 카드 {bonusDraw}장 드로우");
        }

        // 적 영웅 데미지
        if (damageToEnemy > 0 && GameManager.instance != null)
        {
            GameManager.instance.DamageEnemyHero(damageToEnemy);
            Debug.Log($"  → 적 영웅에게 {damageToEnemy} 데미지");
        }

        // 플레이어 회복
        if (healPlayer > 0 && GameManager.instance != null)
        {
            GameManager.instance.HealPlayer(healPlayer);
            Debug.Log($"  → 플레이어 {healPlayer} 회복");
        }
    }

    public override string GetDescription()
    {
        System.Text.StringBuilder sb = new System.Text.StringBuilder();
        sb.Append("<color=purple>[릴리스]</color> ");

        System.Collections.Generic.List<string> effects = new System.Collections.Generic.List<string>();

        if (bonusMana > 0) effects.Add($"마나 +{bonusMana}");
        if (bonusFocus > 0) effects.Add($"집중력 +{bonusFocus}");
        if (bonusDraw > 0) effects.Add($"카드 {bonusDraw}장 드로우");
        if (damageToEnemy > 0) effects.Add($"적 영웅에게 {damageToEnemy} 데미지");
        if (healPlayer > 0) effects.Add($"체력 {healPlayer} 회복");

        sb.Append(string.Join(", ", effects));

        return sb.ToString();
    }
}
