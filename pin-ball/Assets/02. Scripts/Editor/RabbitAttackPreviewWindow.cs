using System;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

public sealed class RabbitAttackPreviewWindow : EditorWindow
{
    private const string SpriteSheetPath =
        "Assets/03. Images/Animals/Rabbit/Rabbit1_Mage_HWarriorStyle_Attack.png";
    private const string AnimationFolder = "Assets/05. Animations/Rabbit";
    private const string ClipPath = AnimationFolder + "/Rabbit1_Mage_Attack.anim";
    private const string ControllerPath = AnimationFolder + "/Rabbit1_Mage_Attack.controller";
    private const string PrefabFolder = "Assets/04. Prefabs/Rabbit";
    private const string PrefabPath = PrefabFolder + "/Rabbit1_Mage_AttackPreview.prefab";
    private const string SessionKey = "PinBall.RabbitAttackPreview.Opened";
    private const int FrameCount = 5;
    private const float FramesPerSecond = 10f;

    private Texture2D spriteSheet;
    private int frameIndex;
    private bool isPlaying = true;
    private double nextFrameTime;

    [InitializeOnLoadMethod]
    private static void OpenOnceAfterImport()
    {
        if (SessionState.GetBool(SessionKey, false))
        {
            return;
        }

        EditorApplication.delayCall += () =>
        {
            if (EnsureAssets())
            {
                SessionState.SetBool(SessionKey, true);
                OpenWindow();
            }
        };
    }

    [MenuItem("Window/Pin Ball/Rabbit Attack Preview")]
    private static void OpenWindow()
    {
        EnsureAssets();
        var window = GetWindow<RabbitAttackPreviewWindow>("Rabbit Attack Preview");
        window.minSize = new Vector2(360f, 420f);
        window.Show();
    }

    private static bool EnsureAssets()
    {
        try
        {
            var sprites = AssetDatabase.LoadAllAssetsAtPath(SpriteSheetPath)
                .OfType<Sprite>()
                .OrderBy(sprite => sprite.name, StringComparer.Ordinal)
                .ToArray();

            if (sprites.Length != FrameCount)
            {
                Debug.LogError(
                    $"Rabbit attack setup expected {FrameCount} sprites but found {sprites.Length} at {SpriteSheetPath}.");
                return false;
            }

            EnsureFolder("Assets/05. Animations", "Rabbit");
            EnsureFolder("Assets/04. Prefabs", "Rabbit");

            var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(ClipPath);
            if (clip == null)
            {
                clip = new AnimationClip { name = "Rabbit1_Mage_Attack", frameRate = FramesPerSecond };
                AssetDatabase.CreateAsset(clip, ClipPath);
            }

            clip.frameRate = FramesPerSecond;
            var keyframes = new ObjectReferenceKeyframe[FrameCount + 1];
            for (var i = 0; i < FrameCount; i++)
            {
                keyframes[i] = new ObjectReferenceKeyframe
                {
                    time = i / FramesPerSecond,
                    value = sprites[i]
                };
            }

            keyframes[FrameCount] = new ObjectReferenceKeyframe
            {
                time = FrameCount / FramesPerSecond,
                value = sprites[FrameCount - 1]
            };

            var binding = new EditorCurveBinding
            {
                path = string.Empty,
                type = typeof(SpriteRenderer),
                propertyName = "m_Sprite"
            };
            AnimationUtility.SetObjectReferenceCurve(clip, binding, keyframes);

            var clipSettings = AnimationUtility.GetAnimationClipSettings(clip);
            clipSettings.loopTime = false;
            AnimationUtility.SetAnimationClipSettings(clip, clipSettings);
            EditorUtility.SetDirty(clip);

            var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath);
            if (controller == null)
            {
                controller = AnimatorController.CreateAnimatorControllerAtPath(ControllerPath);
                var stateMachine = controller.layers[0].stateMachine;
                var state = stateMachine.AddState("Attack");
                state.motion = clip;
                stateMachine.defaultState = state;
                EditorUtility.SetDirty(controller);
            }

            if (AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath) == null)
            {
                var previewObject = new GameObject("Rabbit1_Mage_AttackPreview");
                try
                {
                    var spriteRenderer = previewObject.AddComponent<SpriteRenderer>();
                    spriteRenderer.sprite = sprites[0];

                    var animator = previewObject.AddComponent<Animator>();
                    animator.runtimeAnimatorController = controller;
                    animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;

                    PrefabUtility.SaveAsPrefabAsset(previewObject, PrefabPath);
                }
                finally
                {
                    DestroyImmediate(previewObject);
                }
            }

            AssetDatabase.SaveAssets();
            Debug.Log(
                $"Rabbit attack animation ready: {ClipPath} ({FrameCount} frames, {FramesPerSecond:0} FPS, non-looping). " +
                $"Preview: Window > Pin Ball > Rabbit Attack Preview");
            return true;
        }
        catch (Exception exception)
        {
            Debug.LogException(exception);
            return false;
        }
    }

    private static void EnsureFolder(string parentFolder, string childName)
    {
        var childPath = parentFolder + "/" + childName;
        if (!AssetDatabase.IsValidFolder(childPath))
        {
            AssetDatabase.CreateFolder(parentFolder, childName);
        }
    }

    private void OnEnable()
    {
        spriteSheet = AssetDatabase.LoadAssetAtPath<Texture2D>(SpriteSheetPath);
        nextFrameTime = EditorApplication.timeSinceStartup + 1d / FramesPerSecond;
    }

    private void Update()
    {
        if (!isPlaying || EditorApplication.timeSinceStartup < nextFrameTime)
        {
            return;
        }

        frameIndex = (frameIndex + 1) % FrameCount;
        nextFrameTime = EditorApplication.timeSinceStartup + 1d / FramesPerSecond;
        Repaint();
    }

    private void OnGUI()
    {
        if (spriteSheet == null)
        {
            spriteSheet = AssetDatabase.LoadAssetAtPath<Texture2D>(SpriteSheetPath);
        }

        EditorGUILayout.Space(8f);
        EditorGUILayout.LabelField("Rabbit Mage Attack", EditorStyles.boldLabel);
        EditorGUILayout.LabelField("5 frames · 10 FPS · 0.5 seconds", EditorStyles.miniLabel);
        EditorGUILayout.Space(6f);

        var previewArea = GUILayoutUtility.GetAspectRect(1f, GUILayout.ExpandWidth(true));
        EditorGUI.DrawRect(previewArea, new Color(0.055f, 0.045f, 0.05f, 1f));

        if (spriteSheet != null)
        {
            var inset = Mathf.Max(8f, previewArea.width * 0.04f);
            var drawArea = new Rect(
                previewArea.x + inset,
                previewArea.y + inset,
                previewArea.width - inset * 2f,
                previewArea.height - inset * 2f);
            var uv = new Rect(frameIndex / (float)FrameCount, 0f, 1f / FrameCount, 1f);
            GUI.DrawTextureWithTexCoords(drawArea, spriteSheet, uv, true);
        }

        EditorGUILayout.Space(8f);
        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button(isPlaying ? "Pause" : "Play", GUILayout.Height(28f)))
            {
                isPlaying = !isPlaying;
                nextFrameTime = EditorApplication.timeSinceStartup + 1d / FramesPerSecond;
            }

            if (GUILayout.Button("Restart", GUILayout.Height(28f)))
            {
                frameIndex = 0;
                isPlaying = true;
                nextFrameTime = EditorApplication.timeSinceStartup + 1d / FramesPerSecond;
                Repaint();
            }
        }

        frameIndex = EditorGUILayout.IntSlider("Frame", frameIndex + 1, 1, FrameCount) - 1;
        EditorGUILayout.HelpBox(
            "The preview loops for inspection. The generated Animation Clip itself is non-looping for gameplay attacks.",
            MessageType.Info);
    }
}
