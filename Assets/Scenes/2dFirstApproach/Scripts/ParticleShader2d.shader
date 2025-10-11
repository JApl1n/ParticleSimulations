Shader "Custom/2DParticleRender"
{
    SubShader
    {
        Tags { "Queue" = "Geometry" }
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            StructuredBuffer<float3> _particlePositions;
            StructuredBuffer<float3> _particleVelocities;

            float _maxSpeed;

			float3 colour;

            struct appdata
            {
                float4 vertex : POSITION;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                fixed4 colour : colour0;
            };

            v2f vert(appdata v, uint instanceID : SV_InstanceID)
            {
                v2f o;
                float3 pos = _particlePositions[instanceID];
                float4 worldPos = float4(v.vertex.xyz + pos, 1);
                o.pos = mul(UNITY_MATRIX_VP, worldPos);

                float speed = length(_particleVelocities[instanceID]);
                float speedT = saturate(speed / _maxSpeed);
                o.colour = float4(speedT, 0, 1-speedT, 1);
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                return i.colour;
            }
            ENDCG
        }
    }
}