using UnityEngine;
using UnityEngine.UI;

public class BookUIManager : MonoBehaviour
{
    public GameObject bookPanel;           // �ѥ� UI Panel�]�i�}���j�ѡ^
    public Button bookIconButton;         // �k�U���ѹϥܫ��s
    public Button closeButton;            // �ѥ������� X �������s

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
