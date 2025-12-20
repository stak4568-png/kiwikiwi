using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "NewMonster", menuName = "Kiwi Card Game/Monster Card Data")]
public class MonsterCardData : CardData
{
    [Header("3. 몬스터 기본 스탯")]
    public int attack;          // 공격력 (하수인 공격 시)
    public int health;          // 체력
    public int lustAttack;      // 유혹 공격력 (플레이어 직접 공격 시)

    [Header("4. 유혹 이벤트 연출")]
    [Tooltip("유혹 패널이 뜰 때 나타날 전용 일러스트 (비어있을 경우 카드 아트를 사용)")]
    public Sprite seduceEventArt;

    [Header("5. 키워드 능력 (기본 보유)")]
    [Tooltip("도발, 돌진, 은신 등 기본 키워드 리스트")]
    public List<Keyword> keywords = new List<Keyword>();

    [Header("6. 특수 효과 목록")]
    [Tooltip("이 카드가 가진 발동형 효과들 (ScriptableObject 에셋)")]
    public List<CardEffect> effects = new List<CardEffect>();

    // === 헬퍼 함수 (로직 및 UI용) ===

    /// <summary>
    /// 특정 키워드를 가지고 있는지 확인
    /// </summary>
    public bool HasKeyword(Keyword keyword)
    {
        return keywords != null && keywords.Contains(keyword);
    }

    /// <summary>
    /// 특정 타이밍(소환, 죽음 등)에 발동하는 효과가 있는지 확인
    /// </summary>
    public bool HasEffectWithTiming(EffectTiming timing)
    {
        if (effects == null) return false;
        foreach (var effect in effects)
        {
            if (effect != null && effect.timing == timing)
                return true;
        }
        return false;
    }

    /// <summary>
    /// 카드 UI에 표시될 전체 설명 텍스트를 생성
    /// 키워드 설명 + 특수 효과 설명을 합쳐서 반환합니다.
    /// </summary>
    public string GetEffectsDescription()
    {
        System.Text.StringBuilder sb = new System.Text.StringBuilder();

        // 1. 키워드 표시 (굵게)
        if (keywords != null && keywords.Count > 0)
        {
            foreach (var kw in keywords)
            {
                sb.Append($"<b>[{kw}]</b> ");
            }
            sb.AppendLine(); // 키워드 뒤에 줄바꿈
        }

        // 2. 특수 효과 설명 표시
        if (effects != null && effects.Count > 0)
        {
            foreach (var effect in effects)
            {
                if (effect != null)
                {
                    // 타이밍에 따른 접두사 색상 지정
                    string timingPrefix = effect.timing switch
                    {
                        EffectTiming.OnSummon => "<color=green>[소환]</color> ",
                        EffectTiming.OnDeath => "<color=red>[죽음]</color> ",
                        EffectTiming.OnAttack => "<color=orange>[공격]</color> ",
                        EffectTiming.OnTurnStart => "<color=blue>[턴 시작]</color> ",
                        EffectTiming.OnTurnEnd => "<color=purple>[턴 종료]</color> ",
                        _ => ""
                    };

                    sb.AppendLine($"{timingPrefix}{effect.GetDescription()}");
                }
            }
        }

        return sb.ToString().TrimEnd();
    }
}