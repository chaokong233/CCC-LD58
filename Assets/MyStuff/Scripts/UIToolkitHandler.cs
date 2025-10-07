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
    /// ��ť����ʱ
    /// </summary>
    public void OnPointerEnter(PointerEventData eventData)
    {
        if (toolkit)
            toolkit.SetActive(true);
        UpdateTextInfo();
    }

    /// <summary>
    /// ����UI�ı�
    /// </summary>
    private void UpdateTextInfo()
    {
        if (!toolkitText) return;

        TileType tileType = uIController.currentSelectedTile.tileType;

        string text = "";
        switch(AbilityIndex)
        {
            case 1:
                text += $"�������\n�����浫������Ч�ķ�ʽ.\n";
                break;
            case 2:
                text += $"���ɴ���\n�����棬������������������.\n";
                break;
            case 3:
                text += $"ƽϢ��ŭ\n���������ʹ��.\n";
                break;
            case 4:
                text += $"��������\n����������Ч��.\n";
                break;
            case 5:
                text += $"ƽϢ��ŭ\n�⽫ƽϢ�����������ʹ���ǲ����ܱߵ�������Ӱ��.";
                break;
            case 6:
                text += $"ƽ��\n�⽫���½����������.";
                break;

        }

        text += $"\n����: {HexTile.abilityCost[(int)tileType, AbilityIndex]}\n";
        text += attributeText;

        toolkitText.text = text;
    }

    /// <summary>
    /// ��ť�뿪ʱ
    /// </summary>
    public void OnPointerExit(PointerEventData eventData)
    {
        if (toolkit)
            toolkit.SetActive(false);
    }

    /// <summary>
    /// ��ť����¼�
    /// </summary>
    private void OnButtonClicked()
    {
        if (toolkit)
            toolkit.SetActive(false);
    }

}