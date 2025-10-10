using LogitechG29.Sample.Input;
using UnityEngine;

public class CarController : MonoBehaviour
{
    [Header("Wheel References")]
    public WheelControl frontLeftWheel;
    public WheelControl frontRightWheel;
    public WheelControl rearLeftWheel;
    public WheelControl rearRightWheel;

    [Header("Wheel Positions")]
    public Vector3 frontLeftPosition = new Vector3(-0.8f, 0.3f, 1.3f);
    public Vector3 frontRightPosition = new Vector3(0.8f, 0.3f, 1.3f);
    public Vector3 rearLeftPosition = new Vector3(-0.8f, 0.3f, -1.3f);
    public Vector3 rearRightPosition = new Vector3(0.8f, 0.3f, -1.3f);

    [Header("Capsule 2DoF Controller")]
    public Capsule2DoFController capsuleController;

    [Header("Input Controller")]
    public InputControllerReader inputController;

    [Header("Steering Wheel Reference")]
    public Transform carSteeringWheel;
    public float maxSteeringWheelAngle = 360f;

    [Header("Car Settings")]
    public float maxMotorTorque = 800f;
    public float maxSteeringAngle = 25f;
    public float maxBrakeTorque = 2000f;
    public float maxSpeed = 50f;
    public float maxReverseSpeed = 20f;

    [Header("Debug - Input Values")]
    [SerializeField] private float debugThrottle = 0f;
    [SerializeField] private float debugBrake = 0f;
    [SerializeField] private float debugSteering = 0f;
    [SerializeField] private bool debugIsReverse = false;

    private float currentThrottle;
    private float currentBrake;
    private float currentSteering;
    private Rigidbody carRigidbody;
    private bool isReverse = false;

    // Для эффектов капсулы
    private Vector3 lastVelocity;
    private Vector3 currentAcceleration;
    private float lastForwardSpeed;
    private float currentForwardAcceleration;

    void Start()
    {
        InitializeCar();
    }

    void InitializeCar()
    {
        carRigidbody = GetComponent<Rigidbody>();
        if (carRigidbody == null)
        {
            carRigidbody = gameObject.AddComponent<Rigidbody>();
        }
        SetupRigidbody();

        AutoPositionWheels();
        SetupWheelControls();
        DisableProblematicColliders();

        if (inputController == null)
            inputController = FindObjectOfType<InputControllerReader>();

        if (capsuleController == null)
            capsuleController = FindObjectOfType<Capsule2DoFController>();

        lastVelocity = carRigidbody.linearVelocity;
        lastForwardSpeed = GetForwardVelocity();

        Debug.Log("Car initialized. Press Shift4 for reverse gear.");
    }

    void Update()
    {
        // Простая проверка передачи - если Shift4 активен, то задний ход
        if (inputController != null)
        {
            isReverse = inputController.GetShift4();
        }

        // Fallback для клавиатуры - R для заднего хода
        if (Input.GetKeyDown(KeyCode.R))
        {
            isReverse = !isReverse;
            Debug.Log(isReverse ? "REVERSE gear engaged" : "DRIVE gear engaged");
        }
    }

    void FixedUpdate()
    {
        GetSmoothedInput();
        ApplyWheelPhysics();
        CalculateMotionEffects();
        UpdateCapsuleMotion();
        UpdateCarSteeringWheel();
        StabilizeCar();

        // Обновляем debug значения
        debugThrottle = currentThrottle;
        debugBrake = currentBrake;
        debugSteering = currentSteering;
        debugIsReverse = isReverse;
    }

    void GetSmoothedInput()
    {
        if (inputController != null)
        {
            currentThrottle = inputController.GetSmoothedThrottle();
            currentBrake = inputController.GetSmoothedBrake();
            currentSteering = inputController.GetSmoothedSteering();
        }
        else
        {
            // Fallback для клавиатуры
            currentThrottle = Mathf.Clamp01(Input.GetAxis("Vertical"));
            currentBrake = Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.Space) ? 1f : 0f;
            currentSteering = Input.GetAxis("Horizontal");
        }
    }

    void ApplyWheelPhysics()
    {
        float currentSpeed = GetCurrentSpeed();
        float forwardVelocity = GetForwardVelocity();

        // РУЛЕВОЕ УПРАВЛЕНИЕ
        float steeringAngle = currentSteering * maxSteeringAngle;

        if (frontLeftWheel != null && frontLeftWheel.steerable)
            frontLeftWheel.WheelCollider.steerAngle = steeringAngle;

        if (frontRightWheel != null && frontRightWheel.steerable)
            frontRightWheel.WheelCollider.steerAngle = steeringAngle;

        // МОТОРНЫЙ КРУТЯЩИЙ МОМЕНТ
        float motorTorque = 0f;

        if (currentThrottle > 0 && currentBrake == 0)
        {
            if (isReverse)
            {
                // ЗАДНИЙ ХОД - отрицательный момент
                float reverseSpeed = Mathf.Abs(Mathf.Min(forwardVelocity, 0));
                float speedFactor = Mathf.Clamp01(1 - (reverseSpeed / maxReverseSpeed));
                motorTorque = -currentThrottle * maxMotorTorque * 0.8f * speedFactor;
            }
            else
            {
                // ПЕРЕДНИЙ ХОД - положительный момент
                float speedFactor = Mathf.Clamp01(1 - (currentSpeed / maxSpeed));
                motorTorque = currentThrottle * maxMotorTorque * speedFactor;
            }
        }

        // ТОРМОЖЕНИЕ
        float brakeTorque = 0f;

        if (currentBrake > 0)
        {
            // Активное торможение
            brakeTorque = currentBrake * maxBrakeTorque;
        }
        else if (currentThrottle == 0 && currentSpeed > 5f)
        {
            // Торможение двигателем
            brakeTorque = maxBrakeTorque * 0.3f;
        }

        // Применяем вычисленные значения
        ApplyMotorTorqueToAllWheels(motorTorque);
        ApplyBrakeToAllWheels(brakeTorque);

        // Debug информация
        if (motorTorque != 0)
        {
            string direction = isReverse ? "REVERSE" : "FORWARD";
            Debug.Log($"{direction} - Throttle: {currentThrottle}, Motor: {motorTorque:F0}, Speed: {currentSpeed:F1}");
        }
    }

    void ApplyMotorTorqueToAllWheels(float motorTorque)
    {
        ApplyMotorTorque(frontLeftWheel, motorTorque);
        ApplyMotorTorque(frontRightWheel, motorTorque);
        ApplyMotorTorque(rearLeftWheel, motorTorque);
        ApplyMotorTorque(rearRightWheel, motorTorque);
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

    void UpdateCarSteeringWheel()
    {
        if (carSteeringWheel != null && frontLeftWheel != null)
        {
            float steeringWheelAngle = (frontLeftWheel.WheelCollider.steerAngle / maxSteeringAngle) * maxSteeringWheelAngle;
            float smoothedAngle = Mathf.LerpAngle(carSteeringWheel.localEulerAngles.z, steeringWheelAngle, 5f * Time.deltaTime);
            carSteeringWheel.localRotation = Quaternion.Euler(0, 0, smoothedAngle);
        }
    }

    void CalculateMotionEffects()
    {
        currentAcceleration = (carRigidbody.linearVelocity - lastVelocity) / Time.fixedDeltaTime;
        lastVelocity = carRigidbody.linearVelocity;

        float currentForwardSpeed = GetForwardVelocity();
        currentForwardAcceleration = (currentForwardSpeed - lastForwardSpeed) / Time.fixedDeltaTime;
        lastForwardSpeed = currentForwardSpeed;
    }

    float GetForwardVelocity()
    {
        return Vector3.Dot(carRigidbody.linearVelocity, transform.forward);
    }

    float GetCurrentSpeed()
    {
        return carRigidbody.linearVelocity.magnitude * 3.6f;
    }

    void UpdateCapsuleMotion()
    {
        if (capsuleController == null) return;

        capsuleController.UpdateCapsuleFromCarMotion(
            currentForwardAcceleration,
            currentAcceleration,
            currentSteering,
            carRigidbody.linearVelocity.magnitude
        );
    }

    void StabilizeCar()
    {
        if (carRigidbody.linearVelocity.magnitude < 2f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(transform.forward, Vector3.up);
            carRigidbody.MoveRotation(Quaternion.Slerp(carRigidbody.rotation, targetRotation, 2f * Time.fixedDeltaTime));
        }
    }

    void SetupRigidbody()
    {
        carRigidbody.mass = 1200f;
        carRigidbody.linearDamping = 0.05f;
        carRigidbody.angularDamping = 2f;
        carRigidbody.centerOfMass = new Vector3(0, -0.8f, 0);
        carRigidbody.interpolation = RigidbodyInterpolation.Interpolate;
        carRigidbody.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
    }

    void AutoPositionWheels()
    {
        if (frontLeftWheel == null || frontRightWheel == null ||
            rearLeftWheel == null || rearRightWheel == null)
        {
            FindAndPositionAllWheels();
        }
        else
        {
            PositionSingleWheel(frontLeftWheel, frontLeftPosition);
            PositionSingleWheel(frontRightWheel, frontRightPosition);
            PositionSingleWheel(rearLeftWheel, rearLeftPosition);
            PositionSingleWheel(rearRightWheel, rearRightPosition);
        }
    }

    void FindAndPositionAllWheels()
    {
        WheelControl[] allWheels = GetComponentsInChildren<WheelControl>();

        foreach (WheelControl wheel in allWheels)
        {
            string wheelName = wheel.name.ToLower();

            if (wheelName.Contains("front"))
            {
                if (wheelName.Contains("left"))
                {
                    frontLeftWheel = wheel;
                    PositionSingleWheel(wheel, frontLeftPosition);
                }
                else if (wheelName.Contains("right"))
                {
                    frontRightWheel = wheel;
                    PositionSingleWheel(wheel, frontRightPosition);
                }
            }
            else if (wheelName.Contains("rear") || wheelName.Contains("back"))
            {
                if (wheelName.Contains("left"))
                {
                    rearLeftWheel = wheel;
                    PositionSingleWheel(wheel, rearLeftPosition);
                }
                else if (wheelName.Contains("right"))
                {
                    rearRightWheel = wheel;
                    PositionSingleWheel(wheel, rearRightPosition);
                }
            }
        }
    }

    void PositionSingleWheel(WheelControl wheel, Vector3 localPosition)
    {
        if (wheel != null)
        {
            wheel.transform.localPosition = localPosition;
            wheel.transform.localRotation = Quaternion.identity;
        }
    }

    void SetupWheelControls()
    {
        WheelControl[] allWheels = { frontLeftWheel, frontRightWheel, rearLeftWheel, rearRightWheel };

        foreach (WheelControl wheelControl in allWheels)
        {
            if (wheelControl != null && wheelControl.WheelCollider != null)
            {
                SetupWheelCollider(wheelControl.WheelCollider);
            }
        }
    }

    void SetupWheelCollider(WheelCollider wheel)
    {
        JointSpring spring = wheel.suspensionSpring;
        spring.spring = 35000f;
        spring.damper = 3000f;
        wheel.suspensionSpring = spring;
        wheel.suspensionDistance = 0.15f;
        wheel.forceAppPointDistance = 0f;

        WheelFrictionCurve forwardFriction = wheel.forwardFriction;
        forwardFriction.extremumSlip = 0.6f;
        forwardFriction.extremumValue = 1.2f;
        forwardFriction.asymptoteSlip = 1f;
        forwardFriction.asymptoteValue = 0.8f;
        wheel.forwardFriction = forwardFriction;

        WheelFrictionCurve sidewaysFriction = wheel.sidewaysFriction;
        sidewaysFriction.extremumSlip = 0.4f;
        sidewaysFriction.extremumValue = 1.5f;
        sidewaysFriction.asymptoteSlip = 0.8f;
        sidewaysFriction.asymptoteValue = 1f;
        wheel.sidewaysFriction = sidewaysFriction;

        wheel.mass = 30f;
    }

    void DisableProblematicColliders()
    {
        MeshCollider meshCollider = GetComponent<MeshCollider>();
        if (meshCollider != null)
        {
            meshCollider.enabled = false;
        }
    }

    [ContextMenu("Toggle Reverse Gear")]
    public void ToggleReverseGear()
    {
        isReverse = !isReverse;
        Debug.Log(isReverse ? "REVERSE gear engaged" : "DRIVE gear engaged");
    }

    [ContextMenu("Set Drive Gear")]
    public void SetDriveGear()
    {
        isReverse = false;
        Debug.Log("DRIVE gear engaged");
    }

    [ContextMenu("Set Reverse Gear")]
    public void SetReverseGear()
    {
        isReverse = true;
        Debug.Log("REVERSE gear engaged");
    }
}