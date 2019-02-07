using UnityEngine;
using UnityEditor;
using BendinGrass;

[CustomEditor(typeof(PermanentBendingVolume))]
public class PermanentBendingVolumeEditor : Editor{

	PermanentBendingVolume targ;
	private void OnEnable()
	{
		targ = (PermanentBendingVolume)target;
		forcedCellSize = (targ.CellSize.x + targ.CellSize.y + targ.CellSize.z) / 3f;
	}

	static bool _showTexture;

	static float forcedCellSize;
	public override void OnInspectorGUI()
	{
		EditorGUILayout.Space();

		targ.volumeMap = EditorGUILayout.ObjectField("Volume Map", targ.volumeMap, typeof(CustomRenderTexture)) as CustomRenderTexture;

		forcedCellSize = EditorGUILayout.Slider("Desired cell size", forcedCellSize, .3f, 5f);

		if (GUILayout.Button("Force cell size"))
		{
			targ.ForceCellSize(forcedCellSize);
		}

		if (!targ.volumeMap)
		{
			EditorGUILayout.HelpBox("This script needs a CustomRenderTexture (3D) to work properly, assign it!", MessageType.Error);
			return;
		}
		else
		{
			if (targ.volumeMap.dimension != UnityEngine.Rendering.TextureDimension.Tex3D)
			{
				EditorGUILayout.HelpBox("The CustomRenderTexture assigned is not in 3D mode, please fix it!", MessageType.Error);
				return;
			}
		}
		targ.DisablePermanentBend = EditorGUILayout.Toggle("Disable Permanent Bending", targ.DisablePermanentBend);
		if (!targ.DisablePermanentBend)
		{
			DrawPermanentOptions();
		}

		EditorGUILayout.Space();

		targ.DisableVertexBend = EditorGUILayout.Toggle("Disable Vertex Bending", targ.DisableVertexBend);

		if (targ.DisableVertexBend && targ.DisablePermanentBend)
			EditorGUILayout.HelpBox("Both vertex and permanent mode are disabled, the grass won't interact with any of the grass benders.", MessageType.Info);


		DrawInfo();
		serializedObject.UpdateIfRequiredOrScript();
		targ.UpdateKeywords();
		
	}

	void DrawPermanentOptions()
	{
		EditorGUI.indentLevel++;
		targ.updateFrequency = EditorGUILayout.Slider(new GUIContent("Update Frequency", "Frequency of the grid update.\n100% is every frame\n50% every 2 frames\n33% every 3 frames\netc."), targ.updateFrequency, 0f, 1f);
		targ.VolumeBendMult = EditorGUILayout.Slider("Bending distance", targ.VolumeBendMult, 0f, 5f);
		EditorGUI.indentLevel--;
	}

	bool _showInfo;
	 
	void DrawInfo()
	{
		
		_showInfo = EditorGUILayout.Foldout(_showInfo, "Volume info");
		if (_showInfo)
		{
			Vector3 texSize = new Vector3(targ.volumeMap.width, targ.volumeMap.height, targ.volumeMap.volumeDepth);
			Vector3 cellSize = targ.CellSize;
			string content = string.Format("Volume Texture Size : {0} (pixels)\nVolume Cell Size : {1} (world units)", texSize, cellSize);
			EditorGUI.indentLevel++;
			
			EditorGUILayout.HelpBox(content, MessageType.Info, true);
			EditorGUI.indentLevel--;
		}
	}

	
}
