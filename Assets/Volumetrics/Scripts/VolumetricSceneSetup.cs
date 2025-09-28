using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class VolumetricSceneSetup : MonoBehaviour
{
    [Header("Quick Scene Setup")]
    public bool setupOnStart = true;
    
    [Header("Scene Elements")]
    public GameObject terrainPrefab;
    public Material terrainMaterial;
    public bool createTerrain = true;
    
    [Header("Lighting")]
    public bool setupLighting = true;
    public Gradient skyGradient;
    
    void Start()
    {
        if (setupOnStart)
        {
            SetupDemoScene();
        }
    }
    
    [ContextMenu("Setup Demo Scene")]
    public void SetupDemoScene()
    {
        Debug.Log("Setting up Volumetric Demo Scene...");
        
        SetupCamera();
        
        if (setupLighting)
            SetupSceneLighting();
        
        if (createTerrain)
            CreateEnvironment();
        
        SetupRenderSettings();
        
        Debug.Log("Demo scene setup complete!");
    }
    
    void SetupCamera()
    {
        Camera mainCamera = Camera.main;
        if (mainCamera == null)
        {
            GameObject camObj = new GameObject("Main Camera");
            mainCamera = camObj.AddComponent<Camera>();
            camObj.tag = "MainCamera";
        }
        
        mainCamera.transform.position = new Vector3(25, 15, -35);
        mainCamera.transform.rotation = Quaternion.Euler(10, -30, 0);
        
        if (!mainCamera.GetComponent<VolumetricController>())
        {
            VolumetricController volumetric = mainCamera.gameObject.AddComponent<VolumetricController>();
            volumetric.preset = VolumetricController.VolumetricPreset.LightClouds;
        }
        
        if (!mainCamera.GetComponent<DemoCameraController>())
        {
            mainCamera.gameObject.AddComponent<DemoCameraController>();
        }
        
        if (!mainCamera.GetComponent<VolumetricPresetCycler>())
        {
            mainCamera.gameObject.AddComponent<VolumetricPresetCycler>();
        }
        
        mainCamera.fieldOfView = 60f;
        mainCamera.nearClipPlane = 0.3f;
        mainCamera.farClipPlane = 500f;
    }
    
    void SetupSceneLighting()
    {
        Light sunLight = FindFirstObjectByType<Light>();
        if (sunLight == null || sunLight.type != LightType.Directional)
        {
            GameObject lightObj = new GameObject("Directional Light (Sun)");
            sunLight = lightObj.AddComponent<Light>();
            sunLight.type = LightType.Directional;
        }
        
        sunLight.transform.rotation = Quaternion.Euler(35f, -30f, 0f);
        sunLight.intensity = 1.2f;
        sunLight.color = new Color(1f, 0.95f, 0.8f);
        sunLight.shadows = LightShadows.Soft;
        sunLight.shadowStrength = 0.8f;
        sunLight.shadowBias = 0.05f;
        sunLight.shadowNormalBias = 0.4f;
    }
    
    void CreateEnvironment()
    {
        GameObject terrain = GameObject.Find("Terrain");
        if (terrain == null)
        {
            terrain = GameObject.CreatePrimitive(PrimitiveType.Plane);
            terrain.name = "Terrain";
            terrain.transform.localScale = new Vector3(20, 1, 20);
            terrain.transform.position = Vector3.zero;
            
            MeshRenderer renderer = terrain.GetComponent<MeshRenderer>();
            if (terrainMaterial != null)
            {
                renderer.material = terrainMaterial;
            }
            else
            {
                Material mat = new Material(Shader.Find("Standard"));
                mat.color = new Color(0.3f, 0.4f, 0.2f); // Grass-like color
                mat.SetFloat("_Metallic", 0f);
                mat.SetFloat("_Glossiness", 0.2f);
                renderer.material = mat;
            }
        }
        
        CreateLandmarks();
    }
    
    void CreateLandmarks()
    {
        GameObject landmarksParent = GameObject.Find("Landmarks");
        if (landmarksParent == null)
        {
            landmarksParent = new GameObject("Landmarks");
        }
        
        for (int i = 0; i < 5; i++)
        {
            GameObject hill = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            hill.name = $"Hill_{i}";
            hill.transform.parent = landmarksParent.transform;
            
            float distance = Random.Range(30f, 100f);
            float angle = Random.Range(0f, 360f) * Mathf.Deg2Rad;
            float x = Mathf.Sin(angle) * distance;
            float z = Mathf.Cos(angle) * distance;
            float scale = Random.Range(15f, 35f);
            float height = scale * Random.Range(0.3f, 0.6f);
            
            hill.transform.position = new Vector3(x, -height * 0.3f, z);
            hill.transform.localScale = new Vector3(scale, height, scale);
            
            Material hillMat = new Material(Shader.Find("Standard"));
            hillMat.color = new Color(0.4f, 0.35f, 0.3f);
            hillMat.SetFloat("_Metallic", 0f);
            hillMat.SetFloat("_Glossiness", 0.1f);
            hill.GetComponent<MeshRenderer>().material = hillMat;
        }
        
        for (int i = 0; i < 15; i++)
        {
            GameObject tree = new GameObject($"Tree_{i}");
            tree.transform.parent = landmarksParent.transform;
            
            GameObject trunk = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            trunk.name = "Trunk";
            trunk.transform.parent = tree.transform;
            trunk.transform.localScale = new Vector3(0.5f, 3f, 0.5f);
            trunk.transform.localPosition = new Vector3(0, 3f, 0);
            
            Material trunkMat = new Material(Shader.Find("Standard"));
            trunkMat.color = new Color(0.3f, 0.2f, 0.1f);
            trunk.GetComponent<MeshRenderer>().material = trunkMat;
            
            GameObject leaves = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            leaves.name = "Leaves";
            leaves.transform.parent = tree.transform;
            leaves.transform.localScale = new Vector3(3f, 3f, 3f);
            leaves.transform.localPosition = new Vector3(0, 7f, 0);
            
            Material leavesMat = new Material(Shader.Find("Standard"));
            leavesMat.color = new Color(0.2f, 0.5f, 0.1f);
            leaves.GetComponent<MeshRenderer>().material = leavesMat;
            
            float distance = Random.Range(10f, 60f);
            float angle = Random.Range(0f, 360f) * Mathf.Deg2Rad;
            tree.transform.position = new Vector3(
                Mathf.Sin(angle) * distance,
                0,
                Mathf.Cos(angle) * distance
            );
            
            float treeScale = Random.Range(0.8f, 1.5f);
            tree.transform.localScale = Vector3.one * treeScale;
        }
        
        for (int i = 0; i < 8; i++)
        {
            GameObject building = GameObject.CreatePrimitive(PrimitiveType.Cube);
            building.name = $"Building_{i}";
            building.transform.parent = landmarksParent.transform;
            
            float distance = Random.Range(20f, 50f);
            float angle = Random.Range(0f, 360f) * Mathf.Deg2Rad;
            float height = Random.Range(5f, 20f);
            
            building.transform.position = new Vector3(
                Mathf.Sin(angle) * distance,
                height * 0.5f,
                Mathf.Cos(angle) * distance
            );
            
            building.transform.localScale = new Vector3(
                Random.Range(3f, 6f),
                height,
                Random.Range(3f, 6f)
            );
            
            Material buildingMat = new Material(Shader.Find("Standard"));
            float gray = Random.Range(0.3f, 0.5f);
            buildingMat.color = new Color(gray, gray, gray);
            buildingMat.SetFloat("_Metallic", 0.2f);
            buildingMat.SetFloat("_Glossiness", 0.6f);
            building.GetComponent<MeshRenderer>().material = buildingMat;
        }
    }
    
    void SetupRenderSettings()
    {
        RenderSettings.fog = false;
        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Trilight;
        RenderSettings.ambientSkyColor = new Color(0.5f, 0.6f, 0.7f);
        RenderSettings.ambientEquatorColor = new Color(0.4f, 0.45f, 0.5f);
        RenderSettings.ambientGroundColor = new Color(0.2f, 0.25f, 0.3f);
        RenderSettings.ambientIntensity = 0.8f;
        
        Material skybox = RenderSettings.skybox;
        if (skybox == null)
        {
            skybox = new Material(Shader.Find("Skybox/Procedural"));
            if (skybox != null)
            {
                skybox.SetFloat("_SunSize", 0.04f);
                skybox.SetFloat("_SunSizeConvergence", 5f);
                skybox.SetFloat("_AtmosphereThickness", 1f);
                skybox.SetColor("_SkyTint", new Color(0.5f, 0.5f, 0.5f));
                skybox.SetColor("_GroundColor", new Color(0.369f, 0.349f, 0.341f));
                skybox.SetFloat("_Exposure", 1.3f);
                RenderSettings.skybox = skybox;
            }
        }
    }
    
#if UNITY_EDITOR
    [CustomEditor(typeof(VolumetricSceneSetup))]
    public class VolumetricSceneSetupEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
            
            VolumetricSceneSetup setup = (VolumetricSceneSetup)target;
            
            EditorGUILayout.Space();
            
            if (GUILayout.Button("Setup Demo Scene", GUILayout.Height(30)))
            {
                setup.SetupDemoScene();
            }
            
            EditorGUILayout.Space();
            
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Create Environment Only"))
            {
                setup.CreateEnvironment();
            }
            if (GUILayout.Button("Setup Lighting Only"))
            {
                setup.SetupSceneLighting();
            }
            EditorGUILayout.EndHorizontal();
        }
    }
#endif
}