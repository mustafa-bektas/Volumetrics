using UnityEngine;

[ExecuteInEditMode]
[RequireComponent(typeof(Camera))]
public class VolumetricController : MonoBehaviour
{
    [System.Serializable]
    public enum VolumetricPreset
    {
        ClearSky,
        LightClouds,
        DenseClouds,
        StormyClouds,
        GroundFog,
        MorningMist,
        Sunset,
        Custom
    }

    [Header("Quick Setup")]
    [SerializeField] public VolumetricPreset preset = VolumetricPreset.LightClouds;
    private VolumetricPreset lastPreset;

    [Header("Basic Controls")]
    [Range(0f, 1f)]
    public float cloudIntensity = 0.6f;
    [Range(0f, 2f)]
    public float windSpeed = 2.0f;
    public Vector2 windDirection = new Vector2(1, 1).normalized;
    
    [Header("Visual Settings")]
    public Color fogColor = new Color(0.76f, 0.81f, 0.85f);
    public Color sunColor = new Color(1f, 0.95f, 0.8f);
    [Range(0f, 3f)]
    public float sunIntensity = 1.5f;

    [Header("Performance")]
    [Range(32, 512)]
    public int quality = 256;
    [Range(0.5f, 1f)]
    public float renderScale = 0.75f;
    public bool enableTemporalFiltering = false;

    [Header("Advanced Settings")]
    public bool showAdvanced = false;
    
    [ConditionalHide("showAdvanced", true)]
    [Range(0f, 0.1f)]
    public float fogDensity = 0.01f;
    
    [ConditionalHide("showAdvanced", true)]
    [Range(10f, 100f)]
    public float cloudBaseHeight = 20f;
    
    [ConditionalHide("showAdvanced", true)]
    [Range(10f, 100f)]
    public float cloudTopHeight = 50f;
    
    [ConditionalHide("showAdvanced", true)]
    [Range(0f, 1f)]
    public float cloudCoverage = 0.6f;
    
    [ConditionalHide("showAdvanced", true)]
    [Range(0.5f, 3f)]
    public float noiseScale = 1.5f;
    
    [ConditionalHide("showAdvanced", true)]
    [Range(0f, 1f)]
    public float scatteringIntensity = 0.7f;
    
    [ConditionalHide("showAdvanced", true)]
    [Range(0f, 2f)]
    public float silverLining = 1.2f;

    [Header("Debug")]
    public bool debugView = false;
    public int debugMode = 0;

    // Private variables
    private Camera cam;
    private Material volumetricMaterial;
    private Light mainLight;
    private RenderTexture previousFrame;
    private RenderTexture volumetricBuffer;
    private Matrix4x4 previousViewProjectionMatrix;

    void Start()
    {
        Initialize();
        ApplyPreset();
    }

    void OnEnable()
    {
        Initialize();
    }

    void Initialize()
    {
        cam = GetComponent<Camera>();
        cam.depthTextureMode |= DepthTextureMode.Depth;

        mainLight = FindFirstObjectByType<Light>();
        
        Shader volumetricShader = Shader.Find("Custom/Volumetric");
        if (volumetricShader != null)
        {
            volumetricMaterial = new Material(volumetricShader);
        }
        else
        {
            Debug.LogError("Volumetric shader not found!");
        }
    }

    void Update()
    {
        if (preset != lastPreset && preset != VolumetricPreset.Custom)
        {
            ApplyPreset();
            lastPreset = preset;
        }
    }

    void OnDestroy()
    {
        ReleaseBuffers();
    }

    void OnDisable()
    {
        ReleaseBuffers();
    }

    void ReleaseBuffers()
    {
        if (previousFrame != null)
        {
            previousFrame.Release();
            previousFrame = null;
        }
        if (volumetricBuffer != null)
        {
            volumetricBuffer.Release();
            volumetricBuffer = null;
        }
    }

    void ApplyPreset()
    {
        switch (preset)
        {
            case VolumetricPreset.ClearSky:
                cloudIntensity = 0.2f;
                fogDensity = 0.005f;
                cloudBaseHeight = 30f;
                cloudTopHeight = 60f;
                cloudCoverage = 0.3f;
                noiseScale = 3f;
                windSpeed = 2.0f;
                scatteringIntensity = 0.5f;
                silverLining = 1.5f;
                sunIntensity = 2f;
                fogColor = new Color(0.85f, 0.9f, 0.95f);
                sunColor = new Color(1f, 1f, 0.9f);
                break;

            case VolumetricPreset.LightClouds:
                cloudIntensity = 0.5f;
                fogDensity = 0.008f;
                cloudBaseHeight = 25f;
                cloudTopHeight = 50f;
                cloudCoverage = 0.5f;
                noiseScale = 3f;
                windSpeed = 1.5f;
                scatteringIntensity = 0.6f;
                silverLining = 1.2f;
                sunIntensity = 3f;
                fogColor = new Color(0.76f, 0.81f, 0.85f);
                sunColor = new Color(1f, 1.0f, 1.0f);
                break;

            case VolumetricPreset.DenseClouds:
                cloudIntensity = 0.8f;
                fogDensity = 0.015f;
                cloudBaseHeight = 15f;
                cloudTopHeight = 45f;
                cloudCoverage = 0.7f;
                noiseScale = 3f;
                windSpeed = 2.0f;
                scatteringIntensity = 0.7f;
                silverLining = 0.8f;
                sunIntensity = 1f;
                fogColor = new Color(0.6f, 0.65f, 0.7f);
                sunColor = new Color(0.9f, 0.9f, 0.85f);
                break;

            case VolumetricPreset.StormyClouds:
                cloudIntensity = 1f;
                fogDensity = 0.025f;
                cloudBaseHeight = 10f;
                cloudTopHeight = 40f;
                cloudCoverage = 0.85f;
                noiseScale = 3f;
                windSpeed = 2.0f;
                scatteringIntensity = 0.8f;
                silverLining = 0.5f;
                sunIntensity = 0.5f;
                fogColor = new Color(0.4f, 0.45f, 0.5f);
                sunColor = new Color(0.7f, 0.7f, 0.7f);
                break;

            case VolumetricPreset.GroundFog:
                cloudIntensity = 0.6f;
                fogDensity = 0.02f;
                cloudBaseHeight = 0f;
                cloudTopHeight = 8f;
                cloudCoverage = 0.8f;
                noiseScale = 3f;
                windSpeed = 2.0f;
                scatteringIntensity = 0.9f;
                silverLining = 0.2f;
                sunIntensity = 0.8f;
                fogColor = new Color(0.7f, 0.75f, 0.8f);
                sunColor = new Color(0.9f, 0.85f, 0.7f);
                break;

            case VolumetricPreset.MorningMist:
                cloudIntensity = 0.4f;
                fogDensity = 0.0018f;
                cloudBaseHeight = 0f;
                cloudTopHeight = 15f;
                cloudCoverage = 0.6f;
                noiseScale = 3f;
                windSpeed = 2.0f;
                scatteringIntensity = 0.8f;
                silverLining = 1.8f;
                sunIntensity = 1.2f;
                fogColor = new Color(0.9f, 0.85f, 0.7f);
                sunColor = new Color(1f, 0.9f, 0.6f);
                break;

            case VolumetricPreset.Sunset:
                cloudIntensity = 0.6f;
                fogDensity = 0.01f;
                cloudBaseHeight = 20f;
                cloudTopHeight = 55f;
                cloudCoverage = 0.55f;
                noiseScale = 3f;
                windSpeed = 2.0f;
                scatteringIntensity = 0.9f;
                silverLining = 2f;
                sunIntensity = 2.5f;
                fogColor = new Color(1f, 0.7f, 0.4f);
                sunColor = new Color(1f, 0.6f, 0.2f);
                break;
        }
    }

    void OnRenderImage(RenderTexture source, RenderTexture destination)
    {
        if (volumetricMaterial == null)
        {
            Graphics.Blit(source, destination);
            return;
        }

        int targetWidth = Mathf.RoundToInt(source.width * renderScale);
        int targetHeight = Mathf.RoundToInt(source.height * renderScale);

        if (volumetricBuffer == null || volumetricBuffer.width != targetWidth || volumetricBuffer.height != targetHeight)
        {
            if (volumetricBuffer != null)
                volumetricBuffer.Release();
            
            volumetricBuffer = new RenderTexture(targetWidth, targetHeight, 0, RenderTextureFormat.ARGBHalf);
            volumetricBuffer.name = "VolumetricBuffer";
            volumetricBuffer.filterMode = FilterMode.Bilinear;
            volumetricBuffer.Create();
        }

        if (enableTemporalFiltering)
        {
            if (previousFrame == null || previousFrame.width != targetWidth || previousFrame.height != targetHeight)
            {
                if (previousFrame != null)
                    previousFrame.Release();
                
                previousFrame = new RenderTexture(targetWidth, targetHeight, 0, RenderTextureFormat.ARGBHalf);
                previousFrame.name = "VolumetricPreviousFrame";
                previousFrame.Create();
            }
        }

        SetShaderParameters();

        Graphics.Blit(source, volumetricBuffer, volumetricMaterial);

        if (enableTemporalFiltering && previousFrame != null)
        {
            Graphics.Blit(volumetricBuffer, previousFrame);
            previousViewProjectionMatrix = cam.projectionMatrix * cam.worldToCameraMatrix;
        }

        Graphics.Blit(volumetricBuffer, destination);
    }

    void SetShaderParameters()
    {
        // basic parameters
        volumetricMaterial.SetFloat("_FogDensity", fogDensity * cloudIntensity);
        volumetricMaterial.SetColor("_FogColor", fogColor);
        volumetricMaterial.SetInt("_StepCount", quality);
        volumetricMaterial.SetFloat("_MaxDistance", 200f);
        
        // cloud parameters
        volumetricMaterial.SetFloat("_CloudBaseHeight", cloudBaseHeight);
        volumetricMaterial.SetFloat("_CloudTopHeight", cloudTopHeight);
        volumetricMaterial.SetFloat("_CloudCoverage", cloudCoverage);
        volumetricMaterial.SetFloat("_CloudDensity", cloudIntensity);
        volumetricMaterial.SetFloat("_NoiseScale", noiseScale);
        volumetricMaterial.SetFloat("_NoiseDetailScale", noiseScale * 0.5f);
        
        // animation
        volumetricMaterial.SetFloat("_WindSpeed", windSpeed);
        volumetricMaterial.SetVector("_WindDirection", windDirection.normalized);
        
        // lighting
        volumetricMaterial.SetFloat("_ScatteringCoefficient", scatteringIntensity);
        volumetricMaterial.SetColor("_LightColor", sunColor);
        volumetricMaterial.SetFloat("_LightIntensity", sunIntensity);
        volumetricMaterial.SetFloat("_SilverLining", silverLining);
        volumetricMaterial.SetFloat("_AmbientLighting", 0.3f);
        
        // light direction
        if (mainLight != null)
        {
            volumetricMaterial.SetVector("_LightDirection", -mainLight.transform.forward);
        }
        
        // matrix for reprojection
        volumetricMaterial.SetMatrix("_CameraInverseProjection", cam.projectionMatrix.inverse);
        volumetricMaterial.SetMatrix("_ViewProjectionMatrix", cam.projectionMatrix * cam.worldToCameraMatrix);
        volumetricMaterial.SetMatrix("_PreviousViewProjectionMatrix", previousViewProjectionMatrix);
        
        // temporal
        volumetricMaterial.SetInt("_UseTemporalAccumulation", enableTemporalFiltering && previousFrame != null ? 1 : 0);
        volumetricMaterial.SetFloat("_TemporalBlendFactor", 0.9f);
        if (previousFrame != null)
        {
            volumetricMaterial.SetTexture("_PreviousFrame", previousFrame);
        }
        
        // debug
        volumetricMaterial.SetInt("_ShowFogOnly", debugView ? 1 : 0);
        volumetricMaterial.SetInt("_DebugMode", debugMode);
    }
}

public class ConditionalHideAttribute : PropertyAttribute
{
    public string ConditionalSourceField;
    public bool HideInInspector;

    public ConditionalHideAttribute(string conditionalSourceField, bool hideInInspector = false)
    {
        ConditionalSourceField = conditionalSourceField;
        HideInInspector = hideInInspector;
    }
}