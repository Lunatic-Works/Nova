// Gaussian quad, Stroud71, p324, E2(r^2): 7-1, rotated by 15 degrees
#define DO_GAUSSIAN_BLUR \
    sum += GRAB_PIXEL(WEIGHT1, +1.67303261, +0.44828774); \
    sum += GRAB_PIXEL(WEIGHT1, -1.67303261, -0.44828774); \
    sum += GRAB_PIXEL(WEIGHT1, -0.44828774, +1.67303261); \
    sum += GRAB_PIXEL(WEIGHT1, +0.44828774, -1.67303261); \
    sum += GRAB_PIXEL(WEIGHT2, +0.37846698, +0.65552403); \
    sum += GRAB_PIXEL(WEIGHT2, -0.65552403, +0.37846698); \
    sum += GRAB_PIXEL(WEIGHT2, +0.65552403, -0.37846698); \
    sum += GRAB_PIXEL(WEIGHT2, -0.37846698, -0.65552403); \
    sum += GRAB_PIXEL(WEIGHT3, +0.99083942, +1.71618421); \
    sum += GRAB_PIXEL(WEIGHT3, -1.71618421, +0.99083942); \
    sum += GRAB_PIXEL(WEIGHT3, +1.71618421, -0.99083942); \
    sum += GRAB_PIXEL(WEIGHT3, -0.99083942, -1.71618421); \

// Gaussian quad, Stroud71, p324, E2(r^2): 5-1, rotated by 7.5 degrees
// Also disk quad, Stroud71, p279, S2: 5-1, with R = sqrt(3), rotated by 7.5 degrees
#define DO_CHEAP_GAUSSIAN_BLUR \
    sum += GRAB_PIXEL(WEIGHT1,  0.00000000,  0.00000000); \
    sum += GRAB_PIXEL(WEIGHT2, +1.40211477, +0.18459191); \
    sum += GRAB_PIXEL(WEIGHT2, -1.40211477, -0.18459191); \
    sum += GRAB_PIXEL(WEIGHT2, +0.54119610, +1.30656296); \
    sum += GRAB_PIXEL(WEIGHT2, +0.86091867, -1.12197105); \
    sum += GRAB_PIXEL(WEIGHT2, -0.86091867, +1.12197105); \
    sum += GRAB_PIXEL(WEIGHT2, -0.54119610, -1.30656296); \

// Disk quad, Stroud71, p324, S2: 7-1, with R = sqrt(3), rotated by 15 degrees
#define DO_LENS_BLUR \
    sum += GRAB_PIXEL(WEIGHT1, +1.44888874, +0.38822857); \
    sum += GRAB_PIXEL(WEIGHT1, -1.44888874, -0.38822857); \
    sum += GRAB_PIXEL(WEIGHT1, -0.38822857, +1.44888874); \
    sum += GRAB_PIXEL(WEIGHT1, +0.38822857, -1.44888874); \
    sum += GRAB_PIXEL(WEIGHT2, +0.39548848, +0.68500614); \
    sum += GRAB_PIXEL(WEIGHT2, -0.68500614, +0.39548848); \
    sum += GRAB_PIXEL(WEIGHT2, +0.68500614, -0.39548848); \
    sum += GRAB_PIXEL(WEIGHT2, -0.39548848, -0.68500614); \
    sum += GRAB_PIXEL(WEIGHT3, +0.78894551, +1.36649370); \
    sum += GRAB_PIXEL(WEIGHT3, -1.36649370, +0.78894551); \
    sum += GRAB_PIXEL(WEIGHT3, +1.36649370, -0.78894551); \
    sum += GRAB_PIXEL(WEIGHT3, -0.78894551, -1.36649370); \

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

// https://bartwronski.com/2022/03/07/fast-gpu-friendly-antialiasing-downsampling-filter/
#define DO_DOWNSAMPLE \
    sum += GRAB_PIXEL(WEIGHT1, -0.75777156, -0.75777156); \
    sum += GRAB_PIXEL(WEIGHT1, +0.75777156, -0.75777156); \
    sum += GRAB_PIXEL(WEIGHT1, +0.75777156, +0.75777156); \
    sum += GRAB_PIXEL(WEIGHT1, -0.75777156, +0.75777156); \
    sum += GRAB_PIXEL(WEIGHT2, -2.90709914,  0.00000000); \
    sum += GRAB_PIXEL(WEIGHT2, +2.90709914,  0.00000000); \
    sum += GRAB_PIXEL(WEIGHT2,  0.00000000, -2.90709914); \
    sum += GRAB_PIXEL(WEIGHT2,  0.00000000, +2.90709914); \

half4 tex2DGaussianBlur(sampler2D tex, half4 texelSize, half2 uv, half size)
{
    half4 sum = half4(0.0, 0.0, 0.0, 0.0);
    half2 kernelSize = texelSize.xy * size;

    #define WEIGHT1 0.02777778
    #define WEIGHT2 0.21049191
    #define WEIGHT3 0.01173031
    #define GRAB_PIXEL(weight, kernelX, kernelY) ((weight) * tex2D(tex, uv + half2((kernelX), (kernelY)) * kernelSize))

    DO_GAUSSIAN_BLUR

    #undef WEIGHT1
    #undef WEIGHT2
    #undef WEIGHT3
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

    #define WEIGHT1 0.07407407
    #define WEIGHT2 0.12321069
    #define WEIGHT3 0.05271524
    #define GRAB_PIXEL(weight, kernelX, kernelY) ((weight) * tex2D(tex, uv + half2((kernelX), (kernelY)) * kernelSize))

    DO_LENS_BLUR

    #undef WEIGHT1
    #undef WEIGHT2
    #undef WEIGHT3
    #undef GRAB_PIXEL

    return sum;
}

half4 tex2DCheapLensBlur(sampler2D tex, half4 texelSize, half2 uv, half size)
{
    half4 sum = half4(0.0, 0.0, 0.0, 0.0);
    half2 kernelSize = texelSize.xy * size;

    #define WEIGHT1 0.25
    #define WEIGHT2 0.125
    #define GRAB_PIXEL(weight, kernelX, kernelY) ((weight) * tex2D(tex, uv + half2((kernelX), (kernelY)) * kernelSize))

    DO_CHEAP_GAUSSIAN_BLUR

    #undef WEIGHT1
    #undef WEIGHT2
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

half4 tex2DDownsample(sampler2D tex, half4 texelSize, half2 uv, half size)
{
    half4 sum = half4(0.0, 0.0, 0.0, 0.0);
    half2 kernelSize = texelSize.xy * size;

    #define WEIGHT1 0.37487566
    #define WEIGHT2 -0.12487566
    #define GRAB_PIXEL(weight, kernelX, kernelY) ((weight) * tex2D(tex, uv + half2((kernelX), (kernelY)) * kernelSize))

    DO_DOWNSAMPLE

    #undef WEIGHT1
    #undef WEIGHT2
    #undef GRAB_PIXEL

    return sum;
}

half4 tex2DProjGaussianBlur(sampler2D tex, half4 texelSize, half4 pos, half size)
{
    half4 sum = half4(0.0, 0.0, 0.0, 0.0);
    half2 kernelSize = texelSize.xy * size;

    #define WEIGHT1 0.02777778
    #define WEIGHT2 0.21049191
    #define WEIGHT3 0.01173031
    #define GRAB_PIXEL(weight, kernelX, kernelY) ((weight) * tex2Dproj(tex, UNITY_PROJ_COORD(half4(pos.xy + half2((kernelX), (kernelY)) * kernelSize, pos.zw))))

    DO_GAUSSIAN_BLUR

    #undef WEIGHT1
    #undef WEIGHT2
    #undef WEIGHT3
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

    #define WEIGHT1 0.07407407
    #define WEIGHT2 0.12321069
    #define WEIGHT3 0.05271524
    #define GRAB_PIXEL(weight, kernelX, kernelY) ((weight) * tex2Dproj(tex, UNITY_PROJ_COORD(half4(pos.xy + half2((kernelX), (kernelY)) * kernelSize, pos.zw))))

    DO_LENS_BLUR

    #undef WEIGHT1
    #undef WEIGHT2
    #undef WEIGHT3
    #undef GRAB_PIXEL

    return sum;
}

half4 tex2DProjCheapLensBlur(sampler2D tex, half4 texelSize, half4 pos, half size)
{
    half4 sum = half4(0.0, 0.0, 0.0, 0.0);
    half2 kernelSize = texelSize.xy * size;

    #define WEIGHT1 0.25
    #define WEIGHT2 0.125
    #define GRAB_PIXEL(weight, kernelX, kernelY) ((weight) * tex2Dproj(tex, UNITY_PROJ_COORD(half4(pos.xy + half2((kernelX), (kernelY)) * kernelSize, pos.zw))))

    DO_CHEAP_GAUSSIAN_BLUR

    #undef WEIGHT1
    #undef WEIGHT2
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
