using UnityEngine;
public class G29InputConfig : MonoBehaviour
{
    void Start()
    {
        Debug.Log("G29 Input Configuration:");

        // Проверяем все оси для идентификации
        for (int i = 0; i < 10; i++)
        {
            string axisName = "Axis" + i;
            try
            {
                float value = Input.GetAxis(axisName);
                if (Mathf.Abs(value) > 0.01f)
                {
                    Debug.Log($"{axisName}: {value}");
                }
            }
            catch { }
        }
    }
}