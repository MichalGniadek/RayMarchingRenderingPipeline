using System.Globalization;
using System.IO;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

public static partial class CodeGenerator
{
    public static void GenerateCSharpScript(string name, string buffers, string mainCode)
    {
        string s = "using UnityEngine;\nusing static hlsl2csh.hlsl2csh;\n";
        s += "namespace RayMarchingScenes\n{\n";

        s += $"\t public class {name.Replace(" ", "_")}_gen\n{{\n";

        s += hlsl2csh.hlsl2csh.Convert(buffers);

        s += hlsl2csh.hlsl2csh.Convert(HandleVariants(helperFunctions, "#", true));

        s += "\t\tpublic float SceneSDF(Float4 position)\n{\n";

        s += hlsl2csh.hlsl2csh.Convert(HandleVariants(mainCode, "#", true));

        s += "\nreturn distance;\n}\n}\n}";

        WriteToFile(name.Replace(" ", "_") + "_gen.cs", s);
    }
}