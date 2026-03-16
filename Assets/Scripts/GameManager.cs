using UnityEngine;

/// <summary>
/// Kết nối MessageReceiver (HTTP) với MessageSpawner (hiển thị).
///
/// Scene setup:
///   1. Tạo empty GameObject, đặt tên "GameManager"
///   2. Gắn 3 script: MessageReceiver, MessageSpawner, GameManager
///   3. Kéo prefab vào slot "Message Prefab" của MessageSpawner
///   4. Nhấn Play
/// </summary>
public class GameManager : MonoBehaviour
{
    public MessageReceiver receiver;
    public MessageSpawner spawner;

    [Header("Test trong Editor")]
    public string testMessage = "Xin chào!";

    void Awake()
    {
        if (receiver == null) receiver = GetComponent<MessageReceiver>();
        if (spawner == null) spawner = GetComponent<MessageSpawner>();
    }

    void Start()
    {
        receiver.OnMessageReceived += spawner.ShowMessage;

        Debug.Log("[GameManager] Sẵn sàng. Gửi POST tới http://localhost:8080/message/");
    }

    void OnDestroy()
    {
        if (receiver != null)
            receiver.OnMessageReceived -= spawner.ShowMessage;
    }

}
