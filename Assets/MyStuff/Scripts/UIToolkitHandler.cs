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
                text += $"Gental Collection\nLow-yield but effective long-term strategy.\n";
                break;
            case 2:
                text += $"Legal Collection\nHigh immediate returns, but unwise in the long run.\n";
                break;
            case 3:
                text += $"Quell Collection\nFor emergency use only.\n";
                break;
            case 4:
                text += $"Violent Collection\nThis takes effect immediately.\n";
                break;
            case 5:
                text += $"CalmDown\nThis will quell the public outrage and undermine their ability to unite the surrounding regions.";
                break;
            case 6:
                text += $"Quell\nThis allows for the region to be reinvested in.";
                break;

        }

        text += $"\nCost: {HexTile.abilityCost[(int)tileType, AbilityIndex]}\n";
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