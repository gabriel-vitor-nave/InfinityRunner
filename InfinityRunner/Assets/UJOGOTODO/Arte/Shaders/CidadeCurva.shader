Shader "TetiCorre/Cidade Curva"
{
    Properties
    {
        _MainTex ("Textura", 2D) = "white" {}
        _Color ("Cor", Color) = (1,1,1,1)
        [HDR] _EmissionColor ("Luz própria", Color) = (0,0,0,0)
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 150
        CGPROGRAM
        #pragma surface surf Lambert vertex:vert addshadow fullforwardshadows
        #pragma target 3.0
        #pragma multi_compile_instancing
        sampler2D _MainTex;
        fixed4 _Color;
        half3 _EmissionColor;
        float _TetiZ, _TetiInicioCurva, _TetiForcaCurva, _TetiNoite, _TetiSentidoCurva;
        struct Input { float2 uv_MainTex; };
        void vert(inout appdata_full v)
        {
            float3 mundo = mul(unity_ObjectToWorld, v.vertex).xyz;
            float distancia = max(0, (mundo.z - _TetiZ) * _TetiSentidoCurva - _TetiInicioCurva);
            mundo.y -= distancia * distancia * _TetiForcaCurva;
            v.vertex = mul(unity_WorldToObject, float4(mundo, 1));
            float3 normal = UnityObjectToWorldNormal(v.normal);
            normal.z += 2 * distancia * _TetiForcaCurva * _TetiSentidoCurva * normal.y;
            v.normal = mul((float3x3)transpose(unity_ObjectToWorld), normalize(normal));
        }
        void surf(Input IN, inout SurfaceOutput o)
        {
            fixed4 cor = tex2D(_MainTex, IN.uv_MainTex) * _Color;
            o.Albedo = cor.rgb;
            o.Alpha = cor.a;
            o.Emission = _EmissionColor * _TetiNoite;
        }
        ENDCG
    }
    Fallback "Diffuse"
}
