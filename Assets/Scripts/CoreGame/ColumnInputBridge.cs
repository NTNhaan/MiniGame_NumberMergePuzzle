using UnityEngine;

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
        }
    }

    private void OnDisable()
    {
        if (gridController != null)
        {
            gridController.OnColumnSelected -= HandleColumnSelected;
        }
    }

    private void HandleColumnSelected(int column)
    {
        if (spawnQueue == null) return;
        bool ok = spawnQueue.SpawnIntoColumn(column);
    }
}
