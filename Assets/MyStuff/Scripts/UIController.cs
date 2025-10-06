using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class HexUIController : MonoBehaviour
{
    [Header("UI引用")]
    [Header("   Panel")]
    public GameObject hexPanel;
    public Button unlockButton;
    public TextMeshProUGUI tileInfoText;
    public TextMeshProUGUI costText;
    public TextMeshProUGUI fundsText;
    [Header("   Ability")]
    public GameObject AbilityPanel_normal;
    public Button AbilityButton_01;
    public Button AbilityButton_02;
    public Button AbilityButton_03;
    public Button AbilityButton_04;
    public GameObject AbilityPanel_quell;
    public Button AbilityButton_05;
    public Button AbilityButton_06;
    [Header("   Pause")]
    public GameObject pausePanel;
    public Button returnToGameButton;
    public Button returnToMenuButton;


    [Header("解锁设置")]
    public float unlockCost = 100f;

    [Header("面板设置")]
    public Vector2 panelOffset = new Vector2(0, 80f); // 面板偏移量
    public float screenRLMargin = 20f; // 屏幕边距
    public float screenUpMargin = 150f; // 屏幕边距

    public HexTile currentSelectedTile;
    private GameManager gameManager;
    private Camera mainCamera;
    private RectTransform panelRectTransform;

    private float currentTime_ = 0;

    void Start()
    {
        currentTime_ = 0;
        mainCamera = Camera.main;
        gameManager = FindFirstObjectByType<GameManager>();

        // 绑定按钮事件
        unlockButton.onClick.AddListener(OnUnlockButtonClicked);

        AbilityButton_01.onClick.AddListener(OnAbilityButton_01_ButtonClicked);
        AbilityButton_02.onClick.AddListener(OnAbilityButton_02_ButtonClicked);
        AbilityButton_03.onClick.AddListener(OnAbilityButton_03_ButtonClicked);
        AbilityButton_04.onClick.AddListener(OnAbilityButton_04_ButtonClicked);
        AbilityButton_05.onClick.AddListener(OnAbilityButton_05_ButtonClicked);
        AbilityButton_06.onClick.AddListener(OnAbilityButton_06_ButtonClicked);

        returnToGameButton.onClick.AddListener(OnReturnToGameButtonClicked);
        returnToMenuButton.onClick.AddListener(OnReturnToMenuButtonClicked);

        // AbilityButton_01.

        // 获取面板的RectTransform
        if (hexPanel != null)
            panelRectTransform = hexPanel.GetComponent<RectTransform>();

        // 初始隐藏面板
        if (hexPanel != null)
            hexPanel.SetActive(false);

        // 初始隐藏面板
        if (pausePanel != null)
            pausePanel.SetActive(false);
    }

    void Update()
    {
        currentTime_ += Time.deltaTime;

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
            fundsText.text = $"Fund: {gameManager.currentFunds:F0}\nTime: {currentTime_/60:F0}min{currentTime_%60:F1}s";
        }

        // 按ESC关闭面板
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (hexPanel.activeSelf)
                HidePanel();
            else if (pausePanel.activeSelf)
            {
                ResumeGame();
            }
            else
                PauseGame();
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
        if (gameManager == null || currentSelectedTile == null) return;

        bool isUnlocked = currentSelectedTile.isUnlocked;
        bool isNotRebel = !currentSelectedTile.isRebelContinent;
        TileType tileType = currentSelectedTile.tileType;

        string tiletext = "";
        switch(currentSelectedTile.tileType)
        {
            case TileType.City:
                tiletext += "City";
                break;
            case TileType.Suburb:
                tiletext += "Suburb";
                break;
            case TileType.Rural:
                tiletext += "Rural";
                break;
            case TileType.Mountain:
                tiletext += "Mountain";
                break;
            case TileType.Lake:
                tiletext += "Lake";
                break;
        }
        if(isNotRebel)
        {
            tiletext += "\n";
        }
        else
        {
            tiletext += "(Rebel)\n"; 
        }
        tiletext += "\n";

        // 更新地块信息文本
        if (isUnlocked && isNotRebel)
        {
            tiletext += string.Format("Collection:{0:P1}\n", currentSelectedTile.currentCollectionRate)
                + string.Format("ResistanceLevel:{0:F2}\n", currentSelectedTile.resistanceLevel)
                + string.Format("SupportLevel:{0:F2}\n", currentSelectedTile.supportLevel)
                + string.Format("LeverageLevel:{0:P1}\n", currentSelectedTile.LeverageLevel);
            tileInfoText.text = tiletext;
        }
        else
        {
            if(!isNotRebel)
            {
                tiletext += "the tile rebel\nCost to Quell it";
            }
            tileInfoText.text = tiletext;
        }

        // 判断是否可解锁，并更新提示
        if (isUnlocked)
        {
            unlockButton.interactable = false;
            costText.text = "Unlocked";
        }
        else
        {
            bool hasUnlockedNeighbor = false;
            foreach (var neighbor in currentSelectedTile.neighbors)
            {
                if (neighbor.isUnlocked)
                {
                    hasUnlockedNeighbor = true;
                    break;
                }
            }

            bool enoughFunds = gameManager.currentFunds >= unlockCost;

            bool isNotObstacle = !currentSelectedTile.isObstacle();

            if (hasUnlockedNeighbor && enoughFunds && isNotObstacle)
            {
                unlockButton.interactable = true;
                // 更新成本文本
                costText.text = $"Lending Cost:{unlockCost}";
            }
            else
            {
                unlockButton.interactable = false;
                if(!isNotObstacle)
                    costText.text = $"(Cannot be unlocked)";
                else if (!hasUnlockedNeighbor)
                    costText.text = $"Lending Cost:{unlockCost}\n(Not adjacent)";
                else if(!enoughFunds)
                    costText.text = $"Lending Cost:{unlockCost}\n(Insufficient funds)";
            }         
        }

        // 更新技能面板
        if (isUnlocked && isNotRebel)
        {
            AbilityPanel_normal.SetActive(true);
            bool isAbilityAvaible = currentSelectedTile.currentDebtCollectionMethodCooldown <= 0;
            if(isAbilityAvaible)
            {
                AbilityButton_01.interactable = gameManager.currentFunds >= HexTile.abilityCost[(int)tileType, (int)DebtCollectionMethod.Gentle];
                AbilityButton_02.interactable = gameManager.currentFunds >= HexTile.abilityCost[(int)tileType, (int)DebtCollectionMethod.Legal];
                AbilityButton_03.interactable = gameManager.currentFunds >= HexTile.abilityCost[(int)tileType, (int)DebtCollectionMethod.Quell];
                AbilityButton_04.interactable = gameManager.currentFunds >= HexTile.abilityCost[(int)tileType, (int)DebtCollectionMethod.Violent];
            }
            else
            {
                AbilityButton_01.interactable = false;
                AbilityButton_02.interactable = false;
                AbilityButton_03.interactable = false;
                AbilityButton_04.interactable = false;
            }
        }
        else
        {
            AbilityPanel_normal.SetActive(false);
            if(!isNotRebel)
            {
                AbilityPanel_quell.SetActive(true);
                AbilityButton_05.interactable = gameManager.currentFunds >= HexTile.abilityCost[(int)tileType, (int)QuellMethod.CalmDown];
                AbilityButton_06.interactable = gameManager.currentFunds >= HexTile.abilityCost[(int)tileType, (int)QuellMethod.Permeation];
            }
            else
            {
                AbilityPanel_quell.SetActive(false);
            }
        }
    }

    /// <summary>
    /// 添加按钮提示
    /// </summary>
    private void AddToolkit(Button button, GameObject toolkitPanel)
    {
        //button.OnPointerEnter.
        //button.onPointerExit.AddListener(OnPointerExit);
        //button.onPointerEnter.AddListener((date) => { OnPointerEnter(toolkitPanel); });
        //button.onPointerExit.AddListener(OnPointerExit);
    }


    /// <summary>
    /// 解锁按钮点击事件
    /// </summary>
    private void OnUnlockButtonClicked()
    {
        Debug.Log("ClickButton");
        if (currentSelectedTile != null && gameManager != null)
        {
            // 尝试解锁地块
            if (gameManager.UnlockTile(currentSelectedTile.q, currentSelectedTile.r, unlockCost))
            {
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
    /// 技能按钮点击事件
    /// </summary>
    private void OnAbilityButton_01_ButtonClicked()
    {
        currentSelectedTile.ExecuteDebtCollection(DebtCollectionMethod.Gentle);
    }

    /// <summary>
    /// 技能按钮点击事件
    /// </summary>
    private void OnAbilityButton_02_ButtonClicked()
    {
        currentSelectedTile.ExecuteDebtCollection(DebtCollectionMethod.Legal);
    }

    /// <summary>
    /// 技能按钮点击事件
    /// </summary>
    private void OnAbilityButton_03_ButtonClicked()
    {
        currentSelectedTile.ExecuteDebtCollection(DebtCollectionMethod.Quell);
    }

    /// <summary>
    /// 技能按钮点击事件
    /// </summary>
    private void OnAbilityButton_04_ButtonClicked()
    {
        currentSelectedTile.ExecuteDebtCollection(DebtCollectionMethod.Violent);
    }

    /// <summary>
    /// 技能按钮点击事件
    /// </summary>
    private void OnAbilityButton_05_ButtonClicked()
    {
        currentSelectedTile.ExecuteQuell(QuellMethod.CalmDown);
    }

    /// <summary>
    /// 技能按钮点击事件
    /// </summary>
    private void OnAbilityButton_06_ButtonClicked()
    {
        currentSelectedTile.ExecuteQuell(QuellMethod.Permeation);
    }

    /// <summary>
    /// 设置解锁成本
    /// </summary>
    public void SetUnlockCost(float cost)
    {
        unlockCost = cost;
        UpdatePanelInfo();
    }


    /// <summary>
    /// 暂停游戏
    /// </summary>
    private void PauseGame()
    {
        pausePanel.SetActive(true);
        Time.timeScale = 0;
    }

    /// <summary>
    /// 恢复游戏
    /// </summary>
    private void ResumeGame()
    {
        pausePanel.SetActive(false);
        Time.timeScale = 1;
    }

    /// <summary>
    /// 按钮点击事件
    /// </summary>
    private void OnReturnToGameButtonClicked()
    {
        ResumeGame();
    }

    /// <summary>
    /// 按钮点击事件
    /// </summary>
    private void OnReturnToMenuButtonClicked()
    {
        SceneManager.LoadScene("MainMenu");
    }
}