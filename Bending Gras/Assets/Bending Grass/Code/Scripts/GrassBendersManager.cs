using UnityEngine;
using System.Collections.Generic;

namespace BendinGrass
{
	public class GrassBendersManager
	{
		// List of every GrassBender instanced
		public static List<GrassBender> benders = new List<GrassBender>();

		// Instead of having a manager GameObject, one of the grass benders is going to be the manager.
		public static GrassBender master;

		/// <summary>
		/// Finds a GrassBender that can be the master.
		/// </summary>
		/// <returns>The new master</returns>
		public static GrassBender FindNewMaster()
		{
			foreach (GrassBender gb in benders)
				if (gb.enabled)
					return gb;
			return null;
		}

		/// <summary>
		/// Add a bender to this manager.
		/// </summary>
		public static void AddBender(GrassBender bender)
		{
			if (master == null)
				master = bender;

			if (benders == null)
				benders = new List<GrassBender>();

			if (!benders.Contains(bender))
				benders.Add(bender);
		}

		/// <summary>
		/// Remove a bender from this manager.
		/// </summary>
		public static void Remove(GrassBender bender)
		{
			if (benders.Contains(bender))
				benders.Remove(bender);

			// if this bender was the master
			if (bender.IsMaster)
			{
				// we try to find a new one
				master = FindNewMaster();

				if (!master)
				{
					UpdateBendersShaderArray();
				}
			}
			

		}

		/// <summary>
		/// Perform operations on all the grass benders.
		/// </summary>
		public static void UpdateBenders()
		{
			for (int i = benders.Count - 1; i >= 0; i--)
			{
				if (benders[i] == null || !benders[i].isActiveAndEnabled)
				{
					// If somewhat one bender did not remove himself from the bender list when destroyed or disabled, we remove it here.
					Remove(benders[i]);
				}
			}
		}

		public static GrassBendersContainer container;

		/// <summary>
		/// Generate the benders lists, for the vertex bending shaders.
		/// </summary>
		public static void UpdateBendersShaderArray()
		{
			if (container == null)
				container = new GrassBendersContainer();

			container.ResetLists();
			// we are going to cull benders only if they are too many, otherwise it's probably just better to just pass them all to the shader
			if (benders.Count > GrassSettings.SettingsPreset.MaxActiveBenders)
			{
				Culling();
				for (int i = 0; i < benders.Count; i++)
				{
					if (benders[i].visible)
						if (benders[i].UniformScale > .001f)
						{
							container.AddBender(benders[i].benderData);
							if (container.currentNumber >= GrassSettings.SettingsPreset.MaxActiveBenders)
								break;
						}

				}
			
			}
			else
			{
				for (int i = 0; i < benders.Count; i++)
				{
					if (benders[i].UniformScale > .001f)
					{
						container.AddBender(benders[i].benderData);
					}

				}
			}

			container.GlobalMaterialUpdate();
			
		}


		#region "Culling"

		public static CullingGroup cullingGroup;

		

		static BoundingSphere[] cullingSpheres = new BoundingSphere[1024];

		/// <summary>
		/// Calculates which benders are visible by the camera and their distances.
		/// </summary>
		public static void Culling()
		{
			// For better performances, we wrap the grass benders in bounding spheres, then we feed them to the CullingGroup.
			// Actual data are not ready until the next frame.
			if (cullingGroup == null)
			{
				cullingGroup = new CullingGroup();
				cullingGroup.targetCamera = Camera.main;
				cullingGroup.SetBoundingSpheres(cullingSpheres);
				cullingGroup.SetDistanceReferencePoint(cullingGroup.targetCamera.transform);
			}
			cullingGroup.SetBoundingDistances(GrassSettings.cullingBands);
			cullingGroup.SetBoundingSphereCount(benders.Count);
			for (int i = 0; i < benders.Count; i++)
			{
				cullingSpheres[i] = benders[i].boundingSphere;
				benders[i].cullDist = Mathf.Max(0f, Vector3.Distance(benders[i].boundingSphere.position, Camera.main.transform.position) - benders[i].boundingSphere.radius);
				benders[i].visible = cullingGroup.IsVisible(i);
			}

			SortBendersByDist();
		}

		//In-place bubble sort to order the benders by distance
		public static void SortBendersByDist()
		{
			GrassBender temp = benders[0];
			bool flag = false;
			int n = benders.Count;
			int p = benders.Count;
			do
			{
				flag = false;
				for (int i = 0; i < (n - 1); i++)
					if (benders[i].cullDist > benders[i + 1].cullDist)
					{
						temp = benders[i];
						benders[i] = benders[i + 1];
						benders[i + 1] = temp;
						flag = true;
						p = i + 1;
					}
				n = p;
			}
			while (flag);
		}

		#endregion
	}


	/// <summary>
	/// This class is used to auto generate the arrays that will be passed to the shaders.
	/// </summary>
	public class GrassBendersContainer
	{
		// the list of the benders that we are going to actually process (enabled, visible, ect.)
		public int currentNumber;
		public Matrix4x4[] benderLocal2World, benderWorld2Local;
		public Vector4[] benderHPDN;

		public void ResetLists()
		{
			if (benderLocal2World == null)
				benderLocal2World = new Matrix4x4[GrassSettings.BENDERS_ARRAY_SIZE];

			if (benderWorld2Local == null)
				benderWorld2Local = new Matrix4x4[GrassSettings.BENDERS_ARRAY_SIZE];

			if (benderHPDN == null)
				benderHPDN = new Vector4[GrassSettings.BENDERS_ARRAY_SIZE];

			currentNumber = 0;
		}

		/// <summary>
		/// Add a bender to the arrays.
		/// </summary
		public void AddBender(OptimizedVertexBender b)
		{
			benderLocal2World[currentNumber] = b.benderLocal2World;
			benderWorld2Local[currentNumber] = b.benderWorld2Local;
			benderHPDN[currentNumber] = b.HPDN;
			currentNumber++;
		}

		public void GlobalMaterialUpdate()
		{
			Shader.SetGlobalMatrixArray("_benderWorld2Local", benderWorld2Local);
			Shader.SetGlobalMatrixArray("_benderLocal2World", benderLocal2World);
			Shader.SetGlobalVectorArray("_benderHPDN", benderHPDN);
			Shader.SetGlobalInt("_grassBendersCount", currentNumber);
		}

		public void MaterialUpdate(Material mat)
		{
			mat.SetMatrixArray("_benderWorld2Local", benderWorld2Local);
			mat.SetMatrixArray("_benderLocal2World", benderLocal2World);
			mat.SetVectorArray("_benderHPDN", benderHPDN);
			mat.SetInt("_grassBendersCount", currentNumber);
		}
	}
}