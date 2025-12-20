using UnityEngine;

[CreateAssetMenu(fileName = "ReleaseBonusEffect", menuName = "TCG/Effects/Release Bonus")]
public class ReleaseBonusEffect : CardEffect
{
    [Header("릴리스 보너스 설정")]
    public int bonusMana = 1;
    public int bonusFocus = 0;
    public int bonusDraw = 0;
    public int damageToEnemy = 0;
    public int healPlayer = 0;

    public override void Execute(EffectContext context)
    {
        if (context.sourceCard == null) return;

        Debug.Log($"[릴리스 보너스] {context.sourceCard.cardData.cardName}의 릴리스 효과 발동!");

        // 1. 추가 마나 회복 (GameManager 담당)
        if (bonusMana > 0 && GameManager.instance != null)
        {
            GameManager.instance.GainMana(bonusMana);
        }

        // 2. 추가 집중력 회복 (GameManager 담당)
        if (bonusFocus > 0 && GameManager.instance != null)
        {
            GameManager.instance.GainFocus(bonusFocus);
        }

        // 3. 추가 드로우 (DeckManager 담당)
        if (bonusDraw > 0 && DeckManager.instance != null)
        {
            for (int i = 0; i < bonusDraw; i++)
            {
                DeckManager.instance.DrawCard();
            }
        }

        // 4. 적 영웅 데미지 (수정됨: HeroPortrait.enemyHero 직접 참조)
        if (damageToEnemy > 0 && HeroPortrait.enemyHero != null)
        {
            HeroPortrait.enemyHero.TakeDamage(damageToEnemy);
            Debug.Log($"  → 적 영웅에게 {damageToEnemy} 데미지");
        }

        // 5. 플레이어 체력 회복 (수정됨: HeroPortrait.playerHero 직접 참조)
        if (healPlayer > 0 && HeroPortrait.playerHero != null)
        {
            HeroPortrait.playerHero.Heal(healPlayer);
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