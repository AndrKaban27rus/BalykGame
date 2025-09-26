using UnityEngine;

public class AdvancedCarController : MonoBehaviour
{
    [Header("References")]
    public WheelCollider[] wheelColliders;
    public Transform[] wheelTransforms;

    [Header("Steering Settings")]
    public float maxSteeringAngle = 30f;
    public float steeringSharpness = 2f;
    public float steeringReturnSpeed = 5f;

    [Header("Engine Settings")]
    public float maxMotorTorque = 1500f;
    public float maxSpeed = 180f;
    public AnimationCurve torqueCurve;

    [Header("Physics Settings")]
    public float downForce = 50f;
    public float gripFactor = 1f;

    private float steeringInput;
    private float throttleInput;
    private float brakeInput;
    private float currentSteeringAngle;
    private Rigidbody rb;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        wheelColliders = GetComponentsInChildren<WheelCollider>();
    }

    void Update()
    {
        GetInput();
        ApplyDownForce();
    }

    void FixedUpdate()
    {
        HandleSteering();
        HandleEngine();
        UpdateWheelVisuals();
        AdjustGrip();
    }

    private void GetInput()
    {
        steeringInput = Input.GetAxis("Axis0"); // ����
        throttleInput = Input.GetAxis("Axis3"); // ���

        // ������ (����� ���� �� ��������� ��� ��� ������������)
        brakeInput = Mathf.Clamp01(-Input.GetAxis("Axis4"));

        Debug.Log($"Steering: {steeringInput}, Throttle: {throttleInput}, Brake: {brakeInput}");
    }

    private void HandleSteering()
    {
        // ������� ���������� �����
        float targetSteeringAngle = steeringInput * maxSteeringAngle;
        currentSteeringAngle = Mathf.Lerp(currentSteeringAngle, targetSteeringAngle,
                                        steeringSharpness * Time.fixedDeltaTime);

        // ��������� ������� � �������� �������
        foreach (var wheel in wheelColliders)
        {
            if (wheel.transform.localPosition.z > 0) // �������� ������
            {
                wheel.steerAngle = currentSteeringAngle;
            }
        }
    }

    private void HandleEngine()
    {
        float currentSpeed = rb.velocity.magnitude * 3.6f;

        if (currentSpeed < maxSpeed)
        {
            // ������ ��������� ������� ��� ��������������
            float torqueMultiplier = torqueCurve.Evaluate(currentSpeed / maxSpeed);
            float motorTorque = maxMotorTorque * throttleInput * torqueMultiplier;

            // ������ �� ������ ������
            foreach (var wheel in wheelColliders)
            {
                if (wheel.transform.localPosition.z < 0) // ������ ������
                {
                    wheel.motorTorque = motorTorque;
                    wheel.brakeTorque = brakeInput * 1000f;
                }
            }
        }
    }

    private void ApplyDownForce()
    {
        // ��������� ���� ��� ������� ���������
        rb.AddForce(-transform.up * downForce * rb.velocity.magnitude);
    }

    private void AdjustGrip()
    {
        // ����������� ��������� � ����������� �� �������� ��������
        float sidewaysSpeed = Vector3.Dot(rb.velocity, transform.right);
        Vector3 counterForce = -transform.right * (sidewaysSpeed * gripFactor);
        rb.AddForce(counterForce * rb.mass);
    }

    private void UpdateWheelVisuals()
    {
        for (int i = 0; i < wheelColliders.Length; i++)
        {
            if (wheelTransforms[i] != null)
            {
                wheelColliders[i].GetWorldPose(out Vector3 position, out Quaternion rotation);
                wheelTransforms[i].position = position;
                wheelTransforms[i].rotation = rotation;
            }
        }
    }
}