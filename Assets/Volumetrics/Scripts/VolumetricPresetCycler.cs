using UnityEngine;
using System.Collections;

[RequireComponent(typeof(VolumetricController))]
public class VolumetricPresetCycler : MonoBehaviour
{
    [Header("Preset Cycling")]
    public bool autoCycle = false;
    public float cycleInterval = 10f;
    public bool smoothTransition = true;
    public float transitionDuration = 3f;
    
    [Header("Time of Day Simulation")]
    public bool simulateTimeOfDay = false;
    public float dayDuration = 120f; // Duration of full day cycle in seconds
    [Range(0f, 24f)]
    public float currentTimeOfDay = 12f;
    
    [Header("Weather Patterns")]
    public bool dynamicWeather = false;
    public float weatherChangeInterval = 30f;
    
    [Header("Light Control")]
    public Light sunLight;
    public AnimationCurve sunIntensityCurve;
    public Gradient sunColorGradient;
    public AnimationCurve fogDensityCurve;
    
    private VolumetricController volumetricController;
    private VolumetricController.VolumetricPreset[] presetSequence = {
        VolumetricController.VolumetricPreset.MorningMist,
        VolumetricController.VolumetricPreset.ClearSky,
        VolumetricController.VolumetricPreset.LightClouds,
        VolumetricController.VolumetricPreset.DenseClouds,
        VolumetricController.VolumetricPreset.Sunset,
        VolumetricController.VolumetricPreset.StormyClouds
    };
    
    private int currentPresetIndex = 0;
    private bool isTransitioning = false;
    
    // Transition cache
    private float cachedCloudIntensity;
    private float cachedFogDensity;
    private float cachedWindSpeed;
    private Color cachedFogColor;
    private Color cachedSunColor;
    private float cachedSunIntensity;
    
    void Start()
    {
        volumetricController = GetComponent<VolumetricController>();
        
        if (sunLight == null)
            sunLight = FindFirstObjectByType<Light>();
        
        SetupDefaultCurves();
        
        if (autoCycle)
            StartCoroutine(AutoCyclePresets());
        
        if (simulateTimeOfDay)
            StartCoroutine(TimeOfDaySimulation());
        
        if (dynamicWeather)
            StartCoroutine(DynamicWeatherSystem());
    }
    
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.RightArrow))
        {
            NextPreset();
        }
        else if (Input.GetKeyDown(KeyCode.LeftArrow))
        {
            PreviousPreset();
        }
        
        if (Input.GetKeyDown(KeyCode.T))
        {
            simulateTimeOfDay = !simulateTimeOfDay;
            if (simulateTimeOfDay)
                StartCoroutine(TimeOfDaySimulation());
        }
        
        if (Input.GetKeyDown(KeyCode.C))
        {
            autoCycle = !autoCycle;
            if (autoCycle)
                StartCoroutine(AutoCyclePresets());
        }
        
        /* if (Input.GetKeyDown(KeyCode.W))
        {
            dynamicWeather = !dynamicWeather;
            if (dynamicWeather)
                StartCoroutine(DynamicWeatherSystem());
        } */
        
        for (int i = 0; i < 7; i++)
        {
            if (Input.GetKeyDown(KeyCode.F1 + i))
            {
                SetPreset((VolumetricController.VolumetricPreset)i);
            }
        }
    }
    
    void SetupDefaultCurves()
    {
        // Sun intensity over day
        if (sunIntensityCurve == null || sunIntensityCurve.keys.Length == 0)
        {
            sunIntensityCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
            sunIntensityCurve.AddKey(0.25f, 0.3f);  // Dawn
            sunIntensityCurve.AddKey(0.5f, 1f);     // Noon
            sunIntensityCurve.AddKey(0.75f, 0.5f);  // Dusk
        }
        
        // Sun color over day
        if (sunColorGradient == null || sunColorGradient.colorKeys.Length == 0)
        {
            GradientColorKey[] colorKeys = new GradientColorKey[5];
            colorKeys[0].color = new Color(0.2f, 0.2f, 0.3f);  // Night
            colorKeys[0].time = 0f;
            colorKeys[1].color = new Color(1f, 0.6f, 0.3f);    // Dawn
            colorKeys[1].time = 0.25f;
            colorKeys[2].color = new Color(1f, 1f, 0.9f);      // Day
            colorKeys[2].time = 0.5f;
            colorKeys[3].color = new Color(1f, 0.7f, 0.4f);    // Dusk
            colorKeys[3].time = 0.75f;
            colorKeys[4].color = new Color(0.2f, 0.2f, 0.3f);  // Night
            colorKeys[4].time = 1f;
            
            GradientAlphaKey[] alphaKeys = new GradientAlphaKey[2];
            alphaKeys[0].alpha = 1f;
            alphaKeys[0].time = 0f;
            alphaKeys[1].alpha = 1f;
            alphaKeys[1].time = 1f;
            
            sunColorGradient = new Gradient();
            sunColorGradient.SetKeys(colorKeys, alphaKeys);
        }
        
        // Fog density over day
        if (fogDensityCurve == null || fogDensityCurve.keys.Length == 0)
        {
            fogDensityCurve = AnimationCurve.EaseInOut(0f, 0.02f, 1f, 0.005f);
            fogDensityCurve.AddKey(0.25f, 0.015f);  // Dawn fog
            fogDensityCurve.AddKey(0.5f, 0.008f);   // Clear noon
            fogDensityCurve.AddKey(0.75f, 0.012f);  // Evening haze
        }
    }
    
    IEnumerator AutoCyclePresets()
    {
        while (autoCycle)
        {
            yield return new WaitForSeconds(cycleInterval);
            
            if (!isTransitioning)
            {
                NextPreset();
            }
        }
    }
    
    IEnumerator TimeOfDaySimulation()
    {
        while (simulateTimeOfDay)
        {
            currentTimeOfDay += (24f / dayDuration) * Time.deltaTime;
            if (currentTimeOfDay >= 24f)
                currentTimeOfDay -= 24f;
            
            float normalizedTime = currentTimeOfDay / 24f;
            
            // Update sun rotation
            if (sunLight != null)
            {
                float sunAngle = (normalizedTime * 360f) - 90f;
                sunLight.transform.rotation = Quaternion.Euler(sunAngle, 170f, 0f);
                
                // Update sun intensity
                float intensity = sunIntensityCurve.Evaluate(normalizedTime);
                sunLight.intensity = intensity * 1.5f;
                
                // Update sun color
                sunLight.color = sunColorGradient.Evaluate(normalizedTime);
                volumetricController.sunColor = sunLight.color;
                volumetricController.sunIntensity = intensity * 2f;
            }
            
            // Update fog based on time
            float baseFogDensity = fogDensityCurve.Evaluate(normalizedTime);
            volumetricController.fogDensity = baseFogDensity;
            
            // Adjust fog color based on time
            Color fogColor = Color.Lerp(
                sunColorGradient.Evaluate(normalizedTime),
                new Color(0.7f, 0.75f, 0.8f),
                0.5f
            );
            volumetricController.fogColor = fogColor;
            
            yield return null;
        }
    }
    
    IEnumerator DynamicWeatherSystem()
    {
        while (dynamicWeather)
        {
            yield return new WaitForSeconds(weatherChangeInterval);
            
            // Random weather pattern
            float randomValue = Random.Range(0f, 1f);
            
            if (randomValue < 0.3f)
            {
                // Clear weather
                StartCoroutine(TransitionToPreset(VolumetricController.VolumetricPreset.ClearSky));
            }
            else if (randomValue < 0.6f)
            {
                // Light clouds
                StartCoroutine(TransitionToPreset(VolumetricController.VolumetricPreset.LightClouds));
            }
            else if (randomValue < 0.85f)
            {
                // Dense clouds
                StartCoroutine(TransitionToPreset(VolumetricController.VolumetricPreset.DenseClouds));
            }
            else
            {
                // Storm
                StartCoroutine(TransitionToPreset(VolumetricController.VolumetricPreset.StormyClouds));
            }
        }
    }
    
    public void NextPreset()
    {
        currentPresetIndex = (currentPresetIndex + 1) % presetSequence.Length;
        SetPreset(presetSequence[currentPresetIndex]);
    }
    
    public void PreviousPreset()
    {
        currentPresetIndex--;
        if (currentPresetIndex < 0)
            currentPresetIndex = presetSequence.Length - 1;
        SetPreset(presetSequence[currentPresetIndex]);
    }
    
    public void SetPreset(VolumetricController.VolumetricPreset preset)
    {
        if (smoothTransition)
        {
            StartCoroutine(TransitionToPreset(preset));
        }
        else
        {
            volumetricController.preset = preset;
        }
    }
    
    IEnumerator TransitionToPreset(VolumetricController.VolumetricPreset targetPreset)
    {
        if (isTransitioning) yield break;
        isTransitioning = true;
        
        // Cache current values
        cachedCloudIntensity = volumetricController.cloudIntensity;
        cachedFogDensity = volumetricController.fogDensity;
        cachedWindSpeed = volumetricController.windSpeed;
        cachedFogColor = volumetricController.fogColor;
        cachedSunColor = volumetricController.sunColor;
        cachedSunIntensity = volumetricController.sunIntensity;
        
        // Apply target preset to get target values
        volumetricController.preset = targetPreset;
        
        // Store target values
        float targetCloudIntensity = volumetricController.cloudIntensity;
        float targetFogDensity = volumetricController.fogDensity;
        float targetWindSpeed = volumetricController.windSpeed;
        Color targetFogColor = volumetricController.fogColor;
        Color targetSunColor = volumetricController.sunColor;
        float targetSunIntensity = volumetricController.sunIntensity;
        
        // Animate transition
        float elapsed = 0f;
        while (elapsed < transitionDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, elapsed / transitionDuration);
            
            volumetricController.cloudIntensity = Mathf.Lerp(cachedCloudIntensity, targetCloudIntensity, t);
            volumetricController.fogDensity = Mathf.Lerp(cachedFogDensity, targetFogDensity, t);
            volumetricController.windSpeed = Mathf.Lerp(cachedWindSpeed, targetWindSpeed, t);
            volumetricController.fogColor = Color.Lerp(cachedFogColor, targetFogColor, t);
            volumetricController.sunColor = Color.Lerp(cachedSunColor, targetSunColor, t);
            volumetricController.sunIntensity = Mathf.Lerp(cachedSunIntensity, targetSunIntensity, t);
            
            yield return null;
        }
        
        // Ensure final values are set
        volumetricController.cloudIntensity = targetCloudIntensity;
        volumetricController.fogDensity = targetFogDensity;
        volumetricController.windSpeed = targetWindSpeed;
        volumetricController.fogColor = targetFogColor;
        volumetricController.sunColor = targetSunColor;
        volumetricController.sunIntensity = targetSunIntensity;
        
        isTransitioning = false;
    }
    
    void OnGUI()
    {
        int yPos = 10;
        int ySpacing = 25;
        
        /* GUI.Label(new Rect(10, yPos, 300, 20), "ATMOSPHERE CONTROLS");
        yPos += ySpacing;
        
        GUI.Label(new Rect(10, yPos, 300, 20), "← → - Change Preset");
        yPos += 20;
        
        GUI.Label(new Rect(10, yPos, 300, 20), "F1-F7 - Quick Preset Select");
        yPos += 20;
        
        GUI.Label(new Rect(10, yPos, 300, 20), "C - Toggle Auto Cycle");
        yPos += 20;
        
        GUI.Label(new Rect(10, yPos, 300, 20), "T - Toggle Time of Day");
        yPos += 20;
        
        GUI.Label(new Rect(10, yPos, 300, 20), "W - Toggle Dynamic Weather");
        yPos += ySpacing; */
        
        // Status
        string currentPresetName = volumetricController.preset.ToString();
        GUIStyle presetStyle = new GUIStyle(GUI.skin.label) {
            fontSize = 22,                // increase or decrease this value
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleLeft
        };
        GUI.Label(new Rect(10, yPos, 400, 30), $"Current: {currentPresetName}", presetStyle);
        yPos += 30;
        
        if (simulateTimeOfDay)
        {
            int hours = Mathf.FloorToInt(currentTimeOfDay);
            int minutes = Mathf.FloorToInt((currentTimeOfDay - hours) * 60);
            GUI.Label(new Rect(10, yPos, 300, 20), $"Time: {hours:00}:{minutes:00}");
        }
    }
}