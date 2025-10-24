using UnityEngine;

public class Smoothed : MonoBehaviour
{
    public float GetSmoothedThrottle()
    {
        // Ваша реализация плавного газа
        return Mathf.Clamp01(Input.GetAxis("Vertical"));
    }

    public float GetSmoothedBrake()
    {
        // Ваша реализация плавного тормоза
        return Mathf.Clamp01(Input.GetAxis("Jump"));
    }

    public float GetSmoothedSteering()
    {
        // Ваша реализация плавного руления
        return Mathf.Clamp(Input.GetAxis("Horizontal"), -1f, 1f);
    }
}
