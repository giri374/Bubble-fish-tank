using UnityEngine;

public class BubbleFloat : MonoBehaviour
{
    [SerializeField] private float upwardSpeed = 1.5f; // Tốc độ bay lên cơ bản
    [SerializeField] private float windStrength = 2.0f; // Độ mạnh của gió
    [SerializeField] private float noiseScale = 0.5f;   // Độ mượt của sự thay đổi hướng gió

    public float holdDuarationEffect = 2f; // Thời gian giữ hiệu ứng 
    public float fadeDurationEffect = 1f; // Thời gian fade in/out hiệu ứng

    private Rigidbody rb;

    private void Start()
    {
        rb = GetComponent<Rigidbody>();
        rb.useGravity = false; // Tắt trọng lực để tự điều khiển
    }

    private void FixedUpdate()
    {
        // 1. Tạo lực bay lên cố định
        Vector3 lift = Vector3.up * upwardSpeed;

        // 2. Tạo gió ngẫu nhiên bằng Perlin Noise để hướng gió thay đổi mượt mà
        float windX = Mathf.PerlinNoise(Time.time * noiseScale, 0) * 2 - 1;
        float windZ = Mathf.PerlinNoise(0, Time.time * noiseScale) * 2 - 1;
        Vector3 wind = new Vector3(windX, 0, windZ) * windStrength;

        // 3. Áp dụng lực vào Rigidbody
        rb.AddForce(lift + wind);
    }
}
