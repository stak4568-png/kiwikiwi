using UnityEngine;

[CreateAssetMenu(fileName = "NewDrawEffect", menuName = "TCG/Effects/Draw")]
public class DrawEffect : CardEffect
{
    [Header("드로우 설정")]
    public int drawCount = 1;

    public override void Execute(EffectContext context)
    {
        if (context == null || context.sourceCard == null) return;

        if (DeckManager.instance != null)
        {
            // ★ 수정 포인트 ★
            // 효과를 발동한 카드(sourceCard)가 플레이어의 카드(isMine)라면 플레이어가 뽑고,
            // 아니라면 적이 뽑도록 합니다.
            bool isPlayerDrawing = context.sourceCard.isMine;

            for (int i = 0; i < drawCount; i++)
            {
                DeckManager.instance.DrawCard(isPlayerDrawing);
            }

            string ownerName = isPlayerDrawing ? "플레이어" : "적";
            Debug.Log($"[효과] {ownerName}가 카드 {drawCount}장을 드로우합니다.");
        }
    }

    public override string GetDescription()
    {
        return $"카드를 {drawCount}장 뽑습니다.";
    }
}