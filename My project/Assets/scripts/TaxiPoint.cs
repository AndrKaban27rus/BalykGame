using UnityEngine;

public class TaxiPoint : MonoBehaviour
{
    [Header("Настройки точки")]
    public string pointName;
    public bool isPickupPoint = false;
    public bool isDestinationPoint = false;

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            TaxiOrderManager taxiManager = FindObjectOfType<TaxiOrderManager>();
            if (taxiManager != null)
            {
                taxiManager.OnPlayerReachedPoint(transform);
            }

            Debug.Log($"Игрок достиг точки: {pointName}");
        }
    }
}