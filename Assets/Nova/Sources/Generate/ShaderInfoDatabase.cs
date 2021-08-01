// This file is generated. Do not edit it manually. Please edit .shaderproto files.

using System.Collections.Generic;
using UnityEngine;

namespace Nova.Generate
{
    public static class ShaderInfoDatabase
    {
        public static readonly Dictionary<string, Dictionary<string, ShaderPropertyType>> TypeData =
            new Dictionary<string, Dictionary<string, ShaderPropertyType>>
        {
            {
                "Barrel",
                new Dictionary<string, ShaderPropertyType>
                {
                    {"_Aspect", ShaderPropertyType.Float},
                    {"_BackColor", ShaderPropertyType.Color},
                    {"_Scale", ShaderPropertyType.Float},
                    {"_Sigma", ShaderPropertyType.Float},
                }
            },
            {
                "Barrel Chroma",
                new Dictionary<string, ShaderPropertyType>
                {
                    {"_Aspect", ShaderPropertyType.Float},
                    {"_Offset", ShaderPropertyType.Float},
                    {"_Scale", ShaderPropertyType.Float},
                    {"_Sigma", ShaderPropertyType.Float},
                    {"_Strength", ShaderPropertyType.Float},
                    {"_T", ShaderPropertyType.Float},
                }
            },
            {
                "Blink",
                new Dictionary<string, ShaderPropertyType>
                {
                    {"_Amp", ShaderPropertyType.Float},
                    {"_Freq", ShaderPropertyType.Float},
                    {"_Mul", ShaderPropertyType.Float},
                    {"_Offset", ShaderPropertyType.Float},
                    {"_T", ShaderPropertyType.Float},
                }
            },
            {
                "Broken TV",
                new Dictionary<string, ShaderPropertyType>
                {
                    {"_Roll", ShaderPropertyType.Float},
                    {"_T", ShaderPropertyType.Float},
                }
            },
            {
                "Change Texture With Fade",
                new Dictionary<string, ShaderPropertyType>
                {
                    {"_Color", ShaderPropertyType.Color},
                    {"_Offsets", ShaderPropertyType.Vector},
                    {"_PrimaryTex", ShaderPropertyType.TexEnv},
                    {"_SubColor", ShaderPropertyType.Color},
                    {"_SubTex", ShaderPropertyType.TexEnv},
                    {"_T", ShaderPropertyType.Float},
                }
            },
            {
                "Color Correction",
                new Dictionary<string, ShaderPropertyType>
                {
                    {"_ColorAdd", ShaderPropertyType.Color},
                    {"_ColorMul", ShaderPropertyType.Color},
                    {"_T", ShaderPropertyType.Float},
                }
            },
            {
                "Colorless",
                new Dictionary<string, ShaderPropertyType>
                {
                    {"_T", ShaderPropertyType.Float},
                }
            },
            {
                "Fade",
                new Dictionary<string, ShaderPropertyType>
                {
                    {"_InvertMask", ShaderPropertyType.Float},
                    {"_Mask", ShaderPropertyType.TexEnv},
                    {"_Offset", ShaderPropertyType.Float},
                    {"_SubColor", ShaderPropertyType.Color},
                    {"_SubTex", ShaderPropertyType.TexEnv},
                    {"_T", ShaderPropertyType.Float},
                    {"_Vague", ShaderPropertyType.Float},
                }
            },
            {
                "Fade Radial Blur",
                new Dictionary<string, ShaderPropertyType>
                {
                    {"_Dir", ShaderPropertyType.Float},
                    {"_Size", ShaderPropertyType.Float},
                    {"_SubColor", ShaderPropertyType.Color},
                    {"_SubTex", ShaderPropertyType.TexEnv},
                    {"_T", ShaderPropertyType.Float},
                    {"_Zoom", ShaderPropertyType.Float},
                }
            },
            {
                "Flip Grid",
                new Dictionary<string, ShaderPropertyType>
                {
                    {"_BackColor", ShaderPropertyType.Color},
                    {"_FlipDuration", ShaderPropertyType.Float},
                    {"_GridSize", ShaderPropertyType.Float},
                    {"_SubColor", ShaderPropertyType.Color},
                    {"_SubTex", ShaderPropertyType.TexEnv},
                    {"_T", ShaderPropertyType.Float},
                }
            },
            {
                "Gaussian Blur",
                new Dictionary<string, ShaderPropertyType>
                {
                    {"_Offset", ShaderPropertyType.Float},
                    {"_Size", ShaderPropertyType.Float},
                    {"_T", ShaderPropertyType.Float},
                }
            },
            {
                "Glitch",
                new Dictionary<string, ShaderPropertyType>
                {
                    {"_T", ShaderPropertyType.Float},
                }
            },
            {
                "Glow",
                new Dictionary<string, ShaderPropertyType>
                {
                    {"_Size", ShaderPropertyType.Float},
                    {"_Strength", ShaderPropertyType.Float},
                    {"_T", ShaderPropertyType.Float},
                }
            },
            {
                "Kaleidoscope",
                new Dictionary<string, ShaderPropertyType>
                {
                    {"_Freq", ShaderPropertyType.Float},
                    {"_Repeat", ShaderPropertyType.Float},
                    {"_T", ShaderPropertyType.Float},
                }
            },
            {
                "Lens Blur",
                new Dictionary<string, ShaderPropertyType>
                {
                    {"_Offset", ShaderPropertyType.Float},
                    {"_Size", ShaderPropertyType.Float},
                    {"_T", ShaderPropertyType.Float},
                }
            },
            {
                "Masked Mosaic",
                new Dictionary<string, ShaderPropertyType>
                {
                    {"_Mask", ShaderPropertyType.TexEnv},
                    {"_Size", ShaderPropertyType.Float},
                    {"_Strength", ShaderPropertyType.Float},
                    {"_T", ShaderPropertyType.Float},
                }
            },
            {
                "Mix Add",
                new Dictionary<string, ShaderPropertyType>
                {
                    {"_AlphaFactor", ShaderPropertyType.Float},
                    {"_ColorAdd", ShaderPropertyType.Color},
                    {"_ColorMul", ShaderPropertyType.Color},
                    {"_InvertMask", ShaderPropertyType.Float},
                    {"_Mask", ShaderPropertyType.TexEnv},
                    {"_T", ShaderPropertyType.Float},
                }
            },
            {
                "Monochrome",
                new Dictionary<string, ShaderPropertyType>
                {
                    {"_Offset", ShaderPropertyType.Float},
                    {"_T", ShaderPropertyType.Float},
                }
            },
            {
                "Monochrome Mosaic",
                new Dictionary<string, ShaderPropertyType>
                {
                    {"_Size", ShaderPropertyType.Float},
                    {"_Strength", ShaderPropertyType.Float},
                    {"_T", ShaderPropertyType.Float},
                }
            },
            {
                "Motion Blur",
                new Dictionary<string, ShaderPropertyType>
                {
                    {"_Offset", ShaderPropertyType.Float},
                    {"_Size", ShaderPropertyType.Float},
                    {"_T", ShaderPropertyType.Float},
                    {"_Theta", ShaderPropertyType.Float},
                }
            },
            {
                "Multiply",
                new Dictionary<string, ShaderPropertyType>
                {
                    {"_T", ShaderPropertyType.Float},
                }
            },
            {
                "Multiply Random Roll",
                new Dictionary<string, ShaderPropertyType>
                {
                    {"_Freq", ShaderPropertyType.Float},
                    {"_T", ShaderPropertyType.Float},
                }
            },
            {
                "Overglow",
                new Dictionary<string, ShaderPropertyType>
                {
                    {"_CenterX", ShaderPropertyType.Float},
                    {"_CenterY", ShaderPropertyType.Float},
                    {"_Mul", ShaderPropertyType.Float},
                    {"_T", ShaderPropertyType.Float},
                    {"_Zoom", ShaderPropertyType.Float},
                }
            },
            {
                "Radial Blur",
                new Dictionary<string, ShaderPropertyType>
                {
                    {"_Offset", ShaderPropertyType.Float},
                    {"_Size", ShaderPropertyType.Float},
                    {"_T", ShaderPropertyType.Float},
                }
            },
            {
                "Rain",
                new Dictionary<string, ShaderPropertyType>
                {
                    {"_Aspect", ShaderPropertyType.Float},
                    {"_Size", ShaderPropertyType.Float},
                    {"_T", ShaderPropertyType.Float},
                }
            },
            {
                "Random Roll",
                new Dictionary<string, ShaderPropertyType>
                {
                    {"_Freq", ShaderPropertyType.Float},
                    {"_T", ShaderPropertyType.Float},
                }
            },
            {
                "Ripple",
                new Dictionary<string, ShaderPropertyType>
                {
                    {"_Amp", ShaderPropertyType.Float},
                    {"_Aspect", ShaderPropertyType.Float},
                    {"_BlurSize", ShaderPropertyType.Float},
                    {"_RFreq", ShaderPropertyType.Float},
                    {"_T", ShaderPropertyType.Float},
                    {"_TFreq", ShaderPropertyType.Float},
                }
            },
            {
                "Ripple Move",
                new Dictionary<string, ShaderPropertyType>
                {
                    {"_Amp", ShaderPropertyType.Float},
                    {"_Aspect", ShaderPropertyType.Float},
                    {"_BlurSize", ShaderPropertyType.Float},
                    {"_Fade", ShaderPropertyType.Float},
                    {"_RFreq", ShaderPropertyType.Float},
                    {"_T", ShaderPropertyType.Float},
                    {"_TFreq", ShaderPropertyType.Float},
                    {"_Width", ShaderPropertyType.Float},
                }
            },
            {
                "Roll",
                new Dictionary<string, ShaderPropertyType>
                {
                    {"_T", ShaderPropertyType.Float},
                    {"_XSpeed", ShaderPropertyType.Float},
                    {"_YSpeed", ShaderPropertyType.Float},
                }
            },
            {
                "Rotation Blur",
                new Dictionary<string, ShaderPropertyType>
                {
                    {"_Offset", ShaderPropertyType.Float},
                    {"_Size", ShaderPropertyType.Float},
                    {"_T", ShaderPropertyType.Float},
                }
            },
            {
                "Screen",
                new Dictionary<string, ShaderPropertyType>
                {
                    {"_T", ShaderPropertyType.Float},
                }
            },
            {
                "Screen Blink",
                new Dictionary<string, ShaderPropertyType>
                {
                    {"_Amp", ShaderPropertyType.Float},
                    {"_Freq", ShaderPropertyType.Float},
                    {"_Mul", ShaderPropertyType.Float},
                    {"_Offset", ShaderPropertyType.Float},
                    {"_T", ShaderPropertyType.Float},
                }
            },
            {
                "Screen Fade",
                new Dictionary<string, ShaderPropertyType>
                {
                    {"_InvertMask", ShaderPropertyType.Float},
                    {"_Mask", ShaderPropertyType.TexEnv},
                    {"_SubColor", ShaderPropertyType.Color},
                    {"_SubTex", ShaderPropertyType.TexEnv},
                    {"_T", ShaderPropertyType.Float},
                    {"_Vague", ShaderPropertyType.Float},
                }
            },
            {
                "Screen Gray Wave",
                new Dictionary<string, ShaderPropertyType>
                {
                    {"_Freq", ShaderPropertyType.Float},
                    {"_Size", ShaderPropertyType.Float},
                    {"_T", ShaderPropertyType.Float},
                }
            },
            {
                "Screen Random Roll",
                new Dictionary<string, ShaderPropertyType>
                {
                    {"_Freq", ShaderPropertyType.Float},
                    {"_T", ShaderPropertyType.Float},
                }
            },
            {
                "Screen Wiggle",
                new Dictionary<string, ShaderPropertyType>
                {
                    {"_AAmp", ShaderPropertyType.Float},
                    {"_BlinkAmp", ShaderPropertyType.Float},
                    {"_BlinkFreq", ShaderPropertyType.Float},
                    {"_Mono", ShaderPropertyType.Float},
                    {"_Mul", ShaderPropertyType.Float},
                    {"_Offset", ShaderPropertyType.Float},
                    {"_T", ShaderPropertyType.Float},
                    {"_TFreq", ShaderPropertyType.Float},
                    {"_XAmp", ShaderPropertyType.Float},
                    {"_XFreq", ShaderPropertyType.Float},
                    {"_YAmp", ShaderPropertyType.Float},
                    {"_YFreq", ShaderPropertyType.Float},
                }
            },
            {
                "Shake",
                new Dictionary<string, ShaderPropertyType>
                {
                    {"_Freq", ShaderPropertyType.Float},
                    {"_T", ShaderPropertyType.Float},
                }
            },
            {
                "Sharpen",
                new Dictionary<string, ShaderPropertyType>
                {
                    {"_Size", ShaderPropertyType.Float},
                    {"_T", ShaderPropertyType.Float},
                }
            },
            {
                "Show Second Texture",
                new Dictionary<string, ShaderPropertyType>
                {
                    {"_SubTex", ShaderPropertyType.TexEnv},
                }
            },
            {
                "Water",
                new Dictionary<string, ShaderPropertyType>
                {
                    {"_Aspect", ShaderPropertyType.Float},
                    {"_Distort", ShaderPropertyType.Float},
                    {"_Freq", ShaderPropertyType.Float},
                    {"_Size", ShaderPropertyType.Float},
                    {"_T", ShaderPropertyType.Float},
                }
            },
            {
                "Wiggle",
                new Dictionary<string, ShaderPropertyType>
                {
                    {"_T", ShaderPropertyType.Float},
                    {"_TFreq", ShaderPropertyType.Float},
                    {"_XAmp", ShaderPropertyType.Float},
                    {"_XFreq", ShaderPropertyType.Float},
                    {"_YAmp", ShaderPropertyType.Float},
                    {"_YFreq", ShaderPropertyType.Float},
                }
            },
        };

        public static readonly Dictionary<string, Dictionary<string, float>> FloatData =
            new Dictionary<string, Dictionary<string, float>>
        {
            {
                "Barrel",
                new Dictionary<string, float>
                {
                    {"_Aspect", 1.77777778f},
                    {"_Scale", 1.0f},
                    {"_Sigma", 0.2f},
                }
            },
            {
                "Barrel Chroma",
                new Dictionary<string, float>
                {
                    {"_Aspect", 1.77777778f},
                    {"_Offset", 0.0f},
                    {"_Scale", 1.0f},
                    {"_Sigma", 0.2f},
                    {"_Strength", 0.02f},
                    {"_T", 0.0f},
                }
            },
            {
                "Blink",
                new Dictionary<string, float>
                {
                    {"_Amp", -0.5f},
                    {"_Freq", 10.0f},
                    {"_Mul", 1.0f},
                    {"_Offset", 0.0f},
                    {"_T", 0.0f},
                }
            },
            {
                "Broken TV",
                new Dictionary<string, float>
                {
                    {"_Roll", 0.07f},
                    {"_T", 0.0f},
                }
            },
            {
                "Change Texture With Fade",
                new Dictionary<string, float>
                {
                    {"_T", 0.0f},
                }
            },
            {
                "Color Correction",
                new Dictionary<string, float>
                {
                    {"_T", 0.0f},
                }
            },
            {
                "Colorless",
                new Dictionary<string, float>
                {
                    {"_T", 0.0f},
                }
            },
            {
                "Fade",
                new Dictionary<string, float>
                {
                    {"_InvertMask", 0.0f},
                    {"_Offset", 0.0f},
                    {"_T", 0.0f},
                    {"_Vague", 0.25f},
                }
            },
            {
                "Fade Radial Blur",
                new Dictionary<string, float>
                {
                    {"_Dir", 0.0f},
                    {"_Size", 1.0f},
                    {"_T", 0.0f},
                    {"_Zoom", 0.5f},
                }
            },
            {
                "Flip Grid",
                new Dictionary<string, float>
                {
                    {"_FlipDuration", 0.5f},
                    {"_GridSize", 64.0f},
                    {"_T", 0.0f},
                }
            },
            {
                "Gaussian Blur",
                new Dictionary<string, float>
                {
                    {"_Offset", 0.0f},
                    {"_Size", 1.0f},
                    {"_T", 0.0f},
                }
            },
            {
                "Glitch",
                new Dictionary<string, float>
                {
                    {"_T", 0.0f},
                }
            },
            {
                "Glow",
                new Dictionary<string, float>
                {
                    {"_Size", 1.0f},
                    {"_Strength", 1.0f},
                    {"_T", 0.0f},
                }
            },
            {
                "Kaleidoscope",
                new Dictionary<string, float>
                {
                    {"_Freq", 1.0f},
                    {"_Repeat", 8.0f},
                    {"_T", 0.0f},
                }
            },
            {
                "Lens Blur",
                new Dictionary<string, float>
                {
                    {"_Offset", 0.0f},
                    {"_Size", 1.0f},
                    {"_T", 0.0f},
                }
            },
            {
                "Masked Mosaic",
                new Dictionary<string, float>
                {
                    {"_Size", 4.0f},
                    {"_Strength", 8.0f},
                    {"_T", 0.0f},
                }
            },
            {
                "Mix Add",
                new Dictionary<string, float>
                {
                    {"_AlphaFactor", 0.0f},
                    {"_InvertMask", 0.0f},
                    {"_T", 0.0f},
                }
            },
            {
                "Monochrome",
                new Dictionary<string, float>
                {
                    {"_Offset", 0.0f},
                    {"_T", 0.0f},
                }
            },
            {
                "Monochrome Mosaic",
                new Dictionary<string, float>
                {
                    {"_Size", 4.0f},
                    {"_Strength", 8.0f},
                    {"_T", 0.0f},
                }
            },
            {
                "Motion Blur",
                new Dictionary<string, float>
                {
                    {"_Offset", 0.0f},
                    {"_Size", 1.0f},
                    {"_T", 0.0f},
                    {"_Theta", 0.0f},
                }
            },
            {
                "Multiply",
                new Dictionary<string, float>
                {
                    {"_T", 0.0f},
                }
            },
            {
                "Multiply Random Roll",
                new Dictionary<string, float>
                {
                    {"_Freq", 10.0f},
                    {"_T", 0.0f},
                }
            },
            {
                "Overglow",
                new Dictionary<string, float>
                {
                    {"_CenterX", 0.5f},
                    {"_CenterY", 0.5f},
                    {"_Mul", 0.5f},
                    {"_T", 0.0f},
                    {"_Zoom", 0.5f},
                }
            },
            {
                "Radial Blur",
                new Dictionary<string, float>
                {
                    {"_Offset", 0.0f},
                    {"_Size", 1.0f},
                    {"_T", 0.0f},
                }
            },
            {
                "Rain",
                new Dictionary<string, float>
                {
                    {"_Aspect", 1.77777778f},
                    {"_Size", 2.0f},
                    {"_T", 0.0f},
                }
            },
            {
                "Random Roll",
                new Dictionary<string, float>
                {
                    {"_Freq", 10.0f},
                    {"_T", 0.0f},
                }
            },
            {
                "Ripple",
                new Dictionary<string, float>
                {
                    {"_Amp", 0.01f},
                    {"_Aspect", 1.77777778f},
                    {"_BlurSize", 1.0f},
                    {"_RFreq", 50.0f},
                    {"_T", 0.0f},
                    {"_TFreq", 3.0f},
                }
            },
            {
                "Ripple Move",
                new Dictionary<string, float>
                {
                    {"_Amp", 0.01f},
                    {"_Aspect", 1.77777778f},
                    {"_BlurSize", 1.0f},
                    {"_Fade", 0.1f},
                    {"_RFreq", 50.0f},
                    {"_T", 0.0f},
                    {"_TFreq", 3.0f},
                    {"_Width", 0.1f},
                }
            },
            {
                "Roll",
                new Dictionary<string, float>
                {
                    {"_T", 0.0f},
                    {"_XSpeed", 0.0f},
                    {"_YSpeed", 0.0f},
                }
            },
            {
                "Rotation Blur",
                new Dictionary<string, float>
                {
                    {"_Offset", 0.0f},
                    {"_Size", 1.0f},
                    {"_T", 0.0f},
                }
            },
            {
                "Screen",
                new Dictionary<string, float>
                {
                    {"_T", 0.0f},
                }
            },
            {
                "Screen Blink",
                new Dictionary<string, float>
                {
                    {"_Amp", -0.5f},
                    {"_Freq", 10.0f},
                    {"_Mul", 1.0f},
                    {"_Offset", 0.0f},
                    {"_T", 0.0f},
                }
            },
            {
                "Screen Fade",
                new Dictionary<string, float>
                {
                    {"_InvertMask", 0.0f},
                    {"_T", 0.0f},
                    {"_Vague", 0.25f},
                }
            },
            {
                "Screen Gray Wave",
                new Dictionary<string, float>
                {
                    {"_Freq", 0.5f},
                    {"_Size", 50.0f},
                    {"_T", 0.0f},
                }
            },
            {
                "Screen Random Roll",
                new Dictionary<string, float>
                {
                    {"_Freq", 10.0f},
                    {"_T", 0.0f},
                }
            },
            {
                "Screen Wiggle",
                new Dictionary<string, float>
                {
                    {"_AAmp", 0.0f},
                    {"_BlinkAmp", 0.0f},
                    {"_BlinkFreq", 0.0f},
                    {"_Mono", 0.0f},
                    {"_Mul", 1.0f},
                    {"_Offset", 0.0f},
                    {"_T", 0.0f},
                    {"_TFreq", 0.0f},
                    {"_XAmp", 0.0f},
                    {"_XFreq", 0.0f},
                    {"_YAmp", 0.0f},
                    {"_YFreq", 0.0f},
                }
            },
            {
                "Shake",
                new Dictionary<string, float>
                {
                    {"_Freq", 10.0f},
                    {"_T", 0.0f},
                }
            },
            {
                "Sharpen",
                new Dictionary<string, float>
                {
                    {"_Size", 1.0f},
                    {"_T", 0.0f},
                }
            },
            {
                "Water",
                new Dictionary<string, float>
                {
                    {"_Aspect", 1.77777778f},
                    {"_Distort", 5.0f},
                    {"_Freq", 50.0f},
                    {"_Size", 10.0f},
                    {"_T", 0.0f},
                }
            },
            {
                "Wiggle",
                new Dictionary<string, float>
                {
                    {"_T", 0.0f},
                    {"_TFreq", 0.0f},
                    {"_XAmp", 0.0f},
                    {"_XFreq", 0.0f},
                    {"_YAmp", 0.0f},
                    {"_YFreq", 0.0f},
                }
            },
        };

        public static readonly Dictionary<string, Dictionary<string, Color>> ColorData =
            new Dictionary<string, Dictionary<string, Color>>
        {
            {
                "Barrel",
                new Dictionary<string, Color>
                {
                    {"_BackColor", Color.black},
                }
            },
            {
                "Change Texture With Fade",
                new Dictionary<string, Color>
                {
                    {"_Color", Color.white},
                    {"_SubColor", Color.white},
                }
            },
            {
                "Color Correction",
                new Dictionary<string, Color>
                {
                    {"_ColorAdd", Color.clear},
                    {"_ColorMul", Color.white},
                }
            },
            {
                "Fade",
                new Dictionary<string, Color>
                {
                    {"_SubColor", Color.white},
                }
            },
            {
                "Fade Radial Blur",
                new Dictionary<string, Color>
                {
                    {"_SubColor", Color.white},
                }
            },
            {
                "Flip Grid",
                new Dictionary<string, Color>
                {
                    {"_BackColor", Color.black},
                    {"_SubColor", Color.white},
                }
            },
            {
                "Mix Add",
                new Dictionary<string, Color>
                {
                    {"_ColorAdd", Color.clear},
                    {"_ColorMul", Color.white},
                }
            },
            {
                "Screen Fade",
                new Dictionary<string, Color>
                {
                    {"_SubColor", Color.white},
                }
            },
        };

        public static readonly Dictionary<string, Dictionary<string, Vector4>> VectorData =
            new Dictionary<string, Dictionary<string, Vector4>>
        {
            {
                "Change Texture With Fade",
                new Dictionary<string, Vector4>
                {
                    {"_Offsets", Vector4.zero},
                }
            },
        };
    }
}
