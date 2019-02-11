// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

Shader "Custom/MyDiffuseDissolveShader" {

	/*
	나중에 꼭 분석해봐~~~~~
	
	
	*/
	Properties
	{
		_MainTex("Texture", 2D) = "white" {}
		_NoiseTex("Texture", 2D) = "white" {}
		[MaterialToggle] PixelSnap("Pixel snap", Float) = 0
		_EdgeColour1("Edge colour 1", Color) = (1.0, 1.0, 1.0, 1.0)
		_EdgeColour2("Edge colour 2", Color) = (1.0, 1.0, 1.0, 1.0)
		_Level("Dissolution level", Range(0.0, 1.0)) = 0.1
		_Edges("Edge width", Range(0.0, 1.0)) = 0.1
			TempColor("tempCol",Color) = (1,1,1,1)
	}
		SubShader
		{
			Tags {
			"Queue" = "Transparent" 
			"RenderType" = "Transparent" 
		//	"LightMode" = "ForwardBase"
		}
			LOD 100

			Pass
			{
				Blend One Zero
				//Blend SrcAlpha OneMinusSrcAlpha


			//	Cull Off
		//	Lighting Off
				//ZWrite Off	//이걸 끄면 ㅈ당연히 z 버퍼 내용이 없으니까 위에 싸고 있는 것 플레이가 안되는데 왜 이걸 원 제작자는 껐는지 모르겠음.
		//	Fog { Mode Off }
				
				CGPROGRAM
				#pragma vertex vert
				#pragma fragment frag
			// make fog work
		//	#pragma multi_compile DUMMY PIXELSNAP_ON

			#include "UnityCG.cginc"

			struct appdata
			{
				float4 vertex : POSITION;
				float2 uv : TEXCOORD0;
				float3 normal : NORMAL;
			};

			struct v2f
			{
				float2 uv : TEXCOORD0;
				float4 vertex : SV_POSITION;
				float3 normal : NORMAL;
				float4 localPos : TEXCOORD4;
			};

			sampler2D _MainTex;
			sampler2D _NoiseTex;
			float4 _EdgeColour1;
			float4 _EdgeColour2;
			float _Level;
			float _Edges;
			float4 _MainTex_ST;

			half4 _LightColor0;//아마도 전역 조명의 빛색까리.
			half4 TempColor;


			v2f vert(appdata v)
			{
				v2f o;
				o.vertex = UnityObjectToClipPos(v.vertex);
				o.uv = TRANSFORM_TEX(v.uv, _MainTex);
				o.normal = v.normal;
				o.localPos = v.vertex;

				#ifdef PIXELSNAP_ON
				o.vertex = UnityPixelSnap(o.vertex);
				#endif

				return o;
			}

			float4 frag(v2f i) : COLOR
			{
				// sample the texture
				float cutout = tex2D(_NoiseTex, i.uv).r;
				fixed4 col = tex2D(_MainTex, i.uv);
				
				if (cutout < _Level)
					discard;

				if (cutout < col.a && cutout < _Level + _Edges)
				{
					col = lerp(_EdgeColour1, _EdgeColour2, (cutout - _Level) / _Edges);

				}
				else
				{
					float3 lightVNormalize = normalize(
					_WorldSpaceCameraPos	//이걸 대신 주면 라이팅 벡터가 카메라 벡터가 되어서, 늘 빛을 정면으로 맞는 쉐이더가 탄생함
						//_WorldSpaceLightPos0
						//지금 월드스페이스라이트 포스 쪽에 문제가 생긴건지 어쩐건지 영 라이트 위치를 이상하게 받아오고 있어.
						//라이팅 위치만 문제가 되는 상환이니까 아예 빛 월드 포스를 하나 딱 둬버리는 것도 나쁘지 않을듯.

						- mul(unity_ObjectToWorld, i.localPos));	//월드공간 라이팅 벡터 노말라이즈
					float3 wnV = mul(unity_ObjectToWorld, i.normal);
					float3 nvNormalize = normalize(
						wnV
					);	//월드공간 노말벡터 노말라이즈.

					/*
					float3 camVNormalize = normalize(_WorldSpaceCameraPos - mul(unity_ObjectToWorld, vertexed.localPos)); //월드공간 카메라 벡터 노말라이즈.
					float3 reflectVNormalize = normalize(lightVNormalize + camVNormalize);	//라이팅 벡터랑 카메라 벡터의 반각벡터 노말라이즈
					*/

					float dotLN = dot(nvNormalize, lightVNormalize);
					//float facing = (dotLN <= 0) ? 0 : 1;

					//float4 textureColor = tex2D(_MainTex, vertexed.uv.xy);

					//float4 diffuseColor = LightColor * max(0, dotLN);	//디퓨즈 컬러

					/*float4 specColor = SpecularColor *
						facing *
						pow(max(0, dot(nvNormalize, reflectVNormalize)), SpecularShiness)
						*(1.0 - textureColor.a)	//이게 글로스 매핑 이라고 부를 수 있는 값임. 텍스쳐의 알파값을 통해 스펙큘러의 정도를 때리는 것임.
						;

					return (textureColor * diffuseColor * DiffuseAmount) + (textureColor *AmbientColor * AmbientAmount) + specColor;
					*/

					col =col * max(0, dotLN)
						* _LightColor0*2 ;

					//col = tex2D(_MainTex, i.uv);
				}
					

				return col;
			}
			ENDCG
			}
		}
}