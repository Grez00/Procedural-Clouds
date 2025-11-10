// Upgrade NOTE: commented out 'float4x4 _CameraToWorld', a built-in variable
// Upgrade NOTE: replaced '_CameraToWorld' with 'unity_CameraToWorld'

Shader "Custom/CloudShader"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 100

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
                float4 worldpos : TEXCOORD1;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;

            sampler2D _CameraDepthTexture;

            float3 _BoxCenter;
            float3 _BoxExtents;
            float3 _BoxX;
            float3 _BoxY;
            float3 _BoxZ;

            struct Ray{
                float3 origin;
                float3 direction;
            };
            Ray CreateRay(float3 origin, float3 direction){
                Ray ray;
                ray.origin = origin;
                ray.direction = direction;
                return ray;
            }

            struct Plane{
                float3 normal;
                float3 p;
            };
            Plane CreatePlane(float3 normal, float3 p){
                Plane plane;
                plane.normal = normal;
                plane.p = p;
                return plane;
            }

            struct Axes{
                float3 x;
                float3 y;
                float3 z;
            };
            struct OBB{
                float3 center;
                float3 extents;
                Axes axes;
            };
            Axes CreateAxes(float3 x, float3 y, float3 z){
                Axes axes;
                axes.x = x;
                axes.y = y;
                axes.z = z;
                return axes;
            }
            OBB CreateOBB(float3 center, float3 extents, Axes axes){
                OBB obb;
                obb.center = center;
                obb.extents = extents;
                obb.axes = axes;
                return obb;
            }

            float3 ToLocalCoords(Axes axes, float3 center, float3 worldpos){
                float3 adjusted_pos = worldpos - center;
                return float3(
                    dot(adjusted_pos, axes.x),
                    dot(adjusted_pos, axes.y),
                    dot(adjusted_pos, axes.z)
                );
            }
            float3 DirToLocalCoords(Axes axes, float3 worlddir){
                return float3(
                    dot(worlddir, axes.x),
                    dot(worlddir, axes.y),
                    dot(worlddir, axes.z)
                );
            }
            float3 ToWorldCoords(Axes axes, float3 center, float3 localpos){
                return center + 
                (localpos.x * axes.x) +
                (localpos.y * axes.y) +
                (localpos.z * axes.z);
            }

            bool IntersectPlane(Ray ray, Plane plane, inout float3 hitpoint){
                float dir_normal = dot(ray.direction, plane.normal);
                if (dir_normal < 0.00001 && dir_normal > -0.00001){
                    return false;
                }
                float t = dot(plane.normal, plane.p - ray.origin) / dir_normal;
                if (t < 0) return false;

                hitpoint = ray.origin + ray.direction * t;
                return true;
            }
            bool IntersectGround(Ray ray){
                float t = -ray.origin.y / ray.direction.y;
                return t >= 0;
            }

            bool IntersectOBB(Ray ray, OBB obb, inout float3 entrypoint, inout float3 exitpoint){
                float tmin = 0.0;
                float tmax = 1.#INF;

                float3 p = ToLocalCoords(obb.axes, obb.center, ray.origin);
                float3 d = DirToLocalCoords(obb.axes, ray.direction);

                float3 min = -obb.extents;
                float3 max = obb.extents;

                for (int i = 0; i < 3; i++){
                    if (abs(d[i]) < 0.000001)
                    {
                        if (p[i] < min[i] || p[i] > max[i]) return false;
                    }
                    else{
                        float t1 = (min[i] - p[i]) / d[i];
                        float t2 = (max[i] - p[i]) / d[i];

                        if (t1 > t2){
                            float temp = t1;
                            t1 = t2;
                            t2 = temp;
                        }

                        if (t1 > tmin) tmin = t1;
                        if (t2 < tmax) tmax = t2;

                        if (tmin > tmax) return false;
                    }
                }

                float3 localentry = p + d * tmin;
                float3 localexit = p + d * tmax;

                entrypoint = ToWorldCoords(obb.axes, obb.center, localentry);
                exitpoint = ToWorldCoords(obb.axes, obb.center, localexit);

                return true;
            }

            v2f vert (appdata v)
            {
                v2f o;
                o.worldpos = mul(unity_ObjectToWorld, v.vertex);
                o.vertex = mul(UNITY_MATRIX_VP, o.worldpos);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                UNITY_TRANSFER_FOG(o,o.vertex);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                // Create uv for camera ray
                float2 cam_uv = float2(i.uv.x, 1.0 - i.uv.y);
                cam_uv = (cam_uv * 2.0) - 1.0;

                // Create origin and direction of camera ray
                float3 origin = mul(unity_CameraToWorld, float4(0.0, 0.0, 0.0, 1.0)).xyz;
                float3 direction = mul(unity_CameraInvProjection, float4(cam_uv, 0.0, 1.0)).xyz;
                direction = mul(unity_CameraToWorld, float4(direction, 0.0)).xyz;
                direction = -normalize(direction);

                // Create camera ray, ground plane, and obb
                Ray ray = CreateRay(origin, direction);
                Plane ground_plane = CreatePlane(float3(0.0, 1.0, 0.0), float3(0.0, 0.0, 0.0));
                Axes axes = CreateAxes(_BoxX, _BoxY, _BoxZ);
                OBB obb = CreateOBB(_BoxCenter, _BoxExtents, axes);

                // sample the texture
                fixed4 col = tex2D(_MainTex, i.uv);

                // sample the depth
                fixed4 rawdepth = tex2D(_CameraDepthTexture, i.uv);
                float lineardepth = Linear01Depth(rawdepth.r);
                float depth = lineardepth * _ProjectionParams.z;

                // test collisions
                float3 hitpoint = float3(99999, 99999, 99999);
                if (IntersectPlane(ray, ground_plane, hitpoint) && depth > length(hitpoint - origin)){
                    col.rgb = ray.direction;
                }
                float3 entrypoint = float3(99999, 99999, 99999);
                float3 exitpoint = float3(99999, 99999, 99999);
                if (IntersectOBB(ray, obb, entrypoint, exitpoint) && depth > length(entrypoint - origin) && length(hitpoint - origin) > length(entrypoint - origin)){
                    col.rgb = float3(i.uv, 0.5);
                }

                // apply fog
                UNITY_APPLY_FOG(i.fogCoord, col);

                return col;
            }
            ENDCG
        }
    }
}
