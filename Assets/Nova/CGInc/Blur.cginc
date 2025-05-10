// Gaussian quad, order 6, 11 points
#define DO_GAUSSIAN_BLUR \
    sum += GRAB_PIXEL(WEIGHT1,  0.00000000,  0.00000000); \
    sum += GRAB_PIXEL(WEIGHT2,  1.58339846,  0.16642188); \
    sum += GRAB_PIXEL(WEIGHT2,  0.33102042,  1.55732861); \
    sum += GRAB_PIXEL(WEIGHT2, -1.37881659,  0.79606013); \
    sum += GRAB_PIXEL(WEIGHT2, -1.18317593, -1.06533640); \
    sum += GRAB_PIXEL(WEIGHT2,  0.64757365, -1.45447423); \
    sum += GRAB_PIXEL(WEIGHT3,  0.76697193,  0.69058463); \
    sum += GRAB_PIXEL(WEIGHT3, -0.41977765,  0.94283604); \
    sum += GRAB_PIXEL(WEIGHT3, -1.02640879, -0.10787991); \
    sum += GRAB_PIXEL(WEIGHT3, -0.21457787, -1.00950949); \
    sum += GRAB_PIXEL(WEIGHT3,  0.89379237, -0.51603127); \

// Gaussian quad, Stroud71, p323, E2(r^2): 4-1, rotated by 6 degrees to reduce horizontal/vertical artifacts
// Also disk quad, Stroud71, p278, S2: 4-1, with R = sqrt(3)
#define DO_CHEAP_GAUSSIAN_BLUR \
    sum += GRAB_PIXEL(WEIGHT1,  0.00000000,  0.00000000); \
    sum += GRAB_PIXEL(WEIGHT2,  1.40646635,  0.14782557); \
    sum += GRAB_PIXEL(WEIGHT2,  0.29403153,  1.38330960); \
    sum += GRAB_PIXEL(WEIGHT2, -1.22474487,  0.70710678); \
    sum += GRAB_PIXEL(WEIGHT2, -1.05096549, -0.94629358); \
    sum += GRAB_PIXEL(WEIGHT2,  0.57521248, -1.29194838); \

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

    #define WEIGHT1 0.40740741
    #define WEIGHT2 0.05018708
    #define WEIGHT3 0.06833144
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
    #define WEIGHT2 0.1
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

    #define WEIGHT1 0.11111111
    #define WEIGHT2 0.07528061
    #define WEIGHT3 0.10249717
    #define GRAB_PIXEL(weight, kernelX, kernelY) ((weight) * tex2D(tex, uv + half2((kernelX), (kernelY)) * kernelSize))

    DO_GAUSSIAN_BLUR

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
    #define WEIGHT2 0.15
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

    #define WEIGHT1 0.06474248
    #define WEIGHT2 0.13985270
    #define WEIGHT3 0.19091503
    #define WEIGHT4 0.20897959
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

    #define WEIGHT1 0.40740741
    #define WEIGHT2 0.05018708
    #define WEIGHT3 0.06833144
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
    #define WEIGHT2 0.1
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

    #define WEIGHT1 0.11111111
    #define WEIGHT2 0.07528061
    #define WEIGHT3 0.10249717
    #define GRAB_PIXEL(weight, kernelX, kernelY) ((weight) * tex2Dproj(tex, UNITY_PROJ_COORD(half4(pos.xy + half2((kernelX), (kernelY)) * kernelSize, pos.zw))))

    DO_GAUSSIAN_BLUR

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
    #define WEIGHT2 0.15
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

    #define WEIGHT1 0.06474248
    #define WEIGHT2 0.13985270
    #define WEIGHT3 0.19091503
    #define WEIGHT4 0.20897959
    #define GRAB_PIXEL(weight, lambda) ((weight) * tex2Dproj(tex, UNITY_PROJ_COORD(half4(pos.xy + (lambda) * kernelSize, pos.zw))))

    DO_MOTION_BLUR

    #undef WEIGHT1
    #undef WEIGHT2
    #undef WEIGHT3
    #undef WEIGHT4
    #undef GRAB_PIXEL

    return sum;
}
