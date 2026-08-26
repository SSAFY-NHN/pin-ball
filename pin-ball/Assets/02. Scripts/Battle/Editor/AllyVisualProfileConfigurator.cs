#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

public static class AllyVisualProfileConfigurator
{
    private const string Root = "Assets/03. Images/Animals/";
    private const string PrefabPath = "Assets/04. Prefabs/AllyUnit.prefab";

    [InitializeOnLoadMethod]
    private static void ConfigureAfterReload()
    {
        EditorApplication.delayCall += () =>
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            var visual = prefab != null ? prefab.GetComponent<BattleUnitVisual>() : null;
            if (visual == null) return;
            var profiles = new SerializedObject(visual)
                .FindProperty("unitAnimations");
            bool hasNewProfiles = false;
            for (int i = 0; i < profiles.arraySize; i++)
            {
                if (profiles.GetArrayElementAtIndex(i)
                        .FindPropertyRelative("unitId").stringValue == "rabbit5")
                {
                    hasNewProfiles = true;
                    break;
                }
            }
            if (!hasNewProfiles) Configure();
        };
    }

    private readonly struct Profile
    {
        public readonly string Id;
        public readonly string Idle;
        public readonly string Walk;
        public readonly string Attack;
        public readonly string Skill;

        public Profile(string id, string folder, string idle, string walk, string attack, string skill = null)
        {
            Id = id;
            Idle = Root + folder + "/" + idle;
            Walk = Root + folder + "/" + walk;
            Attack = Root + folder + "/" + attack;
            Skill = string.IsNullOrEmpty(skill) ? Attack : Root + folder + "/" + skill;
        }
    }

    [MenuItem("Tools/Pin-Ball/Configure Ally Visual Profiles")]
    public static void Configure()
    {
        var profiles = new[]
        {
            new Profile("bear1","Bear","Bear1_Fighter_Tiny32.png","Bear1_Fighter_Walk.png","Bear1_Fighter_Attack.png"),
            new Profile("bear2","Bear","Bear1_Fighter_Evolved_Tiny32.png","Bear2_Fighter_Walk.png","Bear2_Fighter_Attack.png"),
            new Profile("bear3","Bear","Bear3_Ninja_Assassin_Tiny32.png","Bear3_Ninja_Assassin_Tiny32_Walk.png","Bear3_Ninja_Assassin_Tiny32_Attack.png"),
            new Profile("bear4","Bear","Bear4_EvoFighter_Tiny32.png","Bear4_EvoFighter_Walk.png","Bear4_EvoFighter_Attack.png","Bear4_EvoFighter_skill.png"),
            new Profile("bear5","Bear","Bear5_EvoNinja_Assassin_Tiny32.png","Bear5_EvoNinja_Assassin_Walk.png","Bear5_EvoNinja_Assassin_Attack.png","Bear5_EvoNinja_Assassin_Skill.png"),
            new Profile("dog1","Dog","Dog1_Farmer_Sword_Tiny32.png","Dog1_Farmer_Sword_Tiny32_Walk.png","Dog1_Farmer_Sword_Tiny32_Attack.png"),
            new Profile("dog2","Dog","dog2_Warrior.png","Dog2_Warrior_walk.png","dog2_Warrior_Attack.png"),
            new Profile("dog3","Dog","Dog3_Lancer.png","Dog3_Lancer_Walk.png","Dog3_Lancer_Attack.png"),
            new Profile("dog4","Dog","Dog4_EvoWarrior.png","Dog4_EvoWarrior_Walk.png","Dog4_EvoWarrior_Attack.png","Dog4_EvoWarrior_skill.png"),
            new Profile("dog5","Dog","Dog5_EvoLancer.png","Dog5_EvoLancer_Walk.png","Dog5_Lancer.Attack.png","Dog5_EvoLancer_skill.png"),
            new Profile("cat1","Cat","Cat1_archer_Tiny32.png","Cat1_archer_Tiny32_Walk.png","Cat1_archer_Tiny32_Attack.png"),
            new Profile("cat2","Cat","Cat2_gunslinger_Tiny32.png","Cat2_gunslinger_Tiny32_Walk.png","Cat2_gunslinger_Tiny32_Attack.png"),
            new Profile("cat3","Cat","Cat3_CrossBow_Tiny32.png","Cat3_CrossBow_Tiny32_Walk.png","Cat3_CrossBow_Tiny32_Attack.png"),
            new Profile("cat4","Cat","Cat4_Evogunslinger_Tiny32.png","Cat4_Evogunslinger_walk.png","Cat4_Evogunslinger_Attack.png","Cat4_Evogunslinger_skill.png"),
            new Profile("cat5","Cat","Cat5_EvoBow_Tiny32.png","Cat5_EvoBow_Walk.png","Cat5_EvoBow_Attack.png","Cat5_EvoBow_skill.png"),
            new Profile("rabbit1","Rabbit","Rabbit1_Mage_Tiny32.png","Rabbit1_Mage_Walk.png","Rabbit_mage_Attack.png"),
            new Profile("rabbit2","Rabbit","Rabbit2_Healer_Tiny32.png","Rabbit2_Healer_Walk.png","Rabbit2_Healer_Attack.png"),
            new Profile("rabbit3","Rabbit","Rabbit3_EvoMage_Tiny32.png","Rabbit3_EvoMage_Walk_Tiny32.png","Rabbit3_EvoMage_Attack_Tiny32.png"),
            new Profile("rabbit4","Rabbit","Rabbit4_EvoHealer_Tiny32.png","Rabbit4_EvoHealer_Walk.png","Rabbit4_EvoHealer_Attack.png","Rabbit4_EvoHealer_skill.png"),
            new Profile("rabbit5","Rabbit","Rabbit5_EvoMage2_Tiny32.png","Rabbit5_EvoMage2_Walk.png","Rabbit5_EvoMage2_Attack.png","Rabbit5_EvoMage2_skill.png")
        };

        GameObject root = PrefabUtility.LoadPrefabContents(PrefabPath);
        try
        {
            BattleUnitVisual visual = root.GetComponent<BattleUnitVisual>();
            var serializedVisual = new SerializedObject(visual);
            serializedVisual.FindProperty("unitAnimations").ClearArray();
            serializedVisual.ApplyModifiedPropertiesWithoutUndo();
            foreach (var profile in profiles)
            {
                Sprite[] idle = Load(profile.Idle);
                Sprite[] walk = Load(profile.Walk);
                Sprite[] attack = Load(profile.Attack);
                Sprite[] skill = Load(profile.Skill);
                if (idle.Length == 0 || walk.Length == 0 || attack.Length == 0 || skill.Length == 0)
                    throw new InvalidOperationException($"Missing ally sprites: {profile.Id}");
                visual.ConfigureUnitAnimation(profile.Id, new[] { idle[0] }, walk, attack, skill, 10f, 10f, false);
            }
            PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }

        AssetDatabase.SaveAssets();
        Debug.Log("[AllyVisualProfileConfigurator] Configured 20 ally profiles.");
    }

    private static Sprite[] Load(string path)
    {
        return AssetDatabase.LoadAllAssetsAtPath(path)
            .OfType<Sprite>()
            .OrderBy(sprite => FrameIndex(sprite.name))
            .ToArray();
    }

    private static int FrameIndex(string name)
    {
        int separator = name.LastIndexOf('_');
        return separator >= 0 && int.TryParse(name.Substring(separator + 1), out int index)
            ? index
            : 0;
    }
}
#endif
