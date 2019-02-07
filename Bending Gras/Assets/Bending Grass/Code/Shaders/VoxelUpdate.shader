Shader "BendinGrass/VoxelUpdate"
{
	Properties
	{
		_VolumePosition ("Volume Position", Vector) = (0,0,0,0)
		_VolumeSize ("Volume Size", Vector) = (1,1,1,1)
		_UpdateFrequency ("Frequency", float) = 1
	}

		SubShader
	{
		Lighting Off
		Blend One Zero
		Pass
		{
			Name "UpdateVolume"
			CGPROGRAM
			#include "UnityCustomRenderTexture.cginc"
			#include "Bending.cginc"
			#pragma vertex CustomRenderTextureVertexShader
			#pragma fragment frag
			#pragma target 5.0

			float3 _VolumePosition, _VolumeSize;
			float _UpdateFrequency;

			float3 VoxelWorldSpace(float3 coord)
			{
				return _VolumePosition + (_VolumeSize * coord);
			}


			float4 frag(v2f_customrendertexture IN) : COLOR
			{
				float lerpFactor = .1 + .7 * (1 - _UpdateFrequency);
				float3 halfTexel = 1.0/float3(_CustomRenderTextureWidth, _CustomRenderTextureHeight, _CustomRenderTextureDepth)/2;
				//halfTexel /= 2.0;
				float4 lastSample = tex3D(_SelfTexture3D, IN.globalTexcoord);

				float3 worldPos = VoxelWorldSpace(IN.globalTexcoord);

				float influence = 0;
				float4 dir;
				dir.rgb = ApplyBending(worldPos, 1.0, influence)*.5+.5;
				
				if (lastSample.a > 0.95)
				{
					dir.a = 1;
					if (influence < .95)
					{
						return lastSample;
					}
					dir = lerp(lastSample, dir, lerpFactor);
				}
				else
				{
					dir.a = influence;
					dir = lerp(lastSample, dir, lerpFactor);
					
				}

				return dir;
				
			}
			ENDCG
		}

		Pass
		{
			Name "ResetVolume"
			CGPROGRAM
			#include "UnityCustomRenderTexture.cginc"
			#pragma vertex CustomRenderTextureVertexShader
			#pragma fragment frag
			#pragma target 3.0

			float4 frag(v2f_customrendertexture IN) : COLOR
			{
				return float4(.5, .5, .5, 0);

			}
			ENDCG
		}

		Pass
		{
		
		}
	}
}
