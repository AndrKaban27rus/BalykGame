using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System;
using TMPro;

public class TaxiOrderManager : MonoBehaviour
{
    [Header("Точки маршрута")]
    public Transform[] routePoints;

    [Header("UI элементы")]
    public TextMeshProUGUI startPointText;
    public TextMeshProUGUI endPointText;
    public TextMeshProUGUI currentTimeText;
    public TextMeshProUGUI bestTimeText;

    [Header("Настройки")]
    public float timeScale = 1.0f; // Масштаб времени для тестирования

    // Текущий заказ
    private TaxiOrder currentOrder;
    private bool orderInProgress = false;
    private float orderStartTime;
    private float bestTime = 0f;

    // События
    public static event Action<TaxiOrder> OnNewOrder;
    public static event Action<TaxiOrder, float> OnOrderCompleted;

    [SerializeField] private AudioClip orderSound;
    [SerializeField] private AudioClip bestTimeSound;
    [SerializeField] private AudioSource carsAudioSource;

    void Start()
    {
        // Устанавливаем начальное лучшее время
        bestTimeText.text = "Лучшее время: 0:00";
        currentTimeText.text = "Текущее время: 0:00";

        // Генерируем первый заказ
        GenerateNewOrder();
    }

    void Update()
    {
        // Обновляем таймер текущего заказа
        if (orderInProgress)
        {
            float currentTime = Time.time - orderStartTime;
            UpdateTimerDisplay(currentTime);
        }
    }

    public void GenerateNewOrder()
    {
        if (routePoints.Length < 2)
        {
            Debug.LogError("Недостаточно точек маршрута!");
            return;
        }

        // Выбираем случайные стартовую и конечную точки
        int startIndex = UnityEngine.Random.Range(0, routePoints.Length);
        int endIndex;

        do
        {
            endIndex = UnityEngine.Random.Range(0, routePoints.Length);
        } while (endIndex == startIndex);

        // Создаем новый заказ с именами GameObject'ов
        currentOrder = new TaxiOrder(
            routePoints[startIndex],
            routePoints[endIndex],
            routePoints[startIndex].name, // Используем имя GameObject вместо "Точка X"
            routePoints[endIndex].name    // Используем имя GameObject вместо "Точка X"
        );
        carsAudioSource.PlayOneShot(orderSound);
        // Обновляем UI
        UpdateOrderDisplay(currentOrder);

        // Запускаем заказ
        StartOrder();

        // Вызываем событие
        OnNewOrder?.Invoke(currentOrder);
    }


    private void StartOrder()
    {
        orderInProgress = true;
        orderStartTime = Time.time;
        UpdateTimerDisplay(0f);
    }

    public void CompleteOrder()
    {
        if (!orderInProgress || currentOrder == null) return;

        float completionTime = Time.time - orderStartTime;
        orderInProgress = false;

        // Проверяем на лучшее время
        if (bestTime == 0f || completionTime < bestTime)
        {
            bestTime = completionTime;
            bestTimeText.text = $"Лучшее время: {FormatTime(bestTime)}";
            carsAudioSource.PlayOneShot(bestTimeSound);
        }

        // Вызываем событие завершения заказа
        OnOrderCompleted?.Invoke(currentOrder, completionTime);

        Debug.Log($"Заказ завершен! Время: {FormatTime(completionTime)}");

        // Генерируем новый заказ через небольшую задержку
        StartCoroutine(GenerateNewOrderWithDelay(2f));
    }

    private IEnumerator GenerateNewOrderWithDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        GenerateNewOrder();
    }

    private void UpdateOrderDisplay(TaxiOrder order)
    {
        startPointText.text = $"От: {order.StartPointName}";
        endPointText.text = $"До: {order.EndPointName}";
    }

    private void UpdateTimerDisplay(float time)
    {
        currentTimeText.text = $"Текущее время: {FormatTime(time)}";
    }

    private string FormatTime(float timeInSeconds)
    {
        int minutes = Mathf.FloorToInt(timeInSeconds / 60f);
        int seconds = Mathf.FloorToInt(timeInSeconds % 60f);
        return $"{minutes}:{seconds:00}";
    }

    // Метод для проверки достижения точек (можно вызвать из другого скрипта)
    public void OnPlayerReachedPoint(Transform point)
    {
        if (!orderInProgress || currentOrder == null) return;

        // Если игрок достиг конечной точки
        if (point == currentOrder.EndPoint)
        {
            CompleteOrder();
        }
    }

    // Для отладки - завершить заказ по кнопке
    [ContextMenu("Завершить заказ")]
    public void DebugCompleteOrder()
    {
        CompleteOrder();
    }
}

[System.Serializable]
public class TaxiOrder
{
    public Transform StartPoint { get; private set; }
    public Transform EndPoint { get; private set; }
    public string StartPointName { get; private set; }
    public string EndPointName { get; private set; }

    public TaxiOrder(Transform startPoint, Transform endPoint, string startName, string endName)
    {
        StartPoint = startPoint;
        EndPoint = endPoint;
        StartPointName = startName;
        EndPointName = endName;
    }
}