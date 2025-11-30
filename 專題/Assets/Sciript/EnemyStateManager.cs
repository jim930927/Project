using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 🎮 全場景唯一存在的敵人狀態管理器。
/// 用來追蹤每個敵人是否存在、出現次數、位置與追逐狀態。
/// 並提供 ToJson / LoadFromJson 作為存檔與讀檔的接口。
/// </summary>
[DefaultExecutionOrder(-200)] // 提早執行，確保在其他物件前初始化
public class EnemyStateManager : MonoBehaviour
{
    [Serializable]
    public class EnemyState
    {
        public string enemyId;
        public bool isActive;
        public Vector3 position;
        public bool isChasing;
        public int spawnCount;
        public long lastUpdateUnix;
    }

    [Serializable]
    private class EnemyStateSave
    {
        public List<EnemyState> states = new List<EnemyState>();
    }

    public static EnemyStateManager Instance { get; private set; }

    private readonly Dictionary<string, EnemyState> _states = new Dictionary<string, EnemyState>();
    private readonly Dictionary<string, GameObject> _instances = new Dictionary<string, GameObject>();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(this.gameObject);
    }
    private static long NowUnix() => DateTimeOffset.UtcNow.ToUnixTimeSeconds();

    /// <summary>
    /// 記錄敵人生成。
    /// </summary>
    public void MarkSpawned(string enemyId, GameObject go)
    {
        if (!_states.TryGetValue(enemyId, out var st))
        {
            st = new EnemyState { enemyId = enemyId };
            _states[enemyId] = st;
        }

        st.isActive = true;
        st.position = go.transform.position;
        st.spawnCount += 1;
        st.lastUpdateUnix = NowUnix();

        _instances[enemyId] = go;

        // 若敵人有 Controller，可同步追逐狀態
        var ec = go.GetComponent<EnemyController2D>();
        if (ec != null)
            st.isChasing = ec.isChasing;

        Debug.Log($"[EnemyState] {enemyId} 已生成（第 {st.spawnCount} 次）");
    }

    /// <summary>
    /// 記錄敵人消失。
    /// </summary>
    public void MarkDespawned(string enemyId)
    {
        if (!_states.TryGetValue(enemyId, out var st))
        {
            st = new EnemyState { enemyId = enemyId };
            _states[enemyId] = st;
        }

        st.isActive = false;
        st.lastUpdateUnix = NowUnix();

        _instances.Remove(enemyId);
        Debug.Log($"[EnemyState] {enemyId} 已消失");
    }

    /// <summary>
    /// 可在遊戲中手動更新敵人狀態（例如每幀或追逐變化時呼叫）。
    /// </summary>
    public void UpdateRuntimeState(string enemyId, Vector3 position, bool? isChasing = null)
    {
        if (!_states.TryGetValue(enemyId, out var st))
        {
            st = new EnemyState { enemyId = enemyId };
            _states[enemyId] = st;
        }

        st.position = position;
        if (isChasing.HasValue) st.isChasing = isChasing.Value;
        st.lastUpdateUnix = NowUnix();
    }

    /// <summary>
    /// 轉換為 JSON 用於存檔。
    /// </summary>
    public string ToJson(bool pretty = false)
    {
        var save = new EnemyStateSave();
        save.states.AddRange(_states.Values);
        return JsonUtility.ToJson(save, pretty);
    }

    /// <summary>
    /// 從 JSON 還原敵人狀態（包含是否啟用、位置、追逐狀態）。
    /// </summary>
    public void LoadFromJson(string json)
    {
        if (string.IsNullOrWhiteSpace(json)) return;
        var save = JsonUtility.FromJson<EnemyStateSave>(json);
        if (save == null || save.states == null) return;

        _states.Clear();
        foreach (var st in save.states)
            _states[st.enemyId] = st;

        // 還原實體
        foreach (var kv in _states)
        {
            string id = kv.Key;
            var st = kv.Value;

            if (_instances.TryGetValue(id, out var go) && go != null)
            {
                if (go.activeSelf != st.isActive)
                    go.SetActive(st.isActive);

                go.transform.position = st.position;

                var ec = go.GetComponent<EnemyController2D>();
                if (ec != null)
                {
                    if (st.isChasing && !ec.isChasing) ec.StartChase();
                    else if (!st.isChasing && ec.isChasing) ec.StopChase();
                }
            }
        }

        Debug.Log("[EnemyStateManager] 敵人狀態已還原。");
    }

    public int GetSpawnCount(string enemyId)
        => _states.TryGetValue(enemyId, out var st) ? st.spawnCount : 0;

    public bool TryGetState(string enemyId, out EnemyState state)
        => _states.TryGetValue(enemyId, out state);
}
