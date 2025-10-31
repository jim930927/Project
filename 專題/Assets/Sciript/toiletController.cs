using UnityEngine;

public class toiletController : MonoBehaviour
{
    [Header("馬桶圖片切換")]
    public SpriteRenderer toiletRenderer;
    public Sprite defaultToilet;
    public Sprite closeToilet;
    public Sprite openToilet;

    private string currentState = "toilet_close";

    public void ChangeImage(string state)
    {
        if (toiletRenderer == null) return;

        currentState = state; // ✅ 記錄目前狀態（供存檔用）

        switch (state)
        {
            case "toilet_close":
                toiletRenderer.sprite = closeToilet;
                break;
            case "toilet_open":
                toiletRenderer.sprite = openToilet;
                break;
            default:
                toiletRenderer.sprite = defaultToilet;
                break;
        }
        Debug.Log($"🛏️ 馬桶圖片切換為：{state}");
    }

    public string GetCurrentState()
    {
        return currentState;
    }


}
