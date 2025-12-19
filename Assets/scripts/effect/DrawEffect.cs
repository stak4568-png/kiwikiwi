// DrawEffect.cs
// 카드를 뽑는 효과

using UnityEngine;

[CreateAssetMenu(fileName = "NewDrawEffect", menuName = "TCG/Effects/Draw")]
public class DrawEffect : CardEffect
{
    [Header("드로우 설정")]
    public int drawCount = 1;


    public override void Execute(EffectContext context)
    {
        if (context.isCancelled) return;

        if (DeckManager.instance != null)
        {
            for (int i = 0; i < drawCount; i++)
            {
                DeckManager.instance.DrawCard();
            }
            Debug.Log($"[효과] {effectName}: 카드 {drawCount}장 드로우!");
        }
    }

    public override string GetDescription()
    {
        return $"카드를 {drawCount}장 뽑습니다.";
    }
}
