using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 정화 시스템 관리자
/// 페티시 제거/감소, 타락도 정화
/// </summary>
public class PurificationManager : MonoBehaviour
{
    public static PurificationManager instance;

    [Header("정화 방법")]
    public List<PurificationMethod> purificationMethods = new List<PurificationMethod>();

    [Header("설정")]
    public int basePurificationCost = 100;
    public int costPerIntensity = 50;       // 강도당 추가 비용
    public float failChancePerIntensity = 0.1f;  // 강도당 실패 확률

    [Header("UI 참조")]
    public PurificationUI purificationUI;

    [Header("상태")]
    public int playerGold = 0;

    // 이벤트
    public event Action<FetishType, bool> OnPurificationAttempted;
    public event Action<FetishType> OnPurificationSuccess;

    void Awake()
    {
        if (instance == null) instance = this;
        else Destroy(gameObject);
    }

    /// <summary>
    /// 정화 UI 열기
    /// </summary>
    public void OpenPurificationUI()
    {
        if (purificationUI != null)
        {
            var fetishStates = PlayerFetishState.instance?.GetAcquiredFetishes();
            if (fetishStates != null)
            {
                // FetishState 리스트를 FetishType 리스트로 변환
                List<FetishType> fetishTypes = new List<FetishType>();
                foreach (var state in fetishStates)
                {
                    if (state != null && state.isAcquired)
                        fetishTypes.Add(state.type);
                }
                purificationUI.Show(fetishTypes);
            }
        }
    }

    /// <summary>
    /// 정화 비용 계산
    /// </summary>
    public int CalculatePurificationCost(FetishType fetish)
    {
        int intensity = PlayerFetishState.instance?.GetIntensity(fetish) ?? 1;
        return basePurificationCost + (intensity * costPerIntensity);
    }

    /// <summary>
    /// 정화 성공 확률 계산
    /// </summary>
    public float CalculateSuccessChance(FetishType fetish)
    {
        int intensity = PlayerFetishState.instance?.GetIntensity(fetish) ?? 1;
        return Mathf.Clamp01(1f - (intensity * failChancePerIntensity));
    }

    /// <summary>
    /// 정화 시도
    /// </summary>
    public bool AttemptPurification(FetishType fetish)
    {
        if (PlayerFetishState.instance == null) return false;

        int cost = CalculatePurificationCost(fetish);

        // 비용 확인
        if (playerGold < cost)
        {
            Debug.Log("<color=yellow>골드가 부족합니다.</color>");
            return false;
        }

        // 비용 차감
        playerGold -= cost;

        // 성공 확률
        float successChance = CalculateSuccessChance(fetish);
        bool success = UnityEngine.Random.value <= successChance;

        OnPurificationAttempted?.Invoke(fetish, success);

        if (success)
        {
            // 정화 성공
            PlayerFetishState.instance.PurifyFetish(fetish);
            OnPurificationSuccess?.Invoke(fetish);

            Debug.Log($"<color=cyan>정화 성공: {fetish}</color>");
            return true;
        }
        else
        {
            // 정화 실패
            Debug.Log($"<color=red>정화 실패: {fetish}</color>");
            return false;
        }
    }

    /// <summary>
    /// 특수 정화 방법 사용
    /// </summary>
    public bool UsePurificationMethod(PurificationMethod method, FetishType target)
    {
        if (method == null) return false;

        // 조건 확인
        if (!method.CanUse(playerGold))
        {
            Debug.Log("정화 방법 사용 조건을 충족하지 않습니다.");
            return false;
        }

        // 비용 차감
        playerGold -= method.goldCost;

        // 효과 적용
        bool success = method.Apply(target);

        if (success)
        {
            OnPurificationSuccess?.Invoke(target);
        }

        return success;
    }

    /// <summary>
    /// EP 감소 (휴식 등)
    /// </summary>
    public void ReduceEP(int amount)
    {
        if (HeroPortrait.playerHero != null)
        {
            HeroPortrait.playerHero.ReduceLust(amount);
            Debug.Log($"<color=cyan>EP -{amount}</color>");
        }
    }
}

/// <summary>
/// 정화 방법 데이터
/// </summary>
[System.Serializable]
public class PurificationMethod
{
    [Header("기본 정보")]
    public string methodName;
    
    [TextArea(2, 4)]
    public string description;

    [Header("비용")]
    public int goldCost;
    public int focusCost;
    public string requiredItem;

    [Header("효과")]
    public PurificationEffectType effectType;
    public int effectValue;
    public float successRate = 1f;

    [Header("제한")]
    public int usesPerRun = -1;         // -1 = 무제한
    public bool onlyInChurch = true;

    public bool CanUse(int currentGold)
    {
        if (currentGold < goldCost) return false;
        // TODO: 아이템 체크, 사용 횟수 체크
        return true;
    }

    public bool Apply(FetishType target)
    {
        if (UnityEngine.Random.value > successRate)
            return false;

        switch (effectType)
        {
            case PurificationEffectType.ReduceIntensity:
                PlayerFetishState.instance?.PurifyFetish(target);
                return true;

            case PurificationEffectType.RemoveCompletely:
                // 완전 제거 (강도 0으로)
                while (PlayerFetishState.instance?.HasWeakness(target) ?? false)
                {
                    PlayerFetishState.instance?.PurifyFetish(target);
                }
                return true;

            case PurificationEffectType.ReduceEP:
                HeroPortrait.playerHero?.ReduceLust(effectValue);
                return true;

            case PurificationEffectType.ReduceAllIntensity:
                // 모든 페티시 강도 1 감소
                var fetishes = PlayerFetishState.instance?.GetAcquiredFetishes();
                if (fetishes != null)
                {
                    foreach (var f in fetishes)
                    {
                        PlayerFetishState.instance.PurifyFetish(f.type);
                    }
                }
                return true;

            default:
                return false;
        }
    }
}

public enum PurificationEffectType
{
    ReduceIntensity,        // 강도 1 감소
    RemoveCompletely,       // 완전 제거
    ReduceEP,               // EP 감소
    ReduceAllIntensity      // 모든 페티시 강도 감소
}

