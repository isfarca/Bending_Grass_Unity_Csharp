using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;


namespace BendinGrass
{
	[ExecuteInEditMode()]
	public class PermanentBendingVolume : MonoBehaviour
	{
		/// <summary>
		/// The assigned Volume Map
		/// </summary>
		public CustomRenderTexture volumeMap;
		
		public Material updateMaterial;

		[SerializeField]
		private bool disablePermanentBend;
		[SerializeField]
		private bool disableVertexBend;

		[SerializeField]
		private float volumeBendMult = 1f;

		
		/// <summary>
		/// How frequent will the Volume Map update? 0 = never, 1 = Every Frame
		/// </summary>
		[Range(0f, 1f)]
		public float updateFrequency = .5f;



		/// <summary>
		/// Return the world space cell size
		/// </summary>
		public Vector3 CellSize
		{
			get
			{
				return new Vector3(transform.lossyScale.x / volumeMap.width, transform.lossyScale.y / volumeMap.height, transform.lossyScale.z / volumeMap.volumeDepth);
			}
		}

		/// <summary>
		/// Return basically the 3D texture size
		/// </summary>
		public Vector3 VoxelGridSize
		{
			get
			{
				return new Vector3(volumeMap.width, volumeMap.height, volumeMap.volumeDepth);
			}
		}


		/// <summary>
		/// Should vertex bending be disabled when using this volume?
		/// </summary>
		public bool DisableVertexBend
		{
			get
			{
				return disableVertexBend;
			}

			set
			{
				bool changed = disablePermanentBend != value;
				disableVertexBend = value;
				if (changed)
					UpdateKeywords();
			}
		}

		/// <summary>
		/// Should permanent bending be disabled when using this volume?
		/// </summary>
		public bool DisablePermanentBend
		{
			get
			{
				return disablePermanentBend;
			}

			set
			{
				bool changed = disablePermanentBend != value;
				disablePermanentBend = value;
				if (changed)
					UpdateKeywords();
			}
		}

		/// <summary>
		/// How much the permanent grass will be displaced
		/// </summary>
		public float VolumeBendMult
		{
			get
			{
				return volumeBendMult;
			}

			set
			{
				bool changed = volumeBendMult != value;
				volumeBendMult = value;
				if (changed)
					UpdateRenderTexture();
			}
		}

		private void Start()
		{
			InitializeVolumeMap();
			UpdateKeywords();
			
		}

		/// <summary>
		/// Forces the volume map cells to be the closest possible to the specified size
		/// </summary>
		/// <param name="size">Uniform size desired for a single cell</param>
		public void ForceCellSize(float size)
		{
			volumeMap.Release();
			volumeMap.width = Mathf.CeilToInt(transform.lossyScale.x / size);
			volumeMap.height = Mathf.CeilToInt(transform.lossyScale.y / size);
			volumeMap.volumeDepth = Mathf.CeilToInt(transform.lossyScale.z / size);
			volumeMap.Create();
			InitializeVolumeMap();
		}

		/// <summary>
		/// Request a reset of the volume cells (it will take 2 frames)
		/// </summary>
		[ContextMenu("Reset VolumeMap")]
		public void InitializeVolumeMap()
		{
			updateMaterial = volumeMap.material;
			StartCoroutine("ExecutePass", 1);

		}

		

		/// <summary>
		/// Unity coroutine. Execute the specified pass of the update material. It takes 2 frames.
		/// </summary>
		/// <param name="pass">The ID of the wanted pass</param>
		IEnumerator ExecutePass(int pass)
		{
			// Set the desired shader pass
			volumeMap.shaderPass = pass;

			// Make sure the update pass is executed on both buffers (remember it's double buffered)
			volumeMap.Update(2);
			yield return null;	//Wait next frame

			// Sets the default pass again
			volumeMap.shaderPass = 0;
			yield return null;
			UpdateMap();
		}


		/// <summary>
		/// Sets the parameters of the shaders using BendinGrass Permanent Bending, using this volume.
		/// </summary>
		void UpdateMap()
		{
			volumeMap.Update();
			Shader.SetGlobalTexture("_volumeMap", volumeMap);
			Shader.SetGlobalVector("_volumeMapTexSize", this.VoxelGridSize);
			Shader.SetGlobalVector("_volumeOrigin", new Vector4(transform.position.x, transform.position.y, transform.position.z));
			Shader.SetGlobalVector("_volumeSize", new Vector4(transform.lossyScale.x, transform.lossyScale.y, transform.lossyScale.z));
			Shader.SetGlobalFloat("_volumeBendMult", VolumeBendMult);
		}


		/// <summary>
		/// Sets the shader keywords with the parameters of this volume.
		/// </summary>
		public void UpdateKeywords()
		{
			GrassSettings.SetPermanentBending(!disablePermanentBend);
			GrassSettings.SetVertexBending(!disableVertexBend);
		}


		float _updateTimer;
		//Updates the volume update timer.
		bool UpdateTimer()
		{
			_updateTimer += updateFrequency;
			if (_updateTimer >= .999f)
			{
				_updateTimer -= 1f;
				return true;
			}
			return false;
		}

		// Update is called once per frame
		void Update()
		{
			// We want coroutines to work in the editor, but not the actual Update (only in the Editor)
#if UNITY_EDITOR
			if (!Application.isPlaying)
			{
				return;
			}	
#endif
			
			if (!DisablePermanentBend && GrassSettings.PermanentEnabled)
			{
				if (UpdateTimer())
				{
					
					if (volumeMap.shaderPass == 0)
					{
						GenerateBendersList();
						UpdateRenderTexture();
						UpdateMap();

					}
				}

			}
		}

		public GrassBendersContainer permanentContainer;

		// Sets all the variables that are needed in the CustomRenderTexture update material
		void UpdateRenderTexture()
		{
			updateMaterial.SetVector("_VolumePosition", transform.position);
			updateMaterial.SetVector("_VolumeSize", transform.lossyScale);
			updateMaterial.SetFloat("_UpdateFrequency", updateFrequency);
			permanentContainer.MaterialUpdate(updateMaterial);
		}
		
		/// <summary>
		/// Generate the arrays containing all the data for the permanent shaders.
		/// NOTE: while the vertex bending is global, the permanent is relative to the volume, so it will be calculated here, and not in the GrassBenderManager.
		/// </summary>
		public void GenerateBendersList()
		{
			if (permanentContainer == null)
				permanentContainer = new GrassBendersContainer();
			permanentContainer.ResetLists();
			Bounds bound = new Bounds(transform.position + transform.lossyScale / 2f, transform.lossyScale);
			GrassBender current;
			for (int i = 0; i < GrassBendersManager.benders.Count; i++)
			{
				current = GrassBendersManager.benders[i];
				if (current.PermanentBending)
				{

					if (bound.SqrDistance(current.boundingSphere.position) < current.boundingSphere.radius * current.boundingSphere.radius)
					{
						permanentContainer.AddBender(current.benderData);
					}
				}
			}
		}


#if UNITY_EDITOR

		void OnValidate()
		{
			UpdateKeywords();
		}


		/// <summary>
		/// Tries to calculate the volume size from the terrains in the scene. Does nothing if there are no terrains.
		/// </summary>
		[ContextMenu("CalculateFromTerrains")]
		public void VolumeFromTerrains()
		{
			if (Terrain.activeTerrains.Length < 1)
				return;
			Bounds b = Terrain.activeTerrains[0].terrainData.bounds;
			for (int i = 1; i < Terrain.activeTerrains.Length; i++)
			{
				b.Encapsulate(Terrain.activeTerrains[i].terrainData.bounds);
			}

			transform.position = b.min;
			transform.localScale = Vector3.Min(Vector3.one, b.size);
		}

		static Color VOLUME_COLOR = new Color(.3f, .3f, .6f, .5f);

		private void OnDrawGizmos()
		{
			Gizmos.color = VOLUME_COLOR;

			Gizmos.DrawWireCube(transform.position + transform.lossyScale / 2f, transform.lossyScale);
			Gizmos.DrawCube(transform.position + transform.lossyScale / 2f, transform.lossyScale);

			// We want to see/select the volume even when we are on the inside, so we draw another cube, but with inverted scale.
			// May cause some confusion, so it's off by default. 
			// Uncomment the line below if you want to use this feature.
			
			//Gizmos.DrawCube(transform.position + transform.lossyScale / 2f, -transform.lossyScale);
		}

		private void OnDrawGizmosSelected()
		{
			Gizmos.color = VOLUME_COLOR;

			Vector3 cellSize = CellSize;
			Vector3 center = transform.position + cellSize / 2f;
			Vector3 startPos = center;
			for (int i = 0; i < Mathf.CeilToInt(volumeMap.width); i++)
			{
				Gizmos.DrawWireCube(center, cellSize);
				center.x += cellSize.x;
			}
			center = startPos;
			for (int i = 0; i < Mathf.CeilToInt(volumeMap.height); i++)
			{
				Gizmos.DrawWireCube(center, cellSize);
				center.y += cellSize.y;
			}
			center = startPos;
			for (int i = 0; i < Mathf.CeilToInt(volumeMap.volumeDepth); i++)
			{
				Gizmos.DrawWireCube(center, cellSize);
				center.z += cellSize.z;
			}
		}

		[UnityEditor.MenuItem("GameObject/BendinGrass/Bending Volume", false, 10)]
		public static void CreateVolume(UnityEditor.MenuCommand menuCommand)
		{
			PermanentBendingVolume[] volumes = FindObjectsOfType<PermanentBendingVolume>();
			if (volumes.Length > 0)
				Debug.LogWarning("There are already " + volumes.Length + " bending volumes in this scene, keep in mind you can only use 1 at the time.");

			GameObject go = new GameObject("Bending Volume");
			PermanentBendingVolume vol = go.AddComponent<PermanentBendingVolume>();
			vol.VolumeFromTerrains();
			UnityEditor.GameObjectUtility.SetParentAndAlign(go, menuCommand.context as GameObject);
			UnityEditor.Undo.RegisterCreatedObjectUndo(go, "Create " + go.name);
			UnityEditor.Selection.activeObject = go;
		}

#endif
	}
}