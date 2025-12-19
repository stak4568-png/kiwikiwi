// MonsterCardData.cs
// 몬스터 카드 데이터 - 효과 시스템 통합 버전

using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "NewMonster", menuName = "TCG/Cards/Monster")]
public class MonsterCardData : CardData
{
    [Header("3. 몬스터 스탯")]
    public int attack;
    public int health;
    public int lustAttack;

    [Header("4. 키워드 능력 (기본 보유)")]
    public List<Keyword> keywords = new List<Keyword>();

    [Header("5. 효과 목록")]
    [Tooltip("이 카드가 가진 효과들 (ScriptableObject)")]
    public List<CardEffect> effects = new List<CardEffect>();

    // === 헬퍼 함수들 ===

    /// <summary>
    /// 특정 키워드를 가지고 있는지 확인
    /// </summary>
    public bool HasKeyword(Keyword keyword)
    {
        return keywords.Contains(keyword);
    }

    /// <summary>
    /// 특정 타이밍의 효과가 있는지 확인
    /// </summary>
    public bool HasEffectWithTiming(EffectTiming timing)
    {
        foreach (var effect in effects)
        {
            if (effect != null && effect.timing == timing)
                return true;
        }
        return false;
    }

    /// <summary>
    /// 효과 설명 텍스트 생성 (카드 설명에 추가)
    /// </summary>
    public string GetEffectsDescription()
    {
        string result = "";

        // 키워드 먼저
        foreach (var kw in keywords)
        {
            result += $"<b>{kw}</b> ";
        }

        if (keywords.Count > 0 && effects.Count > 0)
            result += "\n";

        // 효과들
        foreach (var effect in effects)
        {
            if (effect != null)
            {
                string timingStr = effect.timing switch
                {
                    EffectTiming.OnSummon => "<color=green>[소환]</color> ",
                    EffectTiming.OnDeath => "<color=red>[죽음]</color> ",
                    EffectTiming.OnAttack => "<color=orange>[공격]</color> ",
                    EffectTiming.OnTurnStart => "<color=blue>[턴 시작]</color> ",
                    EffectTiming.OnTurnEnd => "<color=purple>[턴 종료]</color> ",
                    _ => ""
                };
                result += timingStr + effect.GetDescription() + "\n";
            }
        }

        return result.TrimEnd();
    }
}
