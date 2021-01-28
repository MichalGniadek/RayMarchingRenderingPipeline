using System;
using System.Text.RegularExpressions;
using UnityEngine;

namespace hlsl2csh
{
    public static class hlsl2csh
    {
        public static string Convert(string code)
        {
            string csharpCode = Regex.Replace(code, "@[^@]*@", "");
            csharpCode = Regex.Replace(csharpCode, "float[2|3|4] ", "Float4 ");
            csharpCode = Regex.Replace(csharpCode, @"[\d]+\.[\d]+", m => $"{m}f");
            csharpCode = Regex.Replace(csharpCode, @"float[2|3|4] *\(", "MakeVector(");
            return csharpCode;
        }

        public static Vector4 Swizzle(this float value, string components)
        {
            components = components.PadRight(4, ' ');
            Vector4 ret = new Vector4();
            for (int i = 0; i < 4; i++)
            {
                switch (components[i])
                {
                    case 'x':
                    case 'r':
                        ret[i] = value; break;
                    case 'y':
                    case 'g':
                        ret[i] = value; break;
                    case 'z':
                    case 'b':
                        ret[i] = value; break;
                    case 'w':
                    case 'a':
                        ret[i] = value; break;
                }
            }
            return ret;
        }

        public static Float4 MakeVector(float x, float y = 0, float z = 0, float w = 0)
        {
            return new Float4(x, y, z, w);
        }

        public static float length(Float4 v) => Mathf.Sqrt(v.x * v.x + v.y * v.y + v.z * v.z + v.w * v.w);
        public static float abs(float f) => Mathf.Abs(f);
        public static Float4 abs(Float4 vec) => vec.ForEach(Math.Abs);
        public static float min(float f1, float f2) => Mathf.Min(f1, f2);
        public static Float4 min(Float4 vec, float f) => vec.ForEach(v => Math.Min(v, f));
        public static float max(float f1, float f2) => Mathf.Max(f1, f2);
        public static Float4 max(Float4 vec, float f) => vec.ForEach(v => Math.Max(v, f));
        public static Float4 fmod(Float4 vec1, Float4 vec2) => vec1.ForEach(vec2, (v1, v2) => v1 - v2 * Mathf.Floor(v1 / v2));

        public struct Float4
        {
            public float x, y, z, w;

            public Float4(float x, float y = 0, float z = 0, float w = 0)
            {
                this.x = x;
                this.y = y;
                this.z = z;
                this.w = w;
            }

            public float this[int i]
            {
                get
                {
                    switch (i)
                    {
                        case 1: return y;
                        case 2: return z;
                        case 3: return w;
                        default: return x;
                    };
                }
                set
                {
                    switch (i)
                    {
                        case 1: y = value; break;
                        case 2: z = value; break;
                        case 3: w = value; break;
                        default: x = value; break;
                    };
                }
            }

            public Float4 ForEach(Func<float, float> f)
            {
                return new Float4(f(x), f(y), f(z), f(w));
            }

            public Float4 ForEach(Float4 value2, Func<float, float, float> f)
            {
                return new Float4(f(x, value2.x), f(y, value2.y), f(z, value2.z), f(w, value2.w));
            }

            public static implicit operator Float4(int i) => new Float4(i);
            public static implicit operator Float4(float f) => new Float4(f);
            public static implicit operator Float4(Vector3 vec) => new Float4(vec.x, vec.y, vec.z);

            public static Float4 operator +(Float4 f) => f;
            public static Float4 operator -(Float4 f) => new Float4(-f.x, -f.y, -f.z, -f.w);

            public static Float4 operator +(Float4 f1, Float4 f2)
                => new Float4(f1.x + f2.x, f1.y + f2.y, f1.z + f2.z, f1.w + f2.w);

            public static Float4 operator -(Float4 f1, Float4 f2)
                => new Float4(f1.x - f2.x, f1.y - f2.y, f1.z - f2.z, f1.w - f2.w);

            public static Float4 operator *(Float4 f1, Float4 f2)
                => new Float4(f1.x * f2.x, f1.y * f2.y, f1.z * f2.z, f1.w * f2.w);

            public static Float4 operator /(Float4 f1, Float4 f2)
                => new Float4(f1.x / f2.x, f1.y / f2.y, f1.z / f2.z, f1.w / f2.w);

            public Float4 Swizzle(string components)
            {
                components = components.PadRight(4, ' ');
                Float4 ret = new Float4();
                for (int i = 0; i < 4; i++)
                {
                    switch (components[i])
                    {
                        case 'x':
                        case 'r':
                            ret[i] = x; break;
                        case 'y':
                        case 'g':
                            ret[i] = y; break;
                        case 'z':
                        case 'b':
                            ret[i] = z; break;
                        case 'w':
                        case 'a':
                            ret[i] = w; break;
                    }
                }
                return ret;
            }
        }
    }
}