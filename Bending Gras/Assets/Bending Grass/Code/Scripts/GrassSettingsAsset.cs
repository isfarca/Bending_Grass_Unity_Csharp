using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using System;

[CreateAssetMenu(fileName = "GrassSettings", menuName = "BendinGrass/GrassSettings")]
public class GrassSettingsAsset : ScriptableObject
{
	public static string SETTINGS_FILE = "BendinGrass/Settings/GrassSettings";

	public static GrassSettingsAsset GetAsset()
	{	
		GrassSettingsAsset pre = null;

		pre = Resources.Load<GrassSettingsAsset>(SETTINGS_FILE);

		return pre;
	}


	public int MaxActiveBenders = 64;
	public float MaxBenderDistance = 200.0f;
	public bool VertexEnabled = true;
	public bool PermanentEnabled;
	
}
