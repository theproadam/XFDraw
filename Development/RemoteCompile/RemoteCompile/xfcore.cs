using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using System.Runtime.InteropServices;

namespace RemoteCompile
{
    public static class GL
    {
        static object ThreadSyncLock = new object();

        public static void Pass(Shader shader)
        {
            if (shader == null) 
                throw new ArgumentNullException();

            lock (ThreadSyncLock)
            { 
                

            }
    

        }
    }

    public class GLTexture
    {
        internal object ThreadLock = new object();
        int stride;

        public int Stride { get { return stride; } }

        public GLTexture(int Width, int Height, Type type)
        {
            stride = Marshal.SizeOf(type);
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct Vector3
    {
        public float x;
        public float y;
        public float z;
        /// <summary>
        /// Creates a new Vector3
        /// </summary>
        /// <param name="posX">X Value</param>
        /// <param name="posY">Y Value</param>
        /// <param name="posZ">Z Value</param>
        public Vector3(float posX, float posY, float posZ)
        {
            x = posX;
            y = posY;
            z = posZ;
        }

        /// <summary>
        /// Calculates the 3 dimensional distance between point A and Point B
        /// </summary>
        /// <param name="From">Point A</param>
        /// <param name="To">Point B</param>
        /// <returns></returns>
        public static float Distance(Vector3 From, Vector3 To)
        {
            return (float)Math.Sqrt(Math.Pow(From.x - To.x, 2) + Math.Pow(From.y - To.y, 2) + Math.Pow(From.z - To.z, 2));
        }
        /// <summary>
        /// Adds two Vector3 together
        /// </summary>
        /// <param name="A"></param>
        /// <param name="B"></param>
        /// <returns></returns>
        public static Vector3 operator +(Vector3 A, Vector3 B)
        {
            return new Vector3(A.x + B.x, A.y + B.y, A.z + B.z);
        }
        /// <summary>
        /// Substacts Vector B from Vector A
        /// </summary>
        /// <param name="A">Vector A</param>
        /// <param name="B">Vector B</param>
        /// <returns></returns>
        public static Vector3 operator -(Vector3 A, Vector3 B)
        {
            return new Vector3(A.x - B.x, A.y - B.y, A.z - B.z);
        }

        public static Vector3 operator -(float A, Vector3 B)
        {
            return new Vector3(A - B.x, A - B.y, A - B.z);
        }

        public static Vector3 operator -(Vector3 A, float B)
        {
            return new Vector3(A.x - B, A.y - B, A.z - B);
        }

        public static bool Compare(Vector3 A, Vector3 B)
        {
            return (A.x == B.x && A.y == B.y && A.z == B.z);
        }

        public static Vector3 operator *(Vector3 A, Vector3 B)
        {
            return new Vector3(A.x * B.x, A.y * B.y, A.z * B.z);
        }

        public static Vector3 operator *(Vector3 A, float B)
        {
            return new Vector3(A.x * B, A.y * B, A.z * B);
        }

        public static Vector3 operator *(float A, Vector3 B)
        {
            return new Vector3(A * B.x, A * B.y, A * B.z);
        }

        public static bool operator >(Vector3 A, float B)
        {
            return A.x > B & A.y > B & A.z > B;
        }

        public static bool operator <(Vector3 A, float B)
        {
            return A.x < B & A.y < B & A.z < B;
        }

        public void Clamp01()
        {
            if (x < 0) x = 0;
            if (x > 1) x = 1;

            if (y < 0) y = 0;
            if (y > 1) y = 1;

            if (z < 0) z = 0;
            if (z > 1) z = 1;
        }

        public Vector3 Abs()
        {
            return new Vector3(Math.Abs(x), Math.Abs(y), Math.Abs(z));
        }

        public static Vector3 LerpAngle(Vector3 a, Vector3 b, float t)
        {
            return new Vector3(Lerp1D(a.x, b.x, t), Lerp1D(a.y, b.y, t), Lerp1D(a.z, b.z, t));
        }

        static float Lerp1D(float a, float b, float t)
        {
            float val = Repeat(b - a, 360);
            if (val > 180f)
                val -= 360f;

            return a + val * Clamp01(t);
        }

        static float Repeat(float t, float length)
        {
            return Clamp(t - (float)Math.Floor(t / length) * length, 0f, length);
        }

        public Vector3 Repeat(float length)
        {
            float x1 = Clamp(x - (float)Math.Floor(x / length) * length, 0f, length);
            float y1 = Clamp(y - (float)Math.Floor(y / length) * length, 0f, length);
            float z1 = Clamp(z - (float)Math.Floor(z / length) * length, 0f, length);

            if (x1 > 180f) x1 -= 360f;
            if (y1 > 180f) y1 -= 360f;
            if (z1 > 180f) z1 -= 360f;

            return new Vector3(x1, y1, z1);
        }

        static float Clamp(float v, float min, float max)
        {
            if (v > max) return max;
            else if (v < min) return min;
            else return v;
        }

        static float Clamp01(float v)
        {
            if (v < 0) return 0;
            if (v > 1) return 1;
            else return v;
        }

        public static Vector3 Lerp(Vector3 a, Vector3 b, float t)
        {
            if (t > 1) t = 1;
            else if (t < 0) t = 0;
            return new Vector3(a.x + (b.x - a.x) * t, a.y + (b.y - a.y) * t, a.z + (b.z - a.z) * t);
        }

        public static Vector3 operator -(Vector3 A)
        {
            return new Vector3(-A.x, -A.y, -A.z);
        }

        public static Vector3 operator /(Vector3 a, float d)
        {
            return new Vector3(a.x / d, a.y / d, a.z / d);
        }

        public static float Magnitude(Vector3 vector)
        {
            return (float)Math.Sqrt(vector.x * vector.x + vector.y * vector.y + vector.z * vector.z);
        }


        const float EPSILON = 10E-4f;
        public bool isApproximately(Vector3 CompareTo)
        {
            return Math.Abs(CompareTo.x - x) < EPSILON && Math.Abs(CompareTo.y - y) < EPSILON && Math.Abs(CompareTo.z - z) < EPSILON;
        }

        /// <summary>
        /// Returns a string in the format of "Vector3 X: " + X + ", Y: " + Y + ", Z: " + Z
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return "X: " + x.ToString() + ", Y: " + y.ToString() + ", Z:" + z.ToString();
        }

        public static Vector3 Reflect(Vector3 inDirection, Vector3 inNormal)
        {
            return -2f * Dot(inNormal, inDirection) * inNormal + inDirection;
        }

        public static float Dot(Vector3 lhs, Vector3 rhs)
        {
            return lhs.x * rhs.x + lhs.y * rhs.y + lhs.z * rhs.z;
        }

        public static Vector3 Normalize(Vector3 value)
        {
            float num = Magnitude(value);
            if (num > 1E-05f)
            {
                return value / num;
            }
            return new Vector3(0, 0, 0);
        }

        public static Vector3 Max(Vector3 lhs, Vector3 rhs)
        {
            return new Vector3(Math.Max(lhs.x, rhs.x), Math.Max(lhs.y, rhs.y), Math.Max(lhs.z, rhs.z));
        }

        public static Vector3 Sin(Vector3 AngleDegrees)
        {
            return new Vector3((float)Math.Sin(AngleDegrees.x * (Math.PI / 180f)), (float)Math.Sin(AngleDegrees.y * (Math.PI / 180f)), (float)Math.Sin(AngleDegrees.z * (Math.PI / 180f)));
        }

        public static Vector3 Cos(Vector3 AngleDegrees)
        {
            return new Vector3((float)Math.Cos(AngleDegrees.x * (Math.PI / 180f)), (float)Math.Cos(AngleDegrees.y * (Math.PI / 180f)), (float)Math.Cos(AngleDegrees.z * (Math.PI / 180f)));
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct Vector2
    {
        public float x;
        public float y;

        public Vector2(float posX, float posY)
        {
            x = posX;
            y = posY;
        }

        public Vector2(Vector2 oldVector2)
        {
            x = oldVector2.x;
            y = oldVector2.y;
        }

        public static float Distance(Vector2 From, Vector2 To)
        {
            return (float)Math.Sqrt(Math.Pow(From.x - To.x, 2) + Math.Pow(From.y - To.y, 2));
        }

        public override string ToString()
        {
            return "Vector2 X: " + x.ToString() + ", Y: " + y.ToString();
        }

    }
}
