using UnityEngine;
using TMPro;

/// <summary>
/// Khi nhận được message, Instantiate 1 prefab tại vị trí chỉ định
/// và gán nội dung vào TextMeshPro bên trong prefab.
///
/// Cách setup prefab:
///   1. Tạo một GameObject bất kỳ (ví dụ: Cube, hoặc empty)
///   2. Thêm child GameObject có component TextMeshPro
///   3. Kéo prefab đó vào slot "Message Prefab" ở Inspector
/// </summary>
public class MessageSpawner : MonoBehaviour
{
    [Header("Prefab")]
    [Tooltip("Prefab phải có TextMeshPro ở bất kỳ đâu trong children")]
    public GameObject messagePrefab;

    [Header("Spawn")]
    public Vector3 spawnPosition = Vector3.zero;

    public System.Action OnMessageShown;

    // Gọi hàm này khi nhận message (từ GameManager / MessageReceiver)
    public void ShowMessage(string message)
    {
        if (messagePrefab == null)
        {
            Debug.LogWarning("[MessageSpawner] Chưa gán Message Prefab!");
            return;
        }

        // Instantiate prefab
        GameObject instance = Instantiate(messagePrefab, spawnPosition, Quaternion.identity);
        OnMessageShown?.Invoke();
        // Tìm TextMeshPro trong prefab và gán nội dung
        TextMeshPro tmp = instance.GetComponentInChildren<TextMeshPro>();
        if (tmp != null)
        {
            tmp.text = message;
        }
        else
        {
            Debug.LogWarning("[MessageSpawner] Không tìm thấy TextMeshPro trong prefab!");
        }
    }
}
