using System.Collections.Generic;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine;
using UnityEngine.Rendering;

[CreateAssetMenu(fileName = "SDFScene", menuName = "RayMarchingRP/SDFScene", order = 0)]
public partial class SDFScene : ScriptableObject
{
    public string main_code = null;
    public string skybox_code = null;

    ComputeShader sdfShader = null;
    public ComputeShader SDFShader
    {
        private set => sdfShader = value;
        get => sdfShader ?? ShaderGenerator.LoadShader(this.name);
    }

    [SerializeReference]
    public List<DynamicParameter> parameters = new List<DynamicParameter>();

    [SerializeField]
    public CustomShaderBuffer<float> floatBuffer;

    public void UpdateBuffersData(CommandBuffer buffer, ComputeShader shader)
    {
        int bufferPropertyID = Shader.PropertyToID("SDFBuffers");
        floatBuffer.UpdateBufferData(buffer, shader, bufferPropertyID);
    }

    public void SetAllParameters()
    {
        foreach (var param in parameters)
        {
            if (param.name == "") continue;
            param.SetDataInBuffer(this);
        }
    }

    public void Compile(bool onlyBuffers = false)
    {
        string buffer_text = "";

        floatBuffer.Cleanup();
        foreach (var param in parameters)
        {
            if (param.name == "")
            {
                Debug.LogError("Parameter name can't be empty");
                return;
            }
            buffer_text += param.Compile(this);
        }
        floatBuffer.Setup();

        SDFShader = ShaderGenerator.Generate(this.name, buffer_text, main_code, skybox_code);
    }

    void OnEnable()
    {
        Compile();
    }

    void OnDisable()
    {
        floatBuffer.Cleanup();
    }
}

[System.Serializable]
public class CustomShaderBuffer<T> where T : unmanaged
{
    // Variables for compiling
    public List<T> values;
    public int alignment;

    // Buffer variables
    public Dictionary<string, int> nameToIndex;
    public NativeArray<T> array;
    public ComputeBuffer buffer;

    public void Cleanup()
    {
        values = new List<T>();
        alignment = 0;
        nameToIndex = new Dictionary<string, int>();
        if (array != null && array.IsCreated) array.Dispose();
        if (buffer != null && buffer.IsValid()) buffer.Dispose();
    }

    public void Setup()
    {
        if (values.Count == 0) return;
        while (values.Count * UnsafeUtility.SizeOf<T>() % 16 != 0) values.Add(default);

        array = new NativeArray<T>(values.ToArray(), Allocator.Persistent);
        buffer = new ComputeBuffer(values.Count, UnsafeUtility.SizeOf<T>(),
            ComputeBufferType.Constant);
        buffer.SetData(array);
    }

    public int GetIndex(string name) => nameToIndex[name];

    public void Set(int index, T value)
    {
        array[index] = value;
    }

    public void UpdateBufferData(CommandBuffer cmdBuffer, ComputeShader shader, int bufferPropertyID)
    {
        if (buffer != null && buffer.IsValid() && array != null && array.IsCreated)
        {
            cmdBuffer.SetComputeBufferData(buffer, array);
            cmdBuffer.SetComputeConstantBufferParam(shader, bufferPropertyID,
                        buffer, 0, buffer.stride * buffer.count);
        }
    }
}