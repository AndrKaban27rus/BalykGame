using LogitechG29.Sample.Input;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;
using System.Collections.Generic;

[RequireComponent(typeof(Rigidbody))]
public class AlternativeController : MonoBehaviour
{
    [SerializeField] Transform steerTransform;
    private Rigidbody rb;
    [SerializeField] private int breakForce;
    [SerializeField] private float breakInput;
    [SerializeField] Wheel[] wheels;
    [SerializeField] private int motorTorgue;
    public float verticalInput;
    public float horizontalInput;
    private float _speed;
    [SerializeField] AnimationCurve steeringCurve;

    // Logitech G29 input
    [SerializeField] private InputControllerReader _inputController;

    // Radio system
    [Header("Radio System")]
    [SerializeField] private List<AudioClip> radioSongs; // Список песен для радио
    [SerializeField] private AudioSource radioAudioSource;
    private bool isRadioOn = false;
    private int currentSongIndex = -1;
    private bool wasEastButtonPressed = false;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        if (rb == null)
        {
            rb = GetComponent<Rigidbody>();
        }

        // Initialize Logitech G29 input
        _inputController = GetComponent<InputControllerReader>();

        // Initialize radio audio source if not set
        if (radioAudioSource == null)
        {
            radioAudioSource = GetComponent<AudioSource>();
            if (radioAudioSource == null)
            {
                radioAudioSource = gameObject.AddComponent<AudioSource>();
                radioAudioSource.loop = false;
                radioAudioSource.spatialBlend = 1f; // 3D sound
                radioAudioSource.rolloffMode = AudioRolloffMode.Linear;
                radioAudioSource.maxDistance = 20f;
            }
        }
    }

    void Update()
    {
        Move();
        Steering();
        Break();
        CheckInput();
        CheckRadioInput();
    }

    void CheckRadioInput()
    {
        if (_inputController != null)
        {
            // Check if East button is pressed (button release detection)
            bool isEastButtonPressed = _inputController.EastButton;

            if (isEastButtonPressed && !wasEastButtonPressed)
            {
                ToggleRadio();
            }

            wasEastButtonPressed = isEastButtonPressed;

          
        }

        // Check if current song finished and radio is on
        if (isRadioOn && !radioAudioSource.isPlaying && radioSongs.Count > 0)
        {
            PlayRandomSong();
        }
    }

    void ToggleRadio()
    {
        isRadioOn = !isRadioOn;

        if (isRadioOn)
        {
            TurnOnRadio();
        }
        else
        {
            TurnOffRadio();
        }

        Debug.Log($"Radio is now {(isRadioOn ? "ON" : "OFF")}");
    }

    void TurnOnRadio()
    {
        if (radioSongs == null || radioSongs.Count == 0)
        {
            Debug.LogWarning("No songs in radio playlist!");
            isRadioOn = false;
            return;
        }

        PlayRandomSong();
    }

    void TurnOffRadio()
    {
        if (radioAudioSource != null && radioAudioSource.isPlaying)
        {
            radioAudioSource.Stop();
        }
        currentSongIndex = -1;
    }

    void PlayRandomSong()
    {
        if (radioSongs.Count == 0) return;

        // Ensure we don't play the same song twice in a row
        int newSongIndex;
        do
        {
            newSongIndex = Random.Range(0, radioSongs.Count);
        }
        while (newSongIndex == currentSongIndex && radioSongs.Count > 1);

        currentSongIndex = newSongIndex;

        radioAudioSource.clip = radioSongs[currentSongIndex];
        radioAudioSource.Play();

        Debug.Log($"Now playing: {radioSongs[currentSongIndex].name}");
    }

    // Method to add songs to radio dynamically
    public void AddSongToRadio(AudioClip song)
    {
        if (song != null && !radioSongs.Contains(song))
        {
            radioSongs.Add(song);
        }
    }

    // Method to remove song from radio
    public void RemoveSongFromRadio(AudioClip song)
    {
        if (radioSongs.Contains(song))
        {
            radioSongs.Remove(song);

            // If we removed the current playing song, stop it
            if (currentSongIndex >= 0 && currentSongIndex < radioSongs.Count &&
                radioSongs[currentSongIndex] == song)
            {
                if (isRadioOn)
                {
                    PlayRandomSong();
                }
                else
                {
                    currentSongIndex = -1;
                }
            }
        }
    }

    // Public methods to control radio from other scripts


    public bool IsRadioPlaying()
    {
        return isRadioOn && radioAudioSource.isPlaying;
    }

    public string GetCurrentSongName()
    {
        if (currentSongIndex >= 0 && currentSongIndex < radioSongs.Count && radioAudioSource.isPlaying)
        {
            return radioSongs[currentSongIndex].name;
        }
        return "No song playing";
    }

    // Original methods remain the same
    public void Move()
    {
        _speed = rb.linearVelocity.magnitude;

        foreach (Wheel wheel in wheels)
        {
            wheel.wheelCollider.motorTorque = motorTorgue * verticalInput;
            wheel.UpdateWheelColliders();
        }
    }

    public void Steering()
    {
        int rotationSpeed = 150;
        float zRotation = horizontalInput * rotationSpeed;
        float minangle = -360;
        float maxangle = 360;
        float steeringAngle = horizontalInput * steeringCurve.Evaluate(_speed);
        float slipAngle = Vector3.Angle(transform.forward, rb.linearVelocity - transform.forward);

        if (slipAngle < 120f)
        {
            steeringAngle += Vector3.SignedAngle(transform.forward, rb.linearVelocity, Vector3.up);
            steeringAngle = Mathf.Clamp(steeringAngle, -48, 48);
        }

        zRotation = Mathf.Clamp(zRotation, minangle, maxangle);
        steerTransform.localEulerAngles = new Vector3(0, 180, -zRotation);

        foreach (Wheel wheel in wheels)
        {
            if (wheel.steerible)
            {
                wheel.wheelCollider.steerAngle = steeringAngle;
            }
        }
    }

    void CheckInput()
    {
    
        // Combine keyboard and Logitech G29 input (whichever has greater magnitude)
        if (_inputController != null)
        {
            // Use Logitech G29 throttle for forward movement
            float g29Throttle = _inputController.Throttle;
            // Use Logitech G29 brake for backward movement (inverted)
            float g29Brake = _inputController.Brake;

            // Combine throttle and brake for vertical input
            float g29Vertical = g29Throttle - g29Brake;

            // Use steering from Logitech G29
            float g29Steering = _inputController.Steering;

            // Prioritize input with greater magnitude, or keyboard if no G29 connected
            verticalInput = g29Vertical;
            horizontalInput =  g29Steering  ;
        }

        float movingDirection = Vector3.Dot(transform.forward, rb.linearVelocity);
        breakInput = ((movingDirection < -0.5f && verticalInput > 0) || (movingDirection > 0.5 && verticalInput < 0)) ? Mathf.Abs(verticalInput) : 0f;
    }

    public void Break()
    {

        float totalBrakeInput = breakInput;

        foreach (Wheel wheel in wheels)
        {
            wheel.wheelCollider.brakeTorque = totalBrakeInput * breakForce * (wheel.steerible ? 0.7f : 0.3f);
        }
    }
}

[System.Serializable]
public struct Wheel
{
    public bool steerible;
    public Transform wheelTransform;
    public WheelCollider wheelCollider;
    public Quaternion rotation;
    public Vector3 position;

    public void UpdateWheelColliders()
    {
        wheelCollider.GetWorldPose(out position, out rotation);
        wheelTransform.position = position;
        wheelTransform.rotation = rotation;
    }
}