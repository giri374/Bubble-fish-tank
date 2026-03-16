using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
public class TestMessageButton : MonoBehaviour
{
    [Header("Cấu hình")]
    [Tooltip("Kéo GameObject chứa GameManager vào đây")]
    public GameManager gameManager;

    [Tooltip("Message sẽ gửi khi nhấn nút (nếu không dùng giá trị mặc định trong GameManager)")]
    public string messageToSend = "Test từ Button!";

    private Button button;

    void Awake()
    {
        button = GetComponent<Button>();

        // Tự động tìm GameManager nếu chưa gán
        if (gameManager == null)
        {
            gameManager = FindObjectOfType<GameManager>();
            if (gameManager == null)
            {
                Debug.LogWarning("[TestMessageButton] Không tìm thấy GameManager trong scene!");
            }
        }
    }

    void OnEnable()
    {
        if (button != null)
        {
            button.onClick.AddListener(OnButtonClicked);
        }
    }

    void OnDisable()
    {
        if (button != null)
        {
            button.onClick.RemoveListener(OnButtonClicked);
        }
    }

    void OnButtonClicked()
    {
        if (gameManager == null)
        {
            Debug.LogError("[TestMessageButton] GameManager chưa được gán!");
            return;
        }

        gameManager.spawner?.ShowMessage(messageToSend);

        Debug.Log($"[TestMessageButton] Đã gửi message test: {messageToSend}");
    }

    // Optional: Hiển thị thông tin trong Inspector khi hover
    void OnValidate()
    {
        if (gameManager == null && FindObjectOfType<GameManager>() != null)
        {
            gameManager = FindObjectOfType<GameManager>();
        }
    }
}