using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;

public abstract class RayMarchingScript : ScriptableObject
{
    [SerializeField, Multiline(10)] string code = "";
    public string Code => code;

    [SerializeField, HideInInspector] string functionName = "";
    public string FunctionName => functionName;

    [SerializeField] List<ScriptArgument> functionArguments = new List<ScriptArgument>();
    [SerializeField, HideInInspector] public List<ScriptArgument> FunctionArguments => functionArguments;

    readonly static Regex findFunctionsRegex =
        new Regex(@"(?:[\w\d]+)?\s+([\w\d]+)\s*\(([\w\d ,]*)\)(?=\s*{)");
    readonly static Regex findFunctionArguments = new Regex(@"([\w\d_]+)\s+([\w\d_]+)");

    void OnValidate()
    {
        MatchCollection matches = findFunctionsRegex.Matches(code);
        Match lastFunction = matches[matches.Count - 1];
        functionName = lastFunction.Groups[1].Value;

        functionArguments = new List<ScriptArgument>();
        string argumentText = lastFunction.Groups[2].Value;

        MatchCollection argumentMatches = findFunctionArguments.Matches(argumentText);
        foreach (Match arg in argumentMatches)
        {
            GroupCollection groups = arg.Groups;
            functionArguments.Add(new ScriptArgument
            {
                type = groups[1].Value,
                name = groups[2].Value,
            });
        }

        // Get rid of position argument
        functionArguments.RemoveAt(0);
    }
}


[System.Serializable]
public struct ScriptArgument
{
    public string type;
    public string name;
}