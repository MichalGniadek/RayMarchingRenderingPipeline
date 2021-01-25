using UnityEngine;

public partial class SDFScene
{
    public void SetFloatParameter(string name, float value)
    {
        var p = parameters.Find(p => p.name == name);
        if (p == null)
        {
            Debug.LogError($"Float parameter with name {name} doesn't exist.");
        }
        else
        {
            (p as FloatParameter).value = value;
            floatBuffer.Set(floatBuffer.GetIndex(name), value);
        }
    }

    public void SetFloat2Parameter(string name, Vector2 value)
    {
        var p = parameters.Find(p => p.name == name);
        if (p == null)
        {
            Debug.LogError($"Float parameter with name {name} doesn't exist.");
        }
        else
        {
            (p as Float3Parameter).value = value;
            int index = floatBuffer.GetIndex(name);
            floatBuffer.Set(index + 0, value.x);
        }
    }

    public void SetFloat3Parameter(string name, Vector3 value)
    {
        var p = parameters.Find(p => p.name == name);
        if (p == null)
        {
            Debug.LogError($"Float parameter with name {name} doesn't exist.");
        }
        else
        {
            (p as Float3Parameter).value = value;
            int index = floatBuffer.GetIndex(name);
            floatBuffer.Set(index + 0, value.x);
            floatBuffer.Set(index + 1, value.y);
            floatBuffer.Set(index + 2, value.z);
        }
    }

    public void SetFloat4Parameter(string name, Vector4 value)
    {
        var p = parameters.Find(p => p.name == name);
        if (p == null)
        {
            Debug.LogError($"Float parameter with name {name} doesn't exist.");
        }
        else
        {
            (p as Float3Parameter).value = value;
            int index = floatBuffer.GetIndex(name);
            floatBuffer.Set(index + 0, value.x);
            floatBuffer.Set(index + 1, value.y);
            floatBuffer.Set(index + 2, value.z);
            floatBuffer.Set(index + 4, value.z);
        }
    }
}