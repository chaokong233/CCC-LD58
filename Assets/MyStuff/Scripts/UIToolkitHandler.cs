using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class UIToolkitHandler : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [Header("Toolkit")]
    public GameObject toolkit;
    public TextMeshProUGUI toolkitText;
    public HexUIController uIController;
    [Header("Ability")]
    public int AbilityIndex = 1;
    public string attributeText = "";

    private void Start()
    {
        var button = GetComponent<Button>();
        if (button)
        {
            button.onClick.AddListener(OnButtonClicked);
        }
        if (toolkit)
            toolkit.SetActive(false);
    }

    /// <summary>
    /// 按钮进入时
    /// </summary>
    public void OnPointerEnter(PointerEventData eventData)
    {
        if (toolkit)
            toolkit.SetActive(true);
        UpdateTextInfo();
    }

    /// <summary>
    /// 更新UI文本
    /// </summary>
    private void UpdateTextInfo()
    {
        if (!toolkitText) return;

        TileType tileType = uIController.currentSelectedTile.tileType;

        string text = "";
        switch(AbilityIndex)
        {
            case 1:
                text += $"温柔催账\n低收益但长期有效的方式.\n";
                break;
            case 2:
                text += $"法律催账\n高收益，但长期来看并不明智.\n";
                break;
            case 3:
                text += $"平息众怒\n紧急情况下使用.\n";
                break;
            case 4:
                text += $"暴力催账\n这是立即见效的.\n";
                break;
            case 5:
                text += $"平息众怒\n这将平息人民的情绪，使他们不对周边地区产生影响.";
                break;
            case 6:
                text += $"平复\n这将重新解锁这个地区.";
                break;

        }

        text += $"\n花费: {HexTile.abilityCost[(int)tileType, AbilityIndex]}\n";
        text += attributeText;

        toolkitText.text = text;
    }

    /// <summary>
    /// 按钮离开时
    /// </summary>
    public void OnPointerExit(PointerEventData eventData)
    {
        if (toolkit)
            toolkit.SetActive(false);
    }

    /// <summary>
    /// 按钮点击事件
    /// </summary>
    private void OnButtonClicked()
    {
        if (toolkit)
            toolkit.SetActive(false);
    }

}