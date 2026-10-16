using UnityEditor;

using UnityEngine;
using UnityEngine.SceneManagement;
using Crossroads.Diner;

namespace Crossroads.Diner.Editor
{
    /// <summary>
    /// Assigns the project's imported 2D artwork to the Crossroads data assets,
    /// then rebuilds the generated presentation from DinerTheme.
    /// Put this file under Assets/Crossroads/Editor in the Unity project and run the menu item.
    /// </summary>
    public static class CrossroadsArtAutoSetup
    {
        private const string ScenePath = "Assets/Crossroads/Scenes/CrossroadsDiner.unity";
        private const string ThemePath = "Assets/Crossroads/Data/DinerTheme.asset";

        [MenuItem("Crossroads/Auto-assign imported art")]
        public static void AssignAndRebuild()
        {
            if (EditorApplication.isPlaying)
            {
                EditorUtility.DisplayDialog("Crossroads Art Setup", "Stop Play mode first, then run this command again.", "OK");
                return;
            }

            if (SceneManager.GetActiveScene().path != ScenePath)
            {
                EditorUtility.DisplayDialog("Crossroads Art Setup", "Please open Assets/Crossroads/Scenes/CrossroadsDiner.unity first, then run this command again.", "OK");
                return;
            }

            var theme = AssetDatabase.LoadAssetAtPath<DinerTheme>(ThemePath);
            if (theme == null)
            {
                Debug.LogError("Crossroads art setup: DinerTheme asset not found at " + ThemePath);
                return;
            }

            Undo.RecordObject(theme, "Assign Crossroads artwork");
            AssignThemeSprite(theme, "frontBackground", "Assets/background/FrontHouse Background.png");
            AssignThemeSprite(theme, "kitchenBackground", "Assets/background/Kitchenbackground.png");
            AssignThemeSprite(theme, "processorIcon", "Assets/Tools/desk.png");
            AssignThemeSprite(theme, "potIcon", "Assets/Tools/pot.png");
            AssignThemeSprite(theme, "trashIcon", "Assets/UI/Trash_can.png");

            AssignDefinitionIcon("Assets/Crossroads/Data/Monsters/Vampire.asset", "Assets/Character/vampire.png");
            AssignDefinitionIcon("Assets/Crossroads/Data/Monsters/Werewolf.asset", "Assets/Character/wolfman.png");
            AssignDefinitionIcon("Assets/Crossroads/Data/Monsters/Ghost.asset", "Assets/Character/ghost.png");

            AssignDefinitionIcon("Assets/Crossroads/Data/Tools/Garlic.asset", "Assets/Tools/Garlic.png");
            AssignDefinitionIcon("Assets/Crossroads/Data/Tools/SilverChains.asset", "Assets/Tools/Silver Chain.png");
            AssignDefinitionIcon("Assets/Crossroads/Data/Tools/SpiritSalt.asset", "Assets/Tools/Bottle of Spirit Salt.png");

            AssignDefinitionIcon("Assets/Crossroads/Data/Ingredients/Blood.asset", "Assets/Ingredients/blood.png");
            AssignDefinitionIcon("Assets/Crossroads/Data/Ingredients/Meat.asset", "Assets/Ingredients/Meat.png");
            AssignDefinitionIcon("Assets/Crossroads/Data/Ingredients/Ecto.asset", "Assets/Ingredients/Ectoplasm.png");

            AssignDefinitionIcon("Assets/Crossroads/Data/Recipes/BloodStew.asset", "Assets/Dishes/meat soup.png");
            AssignDefinitionIcon("Assets/Crossroads/Data/Recipes/SpectralStew.asset", "Assets/Dishes/spirt soup.png");
            AssignDefinitionIcon("Assets/Crossroads/Data/Recipes/HauntedSoup.asset", "Assets/Dishes/ghost soup.png");

            EditorUtility.SetDirty(theme);
            AssetDatabase.SaveAssets();

            if (!EditorApplication.ExecuteMenuItem("Crossroads/Rebuild presentation from theme"))
            {
                Debug.LogError("Artwork was assigned, but the rebuild command was unavailable. Check the Console for compile errors, then run Crossroads > Rebuild presentation from theme.");
                return;
            }

            Debug.Log("Crossroads artwork assigned. Open the Game view and press Play to review it.");
            EditorUtility.DisplayDialog("Crossroads Art Setup", "Artwork assigned and the presentation rebuilt. Press Play to review the game.", "OK");
        }

        private static void AssignThemeSprite(DinerTheme theme, string propertyName, string spritePath)
        {
            var sprite = LoadSprite(spritePath, propertyName == "trashIcon");
            if (sprite == null) return;

            var serialized = new SerializedObject(theme);
            var property = serialized.FindProperty(propertyName);
            if (property == null)
            {
                Debug.LogWarning("Crossroads art setup: theme field not found: " + propertyName);
                return;
            }
            property.objectReferenceValue = sprite;
            serialized.ApplyModifiedProperties();
            EditorUtility.SetDirty(theme);
        }

        private static void AssignDefinitionIcon(string assetPath, string spritePath)
        {
            var asset = AssetDatabase.LoadMainAssetAtPath(assetPath) as ScriptableObject;
            var sprite = LoadSprite(spritePath);
            if (asset == null || sprite == null)
            {
                Debug.LogWarning("Crossroads art setup: skipped missing asset or image: " + assetPath + " <- " + spritePath);
                return;
            }

            Undo.RecordObject(asset, "Assign Crossroads artwork");
            var serialized = new SerializedObject(asset);
            var icon = serialized.FindProperty("icon");
            if (icon == null)
            {
                Debug.LogWarning("Crossroads art setup: icon field not found on " + assetPath);
                return;
            }

            icon.objectReferenceValue = sprite;
            var visualPrefab = serialized.FindProperty("visualPrefab");
            if (visualPrefab != null) visualPrefab.objectReferenceValue = null;
            serialized.ApplyModifiedProperties();
            EditorUtility.SetDirty(asset);
        }

        private static Sprite LoadSprite(string path, bool preserveSlices = false)
        {
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer != null && (importer.textureType != TextureImporterType.Sprite || (!preserveSlices && importer.spriteImportMode != SpriteImportMode.Single) || importer.maxTextureSize < 4096 || importer.textureCompression != TextureImporterCompression.Uncompressed || importer.mipmapEnabled))
            {
                importer.textureType = TextureImporterType.Sprite;
                if (!preserveSlices) importer.spriteImportMode = SpriteImportMode.Single;
                importer.maxTextureSize = Mathf.Max(importer.maxTextureSize, 4096);
                importer.textureCompression = TextureImporterCompression.Uncompressed;
                importer.mipmapEnabled = false;
                importer.SaveAndReimport();
            }

            // The trash artwork has a large transparent canvas and an existing tight slice.
            if (preserveSlices)
            {
                Sprite largest = null;
                foreach (var asset in AssetDatabase.LoadAllAssetsAtPath(path))
                {
                    var candidate = asset as Sprite;
                    if (candidate != null && (largest == null || candidate.rect.width * candidate.rect.height > largest.rect.width * largest.rect.height))
                        largest = candidate;
                }
                if (largest != null) return largest;
            }

            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if (sprite == null) Debug.LogWarning("Crossroads art setup: sprite not found or not imported as a Sprite: " + path);
            return sprite;
        }
    }
}
