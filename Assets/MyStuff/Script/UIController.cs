using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class HexUIController : MonoBehaviour
{
    [Header("UI引用")]
    public GameObject hexPanel;
    public Button unlockButton;
    public TextMeshProUGUI tileInfoText;
    public TextMeshProUGUI costText;
    public TextMeshProUGUI fundsText;

    [Header("解锁设置")]
    public float unlockCost = 100f;

    [Header("面板设置")]
    public Vector2 panelOffset = new Vector2(0, 80f); // 面板偏移量
    public float screenRLMargin = 20f; // 屏幕边距
    public float screenUpMargin = 150f; // 屏幕边距

    private HexTile currentSelectedTile;
    private GameManager gameManager;
    private Camera mainCamera;
    private RectTransform panelRectTransform;

    void Start()
    {
        mainCamera = Camera.main;
        gameManager = FindFirstObjectByType<GameManager>();

        // 绑定按钮事件
        unlockButton.onClick.AddListener(OnUnlockButtonClicked);

        // 获取面板的RectTransform
        if (hexPanel != null)
            panelRectTransform = hexPanel.GetComponent<RectTransform>();

        // 初始隐藏面板
        if (hexPanel != null)
            hexPanel.SetActive(false);
    }

    void Update()
    {
        // 检测鼠标点击 // 检查是否点击在UI上
        if (Input.GetMouseButtonDown(0) && !EventSystem.current.IsPointerOverGameObject())
        {
            HandleTileClick();    
        }

        // 如果开着，更新Panel Info
        if (hexPanel.activeSelf)
            UpdatePanelInfo();

        // 更新资金显示
        if (gameManager != null && fundsText != null)
        {
            fundsText.text = $"资金: {gameManager.currentFunds:F0}";
        }

        // 按ESC关闭面板
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            HidePanel();
        }
    }

    /// <summary>
    /// 处理地块点击
    /// </summary>
    void HandleTileClick()
    {
        Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
        RaycastHit2D hit = Physics2D.Raycast(ray.origin, ray.direction);

        if (hit.collider != null)
        {
            HexTile tile = hit.collider.GetComponent<HexTile>();
            if (tile != null)
            {
                ShowUnlockPanel(tile);
                return;
            }
        }

        // 点击空白处隐藏面板
        HidePanel();
    }

    /// <summary>
    /// 显示解锁面板
    /// </summary>
    void ShowUnlockPanel(HexTile tile)
    {
        currentSelectedTile = tile;

        if (hexPanel != null)
        {
            hexPanel.SetActive(true);
            SetUnlockCost(currentSelectedTile.debtCost);

            // 更新面板位置（确保在屏幕内）
            UpdatePanelPosition(tile.transform.position);

            // 更新UI信息
            UpdatePanelInfo();
        }
    }

    /// <summary>
    /// 更新面板位置，确保在屏幕范围内
    /// </summary>
    void UpdatePanelPosition(Vector3 worldPosition)
    {
        if (panelRectTransform == null) return;

        // 将世界坐标转换为屏幕坐标
        Vector3 screenPos = mainCamera.WorldToScreenPoint(worldPosition);

        // 应用偏移量
        screenPos += (Vector3)panelOffset;

        // 获取面板尺寸
        float panelWidth = panelRectTransform.rect.width;
        float panelHeight = panelRectTransform.rect.height;

        // 计算面板边界
        float minX = panelWidth / 2 + screenRLMargin;
        float maxX = Screen.width - panelWidth / 2 - screenRLMargin;
        float minY = panelHeight / 2;
        float maxY = Screen.height - panelHeight / 2 - screenUpMargin;

        // 限制面板在屏幕范围内
        screenPos.x = Mathf.Clamp(screenPos.x, minX, maxX);
        screenPos.y = Mathf.Clamp(screenPos.y, minY, maxY);

        // 设置面板位置
        hexPanel.transform.position = screenPos;

        // 可选：如果面板位置被调整，可以添加一个箭头指向地块
        // 这里可以添加一个箭头指向地块的视觉指示器
    }

    /// <summary>
    /// 隐藏面板
    /// </summary>
    public void HidePanel()
    {
        if (hexPanel != null)
            hexPanel.SetActive(false);

        currentSelectedTile = null;
    }

    /// <summary>
    /// 更新面板信息
    /// </summary>
    void UpdatePanelInfo()
    {
        if (currentSelectedTile == null) return;

        // 更新地块信息文本
        if (currentSelectedTile.isUnlocked)
        {
            tileInfoText.text = $"地块 {currentSelectedTile.tileName}\n"
                + string.Format("当前收账率:{0:P1}\n", currentSelectedTile.currentCollectionRate)
                + string.Format("民怨值:{0:F1}\n", currentSelectedTile.resistanceLevel)
                + string.Format("支持度:{0:F1}\n", currentSelectedTile.supportLevel);
        }
        else
        {
            tileInfoText.text = $"地块 {currentSelectedTile.tileName}\n";
        }

        // 更新成本文本
        costText.text = $"解锁成本: {unlockCost}";

        // 根据资金情况更新按钮状态
        if (gameManager != null)
        {
            unlockButton.interactable = gameManager.currentFunds >= unlockCost && !currentSelectedTile.isUnlocked;

            if (currentSelectedTile.isUnlocked)
            {
                unlockButton.interactable = false;
                costText.text = "已解锁";
            }
        }
    }

    /// <summary>
    /// 解锁按钮点击事件
    /// </summary>
    public void OnUnlockButtonClicked()
    {
        Debug.Log("ClickButton");
        if (currentSelectedTile != null && gameManager != null)
        {
            // 尝试解锁地块
            if (gameManager.UnlockTile(currentSelectedTile.q, currentSelectedTile.r, unlockCost))
            {
                Debug.Log($"成功解锁地块 ({currentSelectedTile.q}, {currentSelectedTile.r})");
                HidePanel();
            }
            else
            {
                Debug.LogWarning("解锁失败：资金不足或地块已解锁");
                // 可以在这里添加解锁失败的反馈（比如按钮抖动、红色闪烁等）
            }
        } 
    }

    /// <summary>
    /// 设置解锁成本
    /// </summary>
    public void SetUnlockCost(float cost)
    {
        unlockCost = cost;
        UpdatePanelInfo();
    }
}