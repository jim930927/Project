using UnityEngine;

/// <summary>
/// 🧠 每個敵人的身份腳本，負責在啟用/停用時通知 EnemyStateManager。
/// 掛在「敵人 Prefab」或場景中每個敵人物件上。
/// </summary>
[DisallowMultipleComponent]
public class EnemyIdentity : MonoBehaviour
{
    [Tooltip("敵人唯一ID（請在 Inspector 手動指定，例如：Enemy_A、Enemy_B、Boss1）")]
    public string enemyId;

    private void Awake()
    {
        // 若忘了填 ID，自動用物件名稱代替
        if (string.IsNullOrWhiteSpace(enemyId))
        {
            enemyId = gameObject.name;
            Debug.LogWarning($"[EnemyIdentity] 未設定 enemyId，暫用物件名稱：{enemyId}");
        }
    }

    private void OnEnable()
    {
        // 生成時自動上報
        EnemyStateManager.Instance?.MarkSpawned(enemyId, this.gameObject);
    }

    private void OnDisable()
    {
        // 關閉/消失時自動上報
        EnemyStateManager.Instance?.MarkDespawned(enemyId);
    }
}
