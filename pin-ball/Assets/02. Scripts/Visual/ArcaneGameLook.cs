using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

/// <summary>
/// Installs the restrained post-processing and palette treatment used by the
/// representative game scene. The setup is runtime-only and scene-gated.
/// </summary>
[DisallowMultipleComponent]
public sealed class ArcaneGameLook : MonoBehaviour
{
    private const string TargetSceneName = "02. Game";
    private const string RuntimeObjectName = "[Arcane Game Look]";

    private VolumeProfile _runtimeProfile;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void RegisterSceneHook()
    {
        UnityEngine.SceneManagement.SceneManager.sceneLoaded -= OnSceneLoaded;
        UnityEngine.SceneManagement.SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.name != TargetSceneName || GameObject.Find(RuntimeObjectName) != null)
        {
            return;
        }

        var runtimeObject = new GameObject(RuntimeObjectName);
        runtimeObject.AddComponent<ArcaneGameLook>();
    }

    private void Start()
    {
        ConfigureCamera();
        ConfigureVolume();
        ApplyScenePalette();
    }

    private static void ConfigureCamera()
    {
        var camera = Camera.main;
        if (camera == null)
        {
            Debug.LogWarning("Arcane game look could not find the Main Camera.");
            return;
        }

        camera.allowHDR = true;
        var cameraData = camera.GetUniversalAdditionalCameraData();
        cameraData.renderPostProcessing = true;
        cameraData.requiresColorOption = CameraOverrideOption.Off;
        cameraData.requiresDepthOption = CameraOverrideOption.Off;
    }

    private void ConfigureVolume()
    {
        var profile = ScriptableObject.CreateInstance<VolumeProfile>();
        profile.name = "Arcane Game Look (Runtime)";
        profile.hideFlags = HideFlags.HideAndDontSave;
        _runtimeProfile = profile;

        var bloom = profile.Add<Bloom>(true);
        bloom.threshold.Override(0.6f);
        bloom.intensity.Override(1.1f);
        bloom.scatter.Override(0.7f);
        bloom.clamp.Override(12f);
        bloom.highQualityFiltering.Override(false);

        var color = profile.Add<ColorAdjustments>(true);
        color.postExposure.Override(0f);
        color.contrast.Override(13f);
        color.saturation.Override(-7f);
        color.colorFilter.Override(new Color(0.88f, 0.94f, 1f, 1f));

        var tonemapping = profile.Add<Tonemapping>(true);
        tonemapping.mode.Override(TonemappingMode.Neutral);

        var vignette = profile.Add<Vignette>(true);
        vignette.color.Override(new Color32(0x9A, 0x81, 0xFF, 0xFF));
        vignette.intensity.Override(0.22f);
        vignette.smoothness.Override(0.68f);
        vignette.rounded.Override(false);

        var volume = gameObject.AddComponent<Volume>();
        volume.isGlobal = true;
        volume.priority = 90f;
        volume.weight = 1f;
        volume.sharedProfile = profile;
    }

    private static void ApplyScenePalette()
    {
        var renderers = FindObjectsByType<SpriteRenderer>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None);

        foreach (var spriteRenderer in renderers)
        {
            if (spriteRenderer.name == "MapBackground")
            {
                spriteRenderer.color = new Color(0.48f, 0.64f, 0.7f, 1f);
            }
        }
    }

    private void OnDestroy()
    {
        if (_runtimeProfile != null)
        {
            Destroy(_runtimeProfile);
        }
    }
}
