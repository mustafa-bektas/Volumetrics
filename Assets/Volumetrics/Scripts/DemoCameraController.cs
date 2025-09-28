using UnityEngine;
using System.Collections;

public class DemoCameraController : MonoBehaviour
{
    [Header("Demo Mode")]
    public bool autoPlay = true;
    public float transitionDuration = 3f;
    
    [Header("Camera Positions")]
    public Transform[] cameraWaypoints;
    private int currentWaypointIndex = 0;
    
    [Header("Manual Controls")]
    public bool enableManualControl = true;
    public float moveSpeed = 10f;
    public float lookSpeed = 2f;
    public float smoothTime = 0.1f;
    
    [Header("Orbit Settings")]
    public Transform orbitTarget;
    public float orbitRadius = 30f;
    public float orbitSpeed = 10f;
    public float orbitHeight = 15f;
    
    private Camera cam;
    private Vector3 currentVelocity;
    private Vector2 currentRotation;
    private bool isTransitioning = false;
    private float orbitAngle = 0f;
    
    // Demo states
    public enum DemoState
    {
        Waypoints,
        Orbit,
        Manual,
        Flythrough
    }
    
    public DemoState currentState = DemoState.Waypoints;
    
    void Start()
    {
        cam = GetComponent<Camera>();
        if (cam == null)
            cam = Camera.main;
        
        currentRotation = new Vector2(transform.eulerAngles.y, transform.eulerAngles.x);
        
        // Create default waypoints if none exist
        if (cameraWaypoints == null || cameraWaypoints.Length == 0)
        {
            CreateDefaultWaypoints();
        }
        
        if (autoPlay)
        {
            StartCoroutine(AutoPlayDemo());
        }
    }
    
    void Update()
    {
        // Switch between demo modes
        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            currentState = DemoState.Waypoints;
            StopAllCoroutines();
            if (autoPlay) StartCoroutine(AutoPlayDemo());
        }
        else if (Input.GetKeyDown(KeyCode.Alpha2))
        {
            currentState = DemoState.Orbit;
        }
        else if (Input.GetKeyDown(KeyCode.Alpha3))
        {
            currentState = DemoState.Manual;
            enableManualControl = true;
        }
        else if (Input.GetKeyDown(KeyCode.Alpha4))
        {
            currentState = DemoState.Flythrough;
            StartCoroutine(FlythroughSequence());
        }
        
        // Handle current state
        switch (currentState)
        {
            case DemoState.Manual:
                if (enableManualControl)
                    HandleManualControl();
                break;
            case DemoState.Orbit:
                HandleOrbitMode();
                break;
        }
        
        // Toggle auto-play
        if (Input.GetKeyDown(KeyCode.Space))
        {
            autoPlay = !autoPlay;
            if (autoPlay && currentState == DemoState.Waypoints)
            {
                StartCoroutine(AutoPlayDemo());
            }
        }
    }
    
    void HandleManualControl()
    {
        // WASD movement
        float horizontal = Input.GetAxis("Horizontal");
        float vertical = Input.GetAxis("Vertical");
        float upDown = 0f;
        
        if (Input.GetKey(KeyCode.Q)) upDown = -1f;
        if (Input.GetKey(KeyCode.E)) upDown = 1f;
        
        Vector3 moveDirection = transform.right * horizontal + 
                              transform.forward * vertical + 
                              transform.up * upDown;
        
        float speedMultiplier = Input.GetKey(KeyCode.LeftShift) ? 2f : 1f;
        transform.position += moveDirection * moveSpeed * speedMultiplier * Time.deltaTime;
        
        // Mouse look
        if (Input.GetMouseButton(1))
        {
            currentRotation.x += Input.GetAxis("Mouse X") * lookSpeed;
            currentRotation.y -= Input.GetAxis("Mouse Y") * lookSpeed;
            currentRotation.y = Mathf.Clamp(currentRotation.y, -89f, 89f);
            
            transform.rotation = Quaternion.Euler(currentRotation.y, currentRotation.x, 0);
        }
    }
    
    void HandleOrbitMode()
    {
        if (orbitTarget == null)
        {
            // Create a default orbit target at scene center
            GameObject target = GameObject.Find("OrbitTarget");
            if (target == null)
            {
                target = new GameObject("OrbitTarget");
                target.transform.position = new Vector3(0, 10, 0);
            }
            orbitTarget = target.transform;
        }
        
        orbitAngle += orbitSpeed * Time.deltaTime;
        
        float x = Mathf.Sin(orbitAngle * Mathf.Deg2Rad) * orbitRadius;
        float z = Mathf.Cos(orbitAngle * Mathf.Deg2Rad) * orbitRadius;
        float y = orbitHeight + Mathf.Sin(orbitAngle * 2f * Mathf.Deg2Rad) * 5f;
        
        Vector3 targetPosition = orbitTarget.position + new Vector3(x, y, z);
        transform.position = Vector3.SmoothDamp(transform.position, targetPosition, ref currentVelocity, smoothTime);
        
        transform.LookAt(orbitTarget.position + Vector3.up * 10f);
    }
    
    IEnumerator AutoPlayDemo()
    {
        while (autoPlay && currentState == DemoState.Waypoints)
        {
            if (cameraWaypoints != null && cameraWaypoints.Length > 0)
            {
                Transform targetWaypoint = cameraWaypoints[currentWaypointIndex];
                if (targetWaypoint != null)
                {
                    yield return StartCoroutine(TransitionToWaypoint(targetWaypoint));
                    yield return new WaitForSeconds(2f); // Pause at waypoint
                }
                
                currentWaypointIndex = (currentWaypointIndex + 1) % cameraWaypoints.Length;
            }
            yield return null;
        }
    }
    
    IEnumerator TransitionToWaypoint(Transform target)
    {
        isTransitioning = true;
        Vector3 startPos = transform.position;
        Quaternion startRot = transform.rotation;
        
        float elapsed = 0f;
        while (elapsed < transitionDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, elapsed / transitionDuration);
            
            transform.position = Vector3.Lerp(startPos, target.position, t);
            transform.rotation = Quaternion.Slerp(startRot, target.rotation, t);
            
            yield return null;
        }
        
        transform.position = target.position;
        transform.rotation = target.rotation;
        isTransitioning = false;
    }
    
    IEnumerator FlythroughSequence()
    {
        // Dynamic flythrough showcasing different heights and angles
        Vector3[] flythroughPath = new Vector3[]
        {
            new Vector3(0, 5, -30),    // Low ground view
            new Vector3(20, 15, -20),  // Mid elevation
            new Vector3(30, 40, 0),    // High altitude
            new Vector3(20, 60, 20),   // Above clouds
            new Vector3(0, 35, 30),    // Through clouds
            new Vector3(-20, 20, 20),  // Side view
            new Vector3(-30, 10, 0),   // Low side
            new Vector3(-20, 5, -20),  // Return low
            new Vector3(0, 5, -30)     // Back to start
        };
        
        for (int i = 0; i < flythroughPath.Length; i++)
        {
            Vector3 targetPos = flythroughPath[i];
            Vector3 lookTarget = Vector3.zero + Vector3.up * 10f;
            
            float segmentDuration = 3f;
            float elapsed = 0f;
            
            Vector3 startPos = transform.position;
            Quaternion startRot = transform.rotation;
            Quaternion targetRot = Quaternion.LookRotation(lookTarget - targetPos);
            
            while (elapsed < segmentDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.SmoothStep(0f, 1f, elapsed / segmentDuration);
                
                transform.position = Vector3.Lerp(startPos, targetPos, t);
                transform.rotation = Quaternion.Slerp(startRot, targetRot, t);
                
                yield return null;
            }
        }
        
        currentState = DemoState.Waypoints;
        if (autoPlay) StartCoroutine(AutoPlayDemo());
    }
    
    void CreateDefaultWaypoints()
    {
        GameObject waypointParent = new GameObject("Camera Waypoints");
        cameraWaypoints = new Transform[3];
        
        // Create diverse camera positions
        string[] waypointNames = { "Ground View", "Mid View", "Dramatic Angle" };
        Vector3[] positions = {
            new Vector3(10, 3, -25),   // Ground level
            new Vector3(20, 15, -15),  // Mid elevation
            new Vector3(-25, 8, -10)   // Dramatic low angle
        };
        
        Vector3[] lookTargets = {
            new Vector3(0, 10, 0),
            new Vector3(0, 20, 0),
            new Vector3(0, 30, 0)
        };
        
        for (int i = 0; i < 3; i++)
        {
            GameObject waypoint = new GameObject(waypointNames[i]);
            waypoint.transform.parent = waypointParent.transform;
            waypoint.transform.position = positions[i];
            waypoint.transform.LookAt(lookTargets[i]);
            cameraWaypoints[i] = waypoint.transform;
        }
    }
    
    void OnGUI()
    {
        /* // Display controls
        int yPos = 10;
        int ySpacing = 25;
        
        GUI.Label(new Rect(10, yPos, 300, 20), "VOLUMETRIC FOG DEMO CONTROLS");
        yPos += ySpacing;
        
        GUI.Label(new Rect(10, yPos, 300, 20), "1 - Waypoint Mode | 2 - Orbit Mode");
        yPos += 20;
        
        GUI.Label(new Rect(10, yPos, 300, 20), "3 - Manual Control | 4 - Flythrough");
        yPos += 20;
        
        GUI.Label(new Rect(10, yPos, 300, 20), "Space - Toggle Auto-play");
        yPos += ySpacing;
        
        if (currentState == DemoState.Manual)
        {
            GUI.Label(new Rect(10, yPos, 300, 20), "WASD - Move | Q/E - Up/Down");
            yPos += 20;
            GUI.Label(new Rect(10, yPos, 300, 20), "Right Mouse - Look Around");
            yPos += 20;
            GUI.Label(new Rect(10, yPos, 300, 20), "Shift - Speed Boost");
        }
        
        // Current mode indicator
        GUI.Label(new Rect(10, Screen.height - 30, 200, 20), 
            $"Mode: {currentState} | Auto: {autoPlay}"); */
    }
}