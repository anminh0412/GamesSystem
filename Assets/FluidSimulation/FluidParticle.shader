Shader "Custom/FluidParticle"
{
    Properties
    {
        _Color ("Particle Deep Color", Color) = (0.0, 0.3, 0.8, 1)
        _FastColor ("Particle Fast Color", Color) = (0.4, 0.8, 1.0, 1)
        _PointSize ("Point Size", Float) = 15.0
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Geometry" }
        LOD 100

        Pass
        {
            Blend SrcAlpha OneMinusSrcAlpha // Alpha blending for slight transparency
            
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct Particle
            {
                float3 position;
                float3 velocity;
                float3 force;
                float density;
                float pressure;
            };

            StructuredBuffer<Particle> _Particles;

            float4 _Color;
            float4 _FastColor;
            float _PointSize;

            struct appdata
            {
                uint vertexID : SV_VertexID;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float size : PSIZE;
                float speed : TEXCOORD0; // To mix color based on speed
            };

            v2f vert (appdata v)
            {
                v2f o;
                Particle p = _Particles[v.vertexID];
                
                // Convert world position from buffer to clip space
                o.pos = UnityObjectToClipPos(float4(p.position, 1.0));
                
                // Point size shrinks as it moves further away from camera
                // Note: PSIZE behavior depends on the graphics API, this may work slightly differently on D3D11 vs OpenGL
                o.size = _PointSize / o.pos.w * 50.0; 
                
                o.speed = length(p.velocity);

                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                // Simple color lerp based on particle speed
                float speedFactor = saturate(i.speed / 10.0);
                fixed4 col = lerp(_Color, _FastColor, speedFactor);
                
                // Make particle circular instead of square
                float2 coord = i.pos.xy / _ScreenParams.xy; 
                // Note: accurate perfect circles in Points topology require SV_Target coordinate tricks,
                // but for simplicity we'll just render the solid point color here.
                
                return col;
            }
            ENDCG
        }
    }
}
