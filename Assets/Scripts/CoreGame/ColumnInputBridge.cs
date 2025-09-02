using UnityEngine;

/// <summary>
/// Kết nối sự kiện chọn cột từ GridController tới SpawnQueue.
/// Gắn script này trong scene, tham chiếu GridController & SpawnQueue.
/// </summary>
public class ColumnInputBridge : MonoBehaviour
{
    [SerializeField] private GridController gridController;
    [SerializeField] private SpawnQueue spawnQueue;
    [SerializeField] private bool debugLog = true;

    private void Awake()
    {
        if (gridController == null) gridController = FindFirstObjectByType<GridController>();
        if (spawnQueue == null) spawnQueue = FindFirstObjectByType<SpawnQueue>();
    }

    private void OnEnable()
    {
        if (gridController != null)
        {
            gridController.OnColumnSelected += HandleColumnSelected;
            if (debugLog) Debug.Log("[ColumnInputBridge] Subscribed to OnColumnSelected");
        }
    }

    private void OnDisable()
    {
        if (gridController != null)
        {
            gridController.OnColumnSelected -= HandleColumnSelected;
            if (debugLog) Debug.Log("[ColumnInputBridge] Unsubscribed from OnColumnSelected");
        }
    }

    private void HandleColumnSelected(int column)
    {
        if (spawnQueue == null) return;
        bool ok = spawnQueue.SpawnIntoColumn(column);
        if (debugLog) Debug.Log($"[ColumnInputBridge] SpawnIntoColumn({column}) result={ok}");
    }
}
