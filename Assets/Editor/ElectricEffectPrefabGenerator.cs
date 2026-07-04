using System.IO;
using Gamejam2026.Presentation;
using UnityEditor;
using UnityEngine;

namespace gamelan2026.Editor
{
    public static class ElectricEffectPrefabGenerator
    {
        private const string PrefabDirectory = "Assets/Prefabs/Effects";
        private const string SparkPrefabPath = PrefabDirectory + "/ElectricSpark.prefab";
        private const string BoltPrefabPath = PrefabDirectory + "/ElectricBolt.prefab";
        private const string ElectricMaterialPath = PrefabDirectory + "/ElectricEffect.mat";

        [MenuItem("Tools/GameJam/Create Electric Effect Prefabs")]
        public static void CreateElectricEffectPrefabs()
        {
            Directory.CreateDirectory(PrefabDirectory);

            Material electricMaterial = CreateElectricMaterial();
            CreateSparkPrefab();
            CreateBoltPrefab(electricMaterial);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[GameJam] Electric effect prefabs generated.");
        }

        private static Material CreateElectricMaterial()
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Particles/Unlit");

            if (shader == null)
            {
                shader = Shader.Find("Universal Render Pipeline/Unlit");
            }

            if (shader == null)
            {
                shader = Shader.Find("Sprites/Default");
            }

            if (shader == null)
            {
                shader = Shader.Find("Legacy Shaders/Particles/Alpha Blended");
            }

            Material material = new Material(shader);
            material.name = "ElectricEffect";
            material.color = Color.white;

            if (File.Exists(ElectricMaterialPath))
            {
                AssetDatabase.DeleteAsset(ElectricMaterialPath);
            }

            AssetDatabase.CreateAsset(material, ElectricMaterialPath);
            return material;
        }

        private static void CreateSparkPrefab()
        {
            GameObject sparkObject = new GameObject("ElectricSpark");
            ParticleSystem particleSystem = sparkObject.AddComponent<ParticleSystem>();
            ParticleSystemRenderer renderer = sparkObject.GetComponent<ParticleSystemRenderer>();
            Material electricMaterial = AssetDatabase.LoadAssetAtPath<Material>(ElectricMaterialPath);

            ParticleSystem.MainModule main = particleSystem.main;
            main.duration = 0.22f;
            main.loop = false;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.08f, 0.18f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(2.2f, 5.2f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.035f, 0.085f);
            main.startColor = new ParticleSystem.MinMaxGradient(
                new Color(0.25f, 0.95f, 1f, 1f),
                new Color(1f, 0.15f, 0.2f, 1f));
            main.gravityModifier = 0f;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.playOnAwake = false;

            ParticleSystem.EmissionModule emission = particleSystem.emission;
            emission.rateOverTime = 0f;
            emission.SetBursts(new[]
            {
                new ParticleSystem.Burst(0f, 24)
            });

            ParticleSystem.ShapeModule shape = particleSystem.shape;
            shape.shapeType = ParticleSystemShapeType.Circle;
            shape.radius = 0.18f;

            ParticleSystem.ColorOverLifetimeModule colorOverLifetime = particleSystem.colorOverLifetime;
            colorOverLifetime.enabled = true;
            Gradient gradient = new Gradient();
            gradient.SetKeys(
                new[]
                {
                    new GradientColorKey(Color.white, 0f),
                    new GradientColorKey(new Color(0.15f, 0.95f, 1f), 0.45f),
                    new GradientColorKey(new Color(1f, 0.1f, 0.18f), 1f)
                },
                new[]
                {
                    new GradientAlphaKey(1f, 0f),
                    new GradientAlphaKey(0.85f, 0.5f),
                    new GradientAlphaKey(0f, 1f)
                });
            colorOverLifetime.color = gradient;

            ParticleSystem.SizeOverLifetimeModule sizeOverLifetime = particleSystem.sizeOverLifetime;
            sizeOverLifetime.enabled = true;
            AnimationCurve sizeCurve = new AnimationCurve(
                new Keyframe(0f, 1f),
                new Keyframe(1f, 0f));
            sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f, sizeCurve);

            renderer.renderMode = ParticleSystemRenderMode.Billboard;
            renderer.sortingOrder = 80;
            renderer.sharedMaterial = electricMaterial;

            SavePrefab(sparkObject, SparkPrefabPath);
        }

        private static void CreateBoltPrefab(Material electricMaterial)
        {
            GameObject boltObject = new GameObject("ElectricBolt");
            LineRenderer line = boltObject.AddComponent<LineRenderer>();
            boltObject.AddComponent<ElectricBolt>();

            line.useWorldSpace = true;
            line.positionCount = 3;
            line.startWidth = 0.045f;
            line.endWidth = 0.015f;
            line.numCapVertices = 2;
            line.numCornerVertices = 2;
            line.sharedMaterial = electricMaterial;
            line.startColor = new Color(0.8f, 1f, 1f, 1f);
            line.endColor = new Color(1f, 0.15f, 0.2f, 0.85f);
            line.sortingOrder = 90;

            SavePrefab(boltObject, BoltPrefabPath);
        }

        private static void SavePrefab(GameObject source, string path)
        {
            if (File.Exists(path))
            {
                AssetDatabase.DeleteAsset(path);
            }

            PrefabUtility.SaveAsPrefabAsset(source, path);
            Object.DestroyImmediate(source);
        }
    }
}
