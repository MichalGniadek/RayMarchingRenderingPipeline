using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

public static class ShaderGenerator
{
    static string Path(string name) => $"RMRP/Shaders/rmrp_{name}.compute";

    public static ComputeShader Generate(string name, string buffers, string main_code, string skybox_code)
    {
        string writePath = Path(name);

        RayMarchingSettings settings =
            (GraphicsSettings.currentRenderPipeline as RMRenderPipelineAsset).settings;

        string s = main
            .Replace("__resolution", (1 / settings.resolution).ToString("F3", CultureInfo.InvariantCulture))
            .Replace("__steps", settings.steps.ToString());

        s += include;

        s += "\n// BUFFERS\ncbuffer SDFBuffers{\n";
        s += buffers;
        s += "};\n";

        s += "\n// USER SHADER\n#line 0\n";

        s += "float rm_sceneSDF(float3 position, int step){\n";
        s += Regex.Replace(main_code, "@[^@]*@", "");
        s += "\n\treturn distance;\n}\n\n";

        s += "float4 rm_materialSceneSDF(float3 position, int step){\n";
        s += Regex.Replace(main_code, "@", "");
        s += "\n\treturn color;\n}";

        s += "float4 skybox_color(int step){\n";
        s += skybox_code;
        s += "\n\treturn color;\n}";

        StreamWriter writer = new StreamWriter(Application.dataPath + "/" + writePath, false);
        writer.Write(s);
        writer.Close();

        AssetDatabase.ImportAsset("Assets/" + writePath, ImportAssetOptions.ForceUpdate);

        return AssetDatabase.LoadAssetAtPath<ComputeShader>("Assets/" + writePath);
    }

    public static ComputeShader LoadShader(string name) =>
        AssetDatabase.LoadAssetAtPath<ComputeShader>("Assets/" + Path(name));


    public static string include = @"
// INCLUDE
float planeSD(float3 position)
{
    return position.y;
}

float sphereSD(float3 position, float radius)
{
    return length(position) - radius;
}

float boxSD(float3 p, float3 b)
{
    float3 q = abs(p) - b;
    return length(max(q,0.0)) + min(max(q.x,max(q.y,q.z)), 0.0);
}

float3 repeat(float3 position, float3 repeat)
{
    return fmod(abs(position+0.5*repeat), repeat)-0.5*repeat;
}

float smoothUnion_m(float a, float b, inout float4 color_a, float4 color_b, float blend)
{
    float m = min(a, b);

    float h_dist = max(blend - abs(a - b), 0.0) / blend;
    m -= h_dist*h_dist*blend*(1.0/4.0);

    color_a = lerp(color_a, color_b, saturate(a - m));//to be optimized

    return m;
}

float smoothUnion(float a, float b, float blend)
{
    float m = min(a, b);

    float h_dist = max(blend - abs(a - b), 0.0) / blend;
    m -= h_dist*h_dist*blend*(1.0/4.0);

    return m;
}

float3 transformModifier(float3 position, float3 transform)
{
    return position - transform;
}

float4 unlitMaterial(float4 color)
{
    return color;
}

float4 litMaterial(float3 normal, float3 light_direction, float4 color)
{
    return saturate(dot(-normalize(light_direction), normal)) * color;
}
";
    public static string main = @"
// MAIN
#pragma kernel CSMain

float rm_sceneSDF(float3 position, int step);
float4 rm_materialSceneSDF(float3 position, int step);
float4 skybox_color(int step);

float4x4 camera_to_world;
float4x4 inverse_projection;
uniform RWTexture2D<float4> output;

struct Ray
{
    float3 origin;
    float3 direction;
};

float2 getOutputDimensions()
{
    float2 dimension;
    output.GetDimensions(dimension.x, dimension.y);
    return dimension;
}

Ray calculateRay(uint2 screen_pos)
{
    Ray r;

    float2 uv = (screen_pos.xy * __resolution) / getOutputDimensions() * 2 - 1;

    r.origin = mul(camera_to_world, float4(0,0,0,1)).xyz;
    r.direction = mul(inverse_projection, float4(uv.xy,0,1)).xyz;

    r.direction /= abs(r.direction.z);

    r.direction = mul(camera_to_world, float4(r.direction,0)).xyz;
    r.direction = normalize(r.direction);

    return r;
}

//technique from http://www.iquilezles.org/www/articles/normalsSDF/normalsSDF.htm
float3 calculate_normal(float3 pos)
{
    const float h = 0.0001;
    const float2 k = float2(h,-h);
    return normalize( k.xyy * rm_sceneSDF( pos + k.xyy, 0) +
                      k.yyx * rm_sceneSDF( pos + k.yyx, 0) +
                      k.yxy * rm_sceneSDF( pos + k.yxy, 0) +
                      k.xxx * rm_sceneSDF( pos + k.xxx, 0) );
}

float4 raymarch(Ray r)
{
    float dist = 0;
    float min_dist = 0.0001f;
	for (int i = 0; i < __steps; i++)
	{
        float current_distance = rm_sceneSDF(r.origin + r.direction * dist, i);
        dist += current_distance;
		if (current_distance < min_dist)
        {
            float4 col = rm_materialSceneSDF(r.origin + r.direction * dist, i);
            //col.rgb = pow(col.rgb, float3(0.4545, 0.4545, 0.4545)); //Gamma correction
            return col;
        }
	}

	return skybox_color(__steps);
}

[numthreads(8,8,1)]
void CSMain (uint3 id : SV_DispatchThreadID)
{
    //change it later so you can blend input color depending on alpha
    Ray r = calculateRay(id.xy);
    output[id.xy] = raymarch(r);
}
";
}