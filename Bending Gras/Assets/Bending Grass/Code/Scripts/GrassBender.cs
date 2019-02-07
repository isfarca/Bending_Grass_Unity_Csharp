using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace BendinGrass
{
	[ExecuteInEditMode()]

	public class GrassBender : MonoBehaviour
	{
		public OptimizedVertexBender benderData;
		public BoundingSphere boundingSphere;

		[SerializeField]
		private float uniformScale = 1f;
		[SerializeField]
		private float hardness = 1f;
		[SerializeField]
		private float power = 1;
		[SerializeField]
		private float direction = 0f;
		[SerializeField]
		private float noise = 0f;

		bool _changed = true;
		bool _generateMatrix = true;

		/// <summary>
		/// Use this to force the bender to recalculate its internal data.
		/// </summary>
		/// <param name="matrix">Should the matrix be regenerated too? (use only if the transform/scale changed)</param>
		public void SetDirtyFlags(bool matrix = false)
		{
			_changed = true;
			_generateMatrix = _generateMatrix || matrix;
		}

		public bool IsDirty
		{
			get
			{
				return _changed || _generateMatrix;
			}
		}

		/// <summary>
		/// Multiplier for the local scale
		/// </summary>
		public float UniformScale
		{
			get
			{
				return uniformScale;
			}

			set
			{
				bool changed = uniformScale != value;
				uniformScale = value;
				if (changed)
					SetDirtyFlags(true);


			}
		}

		/// <summary>
		/// How hard this volume will near its limits
		/// </summary>
		public float Hardness
		{
			get
			{
				return hardness;
			}

			set
			{
				bool changed = hardness != value;
				hardness = value;
				if (changed)
					SetDirtyFlags();
			}
		}


		/// <summary>
		/// How much this bender will push the grass
		/// </summary>
		public float Power
		{
			get
			{
				return power;
			}

			set
			{
				bool changed = power != value;
				power = value;
				if (changed)
					SetDirtyFlags();
			}
		}


		/// <summary>
		/// Should this bender be omnidirectional(0.0) or directional(1.0)?
		/// </summary>
		public float Direction
		{
			get
			{
				return direction;
			}

			set
			{
				bool changed = direction != value;
				direction = value;
				if (changed)
					SetDirtyFlags();
			}
		}

		/// <summary>
		/// The quantity of noise this bender will have inside the volume
		/// </summary>
		public float Noise
		{
			get
			{
				return noise;
			}

			set
			{
				bool changed = noise != value;
				noise = value;
				if (changed)
					SetDirtyFlags();
			}
		}

		[SerializeField]
		private bool permanentBending = false;

		/// <summary>
		/// Should this bender be included in the permanent simulation?
		/// </summary>
		public bool PermanentBending
		{
			get
			{
				return permanentBending;
			}

			set
			{
				permanentBending = value;

			}
		}

		[HideInInspector]
		public bool visible;
		// this is the distance of this bender from the camera (only useful if there are too many benders)
		[HideInInspector]
		public float cullDist;

		// Use this for initialization
		protected virtual void Start()
		{
			UpdateData();
		}

		protected virtual void Update()
		{
			InternalUpdate();
			if (IsMaster)
			{
				GrassBendersManager.UpdateBenders();
				GrassBendersManager.UpdateBendersShaderArray();
			}
		}

		public void InternalUpdate()
		{
			// If there is currently no master bender, this bender will be the new master.
			if (GrassBendersManager.master == null)
				GrassBendersManager.master = this;
			UpdateData();
		}

		protected virtual void OnEnable()
		{
			GrassBendersManager.AddBender(this);
			UpdateData();
		}

		void OnDisable() { Remove(); }

		void OnDestroy() { Remove(); }

		void OnValidate()
		{
			SetDirtyFlags(true);
			UpdateData();
		}

		void Remove() { GrassBendersManager.Remove(this); }


		/// <summary>
		/// Is this bender the one who is currently updating all the others?
		/// </summary>
		public bool IsMaster
		{
			get
			{
				return (this == GrassBendersManager.master);
			}
		}




		#region "Volume"

		// Precalculated just as an information, currently showed in the editor
		[System.NonSerialized]
		public Vector3 worldSpaceSize;
		
		/// <summary>
		/// Generates all the stuff related to the volume space (matrices, bounding spheres, etc.)
		/// </summary>
		void GenerateMatrices()
		{
			this.benderData.benderLocal2World = Matrix4x4.TRS(transform.position, transform.rotation, transform.lossyScale * (UniformScale * .5f));
			this.benderData.benderWorld2Local = Matrix4x4.Inverse(this.benderData.benderLocal2World);
			worldSpaceSize = transform.lossyScale * UniformScale;
			UpdateWorldCorners();
			UpdateBoundingSphere();
			_generateMatrix = false;
			transform.hasChanged = false;
		}

		/// <summary>
		/// Updates every internal parameter that is needed
		/// </summary>
		public virtual void UpdateData()
		{
			if (benderData == null)
			{
				benderData = new OptimizedVertexBender();
				GenerateMatrices();
			}

			if (transform.hasChanged)
				SetDirtyFlags(true);

			if (_changed)
			{
				if (_generateMatrix)
					GenerateMatrices();
				this.benderData.HPDN.Set(Hardness, Power, Direction, Noise);
				_changed = false;
			}

		}

		
		/// <summary>
		/// Updates the bounding sphere used by the culling system.
		/// </summary>
		void UpdateBoundingSphere()
		{
			Vector3 scale = transform.lossyScale;
			boundingSphere.position = transform.position;

			// We use 2 Mathf.Max because the params array overload generates GC.
			boundingSphere.radius = Mathf.Max(scale.x, Mathf.Max(scale.y, scale.z)) * uniformScale * .5f;
		}
	

		public Vector3[] worldCorners = new Vector3[8];

		// Not really used yet, but it may come useful in later updates.
		public void UpdateWorldCorners()
		{
			int count = 0;
			for (int i = 0; i <= 1; i++)
				for (int j = 0; j <= 1; j++)
					for (int k = 0; k <= 1; k++)
						worldCorners[count++] = (Vector3)(benderData.benderLocal2World * new Vector4(i * 2 - 1, j * 2 - 1, k * 2 - 1, 1));
		}

		/// <summary>
		/// Is a world point inside this volume?
		/// </summary>
		/// <param name="worldPoint">Your point</param>
		public bool IsPointInside(Vector3 worldPoint)
		{
			Vector3 localSpace = benderData.benderWorld2Local * new Vector4(worldPoint.x, worldPoint.y, worldPoint.z, 1);

			if (localSpace.x < -1 || localSpace.x > 1)
				return false;

			if (localSpace.y < -1 || localSpace.y > 1)
				return false;

			if (localSpace.z < -1 || localSpace.z > 1)
				return false;

			return true;
		}

		#endregion

		#region "Gizmos & Editor"

#if (UNITY_EDITOR)

		public void DrawPreview(CustomRenderTexture crt)
		{
			if (crt == null)
				return;

			if (!crt.IsCreated())
				return;
			crt.material.SetFloat("_Power", Power);
			crt.material.SetFloat("_Hardness", Hardness);
			crt.material.SetFloat("_Noise", Noise);
			crt.material.SetFloat("_Direction", Direction);
			crt.Update();
		}

		static Color NORMAL_COLOR = new Color(0.1f, 0.8f, 0.2f, 0.2f);
		static Color MASTER_COLOR = new Color(0.8f, 0.1f, 0.2f, 0.2f);
		void OnDrawGizmos()
		{
			
			Gizmos.matrix = transform.localToWorldMatrix;

			if (enabled)
			{
				if (!IsMaster)
					Gizmos.color = NORMAL_COLOR;
				else
					Gizmos.color = MASTER_COLOR;
			}
			else
			{
				if (!IsMaster)
					Gizmos.color = NORMAL_COLOR * new Color(1, 1, 1, .4f);
				else
					Gizmos.color = MASTER_COLOR * new Color(1, 1, 1, .4f); ;

			}
			Gizmos.color = Gizmos.color * new Color(1, 1, 1, _gizmosAlphaMult);
			Gizmos.DrawCube(Vector3.zero, Vector3.one * UniformScale);
			//Gizmos.DrawCube(Vector3.zero, -Vector3.one * UniformScale);
			Gizmos.color = Gizmos.color * new Color(1, 1, 1, 2);
			Gizmos.DrawWireCube(Vector3.zero, Vector3.one * UniformScale);
			Gizmos.matrix = Matrix4x4.identity;

			foreach (Vector3 v in worldCorners)
			{
				Gizmos.DrawSphere(v, .1f);
			}
			_gizmosAlphaMult = .5f;
		}


		float _gizmosAlphaMult = 1f;

		static Color HANDLES_COLOR = new Color(.1f, .1f, .7f, .6f);
		private void OnDrawGizmosSelected()
		{
			UnityEditor.Handles.color = HANDLES_COLOR;
			UnityEditor.Handles.DrawSphere(22, transform.position, transform.rotation, 2f * (1f - Direction));
			UnityEditor.Handles.ArrowHandleCap(22, transform.position + transform.forward * (1f - Direction), transform.rotation, 3f * Direction, EventType.Repaint);

			_gizmosAlphaMult = 1f;

		}
#endif
		#endregion

		#region "Editor"

#if (UNITY_EDITOR)

		static GrassBender CreateBender(string name = "Grass Bender")
		{
			GameObject go = new GameObject(name);
			GrassBender gb = go.AddComponent<GrassBender>();
			//UnityEditor.GameObjectUtility.SetParentAndAlign(go, menuCommand.context as GameObject);
			UnityEditor.Undo.RegisterCreatedObjectUndo(go, "Create " + go.name);
			return gb;
		}

		[UnityEditor.MenuItem("GameObject/BendinGrass/Grass Benders/Bender", false, 10)]
		static GrassBender CreateBender(UnityEditor.MenuCommand menuCommand)
		{
			GrassBender gb = CreateBender();
			UnityEditor.Selection.activeObject = gb.gameObject;
			return gb;
		}

		[UnityEditor.MenuItem("GameObject/BendinGrass/Grass Benders/ChildBender", false, 10)]
		static GrassBender CreateChildBender(UnityEditor.MenuCommand menuCommand)
		{
			GrassBender gb = CreateBender();
			if (UnityEditor.Selection.activeTransform != null)
			{
				gb.transform.SetParent(UnityEditor.Selection.activeTransform, false);
				gb.transform.localPosition = Vector3.zero;
				gb.transform.localRotation = Quaternion.identity;
				gb.transform.localScale = Vector3.one;
			}
			UnityEditor.Selection.activeObject = gb.gameObject;
			return gb;
		}

		[UnityEditor.MenuItem("GameObject/BendinGrass/Grass Benders/CharacterBender", false, 10)]
		static GrassBender CreateCharacterBender(UnityEditor.MenuCommand menuCommand)
		{
			GrassBender gb = CreateBender("Character Bender");
			//gb.transform.localRotation = Quaternion.Euler(90, 0, 0);
			gb.Direction = .9f;
			gb.Hardness = 7f;
			gb.Power = 3f;
			gb.PermanentBending = true;
			if (UnityEditor.Selection.activeTransform != null)
			{
				gb.transform.SetParent(UnityEditor.Selection.activeTransform, true);
				gb.AdaptToBounds(UnityEditor.Selection.activeTransform);
				
			}

			UnityEditor.Selection.activeObject = gb.gameObject;
			return gb;
		}

		[UnityEditor.MenuItem("GameObject/BendinGrass/Grass Benders/Wind", false, 10)]
		static GrassBender CreateWind(UnityEditor.MenuCommand menuCommand)
		{
			GrassBender gb = CreateBender("Wind");
			GameObject go = gb.gameObject;
			go.transform.localScale = new Vector3(1, 1, 5);
			gb.Noise = 1f;
			gb.Hardness = 5f;
			gb.Direction = 1f;
			gb.Power = 1f;
			UnityEditor.Selection.activeObject = go;
			return gb;
		}

		

		public void AdaptToBounds()
		{
			AdaptToBounds(null);
		}

		public void AdaptToBounds(Transform parent)
		{
			if (parent == null)
			{
				parent = transform.parent;
				if (parent == null)
				{
					//If this is the case, it means our transform has no parent
					return;
				}
					
			}

			Bounds bound = new Bounds(parent.position, Vector3.one);
			Renderer[] renderers = parent.GetComponentsInChildren<Renderer>();
			foreach (Renderer r in renderers)
				bound.Encapsulate(r.bounds);

			Collider[] colliders = parent.GetComponentsInChildren<Collider>();
			foreach (Collider c in colliders)
				if (!c.isTrigger)
					bound.Encapsulate(c.bounds);
			
			transform.position = bound.center;
			transform.localScale = bound.size;
			transform.rotation = Quaternion.Euler(90, 0, 0);
			
			
			if (transform!=parent)
				transform.localScale = new Vector3(Mathf.Abs(transform.localScale.x / parent.lossyScale.x), Mathf.Abs(transform.localScale.y / parent.lossyScale.y), Mathf.Abs(transform.localScale.z / parent.lossyScale.z));
			transform.localScale = transform.rotation * transform.localScale;
			
		}

#endif
		#endregion
	}

	
	public class OptimizedVertexBender
	{
		public Matrix4x4 benderWorld2Local, benderLocal2World;

		// float4(Hardness, Power, Direction, Noise)
		//
		// Direction is 0, the bender will be spheric, otherwise it will be directional
		// Noise will add some noise to bended grass (good for wind)
		public Vector4 HPDN;
	}

}