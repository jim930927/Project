using UnityEngine;
using UnityEngine.UI;

public class BookUIManager : MonoBehaviour
{
    public GameObject bookPanel;           // 書本 UI Panel（展開的大書）
    public Button bookIconButton;         // 右下角書圖示按鈕
    public Button closeButton;            // 書本內部的 X 關閉按鈕

    void Start()
    {
        if (bookPanel != null)
            bookPanel.SetActive(false);

        if (bookIconButton != null)
            bookIconButton.onClick.AddListener(OpenBook);

        if (closeButton != null)
            closeButton.onClick.AddListener(CloseBook);
    }

    void OpenBook()
    {
        bookPanel.SetActive(true);
    }

    void CloseBook()
    {
        bookPanel.SetActive(false);
    }
}
