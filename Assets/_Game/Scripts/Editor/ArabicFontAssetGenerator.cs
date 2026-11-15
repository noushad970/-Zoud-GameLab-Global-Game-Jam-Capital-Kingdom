#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEditor;
using UnityEngine;

namespace EmpireBuilder.Editor
{
    /// <summary>
    /// Editor utility that automatically creates and configures the Arabic TMP_FontAsset (Tahoma SDF),
    /// pre-bakes all Arabic characters, numbers, symbols, and connected presentation forms,
    /// and binds it as a fallback font across TMP_Settings and all project font assets.
    /// This permanently resolves missing glyph square/box (tofu) errors when displaying Arabic text.
    /// </summary>
    [InitializeOnLoad]
    public static class ArabicFontAssetGenerator
    {
        private const string SourceTtfPath = "Assets/_Game/Fonts/Tahoma.ttf";
        private const string TargetSdfPath = "Assets/Resources/Fonts/Tahoma SDF.asset";
        private const string BackupSdfPath = "Assets/_Game/Fonts/Tahoma SDF.asset";

        private const string LilitaFontPath = "Assets/Assets No/GUIPackCartoon/Demo/Fonts/LilitaOne - Regular SDF.asset";
        private const string BPreplayFontPath = "Assets/Assets No/GUIPackCartoon/Demo/Fonts/BPreplayExtendedBold SDF.asset";
        private const string LiberationFontPath = "Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF.asset";

        static ArabicFontAssetGenerator()
        {
            EditorApplication.delayCall += EnsureArabicFontSetup;
        }

        [MenuItem("Tools/Empire Builder/Setup Arabic TMP Font Asset (Fix Tofu Boxes)", false, 50)]
        public static void EnsureArabicFontSetup()
        {
            TMP_FontAsset arabicAsset = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(TargetSdfPath);
            if (arabicAsset == null)
            {
                arabicAsset = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(BackupSdfPath);
            }

            if (arabicAsset == null)
            {
                arabicAsset = GenerateDynamicSDF(SourceTtfPath, TargetSdfPath);
            }

            if (arabicAsset != null)
            {
                EnsureAllArabicGlyphs(arabicAsset);
                AssignFallbacksToProject(arabicAsset);
                Debug.Log("<color=#00FF88>[ArabicFontAssetGenerator] Arabic TMP_FontAsset successfully verified, glyphs baked, and assigned as fallback across all project fonts!</color>");
            }
        }

        public static void EnsureArabicFontSetupBatch()
        {
            Debug.Log("[ArabicFontAssetGenerator] Running batch Arabic font setup...");
            EnsureArabicFontSetup();
            Debug.Log("[ArabicFontAssetGenerator] Batch Arabic font setup complete!");
        }

        public static TMP_FontAsset GenerateDynamicSDF(string sourceTtfPath, string targetAssetPath)
        {
            Font font = AssetDatabase.LoadAssetAtPath<Font>(sourceTtfPath);
            if (font == null)
            {
                Debug.LogWarning($"[ArabicFontAssetGenerator] Source TTF not found at {sourceTtfPath}. Trying Resources/Fonts/Tahoma.ttf");
                font = AssetDatabase.LoadAssetAtPath<Font>("Assets/Resources/Fonts/Tahoma.ttf");
            }

            if (font == null)
            {
                Debug.LogError("[ArabicFontAssetGenerator] Tahoma.ttf could not be loaded!");
                return null;
            }

            string folder = Path.GetDirectoryName(targetAssetPath);
            if (!Directory.Exists(folder))
            {
                Directory.CreateDirectory(folder);
                AssetDatabase.Refresh();
            }

            try
            {
                TMP_FontAsset fontAsset = TMP_FontAsset.CreateFontAsset(font);
                if (fontAsset == null)
                {
                    Debug.LogError("[ArabicFontAssetGenerator] TMP_FontAsset.CreateFontAsset returned null.");
                    return null;
                }

                fontAsset.name = "Tahoma SDF";
                AssetDatabase.CreateAsset(fontAsset, targetAssetPath);

                if (fontAsset.material != null)
                {
                    fontAsset.material.name = "Tahoma SDF Material";
                    AssetDatabase.AddObjectToAsset(fontAsset.material, fontAsset);
                }

                if (fontAsset.atlasTextures != null)
                {
                    for (int i = 0; i < fontAsset.atlasTextures.Length; i++)
                    {
                        var tex = fontAsset.atlasTextures[i];
                        if (tex != null)
                        {
                            tex.name = $"Tahoma SDF Atlas {i}";
                            AssetDatabase.AddObjectToAsset(tex, fontAsset);
                        }
                    }
                }

                EditorUtility.SetDirty(fontAsset);
                AssetDatabase.SaveAssets();

                // Also copy to backup path if different
                if (targetAssetPath != BackupSdfPath && !File.Exists(BackupSdfPath))
                {
                    AssetDatabase.CopyAsset(targetAssetPath, BackupSdfPath);
                    AssetDatabase.SaveAssets();
                }

                return fontAsset;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[ArabicFontAssetGenerator] Exception creating TMP_FontAsset: {ex.Message}");
                return null;
            }
        }

        public static void EnsureAllArabicGlyphs(TMP_FontAsset arabicAsset)
        {
            if (arabicAsset == null) return;

            var unicodeList = new HashSet<uint>();

            // Basic ASCII: 32 - 126
            for (uint u = 32; u <= 126; u++) unicodeList.Add(u);

            // Arabic Unicode Block: 0x0600 - 0x06FF (all letters, harakat, digits ٠-٩, punctuation)
            for (uint u = 0x0600; u <= 0x06FF; u++) unicodeList.Add(u);

            // Arabic Presentation Forms-A: 0xFB50 - 0xFDFF (common ligatures and contextual forms)
            for (uint u = 0xFB50; u <= 0xFDFF; u++) unicodeList.Add(u);

            // Arabic Presentation Forms-B: 0xFE70 - 0xFEFC (isolated, initial, medial, final connected forms & Lam-Alef)
            for (uint u = 0xFE70; u <= 0xFEFC; u++) unicodeList.Add(u);

            // Latin-1 Supplement: 0x00A0 - 0x00FF (symbols, accents)
            for (uint u = 0x00A0; u <= 0x00FF; u++) unicodeList.Add(u);

            uint[] unicodes = new uint[unicodeList.Count];
            unicodeList.CopyTo(unicodes);

            uint[] missing;
            arabicAsset.TryAddCharacters(unicodes, out missing, true);

            EditorUtility.SetDirty(arabicAsset);
            AssetDatabase.SaveAssets();

            // Sync to backup file
            if (File.Exists(BackupSdfPath) && TargetSdfPath != BackupSdfPath)
            {
                TMP_FontAsset backupAsset = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(BackupSdfPath);
                if (backupAsset != null)
                {
                    backupAsset.TryAddCharacters(unicodes, out missing, true);
                    EditorUtility.SetDirty(backupAsset);
                    AssetDatabase.SaveAssets();
                }
            }
        }

        public static void AssignFallbacksToProject(TMP_FontAsset arabicAsset)
        {
            if (arabicAsset == null) return;

            // 1. Assign to global TMP_Settings fallback list
            if (TMP_Settings.instance != null)
            {
                var fallbacks = TMP_Settings.fallbackFontAssets;
                if (fallbacks == null) fallbacks = new List<TMP_FontAsset>();
                if (!fallbacks.Contains(arabicAsset))
                {
                    fallbacks.Add(arabicAsset);
                    TMP_Settings.fallbackFontAssets = fallbacks;
                    EditorUtility.SetDirty(TMP_Settings.instance);
                }

                if (TMP_Settings.defaultFontAsset != null && TMP_Settings.defaultFontAsset != arabicAsset)
                {
                    AddFallbackToFont(TMP_Settings.defaultFontAsset, arabicAsset);
                }
            }

            // 2. Assign to LilitaOne font asset (used across game UI and Main Menu)
            TMP_FontAsset lilita = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(LilitaFontPath);
            if (lilita != null)
            {
                AddFallbackToFont(lilita, arabicAsset);
                AddFallbackToFont(arabicAsset, lilita);
            }

            // 3. Assign to BPreplay font asset
            TMP_FontAsset bpreplay = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(BPreplayFontPath);
            if (bpreplay != null)
            {
                AddFallbackToFont(bpreplay, arabicAsset);
                AddFallbackToFont(arabicAsset, bpreplay);
            }

            // 4. Assign to LiberationSans font asset
            TMP_FontAsset liberation = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(LiberationFontPath);
            if (liberation != null)
            {
                AddFallbackToFont(liberation, arabicAsset);
                AddFallbackToFont(arabicAsset, liberation);
            }

            AssetDatabase.SaveAssets();
        }

        private static void AddFallbackToFont(TMP_FontAsset targetFont, TMP_FontAsset fallback)
        {
            if (targetFont == null || fallback == null || targetFont == fallback) return;

            if (targetFont.fallbackFontAssetTable == null)
            {
                targetFont.fallbackFontAssetTable = new List<TMP_FontAsset>();
            }

            if (!targetFont.fallbackFontAssetTable.Contains(fallback))
            {
                targetFont.fallbackFontAssetTable.Add(fallback);
                EditorUtility.SetDirty(targetFont);
            }
        }
    }
}
#endif
