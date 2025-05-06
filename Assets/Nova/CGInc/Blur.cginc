// Gaussian quad with hexgonal points like DO_LENS_BLUR, degree 9, 19 points
#define DO_GAUSSIAN_BLUR \
    sum += GRAB_PIXEL(WEIGHT1,  0.00000000,  0.00000000); \
    sum += GRAB_PIXEL(WEIGHT2, +0.90896014, +2.19442390); \
    sum += GRAB_PIXEL(WEIGHT2, -1.44594677, +1.88439452); \
    sum += GRAB_PIXEL(WEIGHT2, -2.35490691, -0.31002938); \
    sum += GRAB_PIXEL(WEIGHT2, -0.90896014, -2.19442390); \
    sum += GRAB_PIXEL(WEIGHT2, +1.44594677, -1.88439452); \
    sum += GRAB_PIXEL(WEIGHT2, +2.35490691, +0.31002938); \
    sum += GRAB_PIXEL(WEIGHT3, +0.42189665, +1.01854862); \
    sum += GRAB_PIXEL(WEIGHT3, -0.67114066, +0.87464753); \
    sum += GRAB_PIXEL(WEIGHT3, -1.09303731, -0.14390109); \
    sum += GRAB_PIXEL(WEIGHT3, -0.42189665, -1.01854862); \
    sum += GRAB_PIXEL(WEIGHT3, +0.67114066, -0.87464753); \
    sum += GRAB_PIXEL(WEIGHT3, +1.09303731, +0.14390109); \
    sum += GRAB_PIXEL(WEIGHT4, +1.58670668, +1.21752286); \
    sum += GRAB_PIXEL(WEIGHT4, -0.26105238, +1.98288972); \
    sum += GRAB_PIXEL(WEIGHT4, -1.84775906, +0.76536686); \
    sum += GRAB_PIXEL(WEIGHT4, -1.58670668, -1.21752286); \
    sum += GRAB_PIXEL(WEIGHT4, +0.26105238, -1.98288972); \
    sum += GRAB_PIXEL(WEIGHT4, +1.84775906, -0.76536686); \

// Gaussian quad, Stroud71, p324, E2(r^2): 7-1, rotated by 15 degrees
#define DO_CHEAPISH_GAUSSIAN_BLUR \
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

// Disk quad, Stroud71, p281, S2: 9-1, with R = sqrt(3), rotated by 7.5 degrees
#define DO_LENS_BLUR \
    sum += GRAB_PIXEL(WEIGHT1,  0.00000000,  0.00000000); \
    sum += GRAB_PIXEL(WEIGHT2, +0.39071229,  0.94326290); \
    sum += GRAB_PIXEL(WEIGHT2, -0.62153349,  0.80999822); \
    sum += GRAB_PIXEL(WEIGHT2, -1.01224578, -0.13326469); \
    sum += GRAB_PIXEL(WEIGHT2, -0.39071229, -0.94326290); \
    sum += GRAB_PIXEL(WEIGHT2, +0.62153349, -0.80999822); \
    sum += GRAB_PIXEL(WEIGHT2, +1.01224578, +0.13326469); \
    sum += GRAB_PIXEL(WEIGHT3, +0.62574628, +1.51068515); \
    sum += GRAB_PIXEL(WEIGHT3, -0.99541858, +1.29725475); \
    sum += GRAB_PIXEL(WEIGHT3, -1.62116486, -0.21343040); \
    sum += GRAB_PIXEL(WEIGHT3, -0.62574628, -1.51068515); \
    sum += GRAB_PIXEL(WEIGHT3, +0.99541858, -1.29725475); \
    sum += GRAB_PIXEL(WEIGHT3, +1.62116489, +0.21343040); \
    sum += GRAB_PIXEL(WEIGHT4, +1.22905771, +0.94308915); \
    sum += GRAB_PIXEL(WEIGHT4, -0.20221031, +1.53593978); \
    sum += GRAB_PIXEL(WEIGHT4, -1.43126802, +0.59285062); \
    sum += GRAB_PIXEL(WEIGHT4, -1.22905771, -0.94308915); \
    sum += GRAB_PIXEL(WEIGHT4, +0.20221031, -1.53593978); \
    sum += GRAB_PIXEL(WEIGHT4, +1.43126802, -0.59285062); \

// Disk quad, Stroud71, p281, S2: 7-1, with R = sqrt(3), rotated by 15 degrees
#define DO_CHEAPISH_LENS_BLUR \
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

// Gauss-Legendre quad
#define DO_MOTION_BLUR \
    sum += GRAB_PIXEL(WEIGHT1, -0.94910791); \
    sum += GRAB_PIXEL(WEIGHT2, -0.74153119); \
    sum += GRAB_PIXEL(WEIGHT3, -0.40584515); \
    sum += GRAB_PIXEL(WEIGHT4,  0.00000000); \
    sum += GRAB_PIXEL(WEIGHT3, +0.40584515); \
    sum += GRAB_PIXEL(WEIGHT2, +0.74153119); \
    sum += GRAB_PIXEL(WEIGHT1, +0.94910791); \

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

    #define WEIGHT1 0.32291667
    #define WEIGHT2 0.00175169
    #define WEIGHT3 0.10328303
    #define WEIGHT4 0.00781250
    #define GRAB_PIXEL(weight, kernelX, kernelY) ((weight) * tex2D(tex, uv + half2((kernelX), (kernelY)) * kernelSize))

    DO_GAUSSIAN_BLUR

    #undef WEIGHT1
    #undef WEIGHT2
    #undef WEIGHT3
    #undef WEIGHT4
    #undef GRAB_PIXEL

    return sum;
}

half4 tex2DCheapishGaussianBlur(sampler2D tex, half4 texelSize, half2 uv, half size)
{
    half4 sum = half4(0.0, 0.0, 0.0, 0.0);
    half2 kernelSize = texelSize.xy * size;

    #define WEIGHT1 0.02777778
    #define WEIGHT2 0.21049191
    #define WEIGHT3 0.01173031
    #define GRAB_PIXEL(weight, kernelX, kernelY) ((weight) * tex2D(tex, uv + half2((kernelX), (kernelY)) * kernelSize))

    DO_CHEAPISH_GAUSSIAN_BLUR

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

    #define WEIGHT1 0.10894097
    #define WEIGHT2 0.08332903
    #define WEIGHT3 0.02449071
    #define WEIGHT4 0.04069010
    #define GRAB_PIXEL(weight, kernelX, kernelY) ((weight) * tex2D(tex, uv + half2((kernelX), (kernelY)) * kernelSize))

    DO_LENS_BLUR

    #undef WEIGHT1
    #undef WEIGHT2
    #undef WEIGHT3
    #undef WEIGHT4
    #undef GRAB_PIXEL

    return sum;
}

half4 tex2DCheapishLensBlur(sampler2D tex, half4 texelSize, half2 uv, half size)
{
    half4 sum = half4(0.0, 0.0, 0.0, 0.0);
    half2 kernelSize = texelSize.xy * size;

    #define WEIGHT1 0.07407407
    #define WEIGHT2 0.12321069
    #define WEIGHT3 0.05271524
    #define GRAB_PIXEL(weight, kernelX, kernelY) ((weight) * tex2D(tex, uv + half2((kernelX), (kernelY)) * kernelSize))

    DO_CHEAPISH_LENS_BLUR

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

    #define WEIGHT1 0.12948497
    #define WEIGHT2 0.27970539
    #define WEIGHT3 0.38183005
    #define WEIGHT4 0.41795918
    #define GRAB_PIXEL(weight, lambda) ((weight) * tex2D(tex, uv + (lambda) * kernelSize))

    DO_MOTION_BLUR

    #undef WEIGHT1
    #undef WEIGHT2
    #undef WEIGHT3
    #undef WEIGHT4
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

    #define WEIGHT1 0.32291667
    #define WEIGHT2 0.00175169
    #define WEIGHT3 0.10328303
    #define WEIGHT4 0.00781250
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

    #define WEIGHT1 0.10894097
    #define WEIGHT2 0.08332903
    #define WEIGHT3 0.02449071
    #define WEIGHT4 0.04069010
    #define GRAB_PIXEL(weight, kernelX, kernelY) ((weight) * tex2Dproj(tex, UNITY_PROJ_COORD(half4(pos.xy + half2((kernelX), (kernelY)) * kernelSize, pos.zw))))

    DO_LENS_BLUR

    #undef WEIGHT1
    #undef WEIGHT2
    #undef WEIGHT3
    #undef WEIGHT4
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

    #define WEIGHT1 0.12948497
    #define WEIGHT2 0.27970539
    #define WEIGHT3 0.38183005
    #define WEIGHT4 0.41795918
    #define GRAB_PIXEL(weight, lambda) ((weight) * tex2Dproj(tex, UNITY_PROJ_COORD(half4(pos.xy + (lambda) * kernelSize, pos.zw))))

    DO_MOTION_BLUR

    #undef WEIGHT1
    #undef WEIGHT2
    #undef WEIGHT3
    #undef WEIGHT4
    #undef GRAB_PIXEL

    return sum;
}
