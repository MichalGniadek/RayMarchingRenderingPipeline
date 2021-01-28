using System.Globalization;
using System.IO;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

public static partial class CodeGenerator
{
    public static ComputeShader LoadShader(string name) =>
            AssetDatabase.LoadAssetAtPath<ComputeShader>("Assets" + MainPath + $"rmrp_{name}.compute");

    public static void GenerateComputeShader(string name, string buffers, string mainCode, string skybox_code)
    {
        RayMarchingSettings settings =
            (GraphicsSettings.currentRenderPipeline as RMRenderPipelineAsset).settings;

        string s = (shaderCommon + shaderStandardMain)
            .Replace("__resolution", (1 / settings.resolution).ToString("F3", CultureInfo.InvariantCulture))
            .Replace("__steps", settings.steps.ToString());

        s += HandleVariants(helperFunctions, "#", false);

        s += "\n// BUFFERS\ncbuffer SDFBuffers{\n";
        s += buffers;
        s += "};\n";

        s += "\n// USER SHADER\n#line 0\n";

        string shaderCode = HandleVariants(mainCode, "#", false);

        s += "float rm_sceneSDF(float3 position, int step){\n";
        s += HandleVariants(shaderCode, "@", false);
        s += "\n\treturn distance;\n}\n\n";

        s += "float4 rm_materialSceneSDF(float3 position, int step){\n";
        s += HandleVariants(shaderCode, "@", true);
        s += "\n\treturn color;\n}";

        s += "float4 skybox_color(int step){\n";
        s += skybox_code;
        s += "\n\treturn color;\n}";

        WriteToFile($"rmrp_{name}.compute", s);
    }

    public static string shaderCommon = @"
// COMMON
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
    ";

    public static string shaderStandardMain = @"
// MAIN

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

#pragma kernel CSMain
[numthreads(8,8,1)]
void CSMain (uint3 id : SV_DispatchThreadID)
{
    //change it later so you can blend input color depending on alpha
    Ray r = calculateRay(id.xy);
    output[id.xy] = raymarch(r);
}
";
}