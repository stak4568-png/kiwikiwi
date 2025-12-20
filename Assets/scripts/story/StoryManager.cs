using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 스토리 이벤트 관리자
/// VN 스타일 대화 시스템
/// </summary>
public class StoryManager : MonoBehaviour
{
    public static StoryManager instance;

    [Header("상태")]
    public bool isPlayingStory = false;
    public StoryData currentStory;
    public int currentSceneIndex = 0;
    public int currentDialogueIndex = 0;

    [Header("스토리 데이터베이스")]
    public List<StoryData> allStories = new List<StoryData>();

    [Header("플래그")]
    public Dictionary<string, int> storyFlags = new Dictionary<string, int>();
    public List<string> completedEventIds = new List<string>();

    [Header("UI 참조")]
    public StoryUI storyUI;

    // 이벤트
    public event Action<StoryData> OnStoryStarted;
    public event Action<StoryData> OnStoryCompleted;
    public event Action<StoryChoice> OnChoiceMade;

    void Awake()
    {
        if (instance == null) instance = this;
        else Destroy(gameObject);
    }

    /// <summary>
    /// 스토리 이벤트 시작
    /// </summary>
    public void StartStory(StoryData story)
    {
        if (story == null || isPlayingStory) return;

        currentStory = story;
        currentSceneIndex = 0;
        currentDialogueIndex = 0;
        isPlayingStory = true;

        OnStoryStarted?.Invoke(story);

        StartCoroutine(PlayStorySequence());
    }

    /// <summary>
    /// ID로 스토리 시작
    /// </summary>
    public void StartStoryById(string eventId)
    {
        StoryData story = allStories.Find(s => s.eventId == eventId);
        if (story != null)
        {
            StartStory(story);
        }
        else
        {
            Debug.LogWarning($"스토리를 찾을 수 없음: {eventId}");
        }
    }

    /// <summary>
    /// 스토리 시퀀스 재생
    /// </summary>
    IEnumerator PlayStorySequence()
    {
        if (currentStory == null) yield break;

        // 배경 및 BGM 설정
        if (storyUI != null)
        {
            storyUI.Show();
            storyUI.SetBackground(currentStory.backgroundImage);
            storyUI.PlayBGM(currentStory.backgroundMusic);
        }

        // 씬 순회
        while (currentSceneIndex < currentStory.scenes.Length)
        {
            StoryScene scene = currentStory.scenes[currentSceneIndex];
            yield return PlayScene(scene);

            // 선택지가 있으면 대기
            if (scene.choices != null && scene.choices.Length > 0)
            {
                yield return WaitForChoice(scene);
            }
            else
            {
                currentSceneIndex++;
            }
        }

        // 스토리 완료
        CompleteStory();
    }

    /// <summary>
    /// 씬 재생
    /// </summary>
    IEnumerator PlayScene(StoryScene scene)
    {
        // 배경 오버라이드
        if (scene.backgroundOverride != null && storyUI != null)
        {
            storyUI.SetBackground(scene.backgroundOverride);
        }

        // 대사 순회
        currentDialogueIndex = 0;
        while (currentDialogueIndex < scene.dialogues.Length)
        {
            StoryDialogue dialogue = scene.dialogues[currentDialogueIndex];

            if (storyUI != null)
            {
                storyUI.ShowDialogue(dialogue);
            }

            // 자동 진행 또는 클릭 대기
            if (scene.autoProgress)
            {
                yield return new WaitForSeconds(scene.autoProgressDelay);
            }
            else
            {
                yield return WaitForInput();
            }

            currentDialogueIndex++;
        }
    }

    /// <summary>
    /// 선택지 대기
    /// </summary>
    IEnumerator WaitForChoice(StoryScene scene)
    {
        // 조건 필터링된 선택지 표시
        List<StoryChoice> validChoices = FilterChoices(scene.choices);

        if (validChoices.Count == 0)
        {
            // 유효한 선택지가 없으면 다음 씬으로
            currentSceneIndex++;
            yield break;
        }

        // UI에 선택지 표시
        int selectedIndex = -1;
        if (storyUI != null)
        {
            storyUI.ShowChoices(validChoices, (index) => selectedIndex = index);
        }

        // 선택 대기
        while (selectedIndex < 0)
        {
            yield return null;
        }

        // 선택 처리
        StoryChoice choice = validChoices[selectedIndex];
        ProcessChoice(choice);
    }

    /// <summary>
    /// 선택지 조건 필터링
    /// </summary>
    List<StoryChoice> FilterChoices(StoryChoice[] choices)
    {
        List<StoryChoice> valid = new List<StoryChoice>();

        foreach (var choice in choices)
        {
            bool conditionMet = CheckCondition(choice);

            if (conditionMet || !choice.hideIfConditionFails)
            {
                valid.Add(choice);
            }
        }

        return valid;
    }

    /// <summary>
    /// 선택지 조건 확인
    /// </summary>
    bool CheckCondition(StoryChoice choice)
    {
        switch (choice.conditionType)
        {
            case ChoiceConditionType.None:
                return true;

            case ChoiceConditionType.HasCard:
                return PlayerCollection.instance?.HasCard(choice.conditionValue) ?? false;

            case ChoiceConditionType.HasFetish:
                if (System.Enum.TryParse<FetishType>(choice.conditionValue, out var fetish))
                    return PlayerFetishState.instance?.HasWeakness(fetish) ?? false;
                return false;

            case ChoiceConditionType.HasFlag:
                return storyFlags.ContainsKey(choice.conditionValue) && storyFlags[choice.conditionValue] > 0;

            case ChoiceConditionType.EPAbove:
                if (int.TryParse(choice.conditionValue, out int epThreshold))
                    return HeroPortrait.playerHero?.currentLust >= epThreshold;
                return false;

            case ChoiceConditionType.FloorAbove:
                if (int.TryParse(choice.conditionValue, out int floor))
                    return TowerManager.instance?.currentFloor >= floor;
                return false;

            default:
                return true;
        }
    }

    /// <summary>
    /// 선택 처리
    /// </summary>
    void ProcessChoice(StoryChoice choice)
    {
        OnChoiceMade?.Invoke(choice);

        // 플래그 설정
        if (!string.IsNullOrEmpty(choice.setFlag))
        {
            storyFlags[choice.setFlag] = choice.flagValue;
        }

        // 보상 처리
        if (choice.choiceRewards != null)
        {
            foreach (var reward in choice.choiceRewards)
            {
                ProcessReward(reward);
            }
        }

        // 다음 씬으로 이동
        if (!string.IsNullOrEmpty(choice.nextSceneId))
        {
            // 특정 씬으로 점프
            for (int i = 0; i < currentStory.scenes.Length; i++)
            {
                if (currentStory.scenes[i].sceneId == choice.nextSceneId)
                {
                    currentSceneIndex = i;
                    return;
                }
            }
        }

        currentSceneIndex++;
    }

    /// <summary>
    /// 보상 처리
    /// </summary>
    void ProcessReward(StoryReward reward)
    {
        switch (reward.type)
        {
            case RewardType.Card:
                PlayerCollection.instance?.AddCard(reward.rewardId, reward.amount);
                Debug.Log($"<color=green>카드 획득: {reward.rewardId}</color>");
                break;

            case RewardType.RemoveCard:
                PlayerCollection.instance?.RemoveCard(reward.rewardId, reward.amount);
                Debug.Log($"<color=yellow>카드 제거: {reward.rewardId}</color>");
                break;

            case RewardType.Heal:
                if (HeroPortrait.playerHero != null)
                    HeroPortrait.playerHero.Heal(reward.amount);
                break;

            case RewardType.EPReduce:
                if (HeroPortrait.playerHero != null)
                    HeroPortrait.playerHero.ReduceLust(reward.amount);
                break;

            case RewardType.EPIncrease:
                if (HeroPortrait.playerHero != null)
                    HeroPortrait.playerHero.TakeLustDamage(reward.amount, false);
                break;

            case RewardType.Fetish:
                if (System.Enum.TryParse<FetishType>(reward.rewardId, out var fetish))
                    PlayerFetishState.instance?.AcquireFetish(fetish);
                break;

            case RewardType.Flag:
                storyFlags[reward.rewardId] = reward.amount;
                break;

            case RewardType.UnlockGallery:
                GalleryManager.instance?.UnlockEntry(reward.rewardId);
                break;
        }
    }

    /// <summary>
    /// 스토리 완료
    /// </summary>
    void CompleteStory()
    {
        if (currentStory == null) return;

        // 보상 처리
        if (currentStory.rewards != null)
        {
            foreach (var reward in currentStory.rewards)
            {
                ProcessReward(reward);
            }
        }

        // 완료 기록
        if (!currentStory.repeatable && !completedEventIds.Contains(currentStory.eventId))
        {
            completedEventIds.Add(currentStory.eventId);
        }

        OnStoryCompleted?.Invoke(currentStory);

        // UI 닫기
        if (storyUI != null)
        {
            storyUI.Hide();
        }

        isPlayingStory = false;
        currentStory = null;

        Debug.Log("<color=cyan>스토리 이벤트 완료</color>");
    }

    IEnumerator WaitForInput()
    {
        while (!Input.GetMouseButtonDown(0) && !Input.GetKeyDown(KeyCode.Space) && !Input.GetKeyDown(KeyCode.Return))
        {
            yield return null;
        }
    }

    /// <summary>
    /// 이벤트 완료 여부 확인
    /// </summary>
    public bool IsEventCompleted(string eventId)
    {
        return completedEventIds.Contains(eventId);
    }

    /// <summary>
    /// 스토리 스킵
    /// </summary>
    public void SkipStory()
    {
        if (!isPlayingStory) return;

        StopAllCoroutines();
        CompleteStory();
    }
}

