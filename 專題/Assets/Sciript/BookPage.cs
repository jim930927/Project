using UnityEngine;

public class BookPage : MonoBehaviour
{
    public GameObject[] pages; // 頁面陣列（你會拖進去 Page1, Page2, Page3）

    public void ShowPage(int index)
    {
        for (int i = 0; i < pages.Length; i++)
        {
            pages[i].SetActive(i == index);
        }
    }
}
