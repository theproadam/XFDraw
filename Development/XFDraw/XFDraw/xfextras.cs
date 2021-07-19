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

        public static implicit operator int(Color4 color)
        {
            return ((((((byte)color.A << 8) | (byte)color.R) << 8) | (byte)color.G) << 8) | (byte)color.B;

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
}