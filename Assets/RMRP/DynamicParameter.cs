using UnityEngine;

[System.Serializable]
public abstract class DynamicParameter
{
    public string name;

    public abstract void SetDataInBuffer(SDFScene scene);
    public abstract string Compile(SDFScene scene);
    protected string CompileGeneric<T>(CustomShaderBuffer<T> buffer, string type, T[] values) where T : unmanaged
    {
        while (buffer.alignment > 4 - values.Length)
        {
            buffer.values.Add(default);
            buffer.alignment = (buffer.alignment + 1) % 4;
        }
        buffer.nameToIndex.Add(name, buffer.values.Count);

        foreach (T v in values)
        {
            buffer.values.Add(v);
        }
        buffer.alignment = (buffer.alignment + values.Length) % 4;
        return $"\t{type} {name};\n";
    }
}

[System.Serializable]
public class FloatParameter : DynamicParameter
{
    public float value;
    public override string Compile(SDFScene scene) =>
        CompileGeneric<float>(scene.floatBuffer, "float", new float[] { value });

    public override void SetDataInBuffer(SDFScene scene)
    {
        scene.SetFloatParameter(name, value);
    }
}

[System.Serializable]
public class Float2Parameter : DynamicParameter
{
    public Vector2 value;
    public override string Compile(SDFScene scene) =>
        CompileGeneric<float>(scene.floatBuffer, "float2",
                                new float[] { value.x, value.y });

    public override void SetDataInBuffer(SDFScene scene)
    {
        scene.SetFloat2Parameter(name, value);
    }
}

[System.Serializable]
public class Float3Parameter : DynamicParameter
{
    public Vector3 value;
    public override string Compile(SDFScene scene) =>
        CompileGeneric<float>(scene.floatBuffer, "float3",
                                new float[] { value.x, value.y, value.z });

    public override void SetDataInBuffer(SDFScene scene)
    {
        scene.SetFloat3Parameter(name, value);
    }
}

[System.Serializable]
public class Float4Parameter : DynamicParameter
{
    public Vector4 value;
    public override string Compile(SDFScene scene) =>
        CompileGeneric<float>(scene.floatBuffer, "float4",
                                new float[] { value.x, value.y, value.z, value.w });

    public override void SetDataInBuffer(SDFScene scene)
    {
        scene.SetFloat4Parameter(name, value);
    }
}