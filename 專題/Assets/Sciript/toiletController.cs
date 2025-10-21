using UnityEngine;

public class toiletController : MonoBehaviour
{
    [Header("馬桶圖片切換")]
    public SpriteRenderer toiletRenderer;
    public Sprite defaultToilet;
    public Sprite closeToilet;
    public Sprite openToilet;

    public void ChangeImage(string state)
    {
        if (toiletRenderer == null) return;

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
}
