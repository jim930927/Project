using UnityEngine;

public class NPCManager : MonoBehaviour
{
    [Header("唯一識別ID（存檔用）")]
    public string uniqueID;

    [System.Serializable]
    public class NPCPrefab
    {
        public string npcName;
        public GameObject prefab;
    }

    public NPCPrefab[] npcList;

    public void SpawnNPC(string npcName)
    {
        foreach (var n in npcList)
        {
            if (n.npcName == npcName && n.prefab != null)
            {
                Instantiate(n.prefab, Vector3.zero, Quaternion.identity);
                Debug.Log($"✅ 生成 NPC：{npcName}");
                return;
            }
        }
        Debug.LogWarning($"⚠️ 找不到 NPC Prefab：{npcName}");
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (string.IsNullOrEmpty(uniqueID))
            uniqueID = System.Guid.NewGuid().ToString();
    }
#endif

}
