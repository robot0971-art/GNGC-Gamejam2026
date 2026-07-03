using System.IO;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.TextCore.LowLevel;

namespace Gamejam2026.Editor
{
    public static class KoreanTmpFontAssetGenerator
    {
        private const string FontDirectory = "Assets/Fonts";
        private const int SamplingPointSize = 90;
        private const int AtlasPadding = 9;
        private const int AtlasSize = 2048;

        [MenuItem("Tools/Gamejam/Create Korean TMP Font Assets")]
        public static void CreateKoreanTmpFontAssets()
        {
            CreateDynamicFontAsset("Paperlogy-7Bold");
            CreateDynamicFontAsset("Paperlogy-8ExtraBold");
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[Gamejam] Korean TMP font assets generated.");
        }

        private static void CreateDynamicFontAsset(string fontName)
        {
            string fontPath = $"{FontDirectory}/{fontName}.ttf";
            Font sourceFont = AssetDatabase.LoadAssetAtPath<Font>(fontPath);

            if (sourceFont == null)
            {
                Debug.LogWarning($"[Gamejam] Font not found: {fontPath}");
                return;
            }

            TMP_FontAsset fontAsset = TMP_FontAsset.CreateFontAsset(
                sourceFont,
                SamplingPointSize,
                AtlasPadding,
                GlyphRenderMode.SDFAA,
                AtlasSize,
                AtlasSize,
                AtlasPopulationMode.Dynamic,
                true);

            fontAsset.name = $"{fontName} TMP Dynamic";
            fontAsset.atlasPopulationMode = AtlasPopulationMode.Dynamic;

            string assetPath = $"{FontDirectory}/{fontAsset.name}.asset";
            DeleteExistingAsset(assetPath);
            AssetDatabase.CreateAsset(fontAsset, assetPath);
            EditorUtility.SetDirty(fontAsset);
        }

        private static void DeleteExistingAsset(string assetPath)
        {
            if (!File.Exists(assetPath))
            {
                return;
            }

            AssetDatabase.DeleteAsset(assetPath);
        }
    }
}
