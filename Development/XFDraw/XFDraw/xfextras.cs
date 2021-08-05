using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Drawing;
using System.Drawing.Imaging;
using xfcore.Buffers;
using xfcore.Extras;

namespace xfcore.Extras
{
    public class RenderThread
    {
        Thread T;
        bool DontStop = true;
        double TickRate;
        double NextTimeToFire = 0;
        bool finished = false;
        public object RenderLock = new object();

        public bool isStopped
        {
            get { return finished; }
        }

        public bool isAlive
        {
            get { return T.IsAlive; }
        }

        public RenderThread(float TargetFrameTime)
        {
            TickRate = TargetFrameTime;
        }

        public RenderThread(int TargetFrameRate)
        {
            TickRate = 1000f / (float)TargetFrameRate;
        }

        public delegate void TimerFire();
        public event TimerFire RenderFrame;

        public void SetTickRate(float TickRateInMs)
        {
            TickRate = TickRateInMs;
            NextTimeToFire = 0;
        }

        public void Start()
        {
            DontStop = true;
            T = new Thread(RenderCode);
            T.Start();
        }

        public void Abort()
        {
            T.Abort();
        }

        public void Stop()
        {
            DontStop = false;
        }

        void RenderCode()
        {
            Stopwatch sw = new Stopwatch();

            sw.Start();
            while (DontStop)
            {
                if (sw.Elapsed.TotalMilliseconds >= NextTimeToFire)
                {
                    NextTimeToFire = sw.Elapsed.TotalMilliseconds + TickRate;
                    lock (RenderLock)
                    {
                        RenderFrame();
                    }
                }
            }

            finished = true;
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

        public static Vector3 Cross(Vector3 lhs, Vector3 rhs)
        {
            return new Vector3(
                lhs.y * rhs.z - lhs.z * rhs.y,
                lhs.z * rhs.x - lhs.x * rhs.z,
                lhs.x * rhs.y - lhs.y * rhs.x);
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

        public static Vector2 operator +(Vector2 A, Vector2 B)
        {
            return new Vector2(A.x + B.x, A.y + B.y);
        }

        public static Vector2 operator -(Vector2 A, Vector2 B)
        {
            return new Vector2(A.x - B.x, A.y - B.y);
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

    [StructLayout(LayoutKind.Sequential)]
    public struct Color4
    {
        public byte B;
        public byte G;
        public byte R;
        public byte A;

        public Color4(byte r, byte g, byte b)
        {
            A = 255;
            R = r;
            G = g;
            B = b;
        }

        public Color4(byte a, byte r, byte g, byte b)
        {
            A = a;
            R = r;
            G = g;
            B = b;
        }

        public static explicit operator int(Color4 color)
        {
            return ((((((byte)color.A << 8) | (byte)color.R) << 8) | (byte)color.G) << 8) | (byte)color.B;
        }

    }

    [StructLayout(LayoutKind.Sequential)]
    public unsafe struct Matrix4x4
    {
        public float X0Y0;
        public float X1Y0;
        public float X2Y0;
        public float X3Y0;

        public float X0Y1;
        public float X1Y1;
        public float X2Y1;
        public float X3Y1;

        public float X0Y2;
        public float X1Y2;
        public float X2Y2;
        public float X3Y2;

        public float X0Y3;
        public float X1Y3;
        public float X2Y3;
        public float X3Y3;

        public void SetZeroMatrix()
        {
            fixed (Matrix4x4* mat4 = &this)
            {
                float* ptr = (float*)mat4;

                for (int i = 0; i < 16; i++)
                    ptr[i] = 0;
            }
        }

        public void SetIdentityMatrix()
        {
            fixed (Matrix4x4* mat4 = &this)
            {
                float* ptr = (float*)mat4;

                ptr[0] = 1;
                ptr[1] = 0;
                ptr[2] = 0;
                ptr[3] = 0;

                ptr[4] = 0;
                ptr[5] = 1;
                ptr[6] = 0;
                ptr[7] = 0;

                ptr[8] = 0;
                ptr[9] = 0;
                ptr[10] = 1;
                ptr[11] = 0;

                ptr[12] = 0;
                ptr[13] = 0;
                ptr[14] = 0;
                ptr[15] = 1;

            }
        }

        public Matrix4x4(bool makeIdentityMatrix)
        {
            fixed (Matrix4x4* mat4 = &this)
            {
                //tell the compiler to screw off
            }

            if (makeIdentityMatrix)
                this.SetIdentityMatrix();
            else
                this.SetZeroMatrix();
        }

        public Matrix4x4(Matrix3x3 mat3)
        {
            X0Y0 = mat3.X0Y0;
            X1Y0 = mat3.X1Y0;
            X2Y0 = mat3.X2Y0;
            X3Y0 = 0;

            X0Y1 = mat3.X0Y1;
            X1Y1 = mat3.X1Y1;
            X2Y1 = mat3.X2Y1;
            X3Y1 = 0;

            X0Y2 = mat3.X0Y2;
            X1Y2 = mat3.X1Y2;
            X2Y2 = mat3.X2Y2;
            X3Y2 = 0;

            X0Y3 = 0;
            X1Y3 = 0;
            X2Y3 = 0;
            X3Y3 = 1;
        }

        /// <summary>
        /// Create Camera Rotation where EulerAngles are yaw (z axis) * pitch (y axis) * roll (x axis)
        /// </summary>
        /// <param name="EulerAngles"></param>
        /// <returns></returns>
        public static Matrix4x4 RotationMatrix(Vector3 EulerAngles)
        {
            Matrix4x4 result = new Matrix4x4();
            const float deg2rad = (float)(Math.PI / 180d);

            float cosa = (float)Math.Cos(deg2rad * EulerAngles.z);
            float cosb = (float)Math.Cos(deg2rad * EulerAngles.y);
            float cosy = (float)Math.Cos(deg2rad * EulerAngles.x);

            float sina = (float)Math.Sin(deg2rad * EulerAngles.z);
            float sinb = (float)Math.Sin(deg2rad * EulerAngles.y);
            float siny = (float)Math.Sin(deg2rad * EulerAngles.x);

            result.X0Y0 = cosa * cosb;
            result.X1Y0 = cosa * sinb * siny - sina * cosy;
            result.X2Y0 = cosa * sinb * cosy + sina * siny;
            result.X3Y0 = 0;

            result.X0Y1 = sina * cosb;
            result.X1Y1 = sina * sinb * siny + cosa * cosy;
            result.X2Y1 = sina * sinb * cosy - cosa * siny;
            result.X3Y1 = 0;

            result.X0Y2 = -sinb;
            result.X1Y2 = cosb * siny;
            result.X2Y2 = cosb * cosy;
            result.X3Y2 = 0;

            result.X0Y3 = 0;
            result.X1Y3 = 0;
            result.X2Y3 = 0;
            result.X3Y3 = 0;

            return result;
        }

        public static Matrix4x4 TranslationMatrix(Vector3 Position)
        {
            Matrix4x4 result = new Matrix4x4();
            result.SetIdentityMatrix();
            result.X3Y0 = Position.x;
            result.X3Y1 = Position.y;
            result.X3Y2 = Position.z;
            result.X3Y3 = 1;

            return result;
        }

        public static Matrix4x4 operator +(Matrix4x4 A, Matrix4x4 B)
        {
            Matrix4x4 reslt = new Matrix4x4();

            float* ptr = (float*)&reslt;
            float* ptra = (float*)&A;
            float* ptrb = (float*)&B;

            for (int i = 0; i < 16; i++)
                ptr[i] = ptra[i] + ptrb[i];

            return reslt;
        }

        public static Matrix4x4 operator -(Matrix4x4 A, Matrix4x4 B)
        {
            Matrix4x4 reslt = new Matrix4x4();

            float* ptr = (float*)&reslt;
            float* ptra = (float*)&A;
            float* ptrb = (float*)&B;

            for (int i = 0; i < 16; i++)
                ptr[i] = ptra[i] - ptrb[i];

            return reslt;
        }

        public static Matrix4x4 operator *(float A, Matrix4x4 B)
        {
            float* ptr = (float*)&B;

            for (int i = 0; i < 16; i++)
                ptr[i] *= A;

            return B;
        }

        public static Matrix4x4 operator *(Matrix4x4 B, float A)
        {
            float* ptr = (float*)&B;

            for (int i = 0; i < 16; i++)
                ptr[i] *= A;

            return B;
        }

        public static Matrix4x4 operator *(Matrix4x4 A, Matrix4x4 B)
        {
            Matrix4x4 result = new Matrix4x4();

            result.X0Y0 = A.X0Y0 * B.X0Y0 + A.X1Y0 * B.X0Y1 + A.X2Y0 * B.X0Y2 + A.X3Y0 * B.X0Y3;
            result.X1Y0 = A.X0Y0 * B.X1Y0 + A.X1Y0 * B.X1Y1 + A.X2Y0 * B.X1Y2 + A.X3Y0 * B.X1Y3;
            result.X2Y0 = A.X0Y0 * B.X2Y0 + A.X1Y0 * B.X2Y1 + A.X2Y0 * B.X2Y2 + A.X3Y0 * B.X2Y3;
            result.X3Y0 = A.X0Y0 * B.X3Y0 + A.X1Y0 * B.X3Y1 + A.X2Y0 * B.X3Y2 + A.X3Y0 * B.X3Y3;

            result.X0Y1 = A.X0Y1 * B.X0Y0 + A.X1Y1 * B.X0Y1 + A.X2Y1 * B.X0Y2 + A.X3Y1 * B.X0Y3;
            result.X1Y1 = A.X0Y1 * B.X1Y0 + A.X1Y1 * B.X1Y1 + A.X2Y1 * B.X1Y2 + A.X3Y1 * B.X1Y3;
            result.X2Y1 = A.X0Y1 * B.X2Y0 + A.X1Y1 * B.X2Y1 + A.X2Y1 * B.X2Y2 + A.X3Y1 * B.X2Y3;
            result.X3Y1 = A.X0Y1 * B.X3Y0 + A.X1Y1 * B.X3Y1 + A.X2Y1 * B.X3Y2 + A.X3Y1 * B.X3Y3;

            result.X0Y2 = A.X0Y2 * B.X0Y0 + A.X1Y2 * B.X0Y1 + A.X2Y2 * B.X0Y2 + A.X3Y2 * B.X0Y3;
            result.X1Y2 = A.X0Y2 * B.X1Y0 + A.X1Y2 * B.X1Y1 + A.X2Y2 * B.X1Y2 + A.X3Y2 * B.X1Y3;
            result.X2Y2 = A.X0Y2 * B.X2Y0 + A.X1Y2 * B.X2Y1 + A.X2Y2 * B.X2Y2 + A.X3Y2 * B.X2Y3;
            result.X3Y2 = A.X0Y2 * B.X2Y0 + A.X1Y2 * B.X2Y1 + A.X2Y2 * B.X2Y2 + A.X3Y2 * B.X3Y3;

            result.X0Y3 = A.X0Y3 * B.X0Y0 + A.X1Y3 * B.X0Y1 + A.X2Y3 * B.X0Y2 + A.X3Y3 * B.X0Y3;
            result.X1Y3 = A.X0Y3 * B.X1Y0 + A.X1Y3 * B.X1Y1 + A.X2Y3 * B.X1Y2 + A.X3Y3 * B.X1Y3;
            result.X2Y3 = A.X0Y3 * B.X2Y0 + A.X1Y3 * B.X2Y1 + A.X2Y3 * B.X2Y2 + A.X3Y3 * B.X2Y3;
            result.X3Y3 = A.X0Y3 * B.X2Y0 + A.X1Y3 * B.X2Y1 + A.X2Y3 * B.X2Y2 + A.X3Y3 * B.X3Y3;

            return result;
        }

        public static Vector4 operator *(Matrix4x4 A, Vector4 B)
        {
            Vector4 result = new Vector4();
            result.x = A.X0Y0 * B.x + A.X1Y0 * B.y + A.X2Y0 * B.z + A.X3Y0 * B.w;
            result.y = A.X0Y1 * B.x + A.X1Y1 * B.y + A.X2Y1 * B.z + A.X3Y1 * B.w;
            result.z = A.X0Y2 * B.x + A.X1Y2 * B.y + A.X2Y2 * B.z + A.X3Y2 * B.w;
            result.w = A.X0Y3 * B.x + A.X1Y3 * B.y + A.X2Y3 * B.z + A.X3Y3 * B.w;

            return result;
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    public unsafe struct Matrix3x3
    {
        public float X0Y0;
        public float X1Y0;
        public float X2Y0;

        public float X0Y1;
        public float X1Y1;
        public float X2Y1;

        public float X0Y2;
        public float X1Y2;
        public float X2Y2;

        public void SetZeroMatrix()
        {
            fixed (Matrix3x3* mat3 = &this)
            {
                float* ptr = (float*)mat3;

                for (int i = 0; i < 9; i++)
                    ptr[i] = 0;
            }
        }

        public void SetIdentityMatrix()
        {
            fixed (Matrix3x3* mat3 = &this)
            {
                float* ptr = (float*)mat3;

                ptr[0] = 1;
                ptr[1] = 0;
                ptr[2] = 0;
                ptr[3] = 0;
                ptr[4] = 1;
                ptr[5] = 0;
                ptr[6] = 0;
                ptr[7] = 0;
                ptr[8] = 1;

            }
        }

        /// <summary>
        /// Create Camera Rotation where EulerAngles are yaw (z axis) * pitch (y axis) * roll (x axis)
        /// </summary>
        /// <param name="EulerAngles"></param>
        /// <returns></returns>
        public static Matrix3x3 CameraRotation(Vector3 EulerAngles)
        {
            Matrix3x3 result = new Matrix3x3();

            const float deg2rad = (float)(Math.PI / 180d);

            float cosa = (float)Math.Cos(deg2rad * EulerAngles.z);
            float cosb = (float)Math.Cos(deg2rad * EulerAngles.y);
            float cosy = (float)Math.Cos(deg2rad * EulerAngles.x);

            float sina = (float)Math.Sin(deg2rad * EulerAngles.z);
            float sinb = (float)Math.Sin(deg2rad * EulerAngles.y);
            float siny = (float)Math.Sin(deg2rad * EulerAngles.x);

            result.X0Y0 = cosa * cosb;
            result.X1Y0 = cosa * sinb * siny - sina * cosy;
            result.X2Y0 = cosa * sinb * cosy + sina * siny;

            result.X0Y1 = sina * cosb;
            result.X1Y1 = sina * sinb * siny + cosa * cosy;
            result.X2Y1 = sina * sinb * cosy - cosa * siny;

            result.X0Y2 = -sinb;
            result.X1Y2 = cosb * siny;
            result.X2Y2 = cosb * cosy;

            return result;
        }

        public static Matrix3x3 YawMatrix(float zAxisEulerAngle)
        {
            Matrix3x3 result = new Matrix3x3();

            const float deg2rad = (float)(Math.PI / 180d);

            float cosa = (float)Math.Cos(deg2rad * zAxisEulerAngle);
            float sina = (float)Math.Sin(deg2rad * zAxisEulerAngle);

            result.X0Y0 = cosa;
            result.X1Y0 = -sina;
            result.X2Y0 = 0;

            result.X0Y1 = sina;
            result.X1Y1 = cosa;
            result.X2Y1 = 0;

            result.X0Y2 = 0;
            result.X1Y2 = 0;
            result.X2Y2 = 1;

            return result;  
        }

        public static Matrix3x3 PitchMatrix(float yAxisEulerAngle)
        {
            Matrix3x3 result = new Matrix3x3();

            const float deg2rad = (float)(Math.PI / 180d);

            float cosb = (float)Math.Cos(deg2rad * yAxisEulerAngle);
            float sinb = (float)Math.Sin(deg2rad * yAxisEulerAngle);

            result.X0Y0 = cosb;
            result.X1Y0 = 0;
            result.X2Y0 = sinb;

            result.X0Y1 = 0;
            result.X1Y1 = 1;
            result.X2Y1 = 0;

            result.X0Y2 = -sinb;
            result.X1Y2 = 0;
            result.X2Y2 = cosb;

            return result;
        }

        public static Matrix3x3 RollMatrix(float xAxisEulerAngle)
        {
            Matrix3x3 result = new Matrix3x3();

            const float deg2rad = (float)(Math.PI / 180d);

            float cosy = (float)Math.Cos(deg2rad * xAxisEulerAngle);
            float siny = (float)Math.Sin(deg2rad * xAxisEulerAngle);

            result.X0Y0 = 1;
            result.X1Y0 = 0;
            result.X2Y0 = 0;

            result.X0Y1 = 0;
            result.X1Y1 = cosy;
            result.X2Y1 = -siny;

            result.X0Y2 = 0;
            result.X1Y2 = siny;
            result.X2Y2 = cosy;

            return result;
        }

        public static Matrix3x3 operator +(Matrix3x3 A, Matrix3x3 B)
        {
            Matrix3x3 reslt = new Matrix3x3();

            reslt.X0Y0 = A.X0Y0 + B.X0Y0;
            reslt.X1Y0 = A.X1Y0 + B.X1Y0;
            reslt.X2Y0 = A.X2Y0 + B.X2Y0;
            reslt.X0Y1 = A.X0Y1 + B.X0Y1;
            reslt.X1Y1 = A.X1Y1 + B.X1Y1;
            reslt.X2Y1 = A.X2Y1 + B.X2Y1;
            reslt.X0Y2 = A.X0Y2 + B.X0Y2;
            reslt.X1Y2 = A.X1Y2 + B.X1Y2;
            reslt.X2Y2 = A.X2Y2 + B.X2Y2;

            return reslt;
        }

        public static Matrix3x3 operator -(Matrix3x3 A, Matrix3x3 B)
        {
            Matrix3x3 reslt = new Matrix3x3();

            reslt.X0Y0 = A.X0Y0 - B.X0Y0;
            reslt.X1Y0 = A.X1Y0 - B.X1Y0;
            reslt.X2Y0 = A.X2Y0 - B.X2Y0;
            reslt.X0Y1 = A.X0Y1 - B.X0Y1;
            reslt.X1Y1 = A.X1Y1 - B.X1Y1;
            reslt.X2Y1 = A.X2Y1 - B.X2Y1;
            reslt.X0Y2 = A.X0Y2 - B.X0Y2;
            reslt.X1Y2 = A.X1Y2 - B.X1Y2;
            reslt.X2Y2 = A.X2Y2 - B.X2Y2;

            return reslt;
        }

        public static Matrix3x3 operator *(float A, Matrix3x3 B)
        {
            float* ptr = (float*)&B;

            for (int i = 0; i < 9; i++)
                ptr[i] *= A;

            return B;
        }

        public static Matrix3x3 operator *(Matrix3x3 B, float A)
        {
            float* ptr = (float*)&B;

            for (int i = 0; i < 9; i++)
                ptr[i] *= A;

            return B;
        }

        public static Matrix3x3 operator *(Matrix3x3 A, Matrix3x3 B)
        {
            Matrix3x3 result = new Matrix3x3();

            result.X0Y0 = A.X0Y0 * B.X0Y0 + A.X1Y0 * B.X0Y1 + A.X2Y0 * B.X0Y2;
            result.X1Y0 = A.X0Y0 * B.X1Y0 + A.X1Y0 * B.X1Y1 + A.X2Y0 * B.X1Y2;
            result.X2Y0 = A.X0Y0 * B.X2Y0 + A.X1Y0 * B.X2Y1 + A.X2Y0 * B.X2Y2;

            result.X0Y1 = A.X0Y1 * B.X0Y0 + A.X1Y1 * B.X0Y1 + A.X2Y1 * B.X0Y2;
            result.X1Y1 = A.X0Y1 * B.X1Y0 + A.X1Y1 * B.X1Y1 + A.X2Y1 * B.X1Y2;
            result.X2Y1 = A.X0Y1 * B.X2Y0 + A.X1Y1 * B.X2Y1 + A.X2Y1 * B.X2Y2;

            result.X0Y2 = A.X0Y2 * B.X0Y0 + A.X1Y2 * B.X0Y1 + A.X2Y2 * B.X0Y2;
            result.X1Y2 = A.X0Y2 * B.X1Y0 + A.X1Y2 * B.X1Y1 + A.X2Y2 * B.X1Y2;
            result.X2Y2 = A.X0Y2 * B.X2Y0 + A.X1Y2 * B.X2Y1 + A.X2Y2 * B.X2Y2;

            return result;
        }

        public static Vector3 operator *(Matrix3x3 A, Vector3 B)
        {
            Vector3 result = new Vector3();
            result.x = A.X0Y0 * B.x + A.X1Y0 * B.y + A.X2Y0 * B.z;
            result.y = A.X0Y1 * B.x + A.X1Y1 * B.y + A.X2Y1 * B.z;
            result.z = A.X0Y2 * B.x + A.X1Y2 * B.y + A.X2Y2 * B.z;
            return result;
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct Vector4
    {
        public float x, y, z, w;

        public Vector4(float X, float Y, float Z, float W)
        {
            x = X;
            y = Y;
            z = Z;
            w = W;
        }

        public Vector4(Vector3 vec3, float W)
        {
            x = vec3.x;
            y = vec3.y;
            z = vec3.z;
            w = W;
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    internal unsafe struct sampler2D
    {
        int w;
        int h;
        int* TEXTURE_ADDR;

        int wrap_mode;
        int wrap_mode_color;
        int filt_mode;

        public sampler2D(GLTexture source, int s2DMode, int s2DColor, int s2DFilter)
        {
            if (source.Stride != 4)
                throw new Exception("sampler2D only works with 32bpp textures!");

            w = source.Width;
            h = source.Height;
            TEXTURE_ADDR = (int*)source.GetAddress();

            wrap_mode = 0;
            wrap_mode_color = 0;
            filt_mode = 0;
        }

        public sampler2D(xfcore.Shaders.TextureSlot tSlot)
        {
            GLTexture source = tSlot.dataTexture;

            if (source.Stride != 4)
                throw new Exception("sampler2D only works with 32bpp textures!");

            w = source.Width;
            h = source.Height;
            TEXTURE_ADDR = (int*)source.GetAddress();

            wrap_mode = tSlot.s2DMode;
            wrap_mode_color = tSlot.s2DColor;
            filt_mode = tSlot.s2DFilter;
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    internal unsafe struct GLMat
    {
        float nearZ;
        float farZ;

        float rw;
        float rh;

        float fw;
        float fh;

        float ox;
        float oy;

        float iox;
        float ioy;

        float oValue;
        float matrixlerpv;

        float fwi;
        float fhi;

        public GLMat(GLMatrix proj)
        {
            if (proj.ZNear <= 0) throw new Exception("Invalid ZNear!");
            if (proj.ZFar <= 0) throw new Exception("Invalid ZFar");
            if (proj.ZNear >= proj.ZFar) throw new Exception("Invalid ZNear ZFar");

            const float deg2rad = (float)(Math.PI / 180d);

            matrixlerpv = proj.iValue;

            nearZ = proj.ZNear;
            farZ = proj.ZFar;

            fw = 1.0f / (float)Math.Tan(deg2rad * proj.vFOV / 2.0f);
            fh = 1.0f / (float)Math.Tan(deg2rad * proj.hFOV / 2.0f);

            float ow = 0.5f * proj.iValue;
            float oh = 0.5f * proj.iValue;

            ox = 1.0f / (proj.vSize == 0 ? 1 : proj.vSize);
            oy = 1.0f / (proj.hSize == 0 ? 1 : proj.hSize);

            iox = 1f / ox;
            ioy = 1f / oy;

            oValue = ow / (float)Math.Tan(proj.vFOV / 2f) * (1f - matrixlerpv);

            fwi = 1f / fw;
            fhi = 1f / fh;
            rw = 1;
            rh = 1;
        }

        public GLMat(GLMatrix proj, int rWidth, int rHeight)
        {
            if (proj.ZNear <= 0) throw new Exception("Invalid ZNear!");
            if (proj.ZFar <= 0) throw new Exception("Invalid ZFar");
            if (proj.ZNear >= proj.ZFar) throw new Exception("Invalid ZNear ZFar");

            const float deg2rad = (float)(Math.PI / 180d);

            matrixlerpv = proj.iValue;

            nearZ = proj.ZNear;
            farZ = proj.ZFar;

            rw = ((float)rWidth - 1f) / 2f;
            rh = ((float)rHeight - 1f) / 2f;

            fw = rw / (float)Math.Tan(deg2rad * proj.vFOV / 2.0f);
            fh = rh / (float)Math.Tan(deg2rad * proj.hFOV / 2.0f);

            float ow = proj.vSize * proj.iValue;
            float oh = proj.hSize * proj.iValue;

            ox = rw / (proj.vSize == 0 ? 1 : proj.vSize);
            oy = rh / (proj.hSize == 0 ? 1 : proj.hSize);

            iox = 1f / ox;
            ioy = 1f / oy;

            oValue = ow / (float)Math.Tan(proj.vFOV / 2f) * (1f - matrixlerpv);

            fwi = 1f / fw;
            fhi = 1f / fh;
        }

    }

    [StructLayout(LayoutKind.Sequential)]
    internal unsafe struct samplerCube
    {
        int width;
        int height;

        int* front;
        int* back;
        int* left;
        int* right;
        int* top;
        int* bottom;

        int wrap_mode;
        int wrap_mode_color;
        int filt_mode;

        public samplerCube(GLCubemap source, int s2DMode, int s2DColor, int s2DFilter)
        {
            width = source.Width;
            height = source.Height;

            front = (int*)source.FRONT.GetAddress();
            back = (int*)source.BACK.GetAddress();
            left = (int*)source.LEFT.GetAddress();
            right = (int*)source.RIGHT.GetAddress();
            top = (int*)source.TOP.GetAddress();
            bottom = (int*)source.BOTTOM.GetAddress();

            wrap_mode = 0;
            wrap_mode_color = 0;
            filt_mode = 0;
        }

        public samplerCube(xfcore.Shaders.TextureSlot tSlot)
        {
            GLCubemap source = tSlot.dataCubemap;

            width = source.Width;
            height = source.Height;

            front = (int*)source.FRONT.GetAddress();
            back = (int*)source.BACK.GetAddress();
            left = (int*)source.LEFT.GetAddress();
            right = (int*)source.RIGHT.GetAddress();
            top = (int*)source.TOP.GetAddress();
            bottom = (int*)source.BOTTOM.GetAddress();

            wrap_mode = tSlot.s2DMode;
            wrap_mode_color = tSlot.s2DColor;
            filt_mode = tSlot.s2DFilter;
        }
    }

    public static class GLPrimitives
    {
        public static GLBuffer Cube
        {
            get
            {
                return new GLBuffer(new float[] {   
                    //Back
         -0.5f, -0.5f, -0.5f, 1.0f,  1.0f,
         0.5f, -0.5f, -0.5f,  0.0f,  1.0f,
         0.5f,  0.5f, -0.5f,  0.0f,  0.0f,
         0.5f,  0.5f, -0.5f,  0.0f,  0.0f,
        -0.5f,  0.5f, -0.5f,  1.0f,  0.0f,
        -0.5f, -0.5f, -0.5f,  1.0f,  1.0f,

        //front
         0.5f, -0.5f,  0.5f,    1.0f,  1.0f,
        -0.5f, -0.5f,  0.5f,    0.0f,  1.0f,
         0.5f,  0.5f,  0.5f,    1.0f,  0.0f,
         0.5f,  0.5f,  0.5f,    1.0f,  0.0f,
        -0.5f, -0.5f,  0.5f,    0.0f,  1.0f,
        -0.5f,  0.5f,  0.5f,    0.0f,  0.0f,

        //left
        -0.5f,  0.5f,  0.5f,  1.0f,  0.0f,
        -0.5f, -0.5f, -0.5f,  0.0f,  1.0f,
        -0.5f,  0.5f, -0.5f,  0.0f,  0.0f,
        -0.5f, -0.5f, -0.5f,  0.0f,  1.0f,  
        -0.5f,  0.5f,  0.5f,  1.0f,  0.0f,
        -0.5f, -0.5f,  0.5f,  1.0f,  1.0f,

        //right
         0.5f,  0.5f,  0.5f,   0.0f,  0.0f,
         0.5f,  0.5f, -0.5f,  1.0f, 0.0f,
         0.5f, -0.5f, -0.5f,   1.0f,  1.0f,
         0.5f, -0.5f, -0.5f,  1.0f,  1.0f,
         0.5f, -0.5f,  0.5f,   0.0f,  1.0f,
         0.5f,  0.5f,  0.5f,   0.0f,  0.0f,

         //bottom
        -0.5f, -0.5f, -0.5f,   0.0f,  1.0f,
         0.5f, -0.5f,  0.5f,   1.0f,  0.0f,
         0.5f, -0.5f, -0.5f,   1.0f,  1.0f,
         0.5f, -0.5f,  0.5f,    1.0f,  0.0f,
        -0.5f, -0.5f, -0.5f,    0.0f,  1.0f,
        -0.5f, -0.5f,  0.5f,    0.0f,  0.0f,

        //top
         -0.5f,  0.5f, -0.5f,  0.0f,  0.0f,
         0.5f,  0.5f, -0.5f,   1.0f,  0.0f,
         0.5f,  0.5f,  0.5f,   1.0f,  1.0f,
         0.5f,  0.5f,  0.5f,   1.0f,  1.0f,
        -0.5f,  0.5f,  0.5f,   0.0f,  1.0f,
        -0.5f,  0.5f, -0.5f,   0.0f,  0.0f
                }, 5);
            }
        }

        public static GLBuffer PlaneXZ
        {
            get
            {
                return new GLBuffer(new float[] { 
                  //X, Y, Z, U, V
                    -0.5f, 0, -0.5f, 0, 0,
                    0.5f, 0, -0.5f, 1, 0,
                    0.5f, 0, 0.5f, 1, 1,
                    -0.5f, 0, -0.5f, 0, 0,
                    0.5f, 0, 0.5f, 1, 1,
                    -0.5f, 0, 0.5f, 0, 1
                }, 5);
            }
        }
    }

    public unsafe static class GLAssistant
    {
        public delegate Vector3 VertexShaderDelegate(Vector3 input);

        public static void BakeShadows(VertexShaderDelegate vDel, GLMatrix projection, params GLBuffer[] buffers)
        {
            throw new NotImplementedException();
        }

        static void WriteTanData(GLBufferData vecs, int offset, Vector3 pos1, Vector3 norm1, Vector2 uv1, Vector3 tangent, Vector3 bitangent)
        {
            vecs[offset + 0] = pos1.x;
            vecs[offset + 1] = pos1.y;
            vecs[offset + 2] = pos1.z;

            vecs[offset + 3] = norm1.x;
            vecs[offset + 4] = norm1.y;
            vecs[offset + 5] = norm1.z;

            vecs[offset + 6] = uv1.x;
            vecs[offset + 7] = uv1.y;

            vecs[offset + 8] = tangent.x;
            vecs[offset + 9] = tangent.y;
            vecs[offset + 10] = tangent.z;

            vecs[offset + 11] = bitangent.x;
            vecs[offset + 12] = bitangent.y;
            vecs[offset + 13] = bitangent.z;
        }

        static Vector3 GetNormal(Vector3 a, Vector3 b, Vector3 c)
        {
            // Find vectors corresponding to two of the sides of the triangle.
            Vector3 side1 = b - a;
            Vector3 side2 = c - a;

            // Cross the vectors to get a perpendicular vector, then normalize it.
            return Vector3.Normalize(Vector3.Cross(side1, side2));
        }

        public static GLBuffer BuildTanBitangents(GLBuffer buffer)
        {
            throw new NotImplementedException();

            if (buffer.Size % buffer.Stride != 0)
                throw new Exception("Invalid Buffer! (Size and stride don't align!)");

         //   if (buffer.Stride != 8)
         //       throw new Exception("Buffer stride must be 8 = XYZ IJK UV");

            if (buffer.Stride != 5)
                throw new Exception("Buffer stride must be 8 = XYZ IJK UV");


            int vertex_count = buffer.Size / buffer.Stride;

            if (vertex_count % 3 != 0)
                throw new Exception("Invalid Buffer! (Vertex count is not dividable by three!)");

            int stride = buffer.Stride;
            int tris_count = vertex_count / 3;


           // float[] return_data = new float[tris_count * (buffer.Stride + 3)];
            GLBuffer returnBuffer = new GLBuffer(vertex_count * 14);

            int modStride = (buffer.Stride + 6);
            float* addr = (float*)returnBuffer.GetAddress();


            buffer.LockBuffer(delegate(GLBufferData data) { 
                for (int i = 0; i < tris_count; i++)
                {
                    int pos = i * stride * 3;
                    Vector3 pos1 = new Vector3(data[pos + stride * 0 + 0], data[pos + stride * 0 + 1], data[pos + stride * 0 + 2]);
                    Vector3 pos2 = new Vector3(data[pos + stride * 1 + 0], data[pos + stride * 1 + 1], data[pos + stride * 1 + 2]);
                    Vector3 pos3 = new Vector3(data[pos + stride * 2 + 0], data[pos + stride * 2 + 1], data[pos + stride * 2 + 2]);

                    Vector3 norm = GetNormal(pos1, pos2, pos3);

                    Vector3 norm1 = norm;
                    Vector3 norm2 = norm;
                    Vector3 norm3 = norm;


                    /*
                    Vector3 norm1 = new Vector3(data[pos + stride * 0 + 3], data[pos + stride * 0 + 4], data[pos + stride * 0 + 5]);
                    Vector3 norm2 = new Vector3(data[pos + stride * 1 + 3], data[pos + stride * 1 + 4], data[pos + stride * 1 + 5]);
                    Vector3 norm3 = new Vector3(data[pos + stride * 2 + 3], data[pos + stride * 2 + 4], data[pos + stride * 2 + 5]);

                    Vector3 norm = (norm1 + norm2 + norm3) / 3f;
                    */

                    Vector2 uv1 = new Vector2(data[pos + stride * 0 + 6], data[pos + stride * 0 + 7]);
                    Vector2 uv2 = new Vector2(data[pos + stride * 1 + 6], data[pos + stride * 1 + 7]);
                    Vector2 uv3 = new Vector2(data[pos + stride * 2 + 6], data[pos + stride * 2 + 7]);

                    Vector3 edge1 = pos2 - pos1;
                    Vector3 edge2 = pos3 - pos1;
                    Vector2 deltaUV1 = uv2 - uv1;
                    Vector2 deltaUV2 = uv3 - uv1;

                    float f = 1.0f / (deltaUV1.x * deltaUV2.y - deltaUV2.x * deltaUV1.y);

                    Vector3 tangent = new Vector3(), bitangent = new Vector3();
                    tangent.x = f * (deltaUV2.y * edge1.x - deltaUV1.y * edge2.x);
                    tangent.y = f * (deltaUV2.y * edge1.y - deltaUV1.y * edge2.y);
                    tangent.z = f * (deltaUV2.y * edge1.z - deltaUV1.y * edge2.z);

                    bitangent.x = f * (-deltaUV2.x * edge1.x + deltaUV1.x * edge2.x);
                    bitangent.y = f * (-deltaUV2.x * edge1.y + deltaUV1.x * edge2.y);
                    bitangent.z = f * (-deltaUV2.x * edge1.z + deltaUV1.x * edge2.z);

                  //  WriteTanData(addr + (i * modStride * 3) + modStride * 0, pos1, norm1, uv1, tangent, bitangent);
                  //  WriteTanData(addr + (i * modStride * 3) + modStride * 1, pos2, norm2, uv2, tangent, bitangent);
                   // WriteTanData(addr + (i * modStride * 3) + modStride * 2, pos3, norm3, uv3, tangent, bitangent);
                }
            });

            return returnBuffer;
        }

        public static GLBuffer BuildTanBitangentsFromXYZUV(GLBuffer buffer)
        {
            if ((buffer.Size / 4) % buffer.Stride != 0)
                throw new Exception("Invalid Buffer! (Size and stride don't align!)");

            if (buffer.Stride != 5)
                throw new Exception("Buffer stride must be 8 = XYZ IJK UV");

            int vertex_count = (buffer.Size / 4) / buffer.Stride;

            if (vertex_count % 3 != 0)
                throw new Exception("Invalid Buffer! (Vertex count is not dividable by three!)");

            int stride = buffer.Stride;
            int tris_count = vertex_count / 3;

            // float[] return_data = new float[tris_count * (buffer.Stride + 3)];
            GLBuffer returnBuffer = new GLBuffer(vertex_count * (buffer.Stride + 9) * 4, buffer.Stride + 9);

            int modStride = (buffer.Stride + 9);

            returnBuffer.LockBuffer(delegate(GLBufferData addr) 
            {
                buffer.LockBuffer(delegate(GLBufferData data)
                {
                    for (int i = 0; i < tris_count; i++)
                    {
                        int pos = i * stride * 3;
                        Vector3 pos1 = new Vector3(data[pos + stride * 0 + 0], data[pos + stride * 0 + 1], data[pos + stride * 0 + 2]);
                        Vector3 pos2 = new Vector3(data[pos + stride * 1 + 0], data[pos + stride * 1 + 1], data[pos + stride * 1 + 2]);
                        Vector3 pos3 = new Vector3(data[pos + stride * 2 + 0], data[pos + stride * 2 + 1], data[pos + stride * 2 + 2]);

                        Vector3 norm = GetNormal(pos1, pos2, pos3);

                        Vector3 norm1 = norm;
                        Vector3 norm2 = norm;
                        Vector3 norm3 = norm;


                        /*
                        Vector3 norm1 = new Vector3(data[pos + stride * 0 + 3], data[pos + stride * 0 + 4], data[pos + stride * 0 + 5]);
                        Vector3 norm2 = new Vector3(data[pos + stride * 1 + 3], data[pos + stride * 1 + 4], data[pos + stride * 1 + 5]);
                        Vector3 norm3 = new Vector3(data[pos + stride * 2 + 3], data[pos + stride * 2 + 4], data[pos + stride * 2 + 5]);

                        Vector3 norm = (norm1 + norm2 + norm3) / 3f;
                        */

                        Vector2 uv1 = new Vector2(data[pos + stride * 0 + 3], data[pos + stride * 0 + 4]);
                        Vector2 uv2 = new Vector2(data[pos + stride * 1 + 3], data[pos + stride * 1 + 4]);
                        Vector2 uv3 = new Vector2(data[pos + stride * 2 + 3], data[pos + stride * 2 + 4]);

                        Vector3 edge1 = pos2 - pos1;
                        Vector3 edge2 = pos3 - pos1;
                        Vector2 deltaUV1 = uv2 - uv1;
                        Vector2 deltaUV2 = uv3 - uv1;

                        float f = 1.0f / (deltaUV1.x * deltaUV2.y - deltaUV2.x * deltaUV1.y);

                        Vector3 tangent = new Vector3(), bitangent = new Vector3();
                        tangent.x = f * (deltaUV2.y * edge1.x - deltaUV1.y * edge2.x);
                        tangent.y = f * (deltaUV2.y * edge1.y - deltaUV1.y * edge2.y);
                        tangent.z = f * (deltaUV2.y * edge1.z - deltaUV1.y * edge2.z);

                        bitangent.x = f * (-deltaUV2.x * edge1.x + deltaUV1.x * edge2.x);
                        bitangent.y = f * (-deltaUV2.x * edge1.y + deltaUV1.x * edge2.y);
                        bitangent.z = f * (-deltaUV2.x * edge1.z + deltaUV1.x * edge2.z);

                        WriteTanData(addr, (i * modStride * 3) + modStride * 0, pos1, norm1, uv1, tangent, bitangent);
                        WriteTanData(addr, (i * modStride * 3) + modStride * 1, pos2, norm2, uv2, tangent, bitangent);
                        WriteTanData(addr, (i * modStride * 3) + modStride * 2, pos3, norm3, uv3, tangent, bitangent);
                    }
                });
            });

            return returnBuffer;
        }




    }
}

namespace xfcore
{
    public unsafe static class GLExtra
    {
        [DllImport("XFCore.dll", CallingConvention = CallingConvention.Cdecl)]
        static extern void Copy32bpp(int* dest, int* src, int w1, int w2, int h1, int h2, int wSrc, int wDest, int wOffset, int hOffset, int hFlip);

        [DllImport("msvcrt.dll", EntryPoint = "memcpy", CallingConvention = CallingConvention.Cdecl, SetLastError = false)]
        static extern IntPtr memcpy(IntPtr dest, IntPtr src, UIntPtr count);

        public static void BlitIntoBitmap(GLTexture SourceTexture, Bitmap TargetBitmap, Point TargetPoint, Rectangle SourceRectangle)
        {
            if (TargetBitmap == null || SourceTexture == null)
                throw new Exception("Target and Sources cannot be null");

            int sD = Image.GetPixelFormatSize(TargetBitmap.PixelFormat) / 8;
            int bmpWidth = TargetBitmap.Width, bmpHeight = TargetBitmap.Height;

            if (SourceTexture.Stride != 4 || sD != 4)
                throw new Exception("not yet supported!");

            BitmapData bmpData = TargetBitmap.LockBits(new Rectangle(0, 0, bmpWidth, bmpHeight), ImageLockMode.ReadWrite, TargetBitmap.PixelFormat);

            SourceTexture.RequestLock();

            //  SourceRectangle.Y = bmpHeight - SourceRectangle.Y - SourceRectangle.Height;
            SourceRectangle.Y = SourceTexture.Height - SourceRectangle.Y - SourceRectangle.Height;


            int startXS = SourceRectangle.X;
            int startYS = SourceRectangle.Y;

            if (startXS >= SourceTexture.Width || SourceRectangle.X < 0) throw new Exception("SourceSize is not on SourceTexture!");
            if (startYS >= SourceTexture.Height || SourceRectangle.Y < 0) throw new Exception("SourceSize is not on SourceTexture!");

            if (SourceRectangle.Width < 0 || SourceRectangle.Height < 0) throw new Exception("SourceSize Width/Height cannot be negative!");

            int endXS = SourceRectangle.X + SourceRectangle.Width;
            int endYS = SourceRectangle.Y + SourceRectangle.Height;

            if (endXS > SourceTexture.Width) throw new Exception("SourceSize Width is not on SourceTexture!");
            if (endYS > SourceTexture.Height) throw new Exception("SourceSize Height is not on SourceTexture!");

            int startXT = TargetPoint.X;
            int startYT = TargetPoint.Y;

            if (startXT >= bmpWidth) throw new Exception("TargetPoint is not on Bitmap!");
            if (startYT >= bmpHeight) throw new Exception("TargetPoint is not on Bitmap!");

            int endXT = TargetPoint.X + SourceRectangle.Width;
            int endYT = TargetPoint.Y + SourceRectangle.Height;

            if (endXT > bmpWidth) throw new Exception("SourceSize Width is not on SourceTexture!");
            if (endYT > bmpHeight) throw new Exception("SourceSize Height is not on SourceTexture!");

            int offsetX = TargetPoint.X - SourceRectangle.X;
            int offsetY = TargetPoint.Y - SourceRectangle.Y;


            int hSample = SourceTexture.Height - 1;
            //   hSample = endYS;

            //  int hOffset = SourceTexture.Height - SourceRectangle.Y - SourceRectangle.Height;
            //  hSample -= hOffset;


            //throw new Exception();
            //    FastCopy((int*)bmpData.Scan0, (int*)SourceTexture.HEAP_ptr, startXS, endXS, startYS, endYS, SourceTexture.Width, bmpWidth, offsetX, offsetY, hSample);
            Copy32bpp((int*)bmpData.Scan0, (int*)SourceTexture.GetAddress(), startXS, endXS, startYS, endYS, SourceTexture.Width, bmpWidth, offsetX, offsetY, hSample);

            SourceTexture.ReleaseLock();

            TargetBitmap.UnlockBits(bmpData);
        }

        public static void BlitFromBitmap(Bitmap SourceBitmap, GLTexture TargetTexture, Point TargetPoint, Rectangle SourceRectangle)
        {
            if (TargetTexture == null || SourceBitmap == null)
                throw new Exception("Target and Sources cannot be null");

            int sD = Image.GetPixelFormatSize(SourceBitmap.PixelFormat) / 8;
            int bmpWidth = SourceBitmap.Width, bmpHeight = SourceBitmap.Height;

            if (TargetTexture.Stride != 4 || sD != 4)
                throw new Exception("not yet supported!");

            BitmapData bmpData = SourceBitmap.LockBits(new Rectangle(0, 0, bmpWidth, bmpHeight), ImageLockMode.ReadWrite, SourceBitmap.PixelFormat);

            TargetTexture.RequestLock();

            //   int hDelta = bmpHeight - SourceRectangle.Height;
            //   SourceRectangle.Y += hDelta;

            SourceRectangle.Y = bmpHeight - SourceRectangle.Y - SourceRectangle.Height;

            int startXS = SourceRectangle.X;
            int startYS = SourceRectangle.Y;

            if (startXS >= SourceBitmap.Width || SourceRectangle.X < 0) throw new Exception("SourceSize is not on Bitmap!");
            if (startYS >= SourceBitmap.Height || SourceRectangle.Y < 0) throw new Exception("SourceSize is not on Bitmap!");

            if (SourceRectangle.Width < 0 || SourceRectangle.Height < 0) throw new Exception("SourceSize Width/Height cannot be negative!");

            int endXS = SourceRectangle.X + SourceRectangle.Width;
            int endYS = SourceRectangle.Y + SourceRectangle.Height;

            if (endXS > SourceBitmap.Width) throw new Exception("SourceSize Width is not on Bitmap!");
            if (endYS > SourceBitmap.Height) throw new Exception("SourceSize Height is not on Bitmap!");


            int startXT = TargetPoint.X;
            int startYT = TargetPoint.Y;

            if (startXT >= TargetTexture.Width) throw new Exception("TargetPoint is not on Texture!");
            if (startYT >= TargetTexture.Height) throw new Exception("TargetPoint is not on Texture!");


            int endXT = TargetPoint.X + SourceRectangle.Width;
            int endYT = TargetPoint.Y + SourceRectangle.Height;

            if (endXT > TargetTexture.Width) throw new Exception("SourceSize Width is not on SourceTexture!");
            if (endYT > TargetTexture.Height) throw new Exception("SourceSize Height is not on SourceTexture!");

            int offsetX = TargetPoint.X - SourceRectangle.X;
            int offsetY = TargetPoint.Y - SourceRectangle.Y;

            int hSample = bmpHeight - 1;

            //  FastCopy((int*)TargetTexture.HEAP_ptr, (int*)bmpData.Scan0, startXS, endXS, startYS, endYS, SourceBitmap.Width, TargetTexture.Width, offsetX, offsetY, hSample);


            Copy32bpp((int*)TargetTexture.GetAddress(), (int*)bmpData.Scan0, startXS, endXS, startYS, endYS, SourceBitmap.Width, TargetTexture.Width, offsetX, offsetY, hSample);

            TargetTexture.ReleaseLock();

            SourceBitmap.UnlockBits(bmpData);
        }

        static void FastCopy(int* dest, int* src, int w1, int w2, int h1, int h2, int wSrc, int wDest, int wOffset, int hOffset, int hFlip)
        {
            for (int h = h1; h < h2; ++h)
            {
                for (int w = w1; w < w2; ++w)
                {
                    int a = src[(hFlip - h) * wSrc + w];
                    //  src[(hFlip - h) * wSrc + w] = 12345;

                    dest[(h + hOffset) * wDest + w + wOffset] = a;
                }
            }
        }

        public static GLTexture FromBitmap(Bitmap SourceBitmap, bool FlipDuringLoad = false)
        {
            int sD = Image.GetPixelFormatSize(SourceBitmap.PixelFormat) / 8;
            int bmpWidth = SourceBitmap.Width, bmpHeight = SourceBitmap.Height;

            if (sD != 4) throw new Exception("only 32bpp supported");

            GLTexture ReturnTexture = new GLTexture(bmpWidth, bmpHeight, typeof(Color4));

            BitmapData bmpData = SourceBitmap.LockBits(new Rectangle(0, 0, bmpWidth, bmpHeight), ImageLockMode.ReadWrite, SourceBitmap.PixelFormat);

            if (FlipDuringLoad)
            {
                FastCopy((int*)ReturnTexture.GetAddress(), (int*)bmpData.Scan0, 0, bmpWidth, 0, bmpHeight, bmpWidth, bmpWidth, 0, 0, bmpHeight - 1);
            }
            else
            {
                memcpy(ReturnTexture.GetAddress(), bmpData.Scan0, (UIntPtr)(bmpWidth * bmpHeight * 4));
            }

            SourceBitmap.UnlockBits(bmpData);

            return ReturnTexture;
        }

        public static Vector3 Pan3D(Vector3 Input, Vector3 Rotation, float deltaX, float deltaY, float deltaZ = 0)
        {
            Vector3 I = Input;
            Vector3 RADS = new Vector3(0f, Rotation.y / 57.2958f, Rotation.z / 57.2958f);

            float sinX = (float)Math.Sin(RADS.z); //0
            float sinY = (float)Math.Sin(RADS.y); //0


            float cosX = (float)Math.Cos(RADS.z); //0
            float cosY = (float)Math.Cos(RADS.y); //0

            float XAccel = (cosX * -deltaX + (sinY * deltaY) * sinX) + (sinX * -deltaZ) * cosY;
            float YAccel = (cosY * deltaY) + (sinY * deltaZ);
            float ZAccel = (sinX * deltaX + (sinY * deltaY) * cosX) + (cosX * -deltaZ) * cosY;

            I = I + new Vector3(XAccel, YAccel, ZAccel);

            return I;
        }

        /// <summary>
        /// Gets the cos(x), cos(y), cos(z) values of a euler angle vector
        /// </summary>
        /// <param name="EulerAnglesDEG"></param>
        /// <returns></returns>
        public static Vector3 GetCos(Vector3 EulerAnglesDEG)
        {
            return new Vector3((float)Math.Cos(EulerAnglesDEG.x / 57.2958f), (float)Math.Cos(EulerAnglesDEG.y / 57.2958f), (float)Math.Cos(EulerAnglesDEG.z / 57.2958f));
        }

        /// <summary>
        /// Gets the sin(x), sin(y), sin(z) values of a euler angle vector
        /// </summary>
        /// <param name="EulerAnglesDEG"></param>
        /// <returns></returns>
        public static Vector3 GetSin(Vector3 EulerAnglesDEG)
        {
            return new Vector3((float)Math.Sin(EulerAnglesDEG.x / 57.2958f), (float)Math.Sin(EulerAnglesDEG.y / 57.2958f), (float)Math.Sin(EulerAnglesDEG.z / 57.2958f));
        }

    }

    public unsafe class MSAAData
    {
        [DllImport("XFCore.dll", CallingConvention = CallingConvention.Cdecl)]
        static extern void MSAA_Merge(int* TargetBuffer, int** ptrPtrs, int count, int Width, int Height);

        [DllImport("XFCore.dll", CallingConvention = CallingConvention.Cdecl)]
        static extern void MSAA_Copy(int* TargetBuffer, int** ptrPtrs, int count, int Width, int Height);


        internal Vector2[] sPattern;
        public GLTexture[] colorBuffers;
        internal GLTexture targetBuffer;

        GCHandle sData;
        IntPtr ptrData;

        public MSAAData(GLTexture linkedBuffer, Vector2[] samplePattern)
        {
            if (linkedBuffer.Stride != 4)
                throw new Exception("MSAA is only supported for 32bpp color buffer!");

            int count = samplePattern.Length;

            if (!(count == 2 || count == 4 || count == 8))
                throw new Exception("Pattern size must be either 2, 4 or 8!");

            sPattern = samplePattern.ToArray();

            targetBuffer = linkedBuffer;
            colorBuffers = new GLTexture[count];

            for (int i = 0; i < count; i++)
                colorBuffers[i] = new GLTexture(linkedBuffer.Width, linkedBuffer.Height, typeof(Color4));

        }

        public void Debug()
        {
            colorBuffers[0].Clear();
        }

        public unsafe void Run()
        {
            targetBuffer.RequestLock();

            //Probably not required but always good to do
            for (int i = 0; i < colorBuffers.Length; i++)
                colorBuffers[i].RequestLock();

            //Check buffer sizes are identical!

            for (int i = 0; i < colorBuffers.Length; i++)
            {
                if (colorBuffers[i].Width != targetBuffer.Width)
                    throw new Exception("One or multiple buffers arent the same width!");
                if (colorBuffers[i].Height != targetBuffer.Height)
                    throw new Exception("One or multiple buffers arent the same height!");
            }

            IntPtr ptrData = Marshal.AllocHGlobal(4 * colorBuffers.Length);
            int** data = (int**)ptrData;

            for (int i = 0; i < colorBuffers.Length; i++)
                data[i] = (int*)colorBuffers[i].GetAddress();

            //Call Adv Function to merge each buffer->
            MSAA_Merge((int*)targetBuffer.GetAddress(), data, colorBuffers.Length, targetBuffer.Width, targetBuffer.Height);

            for (int i = 0; i < colorBuffers.Length; i++)
                colorBuffers[i].ReleaseLock();

            Marshal.FreeHGlobal(ptrData);

            targetBuffer.ReleaseLock();
        }

        public void CopyOver()
        {
            targetBuffer.RequestLock();

            //Probably not required but always good to do
            for (int i = 0; i < colorBuffers.Length; i++)
                colorBuffers[i].RequestLock();

            //Check buffer sizes are identical!

            for (int i = 0; i < colorBuffers.Length; i++)
            {
                if (colorBuffers[i].Width != targetBuffer.Width)
                    throw new Exception("One or multiple buffers arent the same width!");
                if (colorBuffers[i].Height != targetBuffer.Height)
                    throw new Exception("One or multiple buffers arent the same height!");
            }

            IntPtr ptrData = Marshal.AllocHGlobal(4 * colorBuffers.Length);
            int** data = (int**)ptrData;

            for (int i = 0; i < colorBuffers.Length; i++)
                data[i] = (int*)colorBuffers[i].GetAddress();

            //Call Adv Function to merge each buffer->
            MSAA_Copy((int*)targetBuffer.GetAddress(), data, colorBuffers.Length, targetBuffer.Width, targetBuffer.Height);

            for (int i = 0; i < colorBuffers.Length; i++)
                colorBuffers[i].ReleaseLock();

            Marshal.FreeHGlobal(ptrData);

            targetBuffer.ReleaseLock();
        }

        public void Clear(Color4 color)
        {
            for (int i = 0; i < colorBuffers.Length; i++)
            {
                GL.Clear(colorBuffers[i], color);
            }
        }

        internal unsafe MSAAConfig CreateConfig()
        {
            sData = GCHandle.Alloc(sPattern, GCHandleType.Pinned);
            float* Pattern = (float*)sData.AddrOfPinnedObject();

            ptrData = Marshal.AllocHGlobal(4 * colorBuffers.Length);
            int** data = (int**)ptrData;

            for (int i = 0; i < colorBuffers.Length; i++)
                data[i] = (int*)colorBuffers[i].GetAddress();

            return new MSAAConfig(colorBuffers.Length, 1f / colorBuffers.Length, Pattern, data);

        }

        internal void Free()
        {
            sData.Free();
            Marshal.FreeHGlobal(ptrData);
        }

    }

    unsafe struct MSAAConfig
    {
	    int** ptrPtrs;
	    int sampleCount;
	    float* sampleBuffer;
	    float sampleMultiply;

        public MSAAConfig(int sCount, float sMult, float* sBuf, int** ptrs)
        {
            ptrPtrs = ptrs;
            sampleCount = sCount;
            sampleBuffer = sBuf;
            sampleMultiply = sMult;
        }
        
    }
}