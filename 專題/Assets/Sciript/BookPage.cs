using UnityEngine;

public class BookPage : MonoBehaviour
{
    public GameObject[] pages; // �����}�C�]�A�|��i�h Page1, Page2, Page3�^

    public void ShowPage(int index)
    {
        for (int i = 0; i < pages.Length; i++)
        {
            pages[i].SetActive(i == index);
        }
    }
}
