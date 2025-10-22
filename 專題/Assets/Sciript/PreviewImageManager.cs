using UnityEngine;
using UnityEngine.UI;

public class PreviewImageManager : MonoBehaviour
{
    public static PreviewImageManager Instance;
    public Image previewImage;

    void Awake()
    {
        // 不再強制 Destroy 其他 Instance，避免打斷互動流程
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            Debug.Log("🧩 PreviewImageManager 註冊成功：" + gameObject.name);
        }
        else if (Instance != this)
        {
            Debug.LogWarning("⚠️ 發現重複的 PreviewImageManager：" + gameObject.name);
        }

        if (previewImage != null)
        {
            previewImage.gameObject.SetActive(false);
        }
        else
        {
            Debug.LogWarning("⚠️ PreviewImageManager：previewImage 尚未指定！");
        }
    }

    public void ShowImage(Sprite sprite)
    {
        if (previewImage == null)
        {
            Debug.LogWarning("⚠️ ShowImage 失敗：previewImage 為 null");
            return;
        }

        if (sprite == null)
        {
            Debug.LogWarning("⚠️ ShowImage 失敗：傳入的 sprite 為 null");
            return;
        }

        // 確保父物件啟用
        if (previewImage.transform.parent != null)
            previewImage.transform.parent.gameObject.SetActive(true);

        previewImage.sprite = sprite;
        previewImage.enabled = true;
        previewImage.gameObject.SetActive(true);
        previewImage.color = Color.white;

        Debug.Log($"🖼️ 顯示圖片：{sprite.name}");
    }


    public void HideImage()
    {
        if (previewImage == null) return;

        // 不清除 sprite（防止後續再用時還是 null）
        previewImage.enabled = false;
        previewImage.gameObject.SetActive(false);
        Debug.Log("🧩 圖片已隱藏");
    }
}
