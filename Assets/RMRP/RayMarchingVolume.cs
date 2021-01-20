using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

public class RayMarchingVolume : MonoBehaviour
{
    [SerializeField] RayMarchingObject shape = null;
    [SerializeField] List<RayMarchingModifier> modifiers = new List<RayMarchingModifier>();

    public string GetShaderText(ref HashSet<RayMarchingScript> usedScripts)
    {
        usedScripts.Add(shape);
        foreach (var modifier in modifiers) usedScripts.Add(modifier);

        string s = "";

        s += "\tdistance = " + shape.FunctionName + "(position);\n";

        return s;
    }

    [MenuItem("Raymarching/ Refresh shaders &s")]
    static void RefreshAllVolumes()
    {
        HashSet<RayMarchingScript> usedScripts = new HashSet<RayMarchingScript>();
        string sdf_code = "";

        var volumes = FindObjectsOfType<RayMarchingVolume>();
        foreach (var v in volumes)
        {
            sdf_code += v.GetShaderText(ref usedScripts);
        }

        string text = "";

        foreach (var s in usedScripts)
        {
            text += s.Code + "\n\n";
        }

        text += "float rm_sceneSDF(float3 position){\n" +
                    "\tfloat3 mod_position;\n" +
                    "\tfloat distance;\n" +
                    "\tfloat distance2;\n";

        text += sdf_code;

        text += "\treturn distance;\n}";

        CreateShader(text);
    }

    public static void CreateShader(string s)
    {
        string write_path = "RMRP/Shaders/generated_rm.compute";

        StreamWriter writer = new StreamWriter(Application.dataPath + "/" + write_path, false);
        writer.Write(s);
        writer.Close();

        AssetDatabase.ImportAsset("RMRP/Shaders/main_rm.compute");
    }
}