using UnityEngine;
using UnityEditor;
using BendinGrass;

public class GrassSettingsWindow : EditorWindow
{
	
	[MenuItem("Window/BendinGrass/Grass Settings")]
	static void Init()
	{
		GrassSettingsWindow window = (GrassSettingsWindow)EditorWindow.GetWindow(typeof(GrassSettingsWindow));
		window.ShowUtility();
		window.titleContent = new GUIContent("Grass Settings");
	}

	
	void OnGUI()
	{
		GUILayout.Label("General Settings", EditorStyles.boldLabel);
		//GrassSettings.SettingsPreset = (GrassSettingsAsset)EditorGUILayout.ObjectField(GrassSettings.SettingsPreset, typeof(GrassSettingsAsset));
		EditorGUI.indentLevel++;
		GrassSettings.VertexEnabled = EditorGUILayout.Toggle("Enable vertex bending", GrassSettings.VertexEnabled);
		GrassSettings.PermanentEnabled = EditorGUILayout.Toggle("Enable permanent bending", GrassSettings.PermanentEnabled);

		EditorGUILayout.Space();

		GrassSettings.SettingsPreset.MaxActiveBenders = EditorGUILayout.IntSlider("Max active benders", GrassSettings.SettingsPreset.MaxActiveBenders, 8, 64);

		//GrassSettings.EffectDistance = EditorGUILayout.Slider("Max bender distance", GrassSettings.EffectDistance, 10f, 400f);

		int l = GameObject.FindObjectsOfType<PermanentBendingVolume>().Length;
		if (l==0)
		{
			
			EditorGUILayout.HelpBox("There are no permanent bending volumes in the scene, please create one if you want to use Permanent bending.", MessageType.Warning);

			Rect buttonRect = EditorGUILayout.GetControlRect(true, 20f);

			buttonRect.xMin = buttonRect.xMax - 100f;

			if (GUI.Button(buttonRect, "Create Volume", EditorStyles.miniButton))
			{
				PermanentBendingVolume.CreateVolume(new MenuCommand(null));
			}
		}

		EditorGUI.indentLevel--;
		

		EditorGUILayout.LabelField("Stats", EditorStyles.boldLabel);
		EditorGUI.indentLevel++;
		EditorGUILayout.LabelField("Total benders  : " + GrassBendersManager.benders.Count);

		EditorGUILayout.LabelField("Shader keywords", EditorStyles.boldLabel);
		CheckShaderKeyword(GrassSettings.DISABLE_VERTEX_KEYWORD);
		CheckShaderKeyword(GrassSettings.PERMANENT_KEYWORD);
		EditorGUI.indentLevel--;

	}

	void CheckShaderKeyword(string keyword)
	{
		EditorGUILayout.LabelField(keyword + ": " + (Shader.IsKeywordEnabled(keyword)?"ON":"OFF"));
	}
}