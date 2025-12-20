using UnityEngine;

[CreateAssetMenu(fileName = "NewSeduceEffect", menuName = "TCG/Effects/Seduce")]
public class SeduceEffect : CardEffect
{
    [Header("유혹 설정")]
    public int lustAmount = 1;          // 흥분도 증가량
    public bool ignoreManaDef = false;  // 마나 방어 무시 여부

    public override void Execute(EffectContext context)
    {
        if (context.isCancelled) return;

        // 수정됨: GameManager 대신 HeroPortrait.playerHero의 메서드를 호출합니다.
        if (HeroPortrait.playerHero != null)
        {
            // TakeLustDamage(데미지량, 마나방어무시여부)
            // ignoreManaDef가 true면 마나를 소모하지 않고 즉시 흥분도가 오릅니다.
            HeroPortrait.playerHero.TakeLustDamage(lustAmount, ignoreManaDef);

            Debug.Log($"[효과] {effectName}: 유혹 공격 {lustAmount}! (방어 무시: {ignoreManaDef})");
        }
    }

    public override string GetDescription()
    {
        if (ignoreManaDef)
            return $"플레이어의 흥분도를 {lustAmount} 증가시킵니다. (마나 방어 무시)";
        else
            return $"플레이어에게 {lustAmount}의 유혹 공격을 합니다.";
    }
}