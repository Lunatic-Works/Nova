#!/usr/bin/env python3

import json
import os
import re

shader_proto_dir = '../../Assets/Nova/ShaderProtos/'
shader_dir = '../../Assets/Resources/Shaders/'
timestamps_filename = 'shader_timestamps.json'
shader_info_cs_filename = '../../Assets/Nova/Sources/Generate/ShaderInfoDatabase.cs'
shader_info_lua_filename = '../../Assets/Nova/Lua/shader_info.lua'


def indent_lines(s, indent):
    if isinstance(indent, int):
        indent = ' ' * indent
    indent = indent.replace('\t', ' ' * 4)
    s = ''.join(indent + line if line else ''
                for line in s.strip('\r\n').splitlines(keepends=True))
    return s


def replace_indented(s, old, new):
    old = old.replace('$', r'\$')
    s = re.compile(fr'^([ \t]*){old}$', re.MULTILINE).sub(
        lambda match: indent_lines(new, match.group(1)), s)
    return s


def write_shader(filename, text, *, ext_name, variant_name, variant_tags,
                 variant_rgb, def_gscale, gscale):
    filename = filename.replace('.shaderproto', ext_name)

    text = text.replace('$VARIANT_NAME$', variant_name)
    text = replace_indented(text, '$VARIANT_TAGS$', variant_tags)
    text = replace_indented(text, '$VARIANT_RGB$', variant_rgb)
    text = replace_indented(text, '$DEF_GSCALE$', def_gscale)
    text = text.replace('$GSCALE$', gscale)
    text = re.compile(r'\n{3,}').sub(r'\n\n', text)

    with open(os.path.join(shader_dir, filename),
              'w',
              encoding='utf-8',
              newline='\n') as f:
        f.write(
            '// This file is generated. Do not edit it manually. Please edit .shaderproto files.\n\n'
        )
        f.write(text)


def generate_shader(filename, text, variant):
    if variant == 'Default':
        write_shader(
            filename,
            text,
            ext_name='.shader',
            variant_name='VFX',
            variant_tags="""
Cull Off ZWrite Off Blend SrcAlpha OneMinusSrcAlpha
Tags { "Queue" = "Transparent" "RenderType" = "Transparent" }
""",
            variant_rgb='',
            def_gscale='',
            gscale='1.0',
        )
    elif variant == 'Multiply':
        write_shader(
            filename,
            text,
            ext_name='.Multiply.shader',
            variant_name='VFX Multiply',
            variant_tags="""
Cull Off ZWrite Off Blend DstColor Zero
Tags { "Queue" = "Transparent" "RenderType" = "Transparent" }
""",
            variant_rgb="""
col.rgb = 1.0 - (1.0 - col.rgb) * col.a;
col.a = 1.0;
""",
            def_gscale='',
            gscale='1.0',
        )
    elif variant == 'Screen':
        write_shader(
            filename,
            text,
            ext_name='.Screen.shader',
            variant_name='VFX Screen',
            variant_tags="""
Cull Off ZWrite Off Blend OneMinusDstColor One
Tags { "Queue" = "Transparent" "RenderType" = "Transparent" }
""",
            variant_rgb="""
col.rgb *= col.a;
col.a = 1.0;
""",
            def_gscale='',
            gscale='1.0',
        )
    elif variant == 'PP':
        write_shader(
            filename,
            text,
            ext_name='.PP.shader',
            variant_name='Post Processing',
            variant_tags="""
Cull Off ZWrite Off ZTest Always
""",
            variant_rgb='',
            def_gscale='float _GScale;',
            gscale='_GScale',
        )
    elif variant == 'Premul':
        write_shader(
            filename,
            text,
            ext_name='.Premul.shader',
            variant_name='Premul',
            variant_tags="""
Cull Off ZWrite Off Blend One OneMinusSrcAlpha
Tags { "Queue" = "Transparent" "RenderType" = "Transparent" }
""",
            variant_rgb='',
            def_gscale='',
            gscale='1.0',
        )
    else:
        raise ValueError(f'Unknown variant: {variant}')


def generate_shaders(filenames):
    print('generate_shaders')

    timestamps = {}
    if os.path.exists(timestamps_filename):
        with open(timestamps_filename, 'r', encoding='utf-8') as f:
            timestamps = json.load(f)

    for filename in filenames:
        path = os.path.join(shader_proto_dir, filename)
        mtime = os.path.getmtime(path)
        if filename in timestamps and mtime == timestamps[filename]:
            continue

        print(filename)
        timestamps[filename] = mtime

        with open(path, 'r', encoding='utf-8') as f:
            text = f.read()
        line = text.split('\n', 1)[0]
        if line.startswith('VARIANTS:'):
            text = text[len(line) + 1:]

            line = line.replace('VARIANTS:', '')
            variants = [x.strip() for x in line.split(',')]
        else:
            variants = ['Default', 'Multiply', 'Screen', 'PP']

        for variant in variants:
            generate_shader(filename, text, variant)

    print()

    with open(timestamps_filename, 'w', encoding='utf-8', newline='\n') as f:
        json.dump(timestamps, f)


def parse_shader_properties(filename):
    shader_name = ''
    type_data = {}
    float_data = {}
    color_data = {}
    vector_data = {}

    with open(filename, 'r', encoding='utf-8') as f:
        line = next(f)
        if line.startswith('VARIANTS:'):
            line = next(f)
        shader_name = line.strip().split('/')[-1][:-1]

        for _ in range(3):
            next(f)

        for line in f:
            line = line.strip()
            if not line:
                continue
            if line == '}':
                break

            match = re.compile(
                r'(\[.*?\] *)*(?P<name>.*?) .*?, (Range|Float).*?\) = (?P<value>.*?)'
            ).fullmatch(line)
            if match:
                name = match.group('name')
                value = float(match.group('value'))
                type_data[name] = 'Float'
                float_data[name] = value
                continue

            match = re.compile(
                r'(\[.*?\] *)*(?P<name>.*?) .*?, Color\) = \((?P<r>.*?), (?P<g>.*?), (?P<b>.*?), (?P<a>.*?)\)'
            ).fullmatch(line)
            if match:
                name = match.group('name')
                r = float(match.group('r'))
                g = float(match.group('g'))
                b = float(match.group('b'))
                a = float(match.group('a'))
                type_data[name] = 'Color'
                color_data[name] = (r, g, b, a)
                continue

            match = re.compile(
                r'(\[.*?\] *)*(?P<name>.*?) .*?, Vector\) = \((?P<x>.*?), (?P<y>.*?), (?P<z>.*?), (?P<w>.*?)\)'
            ).fullmatch(line)
            if match:
                name = match.group('name')
                x = float(match.group('x'))
                y = float(match.group('y'))
                z = float(match.group('z'))
                w = float(match.group('w'))
                type_data[name] = 'Vector'
                vector_data[name] = (x, y, z, w)
                continue

            match = re.compile(
                r'(\[.*?\] *)*(?P<name>.*?) .*?, 2D\) = .*?').fullmatch(line)
            if match:
                name = match.group('name')
                if name == '_MainTex':
                    continue
                type_data[name] = '2D'
                continue

            print(f'Warning: failed to parse line: {line}')

    return shader_name, type_data, float_data, color_data, vector_data


def get_nova_type(_type):
    if _type == 'Float':
        return 'ShaderPropertyType.Float'
    elif _type == 'Color':
        return 'ShaderPropertyType.Color'
    elif _type == 'Vector':
        return 'ShaderPropertyType.Vector'
    elif _type == '2D':
        return 'ShaderPropertyType.TexEnv'
    else:
        raise ValueError(f'Unknown type: {_type}')


def get_unity_color(color):
    if color == (0.0, 0.0, 0.0, 0.0):
        return 'Color.clear'
    elif color == (0.0, 0.0, 0.0, 1.0):
        return 'Color.black'
    elif color == (1.0, 1.0, 1.0, 1.0):
        return 'Color.white'
    else:
        return f'new Color({color[0]}f, {color[1]}f, {color[2]}f, {color[3]}f)'


def get_unity_vector(vector):
    if vector == (0.0, 0.0, 0.0, 0.0):
        return 'Vector4.zero'
    elif vector == (1.0, 1.0, 1.0, 1.0):
        return 'Vector4.one'
    else:
        return f'new Vector4({vector[0]}f, {vector[1]}f, {vector[2]}f, {vector[3]}f)'


def get_lua_color(color):
    if color == (0.0, 0.0, 0.0, 0.0):
        return 'Color.clear'
    elif color == (0.0, 0.0, 0.0, 1.0):
        return 'Color.black'
    elif color == (1.0, 1.0, 1.0, 1.0):
        return 'Color.white'
    else:
        return f'Color({color[0]}, {color[1]}, {color[2]}, {color[3]})'


def get_lua_vector(vector):
    if vector == (0.0, 0.0, 0.0, 0.0):
        return 'Vector4.zero'
    elif vector == (1.0, 1.0, 1.0, 1.0):
        return 'Vector4.one'
    else:
        return f'Vector4({vector[0]}, {vector[1]}, {vector[2]}, {vector[3]})'


def write_shader_info_cs(type_data, float_data, color_data, vector_data):
    with open(shader_info_cs_filename, 'w', encoding='utf-8',
              newline='\n') as f:
        # Begin
        f.write(
            indent_lines(
                """
// This file is generated. Do not edit it manually. Please edit .shaderproto files.

using System.Collections.Generic;
using UnityEngine;

namespace Nova.Generate
{
    public static class ShaderInfoDatabase
    {
""", 0) + '\n')

        # Type
        f.write(
            indent_lines(
                """
public static readonly IReadOnlyDictionary<string, IReadOnlyDictionary<string, ShaderPropertyType>> TypeData =
    new Dictionary<string, IReadOnlyDictionary<string, ShaderPropertyType>>
{
""", 8) + '\n')
        for shader_name, now_type_data in sorted(type_data.items()):
            f.write(
                indent_lines(
                    f"""
{{
    "{shader_name}",
    new Dictionary<string, ShaderPropertyType>
    {{
""", 12) + '\n')
            for name, _type in sorted(now_type_data.items()):
                f.write(
                    indent_lines(f'{{"{name}", {get_nova_type(_type)}}},', 20)
                    + '\n')
            f.write(indent_lines("""
    }
},
""", 12) + '\n')
        f.write(indent_lines('};', 8) + '\n\n')

        # Float
        f.write(
            indent_lines(
                """
public static readonly IReadOnlyDictionary<string, IReadOnlyDictionary<string, float>> FloatData =
    new Dictionary<string, IReadOnlyDictionary<string, float>>
{
""", 8) + '\n')
        for shader_name, now_float_data in sorted(float_data.items()):
            f.write(
                indent_lines(
                    f"""
{{
    "{shader_name}",
    new Dictionary<string, float>
    {{
""", 12) + '\n')
            for name, value in sorted(now_float_data.items()):
                f.write(indent_lines(f'{{"{name}", {value}f}},', 20) + '\n')
            f.write(indent_lines("""
    }
},
""", 12) + '\n')
        f.write(indent_lines('};', 8) + '\n\n')

        # Color
        f.write(
            indent_lines(
                """
public static readonly IReadOnlyDictionary<string, IReadOnlyDictionary<string, Color>> ColorData =
    new Dictionary<string, IReadOnlyDictionary<string, Color>>
{
""", 8) + '\n')
        for shader_name, now_color_data in sorted(color_data.items()):
            f.write(
                indent_lines(
                    f"""
{{
    "{shader_name}",
    new Dictionary<string, Color>
    {{
""", 12) + '\n')
            for name, color in sorted(now_color_data.items()):
                f.write(
                    indent_lines(f'{{"{name}", {get_unity_color(color)}}},',
                                 20) + '\n')
            f.write(indent_lines("""
    }
},
""", 12) + '\n')
        f.write(indent_lines('};', 8) + '\n\n')

        # Vector
        f.write(
            indent_lines(
                """
public static readonly IReadOnlyDictionary<string, IReadOnlyDictionary<string, Vector4>> VectorData =
    new Dictionary<string, IReadOnlyDictionary<string, Vector4>>
{
""", 8) + '\n')
        for shader_name, now_vector_data in sorted(vector_data.items()):
            f.write(
                indent_lines(
                    f"""
{{
    "{shader_name}",
    new Dictionary<string, Vector4>
    {{
""", 12) + '\n')
            for name, vector in sorted(now_vector_data.items()):
                f.write(
                    indent_lines(f'{{"{name}", {get_unity_vector(vector)}}},',
                                 20) + '\n')
            f.write(indent_lines("""
    }
},
""", 12) + '\n')
        f.write(indent_lines('};', 8) + '\n')

        # End
        f.write(indent_lines("""
    }
}
""", 0) + '\n')


def write_shader_info_lua(type_data, float_data, color_data, vector_data):
    with open(shader_info_lua_filename, 'w', encoding='utf-8',
              newline='\n') as f:
        # Begin
        f.write(
            '-- This file is generated. Do not edit it manually. Please edit .shaderproto files.\n\n'
        )

        # Type
        f.write('shader_type_data = {\n')
        for shader_name, now_type_data in sorted(type_data.items()):
            if not now_type_data:
                continue
            f.write(indent_lines(f'[\'{shader_name}\'] = {{', 4) + '\n')
            for name, _type in sorted(now_type_data.items()):
                f.write(indent_lines(f'{name} = \'{_type}\',', 8) + '\n')
            f.write(indent_lines('},', 4) + '\n')
        f.write('}\n\n')

        # Float
        f.write('shader_float_data = {\n')
        for shader_name, now_float_data in sorted(float_data.items()):
            f.write(indent_lines(f'[\'{shader_name}\'] = {{', 4) + '\n')
            for name, value in sorted(now_float_data.items()):
                f.write(indent_lines(f'{name} = {value:.15g},', 8) + '\n')
            f.write(indent_lines('},', 4) + '\n')
        f.write('}\n\n')

        # Color
        f.write('shader_color_data = {\n')
        for shader_name, now_color_data in sorted(color_data.items()):
            f.write(indent_lines(f'[\'{shader_name}\'] = {{', 4) + '\n')
            for name, color in sorted(now_color_data.items()):
                f.write(
                    indent_lines(f'{name} = {get_lua_color(color)},', 8) +
                    '\n')
            f.write(indent_lines('},', 4) + '\n')
        f.write('}\n\n')

        # Vector
        f.write('shader_vector_data = {\n')
        for shader_name, now_vector_data in sorted(vector_data.items()):
            f.write(indent_lines(f'[\'{shader_name}\'] = {{', 4) + '\n')
            for name, vector in sorted(now_vector_data.items()):
                f.write(
                    indent_lines(f'{name} = {get_lua_vector(vector)},', 8) +
                    '\n')
            f.write(indent_lines('},', 4) + '\n')
        f.write('}\n')


def generate_shader_info_database(filenames):
    print('generate_shader_info_database')

    type_data = {}
    float_data = {}
    color_data = {}
    vector_data = {}

    for filename in filenames:
        print(filename)
        path = os.path.join(shader_proto_dir, filename)
        (now_shader_name, now_type_data, now_float_data, now_color_data,
         now_vector_data) = parse_shader_properties(path)
        # Shader name should be tracked in type_data, even if there is no property
        type_data[now_shader_name] = now_type_data
        if now_float_data:
            float_data[now_shader_name] = now_float_data
        if now_color_data:
            color_data[now_shader_name] = now_color_data
        if now_vector_data:
            vector_data[now_shader_name] = now_vector_data

    print()

    write_shader_info_cs(type_data, float_data, color_data, vector_data)
    write_shader_info_lua(type_data, float_data, color_data, vector_data)


def main():
    filenames = sorted(x for x in os.listdir(shader_proto_dir)
                       if x.endswith('.shaderproto'))
    generate_shaders(filenames)
    generate_shader_info_database(filenames)


if __name__ == '__main__':
    main()
