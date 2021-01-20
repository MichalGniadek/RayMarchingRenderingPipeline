using System.Collections.Generic;

public static class ShaderArgument<T>
{
    public static Dictionary<RayMarchingVolume, Dictionary<string, T>> arguments =
        new Dictionary<RayMarchingVolume, Dictionary<string, T>>();

    public static void SetArgument(RayMarchingVolume go, string name, T value)
    {
        Dictionary<string, T> args;
        if (!arguments.TryGetValue(go, out args))
        {
            args = new Dictionary<string, T>();
            arguments.Add(go, args);
        }
        args[name] = value;
    }

    public static T GetArgument(RayMarchingVolume go, string name)
    {
        if (arguments.TryGetValue(go, out Dictionary<string, T> args))
        {
            if (args.TryGetValue(name, out T val))
            {
                return val;
            }
        }
        return default;
    }
}