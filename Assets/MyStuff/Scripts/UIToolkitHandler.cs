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
    /// ��ť����ʱ
    /// </summary>
    public void OnPointerEnter(PointerEventData eventData)
    {
        if (toolkit)
            toolkit.SetActive(true);
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