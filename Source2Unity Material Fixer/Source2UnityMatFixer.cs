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
	public Shader shaderStandard;
	public Shader shaderStandardSpecular;
	public bool trySetTransparency;
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

		if (GUILayout.Button("Assign Textures"))
		{
			if (EditorUtility.DisplayDialog("Confirm Action", "Do you really want to assign textures?", "Yes", "No"))
			{
				AssignTexturesToMaterials();
			}
		}
		if (GUILayout.Button("Fix All Normal Maps"))
		{
			if (EditorUtility.DisplayDialog("Confirm Action", "Do you really want to fix all Normalmaps? This will cause a reimport and unity will be unresponsive for a while.", "Yes", "No"))
			{
				FixAllNormalMaps();
			}
		}
		if (GUILayout.Button("Unassign All Textures"))
		{
			if (EditorUtility.DisplayDialog("Confirm Action", "Do you really want to unassign all textures?", "Yes", "No"))
			{
				RemoveAllTexturesFromMaterials();
			}
		}
		if (failedMaterials.Count > 0)
		{
			GUILayout.Label("Materials that failed texture assignment:", EditorStyles.boldLabel);
			foreach (var material in failedMaterials)
			{
				EditorGUILayout.ObjectField(material, typeof(Material), true);
			}
		}
		if(failedMaterials.Count > 0)
		{
			if (GUILayout.Button("Clear Failed Materials List"))
			{
				if (EditorUtility.DisplayDialog("Confirm Action", "Really Clear the Failed Materials List?", "Yes", "No"))
				{
					failedMaterials.Clear();
				}
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
			bool textureAssigned = false;
			foreach (var texture in textures)
			{
				if (texture == null) continue;
				string textureName = texture.name.ToLower();
				if (materialName.Equals(textureName) &&
					!textureName.Contains("_normal") &&
					!textureName.Contains("_spec") &&
					!textureName.Contains("_specular"))
				{
					material.mainTexture = texture;
					material.SetColor("_Color", Color.white);
					Debug.Log($"Assigned {texture.name} to {material.name}");
					textureAssigned = true;
					break;
				}
			}
			if (!textureAssigned)
			{
				foreach (var texture in textures)
				{
					if (texture == null) continue;
					string textureName = texture.name.ToLower();
					if (materialName.Contains(textureName) &&
						!textureName.Contains("_normal") &&
						!textureName.Contains("_spec") &&
						!textureName.Contains("_specular"))
					{
						material.mainTexture = texture;
						material.SetColor("_Color", Color.white);
						Debug.Log($"Assigned {texture.name} to {material.name} (partial match)");
						textureAssigned = true;
						break;
					}
				}
			}
			if (!textureAssigned)
			{
				Debug.LogWarning($"No suitable texture found for {material.name}. Check naming conventions or assign manually.");
				failedMaterials.Add(material);
			}
			if (assignNormalMaps)
			{
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
			if (useStandardSpecular && zeroSpecularValues)
			{
				material.SetColor("_SpecColor", Color.black);
				material.SetFloat("_Glossiness", 0f);
				Debug.Log($"Set specular color and glossiness to black and zero for {material.name}");
			}
			if (trySetTransparency)
			{
				if (materialName.Contains("stain") || materialName.Contains("glass"))
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
				else if (materialName.Contains("ivy") || materialName.Contains("rail") || materialName.Contains("truss") || materialName.Contains("alpha"))
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
				else
				{
					material.SetFloat("_Mode", 0);
					material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.One);
					material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.Zero);
					material.SetInt("_ZWrite", 1);
					material.DisableKeyword("_ALPHATEST_ON");
					material.DisableKeyword("_ALPHABLEND_ON");
					material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
					material.renderQueue = 2000;
				}
			}
			else
			{
				material.SetFloat("_Mode", 0);
				material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.One);
				material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.Zero);
				material.SetInt("_ZWrite", 1);
				material.DisableKeyword("_ALPHATEST_ON");
				material.DisableKeyword("_ALPHABLEND_ON");
				material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
				material.renderQueue = 2000;
			}
		}
		Debug.Log("Texture assignment complete.");
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