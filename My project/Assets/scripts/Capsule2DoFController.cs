using UnityEngine;

public class Capsule2DoFController : MonoBehaviour
{
    [Header("Capsule Motion Settings")]
    public float maxTiltAngle = 15f;           // Макс. угол наклона
    public float accelerationTiltFactor = 2f;  // Сила наклона при ускорении
    public float brakeTiltFactor = 3f;         // Сила наклона при торможении
    public float turnTiltFactor = 10f;         // Сила крена в поворотах
    public float vibrationIntensity = 0.1f;    // Интенсивность вибраций
    public float motionResponseSpeed = 5f;     // Скорость реакции капсулы

    [Header("Capsule Components")]
    public Transform capsuleBase;              // Основание капсулы
    public Transform seat;                     // Сиденье (для дополнительных эффектов)

    private Vector3 targetRotation;
    private Vector3 currentRotation;
    private Vector3 basePosition;

    void Start()
    {
        if (capsuleBase == null)
            capsuleBase = transform;

        basePosition = capsuleBase.localPosition;
        currentRotation = Vector3.zero;
    }

    public void UpdateCapsuleFromCarMotion(float forwardAcceleration, Vector3 totalAcceleration,
                                         float steering, float speed)
    {
        CalculateTiltEffects(forwardAcceleration, steering, speed);
        ApplyVibrationEffects(totalAcceleration, speed);
        SmoothMotionUpdate();
    }

    // РАСЧЕТ ЭФФЕКТОВ НАКЛОНА
    void CalculateTiltEffects(float forwardAcceleration, float steering, float speed)
    {
        // Наклон вперед/назад при ускорении/торможении
        float pitchTilt = 0f;

        if (forwardAcceleration > 0.1f) // Ускорение
        {
            pitchTilt = -Mathf.Clamp(forwardAcceleration * accelerationTiltFactor, 0, maxTiltAngle);
        }
        else if (forwardAcceleration < -0.1f) // Торможение
        {
            pitchTilt = Mathf.Clamp(-forwardAcceleration * brakeTiltFactor, 0, maxTiltAngle);
        }

        // Крен в поворотах (зависит от скорости и угла поворота)
        float rollTilt = -steering * Mathf.Clamp(speed * turnTiltFactor * 0.1f, 0, maxTiltAngle);

        targetRotation = new Vector3(pitchTilt, 0, rollTilt);
    }

    // ЭФФЕКТЫ ВИБРАЦИИ
    void ApplyVibrationEffects(Vector3 acceleration, float speed)
    {
        // Интенсивность вибрации зависит от ускорения и скорости
        float vibrationPower = acceleration.magnitude * vibrationIntensity *
                              Mathf.Clamp01(speed * 0.1f);

        // Случайные вибрации
        Vector3 vibrationOffset = new Vector3(
            Random.Range(-1f, 1f),
            Random.Range(-1f, 1f),
            Random.Range(-1f, 1f)
        ) * vibrationPower;

        // Вибрации от неровностей дороги
        float roadBump = Mathf.PerlinNoise(Time.time * 10f, 0) * vibrationPower * 0.5f;

        // Применяем вибрации к позиции
        if (capsuleBase != null)
        {
            capsuleBase.localPosition = basePosition + vibrationOffset +
                                      Vector3.up * roadBump * 0.1f;
        }
    }

    // ПЛАВНОЕ ОБНОВЛЕНИЕ ДВИЖЕНИЯ
    void SmoothMotionUpdate()
    {
        // Плавная интерполяция к целевому вращению
        currentRotation = Vector3.Lerp(
            currentRotation,
            targetRotation,
            motionResponseSpeed * Time.deltaTime
        );

        // Применяем вращение к капсуле
        if (capsuleBase != null)
        {
            capsuleBase.localRotation = Quaternion.Euler(currentRotation);
        }
    }

    // ДОПОЛНИТЕЛЬНЫЕ ЭФФЕКТЫ
    public void AddImpactEffect(Vector3 direction, float force)
    {
        // Эффект удара или толчка
        StartCoroutine(ImpactCoroutine(direction * force));
    }

    private System.Collections.IEnumerator ImpactCoroutine(Vector3 impactForce)
    {
        float duration = 0.5f;
        float elapsed = 0f;
        Vector3 startRotation = currentRotation;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float progress = elapsed / duration;

            // Быстрый наклон с последующим возвратом
            currentRotation = Vector3.Lerp(
                startRotation + (Vector3)impactForce,
                targetRotation,
                progress
            );

            yield return null;
        }
    }
}
