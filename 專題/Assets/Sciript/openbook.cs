using UnityEngine;
using UnityEngine.UI;

public class openbook : MonoBehaviour
{
    public Button bookIconButton;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        if (bookIconButton != null)
            bookIconButton.gameObject.SetActive(true); // 預設隱藏書本圖示
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
