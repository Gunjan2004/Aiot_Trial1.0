#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

public class CreateShaderVariantCollection
{
    [MenuItem("Tools/URP/Create Shader Variant Collection")]
    static void CreateURPShaderVariantCollection()
    {
        // Create or get existing collection
        ShaderVariantCollection collection = AssetDatabase.LoadAssetAtPath<ShaderVariantCollection>("Assets/URP_Shaders.shadervariants");
        if (collection == null)
        {
            collection = new ShaderVariantCollection();
            AssetDatabase.CreateAsset(collection, "Assets/URP_Shaders.shadervariants");
            Debug.Log("Created new Shader Variant Collection at Assets/URP_Shaders.shadervariants");
        }

        // Add all URP shaders
        string[] urpShaderNames = new string[]
        {
            "Universal Render Pipeline/Lit",
            "Universal Render Pipeline/Simple Lit",
            "Universal Render Pipeline/Unlit"
        };

        int addedCount = 0;
        foreach (string shaderName in urpShaderNames)
        {
            Shader shader = Shader.Find(shaderName);
            if (shader != null)
            {
                // Add variants for this shader
                collection.Add(new ShaderVariantCollection.ShaderVariant(shader, PassType.Normal));
                collection.Add(new ShaderVariantCollection.ShaderVariant(shader, PassType.ShadowCaster));
                addedCount++;
                Debug.Log($"Added {shaderName} to variant collection");
            }
            else
            {
                Debug.LogWarning($"Could not find shader: {shaderName}");
            }
        }

        EditorUtility.SetDirty(collection);
        AssetDatabase.SaveAssets();
        Debug.Log($"Shader variant collection created with {addedCount} shaders!");

        // Show in project window
        EditorUtility.FocusProjectWindow();
        Selection.activeObject = collection;
    }
}
#endif