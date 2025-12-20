using UnityEngine;

[CreateAssetMenu(fileName = "New Climax Event", menuName = "Kiwi Card Game/Special Event/Climax Data")]
public class ClimaxEventData : ScriptableObject
{
    [Header("이벤트 기본 정보")]
    public string eventTitle;

    [Header("연출 자원")]
    public Sprite climaxIllustration;   // 화면 전체에 뜰 고퀄리티 일러스트

    [Header("텍스트 연출")]
    [TextArea(3, 5)]
    public string[] dialogueLines;       // 순차적으로 출력될 대사들

    [Header("게임 결과")]
    public bool isImmediateGameOver = true; // 이벤트 종료 후 바로 패배 처리할지 여부
}