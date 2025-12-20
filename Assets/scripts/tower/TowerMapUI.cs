using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 탑 맵 UI
/// 층별 노드 맵 표시 및 이동
/// </summary>
public class TowerMapUI : MonoBehaviour
{
    [Header("메인 패널")]
    public GameObject mapPanel;
    public CanvasGroup canvasGroup;

    [Header("맵 정보")]
    public TMP_Text floorText;
    public Image mapBackground;

    [Header("노드 컨테이너")]
    public Transform nodeContainer;
    public GameObject nodePrefab;

    [Header("연결선")]
    public GameObject connectionLinePrefab;
    public Transform connectionContainer;

    [Header("선택된 노드 정보")]
    public GameObject nodeInfoPanel;
    public TMP_Text nodeNameText;
    public TMP_Text nodeTypeText;
    public TMP_Text nodeDescriptionText;
    public Button enterNodeButton;
    public Button closeMapButton;

    // 상태
    private FloorData _currentFloor;
    private NodeData _selectedNode;
    private Dictionary<string, GameObject> _nodeObjects = new Dictionary<string, GameObject>();

    void Start()
    {
        if (mapPanel != null)
            mapPanel.SetActive(false);

        if (nodeInfoPanel != null)
            nodeInfoPanel.SetActive(false);

        if (enterNodeButton != null)
            enterNodeButton.onClick.AddListener(OnEnterNodeClicked);

        if (closeMapButton != null)
            closeMapButton.onClick.AddListener(Hide);
    }

    /// <summary>
    /// 맵 표시
    /// </summary>
    public void Show(FloorData floor)
    {
        if (floor == null) return;

        _currentFloor = floor;
        _selectedNode = null;

        if (mapPanel != null)
            mapPanel.SetActive(true);

        if (floorText != null && TowerManager.instance != null)
            floorText.text = $"{TowerManager.instance.currentFloor}층";

        RefreshMap();
    }

    /// <summary>
    /// 맵 숨기기
    /// </summary>
    public void Hide()
    {
        if (mapPanel != null)
            mapPanel.SetActive(false);

        if (nodeInfoPanel != null)
            nodeInfoPanel.SetActive(false);

        _currentFloor = null;
        _selectedNode = null;
    }

    /// <summary>
    /// 맵 새로고침
    /// </summary>
    void RefreshMap()
    {
        if (_currentFloor == null || nodeContainer == null || nodePrefab == null)
            return;

        // 기존 노드 제거
        foreach (Transform child in nodeContainer)
        {
            Destroy(child.gameObject);
        }
        _nodeObjects.Clear();

        // 기존 연결선 제거
        if (connectionContainer != null)
        {
            foreach (Transform child in connectionContainer)
            {
                Destroy(child.gameObject);
            }
        }

        // 노드 생성
        if (_currentFloor.nodes != null)
        {
            foreach (var node in _currentFloor.nodes)
            {
                if (node == null) continue;

                GameObject nodeObj = Instantiate(nodePrefab, nodeContainer);
                Button nodeButton = nodeObj.GetComponent<Button>();
                TMP_Text nodeText = nodeObj.GetComponentInChildren<TMP_Text>();

                if (nodeText != null)
                    nodeText.text = GetNodeTypeName(node.nodeType);

                // 노드 위치 설정 (간단한 그리드 레이아웃)
                RectTransform rect = nodeObj.GetComponent<RectTransform>();
                if (rect != null)
                {
                    // TODO: 실제 노드 위치 데이터 사용
                    rect.anchoredPosition = new Vector2(
                        Random.Range(-400, 400),
                        Random.Range(-300, 300)
                    );
                }

                // 노드 상태에 따른 색상 설정
                Image nodeImage = nodeObj.GetComponent<Image>();
                if (nodeImage != null)
                {
                    nodeImage.color = GetNodeColor(node);
                }

                if (nodeButton != null)
                {
                    NodeData capturedNode = node; // 클로저를 위한 캡처
                    nodeButton.onClick.AddListener(() => OnNodeClicked(capturedNode));
                }

                _nodeObjects[node.nodeId] = nodeObj;
            }
        }

        // 연결선 그리기 (간단한 구현)
        DrawConnections();
    }

    /// <summary>
    /// 연결선 그리기
    /// </summary>
    void DrawConnections()
    {
        if (_currentFloor == null || _currentFloor.nodes == null || connectionLinePrefab == null || connectionContainer == null)
            return;

        // TODO: 실제 연결 관계 데이터 사용
        // 현재는 간단한 구현으로 생략
    }

    /// <summary>
    /// 노드 클릭
    /// </summary>
    void OnNodeClicked(NodeData node)
    {
        _selectedNode = node;

        if (nodeInfoPanel != null)
            nodeInfoPanel.SetActive(true);

        UpdateNodeInfo();
    }

    /// <summary>
    /// 노드 정보 업데이트
    /// </summary>
    void UpdateNodeInfo()
    {
        if (_selectedNode == null) return;

        if (nodeNameText != null)
            nodeNameText.text = _selectedNode.displayName;

        if (nodeTypeText != null)
            nodeTypeText.text = GetNodeTypeName(_selectedNode.nodeType);

        if (nodeDescriptionText != null)
            nodeDescriptionText.text = _selectedNode.displayName; // TODO: description 필드 추가 시 사용

        // 입장 버튼 활성화 여부
        if (enterNodeButton != null)
        {
            bool canEnter = !IsNodeVisited(_selectedNode.nodeId);
            enterNodeButton.interactable = canEnter;
        }
    }

    /// <summary>
    /// 노드 입장
    /// </summary>
    void OnEnterNodeClicked()
    {
        if (_selectedNode == null || TowerManager.instance == null || _currentFloor == null)
            return;

        // 노드 인덱스 찾기
        int nodeIndex = _currentFloor.nodes.IndexOf(_selectedNode);
        if (nodeIndex >= 0)
        {
            TowerManager.instance.EnterNode(nodeIndex);
            Hide();
        }
    }

    /// <summary>
    /// 노드 타입 이름 가져오기
    /// </summary>
    string GetNodeTypeName(NodeType type)
    {
        switch (type)
        {
            case NodeType.Battle: return "전투";
            case NodeType.EliteBattle: return "정예";
            case NodeType.Boss: return "보스";
            case NodeType.Event: return "이벤트";
            case NodeType.Shop: return "상점";
            case NodeType.Rest: return "휴식";
            case NodeType.Treasure: return "보물";
            case NodeType.Unknown: return "???";
            default: return type.ToString();
        }
    }

    /// <summary>
    /// 노드 색상 가져오기
    /// </summary>
    Color GetNodeColor(NodeData node)
    {
        if (IsNodeVisited(node.nodeId))
            return Color.gray;

        switch (node.nodeType)
        {
            case NodeType.Battle: return Color.red;
            case NodeType.EliteBattle: return Color.magenta;
            case NodeType.Boss: return Color.yellow;
            case NodeType.Event: return Color.cyan;
            case NodeType.Shop: return Color.green;
            case NodeType.Rest: return Color.blue;
            case NodeType.Treasure: return Color.yellow;
            default: return Color.white;
        }
    }

    /// <summary>
    /// 노드 방문 여부 확인
    /// </summary>
    bool IsNodeVisited(string nodeId)
    {
        if (TowerManager.instance == null) return false;
        return TowerManager.instance.visitedNodeIds.Contains(nodeId);
    }
}

