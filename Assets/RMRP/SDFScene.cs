using System.Collections.Generic;
using System.IO;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

[CreateAssetMenu(fileName = "SDFScene", menuName = "RayMarchingRP/SDFScene", order = 0)]
public class SDFScene : ScriptableObject
{
    [TextArea(5, 50)] public string code = null;
    const string write_path = "RMRP/Shaders/rmrp.compute";
    public ComputeShader LoadShader() =>
        AssetDatabase.LoadAssetAtPath<ComputeShader>("Assets/" + write_path);
    int bufferPropertyID = -1;

    [SerializeReference]
    public List<DynamicParameter> parameters = new List<DynamicParameter>();

    [SerializeField] ParameterData<float> floatParameters;

    const double wait_time = 1f;
    double lastInspectorChange = 0f;

    public void ChangedParameterDefault()
    {
        lastInspectorChange = -1f;
        foreach (var param in parameters)
        {
            if (param.name == "") continue;
            switch (param)
            {
                case DynamicFloatParameter fParam:
                    SetFloatParameter(param.name, fParam.value);
                    break;
            }
        }
    }

    public void OnValidate()
    {
        if (lastInspectorChange == -1f)
        {
            lastInspectorChange = 0f;
        }
        else
        {
            lastInspectorChange = EditorApplication.timeSinceStartup;
        }
    }

    public void SetFloatParameter(string name, float value)
    {
        floatParameters.SetParameter(name, value);
    }

    public void UpdateBuffers(CommandBuffer buffer, ComputeShader shader)
    {
        floatParameters.Update(buffer, shader, bufferPropertyID);
    }

    void EditorUpdate()
    {
        if (lastInspectorChange != 0 && lastInspectorChange != -1 &&
            EditorApplication.timeSinceStartup - lastInspectorChange > wait_time)
        {
            lastInspectorChange = 0;
            Recompile();
        }
    }

    public void Recompile()
    {
        string s = ScriptTemplates.include + ScriptTemplates.script_commons + ScriptTemplates.main;

        s += "\n// BUFFERS\ncbuffer SDFBuffers{\n";

        List<float> floatList = new List<float>();
        floatParameters.names = new Dictionary<string, int>();
        foreach (var param in parameters)
        {
            if (param.name == "") continue;
            switch (param)
            {
                case DynamicFloatParameter fParam:
                    s += "\tfloat ";
                    floatList.Add(fParam.value);
                    floatParameters.names.Add(param.name, floatList.Count - 1);
                    break;
            }
            s += param.name + ";\n";
        }

        while (floatList.Count * sizeof(float) % 16 != 0) floatList.Add(0f);

        s += "};\n";

        s += "\n// USER SHADER\n#line 0\n" + code;
        StreamWriter writer = new StreamWriter(Application.dataPath + "/" + write_path, false);
        writer.Write(s);
        writer.Close();

        AssetDatabase.ImportAsset("Assets/" + write_path);

        floatParameters.Setup(floatList);
    }

    void OnEnable()
    {
        bufferPropertyID = Shader.PropertyToID("SDFBuffers");
        EditorApplication.update += EditorUpdate;
    }

    void OnDisable()
    {
        EditorApplication.update -= EditorUpdate;
        floatParameters.Cleanup();
    }
}

[System.Serializable]
public struct ParameterData<T> where T : unmanaged
{
    public Dictionary<string, int> names;
    public NativeArray<T> array;
    public ComputeBuffer buffer;

    public void Cleanup()
    {
        if (array != null && array.IsCreated) array.Dispose();
        if (buffer != null && buffer.IsValid()) buffer.Dispose();
    }

    public void Setup(List<T> l)
    {
        Cleanup();
        if (l.Count == 0) return;
        array = new NativeArray<T>(l.ToArray(), Allocator.Persistent);
        buffer = new ComputeBuffer(l.Count, UnsafeUtility.SizeOf<T>(),
            ComputeBufferType.Constant);
        buffer.SetData(array);
    }

    public void SetParameter(string name, T value)
    {
        array[names[name]] = value;
    }

    public void Update(CommandBuffer cmdBuffer, ComputeShader shader, int bufferPropertyID)
    {
        if (buffer != null && array != null)
        {
            cmdBuffer.SetComputeBufferData(buffer, array);
            cmdBuffer.SetComputeConstantBufferParam(shader, bufferPropertyID,
                        buffer, 0, buffer.stride * buffer.count);
        }
    }
}

[System.Serializable]
public class DynamicParameter
{
    public string name;
}

[System.Serializable]
public class DynamicFloatParameter : DynamicParameter
{
    public float value;
}

[System.Serializable]
public class DynamicFloat3Parameter : DynamicParameter
{
    public Vector3 value;
}

public static class ScriptTemplates
{
    public const string script_commons = @"
// COMMONS
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

    float2 uv = screen_pos.xy / getOutputDimensions() * 2 - 1;

    r.origin = mul(camera_to_world, float4(0,0,0,1)).xyz;
    r.direction = mul(inverse_projection, float4(uv.xy,0,1)).xyz;

    r.direction /= abs(r.direction.z);

    r.direction = mul(camera_to_world, float4(r.direction,0)).xyz;
    r.direction = normalize(r.direction);

    return r;
}
";

    public const string include = @"
// INCLUDE
float plane(float3 position)
{
    return position.y;
}

float sphere(float3 position, float radius)
{
    return length(position) - radius;
}

float3 repeat(float3 position, float3 repeat)
{
    //float3 cell = floor((abs(position)+0.5*repeat)/repeat);

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
";

    public const string main = @"
// MAIN
#pragma kernel CSMain

float rm_sceneSDF(float3 position);
float4 rm_materialSceneSDF(float3 position);

//technique from http://www.iquilezles.org/www/articles/normalsSDF/normalsSDF.htm
float3 calculate_normal(float3 pos)
{
    const float h = 0.01;
    const float2 k = float2(h,-h);
    return normalize( k.xyy * rm_sceneSDF( pos + k.xyy ) +
                      k.yyx * rm_sceneSDF( pos + k.yyx ) +
                      k.yxy * rm_sceneSDF( pos + k.yxy ) +
                      k.xxx * rm_sceneSDF( pos + k.xxx ) );
}

float4 raymarch(Ray r)
{
    const int STEP = 1024;
    float dist = 0;
	for (int i = 0; i < STEP; i++)
	{
        float current_distance = rm_sceneSDF(r.origin + r.direction * dist);
		if (current_distance < 0.001f * dist)//not sure about that * dist bit
        {
            float4 col = rm_materialSceneSDF(r.origin + r.direction * dist);
            //col.rgb = pow(col.rgb, float3(0.4545, 0.4545, 0.4545)); //Gamma correction
            return col;
        }
        dist += current_distance;
	}

	return float4(1, 0, 1, 1);
}

[numthreads(32,32,1)]
void CSMain (uint3 id : SV_DispatchThreadID)
{
    //change it later so you can blend input color depending on alpha
    Ray r = calculateRay(id.xy);
    output[id.xy] = raymarch(r);
}
";
}