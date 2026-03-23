using NativeWebSocket;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

/// <summary>
/// Quản lý kết nối WebSocket và nhận dữ liệu từ Flutter/Web client.
/// Gắn script này vào một GameObject rỗng trong Scene (ví dụ: "WebSocketManager").
/// </summary>
public class WebSocketManager : MonoBehaviour
{
    [Header("WebSocket Settings")]
    [Tooltip("Địa chỉ WebSocket server (ví dụ: ws://192.168.1.x:8080)")]
    public string serverUrl = "ws://localhost:8080";
    [Tooltip("Tự động kết nối lại khi mất kết nối")]
    public bool autoReconnect = true;
    [Tooltip("Thời gian chờ giữa các lần reconnect (giây)")]
    public float reconnectDelay = 3f;

    [Header("Prefab & Scene References")]
    [Tooltip("Prefab của vật thể sẽ được Instantiate khi nhận dữ liệu")]
    public GameObject objectPrefab;
    [Tooltip("Vị trí spawn của vật thể")]
    public Transform spawnPoint;
    [Tooltip("Canvas để hiển thị UI khi va chạm")]
    public Canvas targetCanvas;

    // Events để các script khác có thể lắng nghe
    public static event Action<ReceivedData> OnDataReceived;
    public static event Action OnConnected;
    public static event Action OnDisconnected;

    // Singleton
    public static WebSocketManager Instance { get; private set; }

    private WebSocket _webSocket;
    private bool _isConnecting = false;
    private Queue<ReceivedData> _dataQueue = new Queue<ReceivedData>();
    private readonly object _queueLock = new object();

    // Queue để lưu pending text messages (xử lý multiple messages)
    private Queue<string> _pendingTexts = new Queue<string>();
    private readonly object _textQueueLock = new object();

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }
    }

    private void Start()
    {
        ConnectToServer();
    }

    private void Update()
    {
#if !UNITY_WEBGL || UNITY_EDITOR
        _webSocket?.DispatchMessageQueue();
#endif
        // Xử lý dữ liệu nhận được trên Main Thread (vì Unity API không thread-safe)
        ProcessDataQueue();
    }

    // ─────────────────────────────────────────────
    //  KẾT NỐI
    // ─────────────────────────────────────────────

    public async void ConnectToServer()
    {
        if (_isConnecting) return;
        _isConnecting = true;

        Debug.Log($"[WebSocket] Đang kết nối tới {serverUrl}...");

        _webSocket = new WebSocket(serverUrl);

        _webSocket.OnOpen += () =>
        {
            Debug.Log("[WebSocket] ✅ Kết nối thành công!");
            _isConnecting = false;
            OnConnected?.Invoke();
        };

        _webSocket.OnClose += (code) =>
        {
            Debug.Log($"[WebSocket] ❌ Đã ngắt kết nối. Code: {code}");
            _isConnecting = false;
            OnDisconnected?.Invoke();
            if (autoReconnect && this != null && gameObject != null)
                StartCoroutine(ReconnectAfterDelay());
        };

        _webSocket.OnError += (error) =>
        {
            Debug.LogError($"[WebSocket] Lỗi: {error}");
            _isConnecting = false;
        };

        _webSocket.OnMessage += HandleIncomingMessage;

        await _webSocket.Connect();
    }

    private IEnumerator ReconnectAfterDelay()
    {
        yield return new WaitForSeconds(reconnectDelay);
        if (this != null && gameObject != null)
            ConnectToServer();
    }

    // ─────────────────────────────────────────────
    //  XỬ LÝ TIN NHẮN ĐẾN
    // ─────────────────────────────────────────────

    /// <summary>
    /// Giao thức nhận:
    /// - Nếu là JSON bắt đầu bằng '{': đây là metadata (text + có thể base64 ảnh)
    /// - Nếu là binary: đây là raw PNG bytes
    /// 
    /// Định dạng JSON gửi lên:
    /// { "text": "Hello", "image": "<base64_string_or_empty>" }
    /// </summary>
    private void HandleIncomingMessage(byte[] bytes)
    {
        // Thử parse là UTF-8 text trước
        string message = Encoding.UTF8.GetString(bytes);

        if (message.TrimStart().StartsWith("{"))
        {
            // Đây là JSON payload
            try
            {
                WebSocketPayload payload = JsonUtility.FromJson<WebSocketPayload>(message);
                ReceivedData data = new ReceivedData { text = payload.text };

                if (!string.IsNullOrEmpty(payload.image))
                {
                    // Ảnh được gửi dưới dạng base64
                    byte[] imgBytes = Convert.FromBase64String(payload.image);
                    data.imageBytes = imgBytes;
                }

                lock (_queueLock) { _dataQueue.Enqueue(data); }
                Debug.Log($"[WebSocket] ✅ Nhận JSON: text='{payload.text}', image={(!string.IsNullOrEmpty(payload.image) ? "có" : "không")}");
            }
            catch (Exception e)
            {
                Debug.LogError($"[WebSocket] ❌ Lỗi parse JSON: {e.Message}");
            }
        }
        else if (bytes.Length > 4 && bytes[0] == 0x89 && bytes[1] == 0x50)
        {
            // PNG magic bytes → đây là raw image binary
            string pendingText = null;
            lock (_textQueueLock)
            {
                if (_pendingTexts.Count > 0)
                {
                    pendingText = _pendingTexts.Dequeue();
                }
            }

            if (pendingText != null)
            {
                ReceivedData data = new ReceivedData { text = pendingText, imageBytes = bytes };
                lock (_queueLock) { _dataQueue.Enqueue(data); }
                Debug.Log($"[WebSocket] ✅ Nhận PNG binary (text='{pendingText}', size={bytes.Length} bytes)");
            }
            else
            {
                // Nếu không có pending text, tạo record với image only
                ReceivedData data = new ReceivedData { text = "", imageBytes = bytes };
                lock (_queueLock) { _dataQueue.Enqueue(data); }
                Debug.LogWarning($"[WebSocket] ⚠️ Nhận PNG nhưng không có pending text (size={bytes.Length} bytes)");
            }
        }
        else
        {
            // Có thể là text-only message (không có ảnh), chờ binary tiếp theo
            string textMessage = message.Trim();
            lock (_textQueueLock)
            {
                _pendingTexts.Enqueue(textMessage);
            }
            Debug.Log($"[WebSocket] 📝 Nhận text: '{textMessage}' (đang chờ image...) - Pending queue: {_pendingTexts.Count}");
        }
    }

    // ─────────────────────────────────────────────
    //  XỬ LÝ QUEUE TRÊN MAIN THREAD
    // ─────────────────────────────────────────────

    private void ProcessDataQueue()
    {
        lock (_queueLock)
        {
            while (_dataQueue.Count > 0)
            {
                ReceivedData data = _dataQueue.Dequeue();
                SpawnObject(data);
                OnDataReceived?.Invoke(data);
            }
        }
    }

    private void SpawnObject(ReceivedData data)
    {
        if (objectPrefab == null) { Debug.LogWarning("Chưa gán objectPrefab!"); return; }

        // Tự tìm Canvas nếu chưa gán
        if (targetCanvas == null)
            targetCanvas = FindObjectOfType<Canvas>();

        if (targetCanvas == null) { Debug.LogError("Không tìm thấy Canvas nào trong Scene!"); return; }

        Vector3 spawnPos = spawnPoint != null ? spawnPoint.position : Vector3.zero;
        GameObject obj = Instantiate(objectPrefab, spawnPos, Quaternion.identity);

        ObjectDataHolder dataHolder = obj.GetComponent<ObjectDataHolder>();
        if (dataHolder == null) dataHolder = obj.AddComponent<ObjectDataHolder>();
        dataHolder.Initialize(data, targetCanvas);

        Debug.Log($"[WebSocket] ✅ Spawn object, canvas='{targetCanvas.name}'");
    }

    // ─────────────────────────────────────────────
    //  GỬI DỮ LIỆU (nếu cần)
    // ─────────────────────────────────────────────

    public async void SendMessage(string message)
    {
        if (_webSocket != null && _webSocket.State == WebSocketState.Open)
            await _webSocket.SendText(message);
    }

    private async void OnApplicationQuit()
    {
        if (_webSocket != null && _webSocket.State == WebSocketState.Open)
            await _webSocket.Close();
    }

    [ContextMenu("Test Spawn")]
    public void TestSpawn()
    {
        ReceivedData fakeData = new ReceivedData
        {
            text = "Test spawn hoạt động! 🎉",
            imageBytes = null
        };
        SpawnObject(fakeData);
    }
}

// ─────────────────────────────────────────────
//  DATA MODELS
// ─────────────────────────────────────────────

[Serializable]
public class WebSocketPayload
{
    public string text;
    public string image; // base64 PNG
}

[Serializable]
public class ReceivedData
{
    public string text;
    public byte[] imageBytes; // raw PNG bytes
}
