using UnityEngine;
using UnityEditor;
using BendinGrass;

[CanEditMultipleObjects]
[CustomEditor(typeof(GrassBender))]
public class GrassBenderEditor : Editor {

	public CustomRenderTexture csr;

	GrassBender targ;
	// Serialized properties of GrassBender class
	SerializedProperty hardness, power, noise, direction, uniformScale, permanentBending;

	void OnEnable()
	{
		hardness = serializedObject.FindProperty("hardness");
		power = serializedObject.FindProperty("power");
		noise = serializedObject.FindProperty("noise");
		direction = serializedObject.FindProperty("direction");
		uniformScale = serializedObject.FindProperty("uniformScale");
		permanentBending = serializedObject.FindProperty("permanentBending");

		if (targets.Length>1)
		{
			targ = null;
		}
		else
		{
			targ = (GrassBender)target;
		}
	}

	static bool _showVolumePar;
	

	public override void OnInspectorGUI()
	{
		serializedObject.Update();
		EditorGUI.BeginChangeCheck();
		EditorGUILayout.Space();
		EditorGUILayout.Slider(uniformScale, 0, 20f, "Uniform Scale");
		if (targ)
			EditorGUILayout.HelpBox(string.Format("World Space Scale : \n{0}", targ.worldSpaceSize), MessageType.Info, true);
		EditorGUILayout.Space();

		EditorGUILayout.PropertyField(permanentBending, new GUIContent("Enable permanent"));

		if (permanentBending.boolValue)
		{
			if (!GrassSettings.PermanentEnabled)
			{
				EditorGUILayout.HelpBox("Permanent grass bending is currently disabled, if you want to use it, enable it in the GrassSettings window.", MessageType.Warning);
				if (GUILayout.Button("Open settings"))
					EditorWindow.GetWindow<GrassSettingsWindow>().Show();
			}
		}
		DrawVolumeParameters();
		serializedObject.ApplyModifiedProperties();
	}


	void DrawVolumeParameters()
	{
		EditorGUILayout.Space();
		_showVolumePar = EditorGUILayout.Foldout(_showVolumePar, "Volume Parameters");
		if (_showVolumePar)
		{
			EditorGUI.indentLevel++;
			
			EditorGUILayout.Slider(hardness, .1f, 10f, "Hardness");
			EditorGUILayout.Slider(power, 0f, 10f, "Power");
			EditorGUILayout.Slider(noise, 0f, 4f, "Noise");
			EditorGUILayout.Slider(direction, 0f, 1f, "Directionality");

			if (targ)
			{
				if (targ.Power < .01f)
				{
					EditorGUILayout.HelpBox("This bender multiplier is too low, consider disabling it for better performances.", MessageType.Warning);
				}
				else if (targ.Power <= .95f)    // 0.95 is the fixed threshold alpha value used in the voxel shader
				{
					EditorGUILayout.HelpBox("This bender will not leave any permanent bending on the grass.", MessageType.Info);
				}
			}
			
			
			EditorGUI.indentLevel--;
		}
	}

	public override bool RequiresConstantRepaint()
	{
		return HasPreviewGUI() ;
	}

	public override bool HasPreviewGUI()
	{
		return (targ!=null);
	}

	public override void DrawPreview(Rect previewArea)
	{
		if (targ == null)
			return;
		targ.DrawPreview(csr);
		EditorGUI.DrawTextureTransparent(previewArea, csr, ScaleMode.ScaleToFit);

	}

	void CreateRenderTexture()
	{
		

	}

}
