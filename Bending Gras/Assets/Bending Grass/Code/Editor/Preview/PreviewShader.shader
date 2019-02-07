Shader "CustomRenderTexture/AttenPreview"
{
	Properties
	{
		_Color("Color", Color) = (1,1,1,1)
		_Power("Power", Range(0,4)) = 0
		_Hardness("Hardness", Range(0,14)) = 0
		_Noise("Noise", Range(0,4)) = 0
		_Direction("Direction", Range(0,1)) = 0

		
		[Toggle(SHOW_DIRECTION)] _Dir("Dir", Float) = 0
	}

	SubShader
	{
		
		Lighting Off
		Blend One Zero

		Pass
		{
			CGPROGRAM
			#include "UnityCustomRenderTexture.cginc"

			#pragma multi_compile __ SHOW_DIRECTION

			#pragma vertex CustomRenderTextureVertexShader
			#pragma fragment frag
			#pragma target 5.0

			float4 _Color;
			float _Power, _Hardness, _Noise, _Direction;
			float CalcAtten(float val, float max, float b)
			{
				float atten = 1.0 - saturate(pow(val / max, b));
				return atten;
			}

			float cross(float2 coord, float size, float fade)
			{
				coord = abs(coord);
				float m = min(coord.x, coord.y);
				float M = max(coord.x, coord.y);

				float d = M < fade ? 1.0 : 0.0;
				d = 1-saturate((M) / fade);


				float c = m < size ? 1.0 : 0.0;

				return c * d;
				
			}

			float crossAxis(float2 coord, float size, float fade)
			{
				
				if (coord.x > 0 || coord.y > 0)
					return 0;
				return cross(coord, size, fade);
			}


			float4 frag(v2f_customrendertexture IN) : COLOR
			{
				float2 centeredPos = (.5 - IN.localTexcoord.xy)*2.2;
				float2 powCenteredPos = pow(abs(centeredPos), _Hardness);
				float d = sqrt(dot(powCenteredPos, powCenteredPos));

				float interpDirect = lerp(length(centeredPos) * 2, IN.localTexcoord.y, _Direction);

				float noise = 1+((cos(_Time.y * 3.0 + cos((interpDirect) * 5)) * .5 - .5) * _Noise);
				noise = max(0, noise);
				//d = saturate(CalcAtten(d, .8, _Hardness) * _Power);
				d = saturate((1-d) * _Power * noise);
				half4 col = _Color * d;
				col.a = 1.0;

				float2 rb = normalize(abs(centeredPos));

#ifdef SHOW_DIRECTION
				col.r = 0;
				col.gb *= lerp(normalize(-centeredPos)*.5+.5, float2(0,1), _Direction);
				
#endif

				col.rgb = lerp(col.rgb, half3(1, 0, 0), cross(centeredPos, .02, .8));
				
			
				return col;
			}
			ENDCG
		}
	}
}