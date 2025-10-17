using UnityEngine;
using UnityEngine.UI;

public class PreviewImageManager : MonoBehaviour
{
    public static PreviewImageManager Instance;
    public Image previewImage;

    void Awake()
    {
        Instance = this;
        if (previewImage != null)
            previewImage.gameObject.SetActive(false);
    }

    public void ShowImage(Sprite sprite)
    {
        if (previewImage == null) return;
        previewImage.sprite = sprite;
        previewImage.gameObject.SetActive(true);
    }

    public void HideImage()
    {
        if (previewImage == null) return;
        previewImage.gameObject.SetActive(false);
    }
}
