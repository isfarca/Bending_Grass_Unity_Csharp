

Shader "Hidden/TerrainEngine/Details/WavingDoublePass" {
Properties {
	_WavingTint ("Fade Color", Color) = (.7,.6,.5, 0)
	_MainTex ("Base (RGB) Alpha (A)", 2D) = "white" {}
	_WaveAndDistance ("Wave and distance", Vector) = (12, 3.6, 1, 1)
	_Cutoff ("Cutoff", float) = .5
}

SubShader {
	Tags {
		"Queue" = "Geometry+200"
		"IgnoreProjector"="True"
		"RenderType"="Grass"
	}
	Cull Off


	CGPROGRAM
	#pragma target 5.0
	#pragma surface surf StandardSpecular vertex:vert addshadow
	#pragma multi_compile __ GRASS_PERMANENT_BEND
	#pragma multi_compile __ DISABLE_GRASS_VERTEX_BEND
	
	#include "TerrainEngine.cginc"
	#include "Bending.cginc"

	// Macro that calculates the sqrdistance between 2 points
	#define SQRDISTANCE(A,B) ((A.x-B.x)*(A.x-B.x)+(A.y-B.y)*(A.y-B.y)+(A.z-B.z)*(A.z-B.z))
		
	sampler2D _MainTex;
	float _Cutoff;
	float4 _Color;

	float4 _terrainPositionAndSize;
	sampler2D _terrainGrassTexture;

	struct Input {
		float2 uv_MainTex;
		float4 color : COLOR;

	};


	// Basically just a scaled cos function
	float PingPong (float t)
	{
		float tim = cos(t*3.14)/2.0;
		return 0.5-tim;
	}

	void vert (inout appdata_full v, out Input data)
	{

		UNITY_INITIALIZE_OUTPUT(Input, data);

		float4 _waveXSizeMove = float4(0.048, 0.06, 0.24, 0.096);
		float4 _waveZSizeMove = float4 (0.024, .08, 0.08, 0.2);
		float4 waveSpeed = float4 (1.2, 2, 1.6, 4.8);
		float4 waves;
	
		float4 worldPos;
		
		// The line below is to give the grass an inclination when is placed on a slope.
		// Since the unity terrain just generate batches of grass and assign them the same normal as the terrain below
		// we can't rotate them, but we can add the normal to simulate this a little bit.
		// The grass will always be 1 unit taller tho.
		// Comment this line if you don't want this.
		v.vertex.xyz += v.normal.xyz * v.color.a;
		
		// This function, from Bending.cginc, automatically calculates the benders influence on this vertex (both vertex and permanent)
		fixed permVal = 0;
		BendVertex(v.vertex, v.color.a, permVal);
		
		// This comes straight up from the Unity's grass (global wind)
		// _________________________________________________

		if (v.color.a > 0.1)
		{
			waves = v.vertex.x * _waveXSizeMove;
			waves += v.vertex.z * _waveZSizeMove;
			_waveXSizeMove = float4(0.024, 0.04, -0.12, 0.096);
			_waveZSizeMove = float4 (0.006, .02, -0.02, 0.1);

			// Add in time to model them over time
			waves += _WaveAndDistance.x * waveSpeed;
			float4 s, c;
			waves = frac(waves);
			FastSinCos(waves, s, c);
			float waveAmount = (v.color.a) * _WaveAndDistance.z;

			// Faster winds move the grass more than slow winds 
			s *= normalize(waveSpeed);
			s = s * s;

			float lighting = dot(s, normalize(float4 (1, 1, .4, .2))) * .7;
			s *= waveAmount;

			fixed3 waveColor = lerp(fixed3(0.5, 0.5, 0.5), _WavingTint.rgb, lighting);

			v.color.rgb = v.color.rgb * waveColor * 2;

			float3 waveMove = float3 (0, 0, 0);
			waveMove.x = dot(s, _waveXSizeMove);
			waveMove.z = dot(s, _waveZSizeMove);

			v.vertex.xz -= mul((float3x3)unity_WorldToObject, waveMove).xz * (1 - permVal*.7);
		}

		// __________________________________________________

		// Calculate the grass fade over distance

		// This is the distance to which the grass will begin to fade, in percent (0.8 is 80%)
		// Should be < 0.9 to avoid grass chunks popping out of nowhere
		float maxDist = .8;

		worldPos.xyz = mul(unity_ObjectToWorld, float4(v.vertex.xyz,1)).xyz;

		v.color.a = ((SQRDISTANCE(worldPos, _WorldSpaceCameraPos))/_WaveAndDistance.w);
		if (v.color.a<maxDist)
			v.color.a = 1;
		else
			v.color.a = 1-saturate((v.color.a-maxDist)/(1-maxDist));


	}



	void surf (Input IN, inout SurfaceOutputStandardSpecular o)
	{
		// Standard surface shader
		half4 c = tex2D(_MainTex, IN.uv_MainTex*0.99);
		o.Alpha = c.a * IN.color.a;
		clip(o.Alpha - 0.1);
		

		o.Albedo = c.rgb * IN.color.rgb;
		o.Specular = 0.0;
		o.Smoothness = 0.2;
	}

ENDCG

}
	
	SubShader {
		Tags {
			"Queue" = "Geometry+200"
			"IgnoreProjector"="True"
			"RenderType"="Grass"
		}
		Cull Off
		LOD 200
		ColorMask RGB
		ZWrite On
		
		Pass {
			Material {
				Diffuse (1,1,1,1)
				Ambient (1,1,1,1)
			}
			Lighting On
			
			ColorMaterial AmbientAndDiffuse
			AlphaTest Greater [_Cutoff]
			SetTexture [_MainTex] { combine texture, previous  }
		}

		
	}
	
	Fallback "Diffuse"
}
