using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 처치 이벤트 관리자
/// 여성 캐릭터가 남성 하수인을 처치할 때 미니 이벤트 발동
/// </summary>
public class KillEventManager : MonoBehaviour
{
    public static KillEventManager instance;

    [Header("기본 이벤트")]
    [Tooltip("기본 남성 처치 이벤트 (전용 이벤트가 없을 때 사용)")]
    public KillEventData defaultMaleKillEvent;

    [Header("카드별 전용 이벤트")]
    public List<KillEventData> cardSpecificEvents = new List<KillEventData>();

    [Header("UI 참조")]
    public KillEventUI eventUI;

    [Header("설정")]
    [Tooltip("처치 이벤트 활성화 여부")]
    public bool enableKillEvents = true;

    // 캐싱
    private Dictionary<string, KillEventData> _eventCache;

    // 이벤트
    public event Action<CardDisplay, CardDisplay> OnKillEventTriggered;

    void Awake()
    {
        if (instance == null) instance = this;
        else Destroy(gameObject);

        BuildEventCache();
    }

    void BuildEventCache()
    {
        _eventCache = new Dictionary<string, KillEventData>();

        foreach (var evt in cardSpecificEvents)
        {
            if (evt != null && evt.specificCard != null)
            {
                _eventCache[evt.specificCard.id] = evt;
            }
        }
    }

    /// <summary>
    /// 적이 처치되었을 때 호출 (CardDisplay.Die()에서 호출)
    /// </summary>
    public void OnEnemyKilled(CardDisplay killer, CardDisplay victim)
    {
        if (!enableKillEvents) return;
        if (killer == null || victim == null) return;
        if (killer.data == null || victim.data == null) return;

        // 성별 조건 체크: 여성이 남성을 처치
        if (killer.data.gender == GenderType.Female && victim.data.gender == GenderType.Male)
        {
            StartCoroutine(TriggerKillEvent(killer, victim));
        }
    }

    /// <summary>
    /// 처치 이벤트 발동
    /// </summary>
    IEnumerator TriggerKillEvent(CardDisplay killer, CardDisplay victim)
    {
        // 이벤트 데이터 가져오기
        KillEventData eventData = GetEventForCard(victim.data);
        if (eventData == null)
        {
            // 기본 이벤트도 없으면 스킵
            yield break;
        }

        Debug.Log($"<color=magenta>처치 이벤트 발동: {killer.data.title} → {victim.data.title}</color>");

        // 이벤트 발동 알림
        OnKillEventTriggered?.Invoke(killer, victim);

        // UI로 이벤트 표시
        if (eventUI != null)
        {
            yield return eventUI.ShowKillEvent(eventData, killer, victim);
        }
        else
        {
            // UI가 없으면 딜레이만
            yield return new WaitForSeconds(eventData.displayDuration);
        }

        // EP 상승
        if (HeroPortrait.playerHero != null && eventData.lustGain > 0)
        {
            HeroPortrait.playerHero.TakeLustDamage(eventData.lustGain, true);
            Debug.Log($"<color=magenta>EP +{eventData.lustGain}</color>");
        }
    }

    /// <summary>
    /// 카드에 맞는 이벤트 데이터 가져오기
    /// </summary>
    KillEventData GetEventForCard(CardData card)
    {
        if (card == null) return defaultMaleKillEvent;

        // 전용 이벤트 확인
        if (_eventCache.TryGetValue(card.id, out var evt))
            return evt;

        // 기본 이벤트 반환
        return defaultMaleKillEvent;
    }

    /// <summary>
    /// 처치 이벤트 활성화/비활성화
    /// </summary>
    public void SetEnabled(bool enabled)
    {
        enableKillEvents = enabled;
    }
}

