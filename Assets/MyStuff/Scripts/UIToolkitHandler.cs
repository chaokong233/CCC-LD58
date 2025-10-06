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
                text += $"Gental Collection\n";
                break;
            case 2:
                text += $"Legal Collection\n";
                break;
            case 3:
                text += $"Quell Collection\n";
                break;
            case 4:
                text += $"Violent Collection\n";
                break;
            case 5:
                text += $"CalmDown\n";
                break;
            case 6:
                text += $"Quell\n";
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