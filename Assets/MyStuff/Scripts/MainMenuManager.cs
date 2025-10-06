using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MainMenuManager : MonoBehaviour
{
    [Header("UI Panels")]
    public GameObject menuPanel;
    public GameObject tutorialPanel;

    [Header("Tutorial Settings")]
    public GameObject[] tutorialPages;
    public Button nextButton;
    public Button prevButton;
    public Button closeButton;

    private int currentPageIndex = 0;

    void Start()
    {
        // 确保主界面显示，教程隐藏
        ShowMainPanel();

        // 初始化教程页面
        InitializeTutorial();
    }

    // 开始游戏按钮点击事件
    public void OnStartGameClicked()
    {
        Time.timeScale = 1;

        // 加载游戏场景
        SceneManager.LoadScene("CCCScene");

        // 或者使用异步加载获得更好的体验
        // StartCoroutine(LoadGameSceneAsync());
    }

    // 教程按钮点击事件
    public void OnTutorialClicked()
    {
        ShowTutorialPanel();
    }

    // 显示主面板
    public void ShowMainPanel()
    {
        menuPanel.SetActive(true);
        tutorialPanel.SetActive(false);
    }

    // 显示教程面板
    public void ShowTutorialPanel()
    {
        tutorialPanel.SetActive(true);
        ShowPage(0); // 显示第一页
    }

    // 初始化教程
    private void InitializeTutorial()
    {
        // 隐藏所有页面
        foreach (var page in tutorialPages)
        {
            page.SetActive(false);
        }

        // 添加按钮事件监听
        nextButton.onClick.AddListener(NextPage);
        prevButton.onClick.AddListener(PreviousPage);
        closeButton.onClick.AddListener(CloseTutorial);
    }

    // 显示指定页面
    private void ShowPage(int pageIndex)
    {
        // 隐藏所有页面
        foreach (var page in tutorialPages)
        {
            page.SetActive(false);
        }

        // 显示当前页面
        tutorialPages[pageIndex].SetActive(true);
        currentPageIndex = pageIndex;

        // 更新按钮状态
        UpdateNavigationButtons();
    }

    // 下一页
    public void NextPage()
    {
        if (currentPageIndex < tutorialPages.Length - 1)
        {
            ShowPage(currentPageIndex + 1);
        }
        else
        {
            // 如果是最后一页，可以循环回到第一页或关闭教程
            ShowPage(0);
        }
    }

    // 上一页
    public void PreviousPage()
    {
        if (currentPageIndex > 0)
        {
            ShowPage(currentPageIndex - 1);
        }
        else
        {
            // 如果是第一页，可以跳到最后一页
            ShowPage(tutorialPages.Length - 1);
        }
    }

    // 关闭教程
    public void CloseTutorial()
    {
        ShowMainPanel();
    }

    // 更新导航按钮状态
    private void UpdateNavigationButtons()
    {
        return;
        // 可以根据需要禁用/启用按钮
        //prevButton.interactable = currentPageIndex > 0;
        //nextButton.interactable = currentPageIndex < tutorialPages.Length - 1;
    }

    // 异步加载场景（可选）
    private System.Collections.IEnumerator LoadGameSceneAsync()
    {
        AsyncOperation asyncLoad = SceneManager.LoadSceneAsync("GameScene");

        // 可以在这里添加加载界面
        while (!asyncLoad.isDone)
        {
            // 更新加载进度条等
            yield return null;
        }
    }
}