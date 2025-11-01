using UnityEngine;

public class SaveableEntity : MonoBehaviour
{
    [Tooltip("用於存檔辨識的唯一ID")]
    public string uniqueID;

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (string.IsNullOrEmpty(uniqueID))
            uniqueID = System.Guid.NewGuid().ToString();
    }
#endif
}
