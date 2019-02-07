using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace BendinGrass
{
	public class GrassSettings
	{
		public const int BENDERS_ARRAY_SIZE = 128;
		public static string DISABLE_VERTEX_KEYWORD = "DISABLE_GRASS_VERTEX_BEND";
		public static string PERMANENT_KEYWORD = "GRASS_PERMANENT_BEND";


		private static GrassSettingsAsset settingsPreset;

		public static GrassSettingsAsset SettingsPreset
		{
			get
			{
				if (settingsPreset == null)
					settingsPreset = GrassSettingsAsset.GetAsset();
				return settingsPreset;
			}

			set
			{
				bool changed = settingsPreset != value;
				settingsPreset = value;
				if (changed && settingsPreset != null)
					Refresh();
			}
		}

		public static bool VertexEnabled
		{
			get
			{
				return SettingsPreset.VertexEnabled;
			}
			set
			{
				bool changed = SettingsPreset.VertexEnabled != value;
				SettingsPreset.VertexEnabled = value;
				if (changed)
					SetVertexBending(SettingsPreset.VertexEnabled);
			}
		}

		public static bool PermanentEnabled
		{
			get
			{
				return SettingsPreset.PermanentEnabled;
			}
			set
			{
				bool changed = SettingsPreset.PermanentEnabled != value;
				SettingsPreset.PermanentEnabled = value;
				if (changed)
					SetPermanentBending(SettingsPreset.PermanentEnabled);
			}
		}

		public static void Refresh()
		{
			SetVertexBending(VertexEnabled);
			SetPermanentBending(PermanentEnabled);
		}

		public static float EffectDistance
		{
			get
			{
				return SettingsPreset.MaxBenderDistance;
			}

			set
			{
				bool changed = SettingsPreset.MaxBenderDistance != value;
				SettingsPreset.MaxBenderDistance = value;

				if (changed)
					RefreshCullingBands();
			}
		}

		internal static float[] cullingBands;
		public static void RefreshCullingBands()
		{
			if (cullingBands == null)
			{
				cullingBands = new float[] { SettingsPreset.MaxBenderDistance / 8f, SettingsPreset.MaxBenderDistance / 2f, SettingsPreset.MaxBenderDistance };
			}
			else
			{
				cullingBands[0] = SettingsPreset.MaxBenderDistance / 8f;
				cullingBands[1] = SettingsPreset.MaxBenderDistance / 2f;
				cullingBands[2] = SettingsPreset.MaxBenderDistance;
			}
		}

		public static bool SetPermanentBending(bool enabled)
		{
			enabled = enabled && PermanentEnabled;


			if (!SystemInfo.supports3DRenderTextures)
			{
				Debug.LogWarning("3D RenderTextures are not supported, you can't use Permanent Bending");
				enabled = false;
			}

			if (enabled)
			{
				Shader.EnableKeyword(PERMANENT_KEYWORD);
			}
			else
			{
				Shader.DisableKeyword(PERMANENT_KEYWORD);
			}
			return enabled;
		}

		public static bool SetVertexBending(bool enabled)
		{
			enabled = enabled && VertexEnabled;
			if (!enabled)
				Shader.EnableKeyword(DISABLE_VERTEX_KEYWORD);
			else
				Shader.DisableKeyword(DISABLE_VERTEX_KEYWORD);

			return enabled;
		}


		//It was used by the old culling system, not used anymore.

		static int Sign(int n)
		{
			if (n < 0)
				return -1;
			else if (n == 0)
				return 0;
			return 1;
		}

		static int Sign(float n)
		{
			if (n < 0)
				return -1;
			else if (n < 1)
				return 0;
			return 1;
		}

		static bool IsInRange(float val, float min, float max)
		{

			return (val > min) && (val < max);
		}

		static bool IsPointVisible(Camera cam, Vector3 point)
		{
			Vector3 cameraSpace = cam.worldToCameraMatrix * new Vector4(point.x, point.y, point.z, 1);
			if (cameraSpace.z < 0)
				return false;

			if (cameraSpace.x < -.1 && cameraSpace.x > 1.1)
				return false;

			if (cameraSpace.y < -.1 && cameraSpace.y > 1.1)
				return false;

			return true;
		}

	}
}