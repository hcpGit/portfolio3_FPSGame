Shader "Custom/MyOutlineShader"
{
	Properties
	{
		_MainTex ("Texture", 2D) = "white" {}
	[Toggle]outLineToggle("outLineToggle",Float)=0
		outLineWidth("outline width",Range(0,0.1)) = 1
			outLineColor("outLineColor",Color) = (1,1,1,1)
			occludeColor("occludeColor",Color) = (1,0,0,1)
			[Toggle] setOccludeVision("setOccludeVision",Float) = 0

			[Toggle] rimLightToggle("rimLight",Float) = 0
			rimLightColor("rimLightColor",Color) = (1,1,1,1)
			rimLightRange("rimLightWidth",Range(0,1)) = 0
	}
		SubShader
		{
			Tags { "RenderType" = "Opaque" }
			LOD 100
				Pass	//맵핵 패스
				{
			Name "OccludePass"
					Tags { "Queue" = "Geometry+1" }
					ZTest Greater
					ZWrite Off

					CGPROGRAM
					#pragma vertex vert            
					#pragma fragment frag
					//	#pragma fragmentoption ARB_precision_hint_fastest

					half4 occludeColor;
					float setOccludeVision;

					float4 vert(float4 pos : POSITION) : SV_POSITION
					{
						float4 viewPos = UnityObjectToClipPos(pos);
						return viewPos;
					}

						half4 frag(float4 pos : SV_POSITION) : COLOR
					{					
						if (!setOccludeVision) discard;
						return occludeColor;
					}

					ENDCG
				}
			
				Pass	//외곽선 패스
					{
				Cull Front
				CGPROGRAM
				#pragma vertex vert
				#pragma fragment frag

				sampler2D _MainTex;
				float outLineWidth;
				half4 outLineColor;
				float outLineToggle;

				struct vi {
				float4 vertex  :POSITION;
				float3 normal  :NORMAL;
				};

				float4 vert(vi input) :SV_POSITION
				{
					float4 vertex=  UnityObjectToClipPos(
								input.vertex
								+ normalize(input.normal)*outLineWidth
								);

					return vertex;
			
		//	o.vertex += (float4)(normalize(input.normal)*outLineWidth,1);
			
			
			
		//	float3 normal = mul((float3x3) UNITY_MATRIX_MV, input.normal);
		//	normal.x *= UNITY_MATRIX_P[0][0];
		//	normal.y *= UNITY_MATRIX_P[1][1];
		//	o.vertex.xy += normal.xy * outLineWidth;
			
			//return o;
				}

				fixed4 frag(float4 pos : POSITION) : COLOR
				{
					if (!outLineToggle) discard;
					return outLineColor;
				}
				ENDCG
		}
		
						
							Pass	//림라이팅 외곽선 패스
					{
				//Cull Front
						Tags { "Queue" = "Geometry+1" }
				CGPROGRAM
				#pragma vertex vert
				#pragma fragment frag

				sampler2D _MainTex;
				float outLineWidth;
				half4 outLineColor;
				float rimLightToggle;
				float4 rimLightColor;
				float rimLightRange;

				struct vi {
				float4 vertex  :POSITION;
				float3 normal  :NORMAL;
				float2 uv : TEXCOORD0;
				};
				struct vo {
					float4 vertex :POSITION;
					float3 normal : NORMAL;
					float2 uv : TEXCOORD0;
					float4 wVertex : TEXCOORD1;
					float4 oVertex :TEXCOORD2;
				};

				vo vert(vi input) 
				{
					vo o;
					o.vertex = UnityObjectToClipPos(input.vertex);
					o.normal = input.normal;
					o.uv = input.uv;
					o.wVertex =mul(unity_ObjectToWorld, input.vertex);
					o.oVertex = input.vertex;
					return o;
				}

							fixed4 frag(vo o) : COLOR
							{
								
								half4 texColor = tex2D(_MainTex, o.uv);
								if (!rimLightToggle) return texColor;
								float3 fwPos = _WorldSpaceCameraPos - mul(unity_ObjectToWorld, o.oVertex);

								float3 CameraV = normalize( _WorldSpaceCameraPos - o.wVertex);
								//CameraV = normalize(fwPos);

								float dotCN = dot(CameraV,normalize( mul(unity_ObjectToWorld, o.normal)));

								if (max(0, dotCN) > rimLightRange) return texColor;

								return lerp(rimLightColor, texColor, max(0,dotCN) / rimLightRange);

							}
							ENDCG

					}

					
					/*
					
		Pass
		{
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			// make fog work
			#pragma multi_compile_fog
			
			#include "UnityCG.cginc"

			struct appdata
			{
				float4 vertex : POSITION;
				float2 uv : TEXCOORD0;
			};

			struct v2f
			{
				float2 uv : TEXCOORD0;
				UNITY_FOG_COORDS(1)
				float4 vertex : SV_POSITION;
			};

			sampler2D _MainTex;
			float4 _MainTex_ST;
			
			v2f vert (appdata v)
			{
				v2f o;
				o.vertex = UnityObjectToClipPos(v.vertex);
				o.uv = TRANSFORM_TEX(v.uv, _MainTex);
				UNITY_TRANSFER_FOG(o,o.vertex);
				return o;
			}
			
			fixed4 frag (v2f i) : SV_Target
			{
				// sample the texture
				fixed4 col = tex2D(_MainTex, i.uv);
				// apply fog
				UNITY_APPLY_FOG(i.fogCoord, col);
				return col;
			}
			ENDCG
		}
		*/
		
		}
		FallBack "Mobile/Diffuse"
}
