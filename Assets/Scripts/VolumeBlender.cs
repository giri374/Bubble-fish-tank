using System.Collections;
using UnityEngine;
using UnityEngine.Rendering;

public class VolumeBlender : MonoBehaviour
{
    public Volume volume2; // Volume hiệu ứng (đặt Priority cao hơn Volume 1)
    private float fadeDuration; // Thời gian fade in/out
    private float holdDuration; // Thời gian giữ

    public void Start()
    {

    }

    // Hàm này để bạn gọi từ bên ngoài (ví dụ: khi ăn item, trúng đòn)

    public void TriggerWaterEffect()
    {
        TriggerWaterEffect(fadeDuration, holdDuration);
    }
    private void TriggerWaterEffect(float fadeDuration, float holdDuration)
    {
        StopAllCoroutines();
        StartCoroutine(ProcessWaterColorEffect(fadeDuration, holdDuration));
    }

    private IEnumerator ProcessWaterColorEffect(float fade, float hold)
    {
        float elapsed = 0;
        // Lưu lại giá trị weight hiện tại làm điểm bắt đầu
        float startWeight = volume2.weight;

        // 1. FADE IN: Từ currentWeight lên 1
        while (elapsed < fade)
        {
            elapsed += Time.deltaTime;
            // Lerp giúp nội suy mượt mà từ startWeight đến 1
            volume2.weight = Mathf.Lerp(startWeight, 1f, elapsed / fade);
            yield return null;
        }
        volume2.weight = 1f;

        // 2. WAIT: Giữ nguyên
        yield return new WaitForSeconds(hold);

        // 3. FADE OUT: Từ 1 về 0
        elapsed = 0;
        while (elapsed < fade)
        {
            elapsed += Time.deltaTime;
            volume2.weight = Mathf.Lerp(1f, 0f, elapsed / fade);
            yield return null;
        }
        volume2.weight = 0f;
    }

}
