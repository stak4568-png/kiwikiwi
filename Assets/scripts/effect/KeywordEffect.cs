using UnityEngine;

[CreateAssetMenu(fileName = "NewKeywordEffect", menuName = "TCG/Effects/Keyword")]
public class KeywordEffect : CardEffect
{
    [Header("키워드 설정")]
    public Keyword keyword = Keyword.None; // CardData.cs의 정의를 참조함

    public override void Execute(EffectContext context)
    {
        // 런타임에 키워드를 추가하는 로직 (CardDisplay에 AddKeyword 함수가 있다고 가정)
        if (context.sourceCard != null)
        {
            // context.sourceCard.AddKeyword(keyword); 
            Debug.Log($"[효과] {context.sourceCard.data.title}에게 {keyword} 부여!");
        }
    }
}