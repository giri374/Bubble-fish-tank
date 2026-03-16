using UnityEngine;

public class EffectManager : MonoBehaviour
{
    public static EffectManager Instance { get; private set; }

    public float fadeDuration = 1.5f;
    public float holdTime = 2.0f;

    private void Awake()
    {
        Instance = this;
    }
}
