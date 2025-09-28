using UnityEngine;

[ExecuteInEditMode]
public class VolumetricController : MonoBehaviour
{
    [Header("Fog Settings")]
    [Range(0f, 0.1f)]
    public float fogDensity = 0.01f;
    public Color fogColor = Color.gray;
    
    [Header("Debug")]
    public bool showFogOnly = false;
    
    private Camera cam;
    private Material volumetricMaterial;
    private Shader volumetricShader;
    
    void Start()
    {
        cam = GetComponent<Camera>();
        cam.depthTextureMode = DepthTextureMode.Depth;
        
        // load shader and create material
        volumetricShader = Shader.Find("Custom/Volumetric");
        if (volumetricShader != null)
        {
            volumetricMaterial = new Material(volumetricShader);
        }
    }
    
    void OnRenderImage(RenderTexture source, RenderTexture destination)
    {
        if (volumetricMaterial == null)
        {
            Graphics.Blit(source, destination);
            return;
        }
        
        // pass parameters to shader
        volumetricMaterial.SetFloat("_FogDensity", fogDensity);
        volumetricMaterial.SetColor("_FogColor", fogColor);
        volumetricMaterial.SetInt("_ShowFogOnly", showFogOnly ? 1 : 0);
        
        // apply volumetric effect
        Graphics.Blit(source, destination, volumetricMaterial);
    }
}