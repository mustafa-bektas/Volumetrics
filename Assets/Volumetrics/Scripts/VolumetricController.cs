using UnityEngine;

[ExecuteInEditMode]
public class VolumetricController : MonoBehaviour
{
    [Header("Fog Presets")]
    public FogType fogType = FogType.VolumetricClouds;
    
    [Header("Fog Settings")]
    [Range(0f, 0.1f)]
    public float fogDensity = 0.01f;
    public Color fogColor = Color.gray;
    
    [Header("Ray Marching")]
    [Range(8, 128)]
    public int stepCount = 64;
    public float maxDistance = 200f;
    
    [Header("Lighting")]
    [Range(0f, 1f)]
    public float scatteringCoefficient = 0.5f;
    public Color lightColor = Color.white;
    [Range(0f, 3f)]
    public float lightIntensity = 1.0f;
    
    [Header("Cloud Settings")]
    [Range(5f, 50f)]
    public float cloudBaseHeight = 20f;
    [Range(5f, 50f)]
    public float cloudTopHeight = 40f;
    [Range(0f, 1f)]
    public float cloudCoverage = 0.6f;
    [Range(0f, 2f)]
    public float cloudDensity = 0.8f;
    [Space]
    [Range(0.1f, 5f)]
    public float noiseScale = 1.0f;
    [Range(0.01f, 2f)]
    public float noiseDetailScale = 0.5f;
    [Range(0f, 1f)]
    public float windSpeed = 0.1f;
    public Vector2 windDirection = new Vector2(1, 0);
    
    [Header("Advanced Cloud Controls")]
    [Range(0f, 1f)]
    public float cloudSharpness = 0.5f;
    [Range(0f, 2f)]
    public float silverLining = 1.0f;
    [Range(0f, 1f)]
    public float ambientLighting = 0.3f;
    
    [Header("Temporal Accumulation")]
    public bool useTemporalAccumulation = true;
    [Range(0f, 1f)]
    public float temporalBlendFactor = 0.95f;
    
    [Header("Performance")]
    [Range(0.25f, 1f)]
    public float renderScale = 0.75f;
    public bool useHalfResolution = false;
    
    [Header("Debug")]
    public bool showFogOnly = false;
    public bool showStepCount = false;
    [Range(0, 7)]
    public int debugMode = 0;
    
    public enum FogType
    {
        GroundFog,
        UniformFog,
        HeightFog,
        VolumetricClouds,
        SpookyClouds
    }

    private Camera cam;
    private Material volumetricMaterial;
    private Shader volumetricShader;
    private Light mainLight;

    private RenderTexture previousFrame;
    private Matrix4x4 previousViewProjectionMatrix;
    private bool hasValidPreviousFrame = false;

    void Start()
    {
        cam = GetComponent<Camera>();
        cam.depthTextureMode = DepthTextureMode.Depth;

        mainLight = FindFirstObjectByType<Light>();

        volumetricShader = Shader.Find("Custom/Volumetric");
        if (volumetricShader != null)
        {
            volumetricMaterial = new Material(volumetricShader);
        }

        ApplyFogPreset();

    }

    void OnDestroy()
    {
        if (previousFrame != null)
        {
            previousFrame.Release();
            previousFrame = null;
        }
    }

    void OnRenderImage(RenderTexture source, RenderTexture destination)
    {
        if (volumetricMaterial == null)
        {
            Graphics.Blit(source, destination);
            return;
        }

        int renderWidth = useHalfResolution ? source.width / 2 : Mathf.RoundToInt(source.width * renderScale);
        int renderHeight = useHalfResolution ? source.height / 2 : Mathf.RoundToInt(source.height * renderScale);

        RenderTexture volumetricRT = RenderTexture.GetTemporary(renderWidth, renderHeight, 0, RenderTextureFormat.ARGBHalf);
        RenderTexture downscaledSource = null;

        if (renderWidth != source.width || renderHeight != source.height)
        {
            downscaledSource = RenderTexture.GetTemporary(renderWidth, renderHeight, 0, source.format);
            Graphics.Blit(source, downscaledSource);
        }

        volumetricMaterial.SetFloat("_FogDensity", fogDensity);
        volumetricMaterial.SetColor("_FogColor", fogColor);
        volumetricMaterial.SetInt("_StepCount", stepCount);
        volumetricMaterial.SetFloat("_MaxDistance", maxDistance);
        volumetricMaterial.SetFloat("_ScatteringCoefficient", scatteringCoefficient);
        volumetricMaterial.SetColor("_LightColor", lightColor);
        volumetricMaterial.SetFloat("_LightIntensity", lightIntensity);

        Matrix4x4 viewProjectionMatrix = cam.projectionMatrix * cam.worldToCameraMatrix;
        volumetricMaterial.SetMatrix("_ViewProjectionMatrix", viewProjectionMatrix);
        volumetricMaterial.SetMatrix("_PreviousViewProjectionMatrix", previousViewProjectionMatrix);
        volumetricMaterial.SetMatrix("_CameraInverseProjection", cam.projectionMatrix.inverse);

        volumetricMaterial.SetFloat("_CloudBaseHeight", cloudBaseHeight);
        volumetricMaterial.SetFloat("_CloudTopHeight", cloudTopHeight);
        volumetricMaterial.SetFloat("_CloudCoverage", cloudCoverage);
        volumetricMaterial.SetFloat("_CloudDensity", cloudDensity);
        volumetricMaterial.SetFloat("_NoiseScale", noiseScale);
        volumetricMaterial.SetFloat("_NoiseDetailScale", noiseDetailScale);
        volumetricMaterial.SetFloat("_WindSpeed", windSpeed);
        volumetricMaterial.SetVector("_WindDirection", windDirection);
        volumetricMaterial.SetFloat("_CloudSharpness", cloudSharpness);
        volumetricMaterial.SetFloat("_SilverLining", silverLining);
        volumetricMaterial.SetFloat("_AmbientLighting", ambientLighting);

        if (mainLight != null)
        {
            Vector3 lightDir = -mainLight.transform.forward;
            volumetricMaterial.SetVector("_LightDirection", lightDir);
        }

        bool canUseTemporalAccumulation = useTemporalAccumulation && previousFrame != null && hasValidPreviousFrame;

        volumetricMaterial.SetInt("_UseTemporalAccumulation", canUseTemporalAccumulation ? 1 : 0);
        volumetricMaterial.SetFloat("_TemporalBlendFactor", temporalBlendFactor);

        if (canUseTemporalAccumulation)
        {
            volumetricMaterial.SetTexture("_PreviousFrame", previousFrame);
        }
        else
        {
            volumetricMaterial.SetTexture("_PreviousFrame", Texture2D.blackTexture);
        }

        volumetricMaterial.SetInt("_ShowFogOnly", showFogOnly ? 1 : 0);
        volumetricMaterial.SetInt("_ShowStepCount", showStepCount ? 1 : 0);
        volumetricMaterial.SetInt("_DebugMode", debugMode);

        volumetricMaterial.SetInt("_FogType", (int)fogType);
        volumetricMaterial.SetFloat("_NoiseScale", noiseScale);

        RenderTexture sourceToUse = downscaledSource != null ? downscaledSource : source;
        Graphics.Blit(sourceToUse, volumetricRT, volumetricMaterial);

        if (useTemporalAccumulation)
        {
            UpdateTemporalAccumulation(volumetricRT);
        }

        Graphics.Blit(volumetricRT, destination);

        previousViewProjectionMatrix = viewProjectionMatrix;
        hasValidPreviousFrame = true;

        // Cleanup
        RenderTexture.ReleaseTemporary(volumetricRT);
        if (downscaledSource != null)
            RenderTexture.ReleaseTemporary(downscaledSource);
    }

    void UpdateTemporalAccumulation(RenderTexture currentFrame)
    {
        if (previousFrame == null || previousFrame.width != currentFrame.width || previousFrame.height != currentFrame.height)
        {
            if (previousFrame != null)
            {
                previousFrame.Release();
            }

            previousFrame = new RenderTexture(currentFrame.width, currentFrame.height, 0, RenderTextureFormat.ARGBHalf);
            previousFrame.name = "VolumetricPreviousFrame";
            previousFrame.Create();

            Graphics.Blit(currentFrame, previousFrame);
            hasValidPreviousFrame = false;
            return;
        }

        Graphics.Blit(currentFrame, previousFrame);
    }

    void ApplyFogPreset()
    {
        switch (fogType)
        {
            case FogType.VolumetricClouds:
                stepCount = 64;
                maxDistance = 200f;
                cloudBaseHeight = 20f;
                cloudTopHeight = 40f;
                cloudCoverage = 0.6f;
                cloudDensity = 0.8f;
                noiseScale = 1.0f;
                windSpeed = 0.1f;
                scatteringCoefficient = 0.7f;
                lightIntensity = 1.5f;
                break;
                
            case FogType.GroundFog:
                stepCount = 32;
                maxDistance = 100f;
                cloudBaseHeight = 0f;
                cloudTopHeight = 5f;
                cloudCoverage = 0.8f;
                cloudDensity = 1.0f;
                noiseScale = 0.5f;
                windSpeed = 0.05f;
                break;
        }
    }
}