using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro; // Nếu dùng TextMeshPro; đổi thành UnityEngine.UI.Text nếu dùng Legacy Text

/// <summary>
/// Gắn script này vào GameObject Prefab.
/// Lưu dữ liệu nhận từ WebSocket và hiển thị lên Canvas khi va chạm.
/// </summary>
[RequireComponent(typeof(Collider))] // hoặc Collider2D nếu dùng 2D
public class ObjectDataHolder : MonoBehaviour
{
    [Header("Collision UI Settings")]
    [Tooltip("Tên tag của object sẽ trigger hiển thị UI (ví dụ: 'Player' hoặc 'Ground')")]
    public string collisionTag = "Player";
    [Tooltip("Tự động ẩn UI sau bao nhiêu giây (0 = không tự ẩn)")]
    public float autoDismissDelay = 5f;

    [Header("Prefab Settings")]
    [Tooltip("Prefab chứa Panel, Image, và TextMeshPro")]
    public GameObject dataPanelPrefab;

    // ─── Dữ liệu nhận từ WebSocket ───
    private string _text;
    private byte[] _imageBytes;
    private Sprite _sprite;
    private Canvas _targetCanvas;

    // ─── UI References (sẽ được tạo động) ───
    private GameObject _uiPanel;
    private bool _uiShown = false;
    private Collider _collidingObject;
    private Camera _mainCamera;

    // ─── Gọi từ WebSocketManager sau khi Instantiate ───
    public void Initialize(ReceivedData data, Canvas canvas)
    {
        _text = data.text;
        _imageBytes = data.imageBytes;
        _targetCanvas = canvas;

        if (_imageBytes != null && _imageBytes.Length > 0)
            StartCoroutine(LoadImageFromBytes(_imageBytes));
    }

    // ─────────────────────────────────────────────
    //  LOAD TEXTURE TỪ BYTES
    // ─────────────────────────────────────────────

    IEnumerator LoadImageFromBytes(byte[] bytes)
    {
        Texture2D tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
        bool loaded = tex.LoadImage(bytes); // Hỗ trợ PNG/JPG
        if (loaded)
        {
            _sprite = Sprite.Create(
                tex,
                new Rect(0, 0, tex.width, tex.height),
                new Vector2(0.5f, 0.5f)
            );
            Debug.Log($"[ObjectDataHolder] ✅ Đã load ảnh: {tex.width}x{tex.height}");
        }
        else
        {
            Debug.LogWarning("[ObjectDataHolder] ❌ Không thể load ảnh từ bytes.");
        }
        yield return null;
    }

    // ─────────────────────────────────────────────
    //  VA CHẠM 3D
    // ─────────────────────────────────────────────

    void OnCollisionEnter(Collision collision)
    {
        if (!_uiShown && (string.IsNullOrEmpty(collisionTag) || collision.gameObject.CompareTag(collisionTag)))
        {
            _collidingObject = collision.collider;
            ShowUI();
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (!_uiShown && (string.IsNullOrEmpty(collisionTag) || other.CompareTag(collisionTag)))
        {
            _collidingObject = other;
            ShowUI();
        }
    }


    /// <summary>
    /// Convert vị trí World thành vị trí trên Canvas sử dụng Raycast
    /// Tối ưu cho Orthographic camera + Screen Space - Overlay
    /// </summary>
    Vector2 GetCanvasPosition(Vector3 worldPos)
    {
        if (_mainCamera == null)
            _mainCamera = Camera.main;

        if (_mainCamera == null)
        {
            Debug.LogWarning("[ObjectDataHolder] Không tìm thấy Main Camera!");
            return Vector2.zero;
        }

        RectTransform canvasRect = _targetCanvas.GetComponent<RectTransform>();
        if (canvasRect == null)
        {
            Debug.LogWarning("[ObjectDataHolder] Canvas không có RectTransform!");
            return Vector2.zero;
        }

        // Tạo ray từ camera qua vị trí world position
        Vector3 screenPos = _mainCamera.WorldToScreenPoint(worldPos);
        Ray ray = _mainCamera.ScreenPointToRay(screenPos);

        // Tạo plane vuông góc với camera, đi qua vị trí object
        Plane canvasPlane = new Plane(-_mainCamera.transform.forward, worldPos);

        // Raycast để tìm giao điểm chính xác
        if (canvasPlane.Raycast(ray, out float enter))
        {
            Vector3 hitPoint = ray.origin + ray.direction * enter;

            // Chuyển từ world position sang local position tương ứng canvas
            Vector3 localPos = _mainCamera.transform.InverseTransformPoint(hitPoint);

            // Với Orthographic, tính canvas position từ local position
            float orthoHeight = _mainCamera.orthographicSize * 2f;
            float orthoWidth = orthoHeight * _mainCamera.aspect;

            Vector2 canvasPos = new Vector2(
                (localPos.x / orthoWidth) * canvasRect.rect.width,
                (localPos.y / orthoHeight) * canvasRect.rect.height
            );

            return canvasPos;
        }

        Debug.LogWarning("[ObjectDataHolder] Raycast không tìm thấy intersection!");
        return Vector2.zero;
    }

    void ShowUI()
    {
        if (_targetCanvas == null)
        {
            Debug.LogWarning("[ObjectDataHolder] Chưa gán targetCanvas!");
            return;
        }

        if (dataPanelPrefab == null)
        {
            Debug.LogWarning("[ObjectDataHolder] Chưa gán dataPanelPrefab!");
            return;
        }

        _uiShown = true;
        _uiPanel = Instantiate(dataPanelPrefab, _targetCanvas.transform, false);

        // Set vị trí panel tương ứng với vật thể va chạm
        if (_collidingObject != null)
        {
            Vector2 canvasPos = GetCanvasPosition(_collidingObject.bounds.center);
            RectTransform panelRect = _uiPanel.GetComponent<RectTransform>();
            if (panelRect != null)
            {
                panelRect.anchoredPosition = canvasPos;
            }
        }

        // Tìm Image component và gán sprite
        Image imageComponent = _uiPanel.GetComponentInChildren<Image>(true);
        if (imageComponent != null && _sprite != null)
        {
            imageComponent.sprite = _sprite;
            imageComponent.preserveAspect = true;
        }

        // Tìm TextMeshProUGUI component và gán text
        TMP_Text textComponent = _uiPanel.GetComponentInChildren<TMP_Text>(true);
        if (textComponent != null && !string.IsNullOrEmpty(_text))
        {
            textComponent.text = _text;
        }

        // Tìm Button để xử lý sự kiện đóng
        Button closeButton = _uiPanel.GetComponentInChildren<Button>(true);
        if (closeButton != null)
        {
            closeButton.onClick.AddListener(HideUI);
        }

        if (autoDismissDelay > 0)
            StartCoroutine(AutoDismiss());

        Debug.Log($"[ObjectDataHolder] 🖼️ Hiển thị UI: '{_text}'");
    }

    public void HideUI()
    {
        if (_uiPanel != null)
        {
            Destroy(_uiPanel);
            _uiPanel = null;
            _uiShown = false;
        }
    }

    IEnumerator AutoDismiss()
    {
        yield return new WaitForSeconds(autoDismissDelay);
        HideUI();
    }

    void OnDestroy()
    {
        HideUI();
    }
}
