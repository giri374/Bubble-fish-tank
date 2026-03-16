using System.Collections;
using UnityEngine;

public class MaterialFadeAlpha : MonoBehaviour
{

    [SerializeField] private Material mat;
    [SerializeField] private float targetAlpha = 0f;
    [SerializeField] private float originalAlpha = 0.6f;
    [SerializeField] private float fadeDuration = 1;
    [SerializeField] private float waitTime = 2;
    private string colorProp = "_BaseColor"; // URP: "_BaseColor", Standard: "_Color"
    private Coroutine fadeCoroutine;
    private void Start()
    {
        mat = GetComponent<Renderer>().material;

    }

    public void AlphaTransitionEffect()
    {
        // Dừng coroutine hiện tại nếu có để tránh nhiều animation chạy cùng lúc
        if (fadeCoroutine != null)
        {
            StopCoroutine(fadeCoroutine);
        }

        // Gọi hiệu ứng: Chuyển về Alpha 0 (trong suốt), giữ 2 giây, rồi quay lại 1
        fadeCoroutine = StartCoroutine(FadeSequence(originalAlpha, targetAlpha, fadeDuration, waitTime));
    }

    private IEnumerator FadeSequence(float originalAlpha, float targetAlpha, float fadeDuration, float waitTime)
    {
        // 1. Lưu lại Alpha ban đầu để tính toán
        float currentAlpha = mat.GetColor(colorProp).a;

        // 2. Chuyển DẦN ĐẾN giá trị mục tiêu
        yield return StartCoroutine(LerpAlpha(currentAlpha, targetAlpha, fadeDuration));

        // 3. GIỮ trong một khoảng thời gian
        yield return new WaitForSeconds(waitTime);

        // 4. Chuyển DẦN VỀ giá trị cũ
        yield return StartCoroutine(LerpAlpha(targetAlpha, originalAlpha, fadeDuration));
    }

    // Hàm phụ trợ để xử lý việc nội suy Alpha
    private IEnumerator LerpAlpha(float start, float end, float duration)
    {
        float elapsed = 0f;
        Color color = mat.GetColor(colorProp);

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float currentAlpha = Mathf.Lerp(start, end, elapsed / duration);

            color.a = currentAlpha;
            mat.SetColor(colorProp, color);
            yield return null;
        }
    }
}
