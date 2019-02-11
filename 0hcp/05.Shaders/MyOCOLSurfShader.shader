// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

Shader "Custom/MyOCOLSurfShader" {
	Properties {
		_Color ("Color", Color) = (0,0,0,1)
		_MainTex ("Albedo (RGB)", 2D) = "white" {}
		_Glossiness("Smoothness", Range(0,1)) = 0.5
		_Metallic("Metallic", Range(0,1)) = 0.0
	}
		SubShader
		{
			Tags {"Queue" = "Geometry" "RenderType" = "Opaque" }
			LOD 200
			/*
				Pass{
			Name "Phantom / Occlude Shader"
			CGPROGRAM
	#pragma vertex vert
	#pragma fragment frag
				fixed4  _Color;
		float4 vert(float4 pos : POSITION)	: SV_POSITION
		{
			return UnityObjectToClipPos(pos);
		}

		fixed4 frag(float4 pos : POSITION) : COLOR
		{
			return _Color;
		}

		ENDCG
		}
		*/
			/*
			Pass{
		Name "OutLine / Rim Shader"
		CGPROGRAM
		#pragma vertex vert
#pragma fragment frag

			struct vi {
			float4 pos : POSITION;
			half4 color : COLOR;

};
		struct vo {
			float4 pos : POSITION;
			half4 color : COLOR;
		};

			vo vert(vi i) 
		{
				vo o;
			//	o.pos = i.pos;
			o.pos = UnityObjectToClipPos(i.pos);
			o.color = i.color;
			return o;
		}

		float4 frag(vo o) : COLOR
		{
			return float4(o.color.r,0,0,1);
		}
			ENDCG
		
		}
			*/
			/*
			안돼에에[에ㅔ에에에에에에에ㅐㅔ에엥
			
			*/
		/*
			
		CGPROGRAM
		// Physically based Standard lighting model, and enable shadows on all light types
		#pragma surface surf Standard fullforwardshadows

		// Use shader model 3.0 target, to get nicer looking lighting
		#pragma target 3.0

		sampler2D _MainTex;

		struct Input {
			fixed4 vfDColor : COLOR;
			//float2 uv_MainTex;
			
		};

		half _Glossiness;
		half _Metallic;
		fixed4 _Color;

		// Add instancing support for this shader. You need to check 'Enable Instancing' on materials that use the shader.
		// See https://docs.unity3d.com/Manual/GPUInstancing.html for more information about instancing.
		// #pragma instancing_options assumeuniformscaling
	//	UNITY_INSTANCING_BUFFER_START(Props)
			// put more per-instance properties here
	//	UNITY_INSTANCING_BUFFER_END(Props)

		void surf (Input IN, inout SurfaceOutputStandard o) {
			// Albedo comes from a texture tinted by color
		//	fixed4 c = tex2D (_MainTex, IN.uv_MainTex) * _Color;
		//	o.Albedo = c.rgb;
			// Metallic and smoothness come from slider variables
		//	o.Metallic = _Metallic;
		//	o.Smoothness = _Glossiness;
			//o.Alpha = c.a;

			o.Albedo =float3( IN.vfDColor.x, 
				IN.vfDColor.y,
				//0,
				IN.vfDColor.z);
		}
		ENDCG
		
			*/

			Pass{

	 CGPROGRAM
	 #pragma vertex vert
	 #pragma fragment frag

	 struct vertexInput
	 {
	   float4 vertex : POSITION;
	   float4 color : COLOR;
	 };

	 struct vertexOutput
	 {
	   float4 pos : POSITION;
	   float4 color : COLOR;
	   };

	 vertexOutput vert(vertexInput v) {
	   vertexOutput o;
	   o.pos = UnityObjectToClipPos(v.vertex);
	   o.color = v.color;
	   return o;
	 }

	 fixed4 frag(vertexOutput i) : COLOR {
	   return fixed4(0.2, 0, 0, 1);
	 }

	 ENDCG
		}

			////////////////////////////////////////////////////////
			//Surface Shader  - GREEN                              //
			////////////////////////////////////////////////////////

		 CGPROGRAM
#pragma target 3.0
#pragma surface surf BlinnPhong

		 struct Input {
		 float4 color : COLOR;
	 };

	 void surf(Input IN, inout SurfaceOutput o) {
		 o.Albedo = float3(IN.color.r, 0, 1);
	 }

	 ENDCG
  

		
		}
	FallBack "Diffuse"
}
