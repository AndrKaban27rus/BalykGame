using LogitechG29.Sample.Input;
using UnityEngine;

public class CarController : MonoBehaviour
{
    [Header("Wheel References")]
    public WheelControl frontLeftWheel;
    public WheelControl frontRightWheel;
    public WheelControl rearLeftWheel;
    public WheelControl rearRightWheel;

    [Header("Input Controller")]
    public InputControllerReader inputController;

    [Header("Steering Wheel Reference")]
    public Transform carSteeringWheel;
    public float maxSteeringWheelAngle = 450f;

    [Header("Engine Settings")]
    public float maxMotorTorque = 800f;
    public float maxSpeed = 50f;
    public float maxReverseSpeed = 20f;

    [Header("Braking System")]
    public float maxBrakeTorque = 2000f;
    public float brakeResponseTime = 0.1f;
    public float engineBrakeTorque = 300f;

    [Header("Steering Settings")]
    public float maxSteeringAngle = 30f;
    public float steeringResponse = 5f;

    [Header("Drivetrain")]
    public bool frontWheelDrive = false;
    public bool rearWheelDrive = true;

    [Header("Debug Info")]
    [SerializeField] private float currentSpeed;
    [SerializeField] private bool isReverse;
    [SerializeField] private float currentThrottle;
    [SerializeField] private float currentBrake;
    [SerializeField] private string currentGear;

    // Private variables
    private Rigidbody carRigidbody;
    private float smoothedThrottle;
    private float smoothedBrake;
    private float smoothedSteering;
    private float currentSteeringInput;
    private bool wasShifter4 = false;

    void Start()
    {
        InitializeCar();
    }

    void InitializeCar()
    {
        // Get Rigidbody
        carRigidbody = GetComponent<Rigidbody>();
        if (carRigidbody == null)
            carRigidbody = gameObject.AddComponent<Rigidbody>();

        // Basic Rigidbody setup
        carRigidbody.mass = 1200f;
        carRigidbody.linearDamping = 0.05f;
        carRigidbody.angularDamping = 2f;
        carRigidbody.centerOfMass = new Vector3(0, -0.5f, 0);
        carRigidbody.interpolation = RigidbodyInterpolation.Interpolate;

        Debug.Log("Car initialized - Use Shifter4 for reverse, gas/brake pedals for control");
    }

    void Update()
    {
        HandleGearShifting();
        UpdateSteeringWheelVisual();
    }

    void FixedUpdate()
    {
        ReadInput();
        ApplyVehiclePhysics();
        UpdateDebugInfo();
    }

    void HandleGearShifting()
    {
        if (inputController != null)
        {
            bool currentShifter4 = inputController.Shifter4;

            if (currentShifter4 != wasShifter4)
            {
                isReverse = currentShifter4;
                Debug.Log(isReverse ? "🔄 REVERSE gear engaged" : "🚗 DRIVE gear engaged");
            }

            wasShifter4 = currentShifter4;
        }
    }

    void ReadInput()
    {
        //if (inputController != null)
        //{
        //    // Get raw input
        //    float rawThrottle = inputController.Throttle;
        //    float rawBrake = inputController.Brake;
        //    float rawSteering = inputController.Steering;

        //    // Smooth inputs
        //    smoothedThrottle = Mathf.Lerp(smoothedThrottle, rawThrottle, 3f * Time.fixedDeltaTime);
        //    smoothedBrake = Mathf.Lerp(smoothedBrake, rawBrake, brakeResponseTime * Time.fixedDeltaTime);
        //    smoothedSteering = Mathf.Lerp(smoothedSteering, rawSteering, steeringResponse * Time.fixedDeltaTime);

        //    currentThrottle = smoothedThrottle;
        //    currentBrake = smoothedBrake;
        //    currentSteeringInput = smoothedSteering;
        //}
        //else
        {
            // Keyboard fallback
            currentThrottle = Input.GetKey(KeyCode.W) ? 1f : 0f;
            currentBrake = Input.GetKey(KeyCode.S) ? 1f : 0f;
            currentSteeringInput = Input.GetAxis("Horizontal");
            isReverse = Input.GetKey(KeyCode.R);
        }
    }

    void ApplyVehiclePhysics()
    {
        currentSpeed = GetCurrentSpeed();
        float forwardVelocity = GetForwardVelocity();

        // Apply steering
        ApplySteering();

        // Apply engine power and braking
        ApplyEngineAndBrakes(forwardVelocity);

        // Apply stability control
        StabilizeVehicle();
    }

    void ApplySteering()
    {
        // Simple speed-sensitive steering
        float speedFactor = 1f - (currentSpeed / 150f);
        speedFactor = Mathf.Clamp(speedFactor, 0.3f, 1f);

        float steeringAngle = currentSteeringInput * maxSteeringAngle * speedFactor ;

        if (frontLeftWheel != null && frontLeftWheel.steerable)
            frontLeftWheel.WheelCollider.steerAngle = steeringAngle;

        if (frontRightWheel != null && frontRightWheel.steerable)
            frontRightWheel.WheelCollider.steerAngle = steeringAngle;
    }

    void ApplyEngineAndBrakes(float forwardVelocity)
    {
        float motorTorque = 0f;
        float brakeTorque = 0f;

        // Calculate motor torque based on gear and throttle
        if (currentThrottle > 0.01f && currentBrake < 0.1f)
        {
            if (isReverse)
            {
                // REVERSE GEAR - limited speed, negative torque
                float reverseSpeed = Mathf.Abs(Mathf.Min(forwardVelocity, 0));
                float speedFactor = 1f - (reverseSpeed / maxReverseSpeed);
                speedFactor = Mathf.Clamp01(speedFactor);
                motorTorque = -currentThrottle * maxMotorTorque * 0.7f * speedFactor;
            }
            else
            {
                // DRIVE GEAR - normal acceleration
                float speedFactor = 1f - (currentSpeed / maxSpeed);
                speedFactor = Mathf.Clamp01(speedFactor);
                motorTorque = currentThrottle * maxMotorTorque * speedFactor;
            }
        }

        // Calculate brake torque
        if (currentBrake > 0.01f)
        {
            // Active braking
            brakeTorque = currentBrake * maxBrakeTorque;

            // Reduce engine power when braking hard
            if (currentBrake > 0.5f)
                motorTorque *= 0.1f;
        }
        else if (currentThrottle < 0.1f && currentSpeed > 3f)
        {
            // Engine braking when coasting
            brakeTorque = engineBrakeTorque * (currentSpeed / maxSpeed);
        }

        // Apply calculated torques
        ApplyMotorTorqueToAllWheels(motorTorque);
        ApplyBrakeToAllWheels(brakeTorque);
    }

    void ApplyMotorTorqueToAllWheels(float motorTorque)
    {
        if (frontWheelDrive)
        {
            ApplyMotorTorque(frontLeftWheel, motorTorque);
            ApplyMotorTorque(frontRightWheel, motorTorque);
        }

        if (rearWheelDrive)
        {
            ApplyMotorTorque(rearLeftWheel, motorTorque);
            ApplyMotorTorque(rearRightWheel, motorTorque);
        }
    }

    void ApplyMotorTorque(WheelControl wheelControl, float motorTorque)
    {
        if (wheelControl != null && wheelControl.motorized && wheelControl.WheelCollider != null)
        {
            wheelControl.WheelCollider.motorTorque = motorTorque;
        }
    }

    void ApplyBrakeToAllWheels(float brakeTorque)
    {
        ApplyBrakeToWheel(frontLeftWheel, brakeTorque);
        ApplyBrakeToWheel(frontRightWheel, brakeTorque);
        ApplyBrakeToWheel(rearLeftWheel, brakeTorque);
        ApplyBrakeToWheel(rearRightWheel, brakeTorque);
    }

    void ApplyBrakeToWheel(WheelControl wheelControl, float brakeTorque)
    {
        if (wheelControl != null && wheelControl.WheelCollider != null)
        {
            wheelControl.WheelCollider.brakeTorque = brakeTorque;
        }
    }

    void UpdateSteeringWheelVisual()
    {
        if (carSteeringWheel != null && frontLeftWheel != null)
        {
            float wheelAngle = (frontLeftWheel.WheelCollider.steerAngle / maxSteeringAngle) * maxSteeringWheelAngle;
            float currentAngle = carSteeringWheel.localEulerAngles.z;
            float smoothedAngle = Mathf.LerpAngle(currentAngle, wheelAngle, 8f * Time.deltaTime);

            carSteeringWheel.localRotation = Quaternion.Euler(0, 0, smoothedAngle);
        }
    }

    void StabilizeVehicle()
    {
        // Prevent flipping at low speeds
        if (currentSpeed < 5f)
        {
            // Simple stabilization - keep car upright
            Vector3 currentRotation = transform.eulerAngles;
            if (Mathf.Abs(currentRotation.z) > 10f || Mathf.Abs(currentRotation.x) > 10f)
            {
                Quaternion targetRotation = Quaternion.LookRotation(transform.forward, Vector3.up);
                carRigidbody.MoveRotation(Quaternion.Slerp(carRigidbody.rotation, targetRotation, 2f * Time.fixedDeltaTime));
            }
        }
    }

    void UpdateDebugInfo()
    {
        currentGear = isReverse ? "REVERSE" : "DRIVE";
    }

    float GetForwardVelocity()
    {
        return Vector3.Dot(carRigidbody.linearVelocity, transform.forward);
    }

    float GetCurrentSpeed()
    {
        return carRigidbody.linearVelocity.magnitude * 3.6f; // Convert to km/h
    }

    [ContextMenu("Toggle Reverse Gear")]
    public void ToggleReverseGear()
    {
        isReverse = !isReverse;
        Debug.Log(isReverse ? "🔄 REVERSE gear" : "🚗 DRIVE gear");
    }

    [ContextMenu("Emergency Brake")]
    public void EmergencyBrake()
    {
        ApplyBrakeToAllWheels(maxBrakeTorque);
        Invoke("ReleaseEmergencyBrake", 1f);
        Debug.Log("🚨 EMERGENCY BRAKE!");
    }

    void ReleaseEmergencyBrake()
    {
        ApplyBrakeToAllWheels(0f);
        Debug.Log("✅ Brakes released");
    }

    // Simple method to check if car is moving
    public bool IsMoving()
    {
        return currentSpeed > 1f;
    }

    // Method to get current gear state
    public bool IsInReverse()
    {
        return isReverse;
    }
}