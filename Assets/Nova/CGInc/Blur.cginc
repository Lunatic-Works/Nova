// 2D Gauss–Legendre quadrature over disk
#define DO_GAUSSIAN_BLUR \
    sum += GRAB_PIXEL(WEIGHT1,  0.00000000,  0.00000000); \
    sum += GRAB_PIXEL(WEIGHT2, -0.15678323, +0.58512298); \
    sum += GRAB_PIXEL(WEIGHT2, -0.58512298, -0.15678323); \
    sum += GRAB_PIXEL(WEIGHT2, +0.15678323, -0.58512298); \
    sum += GRAB_PIXEL(WEIGHT2, +0.58512298, +0.15678323); \
    sum += GRAB_PIXEL(WEIGHT3, +0.96793474, +0.74272245); \
    sum += GRAB_PIXEL(WEIGHT3, +0.15924914, +1.20961730); \
    sum += GRAB_PIXEL(WEIGHT3, -0.74272245, +0.96793474); \
    sum += GRAB_PIXEL(WEIGHT3, -1.20961730, +0.15924914); \
    sum += GRAB_PIXEL(WEIGHT3, -0.96793474, -0.74272245); \
    sum += GRAB_PIXEL(WEIGHT3, -0.15924914, -1.20961730); \
    sum += GRAB_PIXEL(WEIGHT3, +0.74272245, -0.96793474); \
    sum += GRAB_PIXEL(WEIGHT3, +1.20961730, -0.15924914); \
    sum += GRAB_PIXEL(WEIGHT4, +1.60483830, +0.92655383); \
    sum += GRAB_PIXEL(WEIGHT4, +0.92655383, +1.60483830); \
    sum += GRAB_PIXEL(WEIGHT4,  0.00000000, +1.85310765); \
    sum += GRAB_PIXEL(WEIGHT4, -0.92655383, +1.60483830); \
    sum += GRAB_PIXEL(WEIGHT4, -1.60483830, +0.92655383); \
    sum += GRAB_PIXEL(WEIGHT4, -1.85310765,  0.00000000); \
    sum += GRAB_PIXEL(WEIGHT4, -1.60483830, -0.92655383); \
    sum += GRAB_PIXEL(WEIGHT4, -0.92655383, -1.60483830); \
    sum += GRAB_PIXEL(WEIGHT4,  0.00000000, -1.85310765); \
    sum += GRAB_PIXEL(WEIGHT4, +0.92655383, -1.60483830); \
    sum += GRAB_PIXEL(WEIGHT4, +1.60483830, -0.92655383); \
    sum += GRAB_PIXEL(WEIGHT4, +1.85310765,  0.00000000); \

#define DO_CHEAP_GAUSSIAN_BLUR \
    sum += GRAB_PIXEL(WEIGHT1,  0.00000000,  0.00000000); \
    sum += GRAB_PIXEL(WEIGHT2, +1.40211477, +0.18459191); \
    sum += GRAB_PIXEL(WEIGHT2, -1.40211477, -0.18459191); \
    sum += GRAB_PIXEL(WEIGHT2, +0.54119610, +1.30656296); \
    sum += GRAB_PIXEL(WEIGHT2, +0.86091867, -1.12197105); \
    sum += GRAB_PIXEL(WEIGHT2, -0.86091867, +1.12197105); \
    sum += GRAB_PIXEL(WEIGHT2, -0.54119610, -1.30656296); \

#define DO_MOTION_BLUR \
    sum += GRAB_PIXEL(WEIGHT, -1.00); \
    sum += GRAB_PIXEL(WEIGHT, -0.75); \
    sum += GRAB_PIXEL(WEIGHT, -0.50); \
    sum += GRAB_PIXEL(WEIGHT, -0.25); \
    sum += GRAB_PIXEL(WEIGHT,  0.00); \
    sum += GRAB_PIXEL(WEIGHT, +0.25); \
    sum += GRAB_PIXEL(WEIGHT, +0.50); \
    sum += GRAB_PIXEL(WEIGHT, +0.75); \
    sum += GRAB_PIXEL(WEIGHT, +1.00); \

half4 tex2DGaussianBlur(sampler2D tex, half4 texelSize, half2 uv, half size)
{
    half4 sum = half4(0.0, 0.0, 0.0, 0.0);
    half2 kernelSize = texelSize.xy * size;

    #define WEIGHT1 0.09722528
    #define WEIGHT2 0.08092767
    #define WEIGHT3 0.04619001
    #define WEIGHT4 0.01746199
    #define GRAB_PIXEL(weight, kernelX, kernelY) ((weight) * tex2D(tex, uv + half2((kernelX), (kernelY)) * kernelSize))

    DO_GAUSSIAN_BLUR

    #undef WEIGHT1
    #undef WEIGHT2
    #undef WEIGHT3
    #undef WEIGHT4
    #undef GRAB_PIXEL

    return sum;
}

half4 tex2DCheapGaussianBlur(sampler2D tex, half4 texelSize, half2 uv, half size)
{
    half4 sum = half4(0.0, 0.0, 0.0, 0.0);
    half2 kernelSize = texelSize.xy * size;

    #define WEIGHT1 0.5
    #define WEIGHT2 (0.5 / 6.0)
    #define GRAB_PIXEL(weight, kernelX, kernelY) ((weight) * tex2D(tex, uv + half2((kernelX), (kernelY)) * kernelSize))

    DO_CHEAP_GAUSSIAN_BLUR

    #undef WEIGHT1
    #undef WEIGHT2
    #undef GRAB_PIXEL

    return sum;
}

half4 tex2DLensBlur(sampler2D tex, half4 texelSize, half2 uv, half size)
{
    half4 sum = half4(0.0, 0.0, 0.0, 0.0);
    half2 kernelSize = texelSize.xy * size;

    #define WEIGHT1 (0.2 / 13.0)
    #define WEIGHT2 WEIGHT1
    #define WEIGHT3 WEIGHT1
    #define WEIGHT4 (0.8 / 12.0)
    #define GRAB_PIXEL(weight, kernelX, kernelY) ((weight) * tex2D(tex, uv + half2((kernelX), (kernelY)) * kernelSize))

    DO_GAUSSIAN_BLUR

    #undef WEIGHT1
    #undef WEIGHT2
    #undef WEIGHT3
    #undef WEIGHT4
    #undef GRAB_PIXEL

    return sum;
}

half4 tex2DMotionBlur(sampler2D tex, half4 texelSize, half2 uv, half2 vel)
{
    half4 sum = half4(0.0, 0.0, 0.0, 0.0);
    half2 kernelSize = texelSize.xy * vel;

    #define WEIGHT (1.0 / 9.0)
    #define GRAB_PIXEL(weight, lambda) ((weight) * tex2D(tex, uv + (lambda) * kernelSize))

    DO_MOTION_BLUR

    #undef WEIGHT
    #undef GRAB_PIXEL

    return sum;
}

half4 tex2DProjGaussianBlur(sampler2D tex, half4 texelSize, half4 pos, half size)
{
    half4 sum = half4(0.0, 0.0, 0.0, 0.0);
    half2 kernelSize = texelSize.xy * size;

    #define WEIGHT1 0.09722528
    #define WEIGHT2 0.08092767
    #define WEIGHT3 0.04619001
    #define WEIGHT4 0.01746199
    #define GRAB_PIXEL(weight, kernelX, kernelY) ((weight) * tex2Dproj(tex, UNITY_PROJ_COORD(half4(pos.xy + half2((kernelX), (kernelY)) * kernelSize, pos.zw))))

    DO_GAUSSIAN_BLUR

    #undef WEIGHT1
    #undef WEIGHT2
    #undef WEIGHT3
    #undef WEIGHT4
    #undef GRAB_PIXEL

    return sum;
}

half4 tex2DProjCheapGaussianBlur(sampler2D tex, half4 texelSize, half4 pos, half size)
{
    half4 sum = half4(0.0, 0.0, 0.0, 0.0);
    half2 kernelSize = texelSize.xy * size;

    #define WEIGHT1 0.5
    #define WEIGHT2 (0.5 / 6.0)
    #define GRAB_PIXEL(weight, kernelX, kernelY) ((weight) * tex2Dproj(tex, UNITY_PROJ_COORD(half4(pos.xy + half2((kernelX), (kernelY)) * kernelSize, pos.zw))))

    DO_CHEAP_GAUSSIAN_BLUR

    #undef WEIGHT1
    #undef WEIGHT2
    #undef GRAB_PIXEL

    return sum;
}

half4 tex2DProjLensBlur(sampler2D tex, half4 texelSize, half4 pos, half size)
{
    half4 sum = half4(0.0, 0.0, 0.0, 0.0);
    half2 kernelSize = texelSize.xy * size;

    #define WEIGHT1 (0.2 / 13.0)
    #define WEIGHT2 WEIGHT1
    #define WEIGHT3 WEIGHT1
    #define WEIGHT4 (0.8 / 12.0)
    #define GRAB_PIXEL(weight, kernelX, kernelY) ((weight) * tex2Dproj(tex, UNITY_PROJ_COORD(half4(pos.xy + half2((kernelX), (kernelY)) * kernelSize, pos.zw))))

    DO_GAUSSIAN_BLUR

    #undef WEIGHT1
    #undef WEIGHT2
    #undef WEIGHT3
    #undef WEIGHT4
    #undef GRAB_PIXEL

    return sum;
}

half4 tex2DProjMotionBlur(sampler2D tex, half4 texelSize, half4 pos, half2 vel)
{
    half4 sum = half4(0.0, 0.0, 0.0, 0.0);
    half2 kernelSize = texelSize.xy * vel;

    #define WEIGHT (1.0 / 9.0)
    #define GRAB_PIXEL(weight, lambda) ((weight) * tex2Dproj(tex, UNITY_PROJ_COORD(half4(pos.xy + (lambda) * kernelSize, pos.zw))))

    DO_MOTION_BLUR

    #undef WEIGHT
    #undef GRAB_PIXEL

    return sum;
}
