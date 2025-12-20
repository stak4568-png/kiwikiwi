using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 플레이어의 페티시 상태 관리
/// 약점이 되는 페티시와 그 강도를 추적
/// </summary>
[System.Serializable]
public class FetishState
{
    public FetishType type;
    public int intensity;           // 0~3, 높을수록 강한 약점
    public int exposureCount;       // 노출 횟수 (3회 노출 시 획득)
    public bool isAcquired;         // 획득 여부
}

public class PlayerFetishState : MonoBehaviour
{
    public static PlayerFetishState instance;

    [Header("페티시 상태")]
    public List<FetishState> fetishStates = new List<FetishState>();

    [Header("초기 선택")]
    public bool hasSelectedInitialFetish = false;
    public FetishType initialFetish = FetishType.None;

    [Header("설정")]
    [Tooltip("페티시당 추가 EP 상승 (강도별)")]
    public int[] intensityBonusPercent = { 0, 25, 50, 100 };

 
    public event Action<FetishType, int> OnFetishAcquired;
    public event Action<FetishType> OnFetishPurified;
    public event Action<FetishType, int> OnFetishIntensityChanged;

    void Awake()
    {
        if (instance == null)
        {
            instance = this;
            InitializeFetishStates();
        }
        else Destroy(gameObject);
    }

    void InitializeFetishStates()
    {
        // 모든 페티시 타입에 대해 상태 초기화
        foreach (FetishType type in System.Enum.GetValues(typeof(FetishType)))
        {
            if (type == FetishType.None) continue;

            fetishStates.Add(new FetishState
            {
                type = type,
                intensity = 0,
                exposureCount = 0,
                isAcquired = false
            });
        }
    }

    /// <summary>
    /// 초기 페티시 선택 (게임 시작 시)
    /// </summary>
    public void SelectInitialFetish(FetishType type)
    {
        if (type == FetishType.None) return;

        initialFetish = type;
        hasSelectedInitialFetish = true;

        // 초기 페티시는 강도 1로 시작
        AcquireFetish(type, 1);

        Debug.Log($"<color=magenta>초기 페티시 선택: {type}</color>");
    }

    /// <summary>
    /// 페티시 획득
    /// </summary>
    public void AcquireFetish(FetishType type, int intensity = 1)
    {
        FetishState state = GetFetishState(type);
        if (state == null) return;

        if (!state.isAcquired)
        {
            state.isAcquired = true;
            state.intensity = Mathf.Clamp(intensity, 1, 3);
            OnFetishAcquired?.Invoke(type, state.intensity);

            Debug.Log($"<color=magenta>페티시 획득: {type} (강도 {state.intensity})</color>");
        }
        else
        {
            // 이미 획득한 경우 강도 증가
            IncreaseIntensity(type);
        }
    }

    /// <summary>
    /// 페티시 강도 증가
    /// </summary>
    public void IncreaseIntensity(FetishType type)
    {
        FetishState state = GetFetishState(type);
        if (state == null || !state.isAcquired) return;

        if (state.intensity < 3)
        {
            state.intensity++;
            OnFetishIntensityChanged?.Invoke(type, state.intensity);

            Debug.Log($"<color=magenta>페티시 강도 증가: {type} → {state.intensity}</color>");
        }
    }

    /// <summary>
    /// 유혹에 노출됨 (3회 노출 시 획득)
    /// </summary>
    public void OnExposedToFetish(FetishType type)
    {
        if (type == FetishType.None) return;

        FetishState state = GetFetishState(type);
        if (state == null) return;

        if (!state.isAcquired)
        {
            state.exposureCount++;
            Debug.Log($"<color=yellow>{type} 노출: {state.exposureCount}/3</color>");

            if (state.exposureCount >= 3)
            {
                AcquireFetish(type);
            }
        }
        else
        {
            // 이미 획득한 경우 일정 확률로 강도 증가
            if (UnityEngine.Random.value < 0.2f)
            {
                IncreaseIntensity(type);
            }
        }
    }

    /// <summary>
    /// 페티시 정화
    /// </summary>
    public bool PurifyFetish(FetishType type)
    {
        FetishState state = GetFetishState(type);
        if (state == null || !state.isAcquired) return false;

        // 강도 감소
        state.intensity--;

        if (state.intensity <= 0)
        {
            state.isAcquired = false;
            state.intensity = 0;
            state.exposureCount = 0;
            OnFetishPurified?.Invoke(type);

            Debug.Log($"<color=cyan>페티시 정화 완료: {type}</color>");
        }
        else
        {
            OnFetishIntensityChanged?.Invoke(type, state.intensity);
            Debug.Log($"<color=cyan>페티시 강도 감소: {type} → {state.intensity}</color>");
        }

        return true;
    }

    /// <summary>
    /// 특정 페티시에 약한지 확인
    /// </summary>
    public bool HasWeakness(FetishType type)
    {
        FetishState state = GetFetishState(type);
        return state != null && state.isAcquired;
    }

    /// <summary>
    /// 페티시 강도 가져오기
    /// </summary>
    public int GetIntensity(FetishType type)
    {
        FetishState state = GetFetishState(type);
        return state != null && state.isAcquired ? state.intensity : 0;
    }

    /// <summary>
    /// 추가 EP 상승률 계산
    /// </summary>
    public int GetBonusLustPercent(FetishType type)
    {
        int intensity = GetIntensity(type);
        if (intensity <= 0 || intensity >= intensityBonusPercent.Length)
            return 0;

        return intensityBonusPercent[intensity];
    }

    /// <summary>
    /// 마나 방어 가능 여부
    /// </summary>
    public bool CanDefendWithMana(FetishType type)
    {
        // 약점인 경우 마나 방어 불가
        return !HasWeakness(type);
    }

    /// <summary>
    /// 획득한 페티시 목록
    /// </summary>
    public List<FetishState> GetAcquiredFetishes()
    {
        return fetishStates.FindAll(s => s.isAcquired);
    }

    FetishState GetFetishState(FetishType type)
    {
        return fetishStates.Find(s => s.type == type);
    }
}

