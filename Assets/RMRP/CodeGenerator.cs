using System.IO;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

public static partial class CodeGenerator
{
    private static string MainPath = "/RMRP/CodeGen/";

    private static void WriteToFile(string fileName, string text)
    {
        StreamWriter writer = new StreamWriter(Application.dataPath + MainPath + fileName, false);
        writer.Write(text);
        writer.Close();

        AssetDatabase.ImportAsset("Assets" + MainPath + fileName, ImportAssetOptions.ForceUpdate);
    }

    static string HandleVariants(string code, string sep, bool keep)
    {
        if (keep) return Regex.Replace(code, sep, "");
        else return Regex.Replace(code, $"{sep}[^{sep}]*{sep}", "");
    }

    public static string helperFunctions = @"
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

// float4 litMaterial(float3 normal, float3 light_direction, float4 color)
// {
//     return saturate(dot(-normalize(light_direction), normal)) * color;
// }
 ";
}