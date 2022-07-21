// https://www.shadertoy.com/view/4djSRW
//
// The input of rand() is any float, and the output is in (0, 1)
// float works on Android, but half does not work

#define MAGIC1 0.1009
#define MAGIC2 0.1013
#define MAGIC3 0.1019
#define MAGIC4 0.1021
#define MAGIC5 33.33

float rand(float p)
{
    p = frac(p * MAGIC1);
    p *= p + MAGIC5;
    p *= p + p;
    return frac(p);
}

float rand(float2 p2)
{
    float3 p3 = frac(p2.xyx * MAGIC1);
    p3 += dot(p3, p3.yzx + MAGIC5);
    return frac((p3.x + p3.y) * p3.z);
}

float rand(float3 p3)
{
    p3 = frac(p3 * MAGIC1);
    p3 += dot(p3, p3.yzx + MAGIC5);
    return frac((p3.x + p3.y) * p3.z);
}

float2 rand2(float p)
{
    float3 p3 = frac(float3(p, p, p) * float3(MAGIC1, MAGIC2, MAGIC3));
    p3 += dot(p3, p3.yzx + MAGIC5);
    return frac((p3.xx + p3.yz) * p3.zy);
}

float2 rand2(float2 p2)
{
    float3 p3 = frac(p2.xyx * float3(MAGIC1, MAGIC2, MAGIC3));
    p3 += dot(p3, p3.yzx + MAGIC5);
    return frac((p3.xx + p3.yz) * p3.zy);
}

float2 rand2(float3 p3)
{
    p3 = frac(p3 * float3(MAGIC1, MAGIC2, MAGIC3));
    p3 += dot(p3, p3.yzx + MAGIC5);
    return frac((p3.xx + p3.yz) * p3.zy);
}

float3 rand3(float p)
{
   float3 p3 = frac(float3(p, p, p) * float3(MAGIC1, MAGIC2, MAGIC3));
   p3 += dot(p3, p3.yzx + MAGIC5);
   return frac((p3.xxy + p3.yzz) * p3.zyx);
}

float3 rand3(float2 p2)
{
    float3 p3 = frac(p2.xyx * float3(MAGIC1, MAGIC2, MAGIC3));
    p3 += dot(p3, p3.yzx + MAGIC5);
    return frac((p3.xxy + p3.yzz) * p3.zyx);
}

float3 rand3(float3 p3)
{
    p3 = frac(p3 * float3(MAGIC1, MAGIC2, MAGIC3));
    p3 += dot(p3, p3.yzx + MAGIC5);
    return frac((p3.xxy + p3.yzz) * p3.zyx);
}

float4 rand4(float p)
{
    float4 p4 = frac(float4(p, p, p, p) * float4(MAGIC1, MAGIC2, MAGIC3, MAGIC4));
    p4 += dot(p4, p4.wzxy + MAGIC5);
    return frac((p4.xxyz + p4.yzzw) * p4.zywx);
}

float4 rand4(float2 p2)
{
    float4 p4 = frac(p2.xyxy * float4(MAGIC1, MAGIC2, MAGIC3, MAGIC4));
    p4 += dot(p4, p4.wzxy + MAGIC5);
    return frac((p4.xxyz + p4.yzzw) * p4.zywx);
}

float4 rand4(float3 p3)
{
    float4 p4 = frac(p3.xyzx * float4(MAGIC1, MAGIC2, MAGIC3, MAGIC4));
    p4 += dot(p4, p4.wzxy + MAGIC5);
    return frac((p4.xxyz + p4.yzzw) * p4.zywx);
}

float4 rand4(float4 p4)
{
    p4 = frac(p4 * float4(MAGIC1, MAGIC2, MAGIC3, MAGIC4));
    p4 += dot(p4, p4.wzxy + MAGIC5);
    return frac((p4.xxyz + p4.yzzw) * p4.zywx);
}

#undef MAGIC1
#undef MAGIC2
#undef MAGIC3
#undef MAGIC4
#undef MAGIC5

float noise(float v)
{
    float a = floor(v);
    float u = v - a;
    u = u * u * (3.0 - 2.0 * u);
    return lerp(rand(a), rand(a + 1.0), u);
}

float noise(float2 v)
{
    float2 a = floor(v);
    float2 u = v - a;
    u = u * u * (3.0 - 2.0 * u);
    return lerp(
        lerp(rand(a), rand(a + float2(1.0, 0.0)), u.x),
        lerp(rand(a + float2(0.0, 1.0)), rand(a + float2(1.0, 1.0)), u.x),
        u.y
    );
}

float noise(float3 v)
{
    float3 a = floor(v);
    float3 u = v - a;
    u = u * u * (3.0 - 2.0 * u);
    return lerp(
        lerp(
            lerp(rand(a), rand(a + float3(1.0, 0.0, 0.0)), u.x),
            lerp(rand(a + float3(0.0, 1.0, 0.0)), rand(a + float3(1.0, 1.0, 0.0)), u.x),
            u.y
        ),
        lerp(
            lerp(rand(a + float3(0.0, 0.0, 1.0)), rand(a + float3(1.0, 0.0, 1.0)), u.x),
            lerp(rand(a + float3(0.0, 1.0, 1.0)), rand(a + float3(1.0, 1.0, 1.0)), u.x),
            u.y
        ),
        u.z
    );
}

float2 noise2(float v)
{
    float a = floor(v);
    float u = v - a;
    u = u * u * (3.0 - 2.0 * u);
    return lerp(rand2(a), rand2(a + 1.0), u);
}

float2 noise2(float2 v)
{
    float2 a = floor(v);
    float2 u = v - a;
    u = u * u * (3.0 - 2.0 * u);
    return lerp(
        lerp(rand2(a), rand2(a + float2(1.0, 0.0)), u.x),
        lerp(rand2(a + float2(0.0, 1.0)), rand2(a + float2(1.0, 1.0)), u.x),
        u.y
    );
}

float2 noise2(float3 v)
{
    float3 a = floor(v);
    float3 u = v - a;
    u = u * u * (3.0 - 2.0 * u);
    return lerp(
        lerp(
            lerp(rand2(a), rand2(a + float3(1.0, 0.0, 0.0)), u.x),
            lerp(rand2(a + float3(0.0, 1.0, 0.0)), rand2(a + float3(1.0, 1.0, 0.0)), u.x),
            u.y
        ),
        lerp(
            lerp(rand2(a + float3(0.0, 0.0, 1.0)), rand2(a + float3(1.0, 0.0, 1.0)), u.x),
            lerp(rand2(a + float3(0.0, 1.0, 1.0)), rand2(a + float3(1.0, 1.0, 1.0)), u.x),
            u.y
        ),
        u.z
    );
}

float3 noise3(float v)
{
    float a = floor(v);
    float u = v - a;
    u = u * u * (3.0 - 2.0 * u);
    return lerp(rand3(a), rand3(a + 1.0), u);
}

float3 noise3(float2 v)
{
    float2 a = floor(v);
    float2 u = v - a;
    u = u * u * (3.0 - 2.0 * u);
    return lerp(
        lerp(rand3(a), rand3(a + float2(1.0, 0.0)), u.x),
        lerp(rand3(a + float2(0.0, 1.0)), rand3(a + float2(1.0, 1.0)), u.x),
        u.y
    );
}

float3 noise3(float3 v)
{
    float3 a = floor(v);
    float3 u = v - a;
    u = u * u * (3.0 - 2.0 * u);
    return lerp(
        lerp(
            lerp(rand3(a), rand3(a + float3(1.0, 0.0, 0.0)), u.x),
            lerp(rand3(a + float3(0.0, 1.0, 0.0)), rand3(a + float3(1.0, 1.0, 0.0)), u.x),
            u.y
        ),
        lerp(
            lerp(rand3(a + float3(0.0, 0.0, 1.0)), rand3(a + float3(1.0, 0.0, 1.0)), u.x),
            lerp(rand3(a + float3(0.0, 1.0, 1.0)), rand3(a + float3(1.0, 1.0, 1.0)), u.x),
            u.y
        ),
        u.z
    );
}

#define srand(v) (rand(v) * 2.0 - 1.0)
#define srand2(v) (rand2(v) * 2.0 - 1.0)
#define srand3(v) (rand3(v) * 2.0 - 1.0)
#define srand4(v) (rand4(v) * 2.0 - 1.0)
#define snoise(v) (noise(v) * 2.0 - 1.0)
#define snoise2(v) (noise2(v) * 2.0 - 1.0)
#define snoise3(v) (noise3(v) * 2.0 - 1.0)
