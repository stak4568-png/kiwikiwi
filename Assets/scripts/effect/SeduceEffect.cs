// SeduceEffect.cs
// 플레이어 흥분도를 증가시키는 유혹 효과

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

        if (ignoreManaDef)
        {
            // 마나 방어 무시 - 직접 흥분도 증가
            GameManager.instance.playerLust += lustAmount;
            if (GameManager.instance.playerLust >= 100)
            {
                GameManager.instance.playerLust = 100;
                Debug.Log("★ CLIMAX!");
            }
            GameManager.instance.UpdateUI();
            Debug.Log($"[효과] {effectName}: 흥분도 {lustAmount} 증가! (방어 무시)");
        }
        else
        {
            // 일반 유혹 데미지 (마나 방어 적용)
            GameManager.instance.TakeLustDamage(lustAmount);
            Debug.Log($"[효과] {effectName}: 유혹 공격 {lustAmount}!");
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
