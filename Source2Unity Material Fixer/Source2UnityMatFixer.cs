using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

public class Source2UnityMatFixer : EditorWindow
{
	public Material[] materials;
	public Texture2D[] textures;
	public bool assignNormalMaps;
	public bool useStandardSpecular;
	public bool zeroSpecularValues;
	public bool trySetTransparency;
	private Shader shaderStandard;
	private Shader shaderStandardSpecular;
	private List<Material> failedMaterials = new List<Material>();

	[MenuItem("RadioArtz/Source2Unity Utils/Material Fixer")]
	public static void ShowWindow()
	{
		GetWindow<Source2UnityMatFixer>("Source2Unity Material Fixer");
	}

	private void OnEnable()
	{
		shaderStandard = Shader.Find("Standard");
		shaderStandardSpecular = Shader.Find("Standard (Specular setup)");
	}

	private void OnGUI()
	{
		GUILayout.Label("Assign Textures to Materials", EditorStyles.boldLabel);

		SerializedObject serializedObject = new SerializedObject(this);
		SerializedProperty materialsProperty = serializedObject.FindProperty("materials");
		SerializedProperty texturesProperty = serializedObject.FindProperty("textures");
		SerializedProperty useStandardSpecularProperty = serializedObject.FindProperty("useStandardSpecular");
		SerializedProperty zeroSpecularValuesProperty = serializedObject.FindProperty("zeroSpecularValues");
		SerializedProperty assignNormalMapsProperty = serializedObject.FindProperty("assignNormalMaps");
		SerializedProperty trySetTransparencyProperty = serializedObject.FindProperty("trySetTransparency");

		EditorGUILayout.PropertyField(materialsProperty, true);
		EditorGUILayout.PropertyField(texturesProperty, true);
		EditorGUILayout.PropertyField(useStandardSpecularProperty);

		if (useStandardSpecular)
		{
			EditorGUILayout.PropertyField(zeroSpecularValuesProperty);
		}

		EditorGUILayout.PropertyField(assignNormalMapsProperty);
		EditorGUILayout.PropertyField(trySetTransparencyProperty);

		serializedObject.ApplyModifiedProperties();

		DrawButtons();
		DisplayFailedMaterials();
	}

	private void DrawButtons()
	{
		if (GUILayout.Button("Assign Textures"))
		{
			if (EditorUtility.DisplayDialog("Confirm Action", "Do you really want to assign textures?", "Yes", "No"))
				AssignTexturesToMaterials();
		}
		if (GUILayout.Button("Fix All Normal Maps"))
		{
			if (EditorUtility.DisplayDialog("Confirm Action", "Do you really want to fix all Normalmaps? This will cause a reimport and unity will be unresponsive for a while.", "Yes", "No"))
				FixAllNormalMaps();
		}
		if (GUILayout.Button("Unassign All Textures"))
		{
			if (EditorUtility.DisplayDialog("Confirm Action", "Do you really want to unassign all textures?", "Yes", "No"))
				RemoveAllTexturesFromMaterials();
		}
		if (failedMaterials.Count > 0 && GUILayout.Button("Clear Failed Materials List"))
		{
			if (EditorUtility.DisplayDialog("Confirm Action", "Really Clear the Failed Materials List?", "Yes", "No"))
				failedMaterials.Clear();
		}
	}

	private void DisplayFailedMaterials()
	{
		if (failedMaterials.Count > 0)
		{
			GUILayout.Label("Materials that failed texture assignment:", EditorStyles.boldLabel);
			foreach (var material in failedMaterials)
			{
				EditorGUILayout.ObjectField(material, typeof(Material), true);
			}
		}
	}

	private void AssignTexturesToMaterials()
	{
		failedMaterials.Clear();
		Shader selectedShader = useStandardSpecular ? shaderStandardSpecular : shaderStandard;

		foreach (var material in materials)
		{
			if (material == null) continue;

			material.shader = selectedShader;
			string materialName = material.name.ToLower();
			bool textureAssigned = TryAssignMainTexture(material, materialName);
			if (!textureAssigned)
			{
				Debug.LogWarning($"No suitable texture found for {material.name}. Check naming conventions or assign manually.");
				failedMaterials.Add(material);
			}

			AssignNormalMap(material, materialName);
			ApplySpecularSettings(material);
			ApplyTransparencySettings(material, materialName);
		}
		Debug.Log("Texture assignment complete.");
	}

	private bool TryAssignMainTexture(Material material, string materialName)
	{
		foreach (var texture in textures)
		{
			if (texture == null) continue;

			string textureName = texture.name.ToLower();
			if (IsMainTextureMatch(materialName, textureName))
			{
				material.mainTexture = texture;
				material.SetColor("_Color", Color.white);
				Debug.Log($"Assigned {texture.name} to {material.name}");
				return true;
			}
		}
		return false;
	}

	private bool IsMainTextureMatch(string materialName, string textureName)
	{
        //Try exact match first, then fallback to partial matching
        if(materialName.Equals(textureName) &&
			   !textureName.Contains("_normal") &&
			   !textureName.Contains("_spec") &&
			   !textureName.Contains("_specular"))
               return true;
        else if(materialName.Contains(textureName) &&
			   !textureName.Contains("_normal") &&
			   !textureName.Contains("_spec") &&
			   !textureName.Contains("_specular"))
               return true;
        else
            return false;
	}

	private void AssignNormalMap(Material material, string materialName)
	{
		if (!assignNormalMaps) return;

		foreach (var texture in textures)
		{
			if (texture == null) continue;

			string textureName = texture.name.ToLower();
			if (textureName.Contains("_normal") && materialName.Contains(textureName.Replace("_normal", "")))
			{
				material.SetTexture("_BumpMap", texture);
				material.EnableKeyword("_NORMALMAP");
				Debug.Log($"Assigned normal map {texture.name} to {material.name}");
				break;
			}
		}
	}

	private void ApplySpecularSettings(Material material)
	{
		if (useStandardSpecular && zeroSpecularValues)
		{
			material.SetColor("_SpecColor", Color.black);
			material.SetFloat("_Glossiness", 0f);
			Debug.Log($"Set specular color and glossiness to black and zero for {material.name}");
		}
		else if(useStandardSpecular)
		{
			material.SetColor("_SpecColor", Color.gray);
			material.SetFloat("_Glossiness", 0.5f);
			Debug.Log($"Set specular color and glossiness to gray and 0.5 for {material.name}");
		}
	}

	private void ApplyTransparencySettings(Material material, string materialName)
	{
		if (trySetTransparency)
		{
			if (materialName.Contains("stain") || materialName.Contains("glass"))
				SetMaterialToFadeTransparency(material);
			else if (materialName.Contains("ivy") || materialName.Contains("rail") || materialName.Contains("truss") || materialName.Contains("alpha"))
				SetMaterialToCutout(material);
			else 
				SetMaterialToOpaque(material);
		}
		else
			SetMaterialToOpaque(material);
	}

	private void SetMaterialToFadeTransparency(Material material)
	{
		material.SetFloat("_Mode", 2);
		material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
		material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
		material.SetInt("_ZWrite", 0);
		material.DisableKeyword("_ALPHATEST_ON");
		material.EnableKeyword("_ALPHABLEND_ON");
		material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
		material.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
	}

	private void SetMaterialToCutout(Material material)
	{
		material.SetFloat("_Mode", 1);
		material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.One);
		material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.Zero);
		material.SetInt("_ZWrite", 1);
		material.EnableKeyword("_ALPHATEST_ON");
		material.DisableKeyword("_ALPHABLEND_ON");
		material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
		material.renderQueue = (int)UnityEngine.Rendering.RenderQueue.AlphaTest;
	}

	private void SetMaterialToOpaque(Material material)
	{
		material.SetFloat("_Mode", 0);
		material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.One);
		material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.Zero);
		material.SetInt("_ZWrite", 1);
		material.DisableKeyword("_ALPHATEST_ON");
		material.DisableKeyword("_ALPHABLEND_ON");
		material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
		material.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Geometry;
	}

	private void FixAllNormalMaps()
	{
		foreach (var texture in textures)
		{
			if (texture == null || !texture.name.ToLower().Contains("_normal")) continue;

			string path = AssetDatabase.GetAssetPath(texture);
			TextureImporter textureImporter = AssetImporter.GetAtPath(path) as TextureImporter;
			if (textureImporter != null)
			{
				textureImporter.textureType = TextureImporterType.NormalMap;
				textureImporter.SaveAndReimport();
				Debug.Log($"Fixed normal map import settings for {texture.name}");
			}
		}
		Debug.Log("Normal map fixing complete.");
	}

	private void RemoveAllTexturesFromMaterials()
	{
		failedMaterials.Clear();
		foreach (var material in materials)
		{
			if (material == null) continue;

			material.mainTexture = null;
			if (material.HasProperty("_BumpMap"))
			{
				material.SetTexture("_BumpMap", null);
				material.DisableKeyword("_NORMALMAP");
			}
			Debug.Log($"Removed all textures from {material.name}");
		}
		Debug.Log("All textures removed from materials.");
	}
}