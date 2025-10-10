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
    public float maxBrakeTorque = 1000f;
    public float maxSpeed = 50f;

    private float currentThrottle;
    private float currentBrake;
    private float currentSteering;
    private Rigidbody carRigidbody;

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
        // Получаем Rigidbody
        carRigidbody = GetComponent<Rigidbody>();
        if (carRigidbody == null)
        {
            carRigidbody = gameObject.AddComponent<Rigidbody>();
        }
        SetupRigidbody();

     

        // Настройка WheelColliders
        SetupWheelControls();

        // Отключаем проблемные коллайдеры
        DisableProblematicColliders();

        // Поиск контроллеров
        if (inputController == null)
            inputController = FindObjectOfType<InputControllerReader>();

        if (capsuleController == null)
            capsuleController = FindObjectOfType<Capsule2DoFController>();

        lastVelocity = carRigidbody.linearVelocity;
        lastForwardSpeed = GetForwardVelocity();
    }

    void AutoPositionWheels()
    {
        // Если колеса не назначены вручную, ищем их автоматически
        if (frontLeftWheel == null || frontRightWheel == null ||
            rearLeftWheel == null || rearRightWheel == null)
        {
            FindAndPositionAllWheels();
        }
        else
        {
            // Если колеса назначены, просто позиционируем их
            PositionSingleWheel(frontLeftWheel, frontLeftPosition);
            PositionSingleWheel(frontRightWheel, frontRightPosition);
            PositionSingleWheel(rearLeftWheel, rearLeftPosition);
            PositionSingleWheel(rearRightWheel, rearRightPosition);
        }
    }

    void FindAndPositionAllWheels()
    {
        WheelControl[] allWheels = GetComponentsInChildren<WheelControl>();

        if (allWheels.Length == 0)
        {
            Debug.LogError("No WheelControl components found! Please add wheels as children of the car.");
            return;
        }

        // Сортируем колеса по имени и позиционируем
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
            else
            {
                // Если имя не распознано, назначаем автоматически
                AutoAssignWheel(wheel, allWheels);
            }
        }

        Debug.Log($"Positioned {allWheels.Length} wheels");
    }

    void AutoAssignWheel(WheelControl wheel, WheelControl[] allWheels)
    {
        // Автоматическое назначение на основе порядка в массиве
        int wheelIndex = System.Array.IndexOf(allWheels, wheel);

        switch (wheelIndex)
        {
            case 0:
                frontLeftWheel = wheel;
                PositionSingleWheel(wheel, frontLeftPosition);
                break;
            case 1:
                frontRightWheel = wheel;
                PositionSingleWheel(wheel, frontRightPosition);
                break;
            case 2:
                rearLeftWheel = wheel;
                PositionSingleWheel(wheel, rearLeftPosition);
                break;
            case 3:
                rearRightWheel = wheel;
                PositionSingleWheel(wheel, rearRightPosition);
                break;
            default:
                // Для дополнительных колес
                PositionSingleWheel(wheel, Vector3.zero);
                break;
        }
    }

    void PositionSingleWheel(WheelControl wheel, Vector3 localPosition)
    {
        if (wheel != null)
        {
            wheel.transform.localPosition = localPosition;
            wheel.transform.localRotation = Quaternion.identity;

            // Настраиваем WheelCollider если он есть
            if (wheel.WheelCollider != null)
            {
                // Сбрасываем позицию WheelCollider
                wheel.WheelCollider.transform.localPosition = Vector3.zero;
            }

            Debug.Log($"Positioned {wheel.name} at {localPosition}");
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

    void FixedUpdate()
    {
        GetSmoothedInput();
        ApplyWheelPhysics();
        CalculateMotionEffects();
        UpdateCapsuleMotion();
        UpdateCarSteeringWheel();
        StabilizeCar();
    }

    // Остальные методы остаются без изменений...
    void GetSmoothedInput()
    {
        if (inputController != null)
        {
            currentThrottle = inputController.GetSmoothedThrottle();
            currentBrake = inputController.GetSmoothedBrake();
            currentSteering = inputController.GetSmoothedSteering();
        }
    }

    void ApplyWheelPhysics()
    {
        float currentSpeed = GetCurrentSpeed();
        float speedFactor = Mathf.Clamp01(1 - (currentSpeed / maxSpeed));

        float steeringAngle = currentSteering * maxSteeringAngle;

        if (frontLeftWheel != null && frontLeftWheel.steerable)
            frontLeftWheel.WheelCollider.steerAngle = steeringAngle;
        if (frontRightWheel != null && frontRightWheel.steerable)
            frontRightWheel.WheelCollider.steerAngle = steeringAngle;

        float motorTorque = currentThrottle * maxMotorTorque * speedFactor;
        ApplyMotorTorqueToAllWheels(motorTorque);

        float brakeTorque = currentBrake * maxBrakeTorque;
        ApplyBrakeToAllWheels(brakeTorque);
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

    [ContextMenu("Reposition All Wheels")]
    public void RepositionAllWheels()
    {
        PositionSingleWheel(frontLeftWheel, frontLeftPosition);
        PositionSingleWheel(frontRightWheel, frontRightPosition);
        PositionSingleWheel(rearLeftWheel, rearLeftPosition);
        PositionSingleWheel(rearRightWheel, rearRightPosition);
        Debug.Log("All wheels repositioned!");
    }

    [ContextMenu("Find Missing Wheels")]
    public void FindMissingWheels()
    {
        AutoPositionWheels();
    }
}