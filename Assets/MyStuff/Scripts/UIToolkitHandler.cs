using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class UIToolkitHandler : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [Header("Toolkit")]
    public GameObject toolkit;

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