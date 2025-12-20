using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 탑 등반 관리자
/// 층별 진행, 노드 이동, 전투/이벤트 발동
/// </summary>
public class TowerManager : MonoBehaviour
{
    public static TowerManager instance;

    [Header("탑 구조")]
    public List<FloorData> floors = new List<FloorData>();

    [Header("현재 상태")]
    public int currentFloor = 1;
    public int currentNodeIndex = 0;
    public TowerState towerState = TowerState.InVillage;

    [Header("진행 기록")]
    public List<string> visitedNodeIds = new List<string>();
    public List<string> clearedBosses = new List<string>();

    [Header("UI 참조")]
    public TowerMapUI mapUI;

    // 현재 층 데이터
    public FloorData CurrentFloorData => (currentFloor > 0 && currentFloor <= floors.Count) 
        ? floors[currentFloor - 1] : null;

    // 이벤트
    public event Action<int> OnFloorChanged;
    public event Action<NodeData> OnNodeEntered;
    public event Action<NodeData> OnNodeCleared;

    void Awake()
    {
        if (instance == null) instance = this;
        else Destroy(gameObject);
    }

    /// <summary>
    /// 탑 시작 (1층부터)
    /// </summary>
    public void StartTower()
    {
        currentFloor = 1;
        currentNodeIndex = 0;
        towerState = TowerState.InVillage;
        visitedNodeIds.Clear();

        OnFloorChanged?.Invoke(currentFloor);

        Debug.Log("<color=cyan>탑 등반 시작!</color>");
    }

    /// <summary>
    /// 노드로 이동
    /// </summary>
    public void EnterNode(int nodeIndex)
    {
        FloorData floor = CurrentFloorData;
        if (floor == null || nodeIndex < 0 || nodeIndex >= floor.nodes.Count)
        {
            Debug.LogWarning("잘못된 노드 인덱스");
            return;
        }

        NodeData node = floor.nodes[nodeIndex];

        // 이동 가능 여부 체크
        if (!CanMoveToNode(nodeIndex))
        {
            Debug.Log("이 노드로 이동할 수 없습니다.");
            return;
        }

        currentNodeIndex = nodeIndex;
        visitedNodeIds.Add(node.nodeId);

        OnNodeEntered?.Invoke(node);

        // 노드 타입에 따른 처리
        ProcessNode(node);
    }

    /// <summary>
    /// 노드 이동 가능 여부
    /// </summary>
    bool CanMoveToNode(int nodeIndex)
    {
        FloorData floor = CurrentFloorData;
        if (floor == null) return false;

        NodeData node = floor.nodes[nodeIndex];

        // 시작 노드는 항상 이동 가능
        if (node.isStartNode) return true;

        // 현재 노드와 연결되어 있는지 확인
        NodeData current = floor.nodes[currentNodeIndex];
        return current.connectedNodeIndices.Contains(nodeIndex);
    }

    /// <summary>
    /// 노드 처리
    /// </summary>
    void ProcessNode(NodeData node)
    {
        towerState = TowerState.InNode;

        switch (node.nodeType)
        {
            case NodeType.Battle:
                StartBattle(node);
                break;

            case NodeType.EliteBattle:
                StartBattle(node, isElite: true);
                break;

            case NodeType.Boss:
                StartBossBattle(node);
                break;

            case NodeType.Event:
                StartEvent(node);
                break;

            case NodeType.Rest:
                EnterRestSite(node);
                break;

            case NodeType.Shop:
                OpenShop(node);
                break;

            case NodeType.Treasure:
                OpenTreasure(node);
                break;

            case NodeType.Village:
                EnterVillage();
                break;
        }
    }

    void StartBattle(NodeData node, bool isElite = false)
    {
        Debug.Log($"<color=orange>{(isElite ? "엘리트" : "")} 전투 시작: {node.displayName}</color>");
        // TODO: BattleManager.instance.StartBattle(node.enemyData);
    }

    void StartBossBattle(NodeData node)
    {
        Debug.Log($"<color=red>보스 전투: {node.displayName}</color>");
        // TODO: BattleManager.instance.StartBossBattle(node.bossData);
    }

    void StartEvent(NodeData node)
    {
        if (node.eventData != null && StoryManager.instance != null)
        {
            StoryManager.instance.StartStory(node.eventData);
        }
    }

    void EnterRestSite(NodeData node)
    {
        Debug.Log("휴식처 입장");
        // TODO: RestUI 표시 (회복/카드 강화 등)
    }

    void OpenShop(NodeData node)
    {
        Debug.Log("상점 입장");
        // TODO: ShopUI 표시
    }

    void OpenTreasure(NodeData node)
    {
        Debug.Log("보물 발견!");
        // TODO: 보상 UI 표시
    }

    /// <summary>
    /// 마을 입장
    /// </summary>
    public void EnterVillage()
    {
        towerState = TowerState.InVillage;
        Debug.Log("<color=green>마을 입장</color>");

        if (VillageManager.instance != null)
        {
            VillageManager.instance.EnterVillage(currentFloor);
        }
    }

    /// <summary>
    /// 노드 클리어
    /// </summary>
    public void OnNodeComplete(NodeData node, bool success)
    {
        if (success)
        {
            OnNodeCleared?.Invoke(node);

            // 보스 클리어 시 다음 층으로
            if (node.nodeType == NodeType.Boss)
            {
                if (!clearedBosses.Contains(node.nodeId))
                    clearedBosses.Add(node.nodeId);

                GoToNextFloor();
            }
        }
        else
        {
            // 패배 시 마을로 복귀
            EnterVillage();
        }
    }

    /// <summary>
    /// 다음 층으로 이동
    /// </summary>
    void GoToNextFloor()
    {
        if (currentFloor >= floors.Count)
        {
            Debug.Log("<color=cyan>탑 정복 완료!</color>");
            // TODO: 엔딩 처리
            return;
        }

        currentFloor++;
        currentNodeIndex = 0;
        visitedNodeIds.Clear();

        OnFloorChanged?.Invoke(currentFloor);

        // 새 층의 마을에서 시작
        EnterVillage();
    }

    /// <summary>
    /// 맵 열기
    /// </summary>
    public void OpenMap()
    {
        if (mapUI != null)
            mapUI.Show(CurrentFloorData);
    }
}

public enum TowerState
{
    InVillage,      // 마을에 있음
    InNode,         // 노드 진행 중
    InBattle,       // 전투 중
    InEvent         // 이벤트 중
}

