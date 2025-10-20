using UnityEngine;

public class BedController : MonoBehaviour
{
    [Header("床圖片切換")]
    public SpriteRenderer bedRenderer;
    public Sprite defaultBed;
    public Sprite quiltBed;
    public Sprite neatBed;

    [Header("可生成物件")]
    public GameObject chestPrefab;
    public Transform chestSpawnPoint;

    private bool chestSpawned = false;

    public void ChangeImage(string state)
    {
        if (bedRenderer == null) return;

        switch (state)
        {
            case "bed_quilt":
                bedRenderer.sprite = quiltBed;
                break;
            case "bed_neat":
                bedRenderer.sprite = neatBed;
                break;
            default:
                bedRenderer.sprite = defaultBed;
                break;
        }
        Debug.Log($"🛏️ 床圖片切換為：{state}");
    }

    public void SpawnObject(string objName)
    {
        if (objName == "chest" && !chestSpawned && chestPrefab != null)
        {
            Instantiate(chestPrefab, chestSpawnPoint.position, Quaternion.identity);
            chestSpawned = true;
            Debug.Log("📦 床底箱子已生成");
        }
    }
}
