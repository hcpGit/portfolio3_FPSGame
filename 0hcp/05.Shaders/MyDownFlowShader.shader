Shader "Custom/MyDownFlowShader"
{
	Properties
	{
		_MainTex ("Texture", 2D) = "white" {}
		flowVector("flowVector",Vector)=(1,1,1,1)
			scaleSphere("scaleSphere",Float)=1
	}
	SubShader
	{
		Tags { "RenderType"="Opaque" "Queue"="Transparent+1" }
	//	LOD 100
			Blend SrcAlpha OneMinusSrcAlpha
			Cull Off

		Pass
		{
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag

			struct vi
			{
				float4 vertex : POSITION;
				float2 uv : TEXCOORD0;
			};

			struct v2f
			{
				float2 uv : TEXCOORD0;
				float4 vertex : SV_POSITION;
			};

			sampler2D _MainTex;
			float4 flowVector;
			float4 _MainTex_ST;
			float scaleSphere;
			
			v2f vert (vi i)
			{
				v2f o;
				o.vertex = UnityObjectToClipPos(i.vertex* scaleSphere);
				o.uv = i.uv;
				return o;
			}
			
			fixed4 frag (v2f i) : SV_Target
			{
				float2 stuv = i.uv * _MainTex_ST.xy;
				float2 uv = stuv + flowVector.xy*_Time.y;
				fixed4 col = tex2D(_MainTex, uv);
				// apply fog
				return col;
			}
			ENDCG
		}
	}
}
