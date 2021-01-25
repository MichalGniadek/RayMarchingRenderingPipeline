using System;
using UnityEngine;
using UnityEngine.Rendering;

[CreateAssetMenu(menuName = "Rendering/Ray Marching Render Pipeline")]
public class RMRenderPipelineAsset : RenderPipelineAsset
{
    public SDFScene scene = null;
    public Action updateDynamicParametersAction = null;
    RMRenderPipeline pipeline = null;

    public RayMarchingSettings settings;

    protected override RenderPipeline CreatePipeline()
    {
        pipeline = new RMRenderPipeline(scene, updateDynamicParametersAction, settings);
        return pipeline;
    }

    override protected void OnValidate()
    {
        scene.Compile();
        if (pipeline != null) pipeline.SetScene(scene);
    }
}

[System.Serializable]
public class RayMarchingSettings
{
    [Range(0.1f, 1)] public float resolution = 1;
    [Range(1, 1024)] public int steps = 256;
}