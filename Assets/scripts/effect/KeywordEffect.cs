// KeywordEffect.cs
// 도발, 돌진 등 키워드 능력 부여 효과

using UnityEngine;

/// <summary>
/// 카드가 가질 수 있는 키워드 능력
/// </summary>
public enum Keyword
{
    None,
    Taunt,      // 도발: 이 카드를 먼저 공격해야 함
    Charge,     // 돌진: 소환 즉시 공격 가능
    Stealth,    // 은신: 공격하기 전까지 대상 지정 불가
    Divine,     // 천상의 보호막: 첫 피해 무효화
    Lifesteal,  // 생명력 흡수: 데미지만큼 영웅 회복
    Poison,     // 독: 피해를 입힌 적 파괴
    Windfury,   // 질풍: 한 턴에 두 번 공격
    Reborn,     // 환생: 처음 죽을 때 체력 1로 부활
    Seduce,     // 유혹: 공격 시 적 흥분도 증가
    Immune      // 면역: 주문/효과 대상 불가
}

[CreateAssetMenu(fileName = "NewKeywordEffect", menuName = "TCG/Effects/Keyword")]
public class KeywordEffect : CardEffect
{
    [Header("키워드 설정")]
    public Keyword keyword = Keyword.None;


    public override void Execute(EffectContext context)
    {
        // 키워드는 별도 처리 (CardDisplay에서 체크)
        // 이 함수는 키워드 '부여' 시에만 호출
        if (context.sourceCard != null)
        {
            context.sourceCard.AddKeyword(keyword);
            Debug.Log($"[효과] {context.sourceCard.cardData.cardName}에게 {keyword} 부여!");
        }
    }

    public override string GetDescription()
    {
        return keyword switch
        {
            Keyword.Taunt => "<b>도발</b> - 이 카드를 먼저 공격해야 합니다.",
            Keyword.Charge => "<b>돌진</b> - 소환 즉시 공격할 수 있습니다.",
            Keyword.Stealth => "<b>은신</b> - 공격하기 전까지 대상으로 지정할 수 없습니다.",
            Keyword.Divine => "<b>천상의 보호막</b> - 처음 받는 피해를 무효화합니다.",
            Keyword.Lifesteal => "<b>생명력 흡수</b> - 이 카드가 주는 피해만큼 영웅이 회복합니다.",
            Keyword.Poison => "<b>독</b> - 이 카드에게 피해를 입은 적은 파괴됩니다.",
            Keyword.Windfury => "<b>질풍</b> - 한 턴에 두 번 공격할 수 있습니다.",
            Keyword.Reborn => "<b>환생</b> - 처음 죽을 때 체력 1로 부활합니다.",
            Keyword.Seduce => "<b>유혹</b> - 공격 시 적 영웅의 흥분도가 증가합니다.",
            Keyword.Immune => "<b>면역</b> - 주문과 효과의 대상이 될 수 없습니다.",
            _ => ""
        };
    }
}
