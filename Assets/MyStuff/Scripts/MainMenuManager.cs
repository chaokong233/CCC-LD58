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
        // ȷ����������ʾ���̳�����
        ShowMainPanel();

        // ��ʼ���̳�ҳ��
        InitializeTutorial();
    }

    // ��ʼ��Ϸ��ť����¼�
    public void OnStartGameClicked()
    {
        Time.timeScale = 1;

        // ������Ϸ����
        SceneManager.LoadScene("CCCScene");

        // ����ʹ���첽���ػ�ø��õ�����
        // StartCoroutine(LoadGameSceneAsync());
    }

    // �̳̰�ť����¼�
    public void OnTutorialClicked()
    {
        ShowTutorialPanel();
    }

    // ��ʾ�����
    public void ShowMainPanel()
    {
        menuPanel.SetActive(true);
        tutorialPanel.SetActive(false);
    }

    // ��ʾ�̳����
    public void ShowTutorialPanel()
    {
        tutorialPanel.SetActive(true);
        ShowPage(0); // ��ʾ��һҳ
    }

    // ��ʼ���̳�
    private void InitializeTutorial()
    {
        // ��������ҳ��
        foreach (var page in tutorialPages)
        {
            page.SetActive(false);
        }

        // ��Ӱ�ť�¼�����
        nextButton.onClick.AddListener(NextPage);
        prevButton.onClick.AddListener(PreviousPage);
        closeButton.onClick.AddListener(CloseTutorial);
    }

    // ��ʾָ��ҳ��
    private void ShowPage(int pageIndex)
    {
        // ��������ҳ��
        foreach (var page in tutorialPages)
        {
            page.SetActive(false);
        }

        // ��ʾ��ǰҳ��
        tutorialPages[pageIndex].SetActive(true);
        currentPageIndex = pageIndex;

        // ���°�ť״̬
        UpdateNavigationButtons();
    }

    // ��һҳ
    public void NextPage()
    {
        if (currentPageIndex < tutorialPages.Length - 1)
        {
            ShowPage(currentPageIndex + 1);
        }
        else
        {
            // ��������һҳ������ѭ���ص���һҳ��رս̳�
            ShowPage(0);
        }
    }

    // ��һҳ
    public void PreviousPage()
    {
        if (currentPageIndex > 0)
        {
            ShowPage(currentPageIndex - 1);
        }
        else
        {
            // ����ǵ�һҳ�������������һҳ
            ShowPage(tutorialPages.Length - 1);
        }
    }

    // �رս̳�
    public void CloseTutorial()
    {
        ShowMainPanel();
    }

    // ���µ�����ť״̬
    private void UpdateNavigationButtons()
    {
        return;
        // ���Ը�����Ҫ����/���ð�ť
        //prevButton.interactable = currentPageIndex > 0;
        //nextButton.interactable = currentPageIndex < tutorialPages.Length - 1;
    }

    // �첽���س�������ѡ��
    private System.Collections.IEnumerator LoadGameSceneAsync()
    {
        AsyncOperation asyncLoad = SceneManager.LoadSceneAsync("GameScene");

        // ������������Ӽ��ؽ���
        while (!asyncLoad.isDone)
        {
            // ���¼��ؽ�������
            yield return null;
        }
    }
}