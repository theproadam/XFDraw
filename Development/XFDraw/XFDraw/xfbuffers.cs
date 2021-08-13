using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using System.Runtime.InteropServices;

namespace xfcore.Buffers
{
    public class GLTexture : IDisposable
    {
        private int _width;
        private int _height;
        private int _stride;

        private object ThreadLock = new object();
        private int LocalLockCount = 0;
        private int LocalLock = 0; //0 - free, 1 - taken
        private int CriticalLock = 0; //0 - free, 1 - taken

        public int Width { get { return _width; } }
        public int Height { get { return _height; } }
        public int Stride { get { return _stride; } }

        private IntPtr HEAP_ptr;
        private bool disposed = false;

        ~GLTexture()
        {
            Dispose(false);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            Interlocked.Increment(ref CriticalLock);
            bool lockTaken = false;
            Monitor.Enter(ThreadLock, ref lockTaken);

            try
            {
                if (!this.disposed){
                    Marshal.FreeHGlobal(HEAP_ptr);
                    HEAP_ptr = IntPtr.Zero;

                    disposed = true;
                }
            }
            finally
            {
                if (lockTaken) Monitor.Exit(ThreadLock);
                Interlocked.Decrement(ref CriticalLock);
            }    
        }

        static int Texture_RAM_Usage = 0;
        public static int TotalRAMUsage
        {
            get { return Interlocked.CompareExchange(ref Texture_RAM_Usage, 0, 0); }
        }
        public static float TotalRAMUsageMB
        {
            get { return Interlocked.CompareExchange(ref Texture_RAM_Usage, 0, 0) / 1024f / 1024f; }
        }

        public delegate void ReadPixelDelegate4(GLBytes4 output);
        public delegate void ReadPixelDelegateFloat(GLFloatBytes output);
        public delegate void ReadPixelDelegate12(GLBytes12 output);

        internal void RequestLock()
        {
            if (Interlocked.CompareExchange(ref CriticalLock, 0, 0) >= 1)
            {
                Monitor.Enter(ThreadLock);
                Interlocked.Increment(ref LocalLockCount);
            }
            else if (Interlocked.CompareExchange(ref LocalLock, 1, 0) == 0)
            {
                Monitor.Enter(ThreadLock);           
                Interlocked.Increment(ref LocalLockCount);    
            }
            else Interlocked.Increment(ref LocalLockCount);
            
        }

        internal void ReleaseLock()
        {
            if (Interlocked.CompareExchange(ref LocalLockCount, 0, 1) == 1)
            {
                Monitor.Exit(ThreadLock);
                Interlocked.Decrement(ref LocalLock);
            }
            else Interlocked.Decrement(ref LocalLockCount);
        }

        public GLTexture(int width, int height, Type type)
        {
            if (width <= 10 || height <= 10)
                throw new Exception("GLTexture must be bigger than 10x10!");

            _width = width;
            _height = height;
            _stride = Marshal.SizeOf(type);
            Interlocked.Add(ref Texture_RAM_Usage, _width * _height * _stride);
            HEAP_ptr = Marshal.AllocHGlobal(Width * Height * Stride);

        }

        public void Resize(int newWidth, int newHeight)
        {
            if (newWidth <= 10 || newHeight <= 10)
                throw new Exception("GLTexture must be bigger than 10x10!");

            Interlocked.Increment(ref CriticalLock);
            bool lockTaken = false;
            Monitor.Enter(ThreadLock, ref lockTaken);

            try
            {
                int Size = newWidth * newHeight;
                Interlocked.Add(ref Texture_RAM_Usage, -Size * Stride);
                _width = newWidth;
                _height = newHeight;      

                HEAP_ptr = Marshal.ReAllocHGlobal(HEAP_ptr, (IntPtr)(newWidth * newHeight * Stride));
                Interlocked.Add(ref Texture_RAM_Usage, Size * Stride);
            }
            finally
            {
                if (lockTaken) Monitor.Exit(ThreadLock);
                Interlocked.Decrement(ref CriticalLock);
            }
        }

        public IntPtr GetAddress()
        {
            if (HEAP_ptr == IntPtr.Zero)
                throw new Exception("FATAL ERROR: GLTexture Handle is IntPtr.Zero!");

            return HEAP_ptr;
        }

        public unsafe void LockPixels(ReadPixelDelegate4 del)
        {
            if (_stride != 4) throw new Exception("Stride is not 4!");


            RequestLock();

            GLBytes4 bytes = new GLBytes4(_width, _height, (int*)HEAP_ptr);
            del(bytes);
            bytes.Dispose();

            ReleaseLock();
        }

        public unsafe void LockPixels(ReadPixelDelegateFloat del)
        {
            if (_stride != 4) throw new Exception("Stride is not 4!");

            RequestLock();

            GLFloatBytes bytes = new GLFloatBytes(_width, _height, (float*)HEAP_ptr);
            del(bytes);
            bytes.Dispose();

            ReleaseLock();
        }


        public unsafe void LockPixels(ReadPixelDelegate12 del)
        {
            if (_stride != 12) throw new Exception("Stride is not 12!");

            RequestLock();

            GLBytes12 bytes = new GLBytes12(_width, _height, (xfcore.Extras.Vector3*)HEAP_ptr);
            del(bytes);
            bytes.Dispose();

            ReleaseLock();
        }

        public void Clear()
        {
            GL.Clear(this);
        }

        bool IsPowerOfTwo(ulong x)
        {
            return (x != 0) && ((x & (x - 1)) == 0);
        }
    }

    public unsafe class GLFloatBytes
    {
        bool disposed = false;
        float* ptr;
        int _width;
        int _height;

        public GLFloatBytes(int width, int height, float* ptr)
        {
            this.ptr = ptr;
            _width = width;
            _height = height;
        }

        public float GetPixel(int x, int y)
        {
            if (x < 0 || x >= _width)
                throw new Exception("Pixel must be on GLTexture!");

            if (y < 0 || y >= _height)
                throw new Exception("Pixel must be on GLTexture!");

            if (disposed)
                throw new Exception("This instance has already been disposed!");

            return ptr[x + y * _width];
        }

        public void SetPixel(int x, int y, float Color)
        {
            if (x < 0 || x >= _width)
                throw new Exception("Pixel must be on GLTexture!");

            if (y < 0 || y >= _height)
                throw new Exception("Pixel must be on GLTexture!");

            if (disposed)
                throw new Exception("This instance has already been disposed!");

            ptr[x + y * _width] = Color;
        }

        public void Dispose()
        {
            disposed = true;
        }
    }


    public unsafe class GLBytes4
    {
        bool disposed = false;
        int* ptr;
        int _width;
        int _height;

        public GLBytes4(int width, int height, int* ptr)
        {
            this.ptr = ptr;
            _width = width;
            _height = height;
        }

        public int GetPixel(int x, int y)
        {
            if (x < 0 || x >= _width)
                throw new Exception("Pixel must be on GLTexture!");

            if (y < 0 || y >= _height)
                throw new Exception("Pixel must be on GLTexture!");

            if (disposed)
                throw new Exception("This instance has already been disposed!");

            return ptr[x + y * _width];
        }

        public void SetPixel(int x, int y, int Color)
        {
            if (x < 0 || x >= _width)
                throw new Exception("Pixel must be on GLTexture!");

            if (y < 0 || y >= _height)
                throw new Exception("Pixel must be on GLTexture!");

            if (disposed)
                throw new Exception("This instance has already been disposed!");

            ptr[x + y * _width] = Color;
        }

        public void Dispose()
        {
            disposed = true;
        }
    }

    public unsafe class GLBytes12
    {
        bool disposed = false;
        xfcore.Extras.Vector3* ptr;
        int _width;
        int _height;

        public GLBytes12(int width, int height, xfcore.Extras.Vector3* ptr)
        {
            this.ptr = ptr;
            _width = width;
            _height = height;
        }

        public xfcore.Extras.Vector3 GetPixel(int x, int y)
        {
            if (x < 0 || x >= _width)
                throw new Exception("Pixel must be on GLTexture!");

            if (y < 0 || y >= _height)
                throw new Exception("Pixel must be on GLTexture!");

            if (disposed)
                throw new Exception("This instance has already been disposed!");

            return ptr[x + y * _width];
        }

        public void SetPixel(int x, int y, xfcore.Extras.Vector3 Value)
        {
            if (x < 0 || x >= _width)
                throw new Exception("Pixel must be on GLTexture!");

            if (y < 0 || y >= _height)
                throw new Exception("Pixel must be on GLTexture!");

            if (disposed)
                throw new Exception("This instance has already been disposed!");

            ptr[x + y * _width] = Value;
        }

        public void Dispose()
        {
            disposed = true;
        }
    }

    public unsafe class GLBufferData
    { 
        bool disposed = false;
        float* fptr;
        int _size;

        public GLBufferData(int size, float* ptr)
        {
            fptr = ptr;
            _size = size / 4;
        }

        public float this[int i]
        {
            get
            {
                if (disposed)
                    throw new Exception("buffer already disposed!");

                if (i >= 0 && i < _size) return fptr[i];
                else throw new IndexOutOfRangeException();
            }
            set
            {
                if (disposed)
                    throw new Exception("buffer already disposed!");

                if (i >= 0 && i < _size) fptr[i] = value;
                else throw new IndexOutOfRangeException();
            }
        }

        public void Dispose()
        {
            disposed = true;
        }
    }

    public unsafe class GLBuffer : IDisposable
    {
        internal IntPtr HEAP_ptr;
        internal int _size;
        float* fptr;

        private int LocalLockCount = 0;
        private int LocalLock = 0; //0 - free, 1 - taken
        private int CriticalLock = 0; //0 - free, 1 - taken

        public int Size { get { return _size; } }
        public int Stride { get { return stride; } }

        static int Buffer_RAM_Usage = 0;
        public delegate void ReadDataDelegate(GLBufferData data);

        bool disposed = false;
        internal object ThreadLock = new object();
        internal int stride;

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        ~GLBuffer()
        {
            Dispose(false);
        }

        protected virtual void Dispose(bool disposing)
        {
            Interlocked.Increment(ref CriticalLock);
            bool lockTaken = false;
            Monitor.Enter(ThreadLock, ref lockTaken);

            try
            {
                if (!this.disposed)
                {
                    Marshal.FreeHGlobal(HEAP_ptr);
                    HEAP_ptr = IntPtr.Zero;

                    disposed = true;
                }
            }
            finally
            {
                if (lockTaken) Monitor.Exit(ThreadLock);
                Interlocked.Decrement(ref CriticalLock);
            }
        }

        public static int TotalRAMUsage
        {
            get { return Interlocked.CompareExchange(ref Buffer_RAM_Usage, 0, 0); }
        }

        public static float TotalRAMUsageMB
        {
            get { return Interlocked.CompareExchange(ref Buffer_RAM_Usage, 0, 0) / 1024f / 1024f; }
        }

        public float this[int i]
        {
            get
            {
                RequestLock();

                if (i >= 0 && i < _size)
                {
                    float d = fptr[i];
                    ReleaseLock();
                    return d;
                }
                else throw new IndexOutOfRangeException();
            }
            set
            {
                RequestLock();
                if (i >= 0 && i < _size)
                    fptr[i] = value;
                else throw new IndexOutOfRangeException();

                ReleaseLock();
            }
        }

        internal void RequestLock()
        {
            if (Interlocked.CompareExchange(ref CriticalLock, 0, 0) >= 1)
            {
                Monitor.Enter(ThreadLock);
                Interlocked.Increment(ref LocalLockCount);
            }
            else if (Interlocked.CompareExchange(ref LocalLock, 1, 0) == 0)
            {
                Monitor.Enter(ThreadLock);
                Interlocked.Increment(ref LocalLockCount);
            }
            else Interlocked.Increment(ref LocalLockCount);

        }

        internal void ReleaseLock()
        {
            if (Interlocked.CompareExchange(ref LocalLockCount, 0, 1) == 1)
            {
                Monitor.Exit(ThreadLock);
                Interlocked.Decrement(ref LocalLock);
            }
            else Interlocked.Decrement(ref LocalLockCount);
        }

        public unsafe void LockBuffer(ReadDataDelegate del)
        {
            RequestLock();

            GLBufferData bytes = new GLBufferData(_size, (float*)GetAddress());
            del(bytes);
            bytes.Dispose();

            ReleaseLock();
        }

        public GLBuffer(int size, int Stride = 3)
        {
            if (size <= 0) throw new Exception("Size must be bigger than zero!");
            if (size % Stride != 0) throw new Exception("Invalid Stride OR Size!");
            if (size % 4 != 0) throw new Exception("GLBuffer only supports FP32 numbers!");
            if (Stride <= 0) throw new Exception("Stride must be bigger than 0!");

            stride = Stride;
            _size = size;
            Interlocked.Add(ref Buffer_RAM_Usage, _size);
            HEAP_ptr = Marshal.AllocHGlobal(size);
            fptr = (float*)HEAP_ptr;
        }

        public GLBuffer(float[] Source, int Stride = 3)
        {
            if (Source == null) throw new ArgumentNullException();

            if (Source.Length <= 0) throw new Exception("Size must be bigger than zero!");
            if (Source.Length % Stride != 0) throw new Exception("Invalid Stride OR Size!");
            
            stride = Stride;
            _size = Source.Length * 4;
            Interlocked.Add(ref Buffer_RAM_Usage, _size);
            HEAP_ptr = Marshal.AllocHGlobal(_size);
            fptr = (float*)HEAP_ptr;

            for (int i = 0; i < Source.Length; i++)
            {
                fptr[i] = Source[i];
            }
        }

        public GLBuffer(xfcore.Extras.Vector3[] Source, int Stride = 3)
        {
            if (Stride % 3 != 0)
                throw new Exception("A Vector3 stride must be a multiple of 3: XYZ, XYZ_IJK, XYZ_IJK_UVW etc");

            if (Source == null) throw new ArgumentNullException();

            int trueSize = Source.Length * 3;

            if (trueSize <= 0) throw new Exception("Size must be bigger than zero!");
            if (trueSize % Stride != 0) throw new Exception("Invalid Stride OR Size!");

            stride = Stride;
            _size = trueSize * 4;
            Interlocked.Add(ref Buffer_RAM_Usage, _size);
            HEAP_ptr = Marshal.AllocHGlobal(_size);
            fptr = (float*)HEAP_ptr;

            for (int i = 0; i < Source.Length; i++)
            {
                fptr[i * 3 + 0] = Source[i].x;
                fptr[i * 3 + 1] = Source[i].y;
                fptr[i * 3 + 2] = Source[i].z;
            }
        }

        public IntPtr GetAddress()
        {
            if (HEAP_ptr == IntPtr.Zero)
                throw new Exception("FATAL ERROR: GLBuffer Handle is IntPtr.Zero!");

            return HEAP_ptr;
        }

        public void Resize(int size, int newStride = 3)
        {
            if (size <= 0) throw new Exception("Cannot allocate a buffer of size zero or less!");
            if (newStride <= 0) throw new Exception("Stride must be bigger than zero!");
            if (size % newStride != 0) throw new Exception("Stride must be a multiple of size!");
            if (size % 4 != 0) throw new Exception("GLBuffer only supports FP32 numbers!");

            Interlocked.Increment(ref CriticalLock);
            bool lockTaken = false;
            Monitor.Enter(ThreadLock, ref lockTaken);

            try
            {
                Interlocked.Add(ref Buffer_RAM_Usage, -size);
                _size = size;

                HEAP_ptr = Marshal.ReAllocHGlobal(HEAP_ptr, (IntPtr)(_size));
                fptr = (float*)HEAP_ptr;
                stride = newStride;
                Interlocked.Add(ref Buffer_RAM_Usage, _size);
            }
            finally
            {
                if (lockTaken) Monitor.Exit(ThreadLock);
                Interlocked.Decrement(ref CriticalLock);
            }
        }
    }

    public class GLCubemap
    {
        internal GLTexture[] cubemap = new GLTexture[6];

        public GLTexture FRONT { 
            get { return cubemap[0]; } 
            set { cubemap[0] = value; } 
        }
        public GLTexture BACK
        {
            get { return cubemap[1]; }
            set { cubemap[1] = value; }
        }
        public GLTexture LEFT
        {
            get { return cubemap[2]; }
            set { cubemap[2] = value; }
        }
        public GLTexture RIGHT
        {
            get { return cubemap[3]; }
            set { cubemap[3] = value; }
        }
        public GLTexture TOP
        {
            get { return cubemap[4]; }
            set { cubemap[4] = value; }
        }
        public GLTexture BOTTOM
        {
            get { return cubemap[5]; }
            set { cubemap[5] = value; }
        }

        public int Width { get { return FRONT.Width; } }
        public int Height { get { return FRONT.Height; } }

        public void Clear(byte R, byte G, byte B)
        {
            for (int i = 0; i < 6; i++)
            {
                GL.Clear(cubemap[i], R, G, B);
            }
        }

        public GLCubemap(GLTexture front, GLTexture back, GLTexture left, GLTexture right, GLTexture top, GLTexture bottom)
        {
            cubemap[0] = front;
            cubemap[1] = back;
            cubemap[2] = left;
            cubemap[3] = right;
            cubemap[4] = top;
            cubemap[5] = bottom;
        }

        internal bool isValid()
        {
            bool h = (FRONT.Height == BACK.Height & BACK.Height == LEFT.Height & LEFT.Height == RIGHT.Height & RIGHT.Height == TOP.Height & TOP.Height == BOTTOM.Height);
            bool w = (FRONT.Width == BACK.Width & BACK.Width == LEFT.Width & LEFT.Width == RIGHT.Width & RIGHT.Width == TOP.Width & TOP.Width == BOTTOM.Width);
            bool s = (FRONT.Stride == BACK.Stride & BACK.Stride == LEFT.Stride & LEFT.Stride == RIGHT.Stride & RIGHT.Stride == TOP.Stride & TOP.Stride == BOTTOM.Stride);
            bool sxsy = FRONT.Height == FRONT.Width;

            if (FRONT.Stride != 4)
                throw new Exception("All cubemaps must be 32bpp");

            return s & w & h & sxsy;
        }

        internal void RequestLock()
        {
            for (int i = 0; i < 6; i++)
            {
                cubemap[i].RequestLock();
            }
        }

        internal void ReleaseLock()
        {
            for (int i = 0; i < 6; i++)
            {
                cubemap[i].ReleaseLock();
            }
        }
    }

    internal class SmartLock
    {
        private object ThreadLock = new object();
        private int LocalLockCount = 0;
        private int LocalLock = 0; //0 - free, 1 - taken
        private int CriticalLock = 0; //0 - free, 1 - taken

        public void RequestLock()
        {
            if (Interlocked.CompareExchange(ref CriticalLock, 0, 0) >= 1)
            {
                Monitor.Enter(ThreadLock);
                Interlocked.Increment(ref LocalLockCount);
            }
            else if (Interlocked.CompareExchange(ref LocalLock, 1, 0) == 0)
            {
                Monitor.Enter(ThreadLock);
                Interlocked.Increment(ref LocalLockCount);
            }
            else Interlocked.Increment(ref LocalLockCount);

        }

        public void ReleaseLock()
        {
            if (Interlocked.CompareExchange(ref LocalLockCount, 0, 1) == 1)
            {
                Monitor.Exit(ThreadLock);
                Interlocked.Decrement(ref LocalLock);
            }
            else Interlocked.Decrement(ref LocalLockCount);
        }

        public void RequestCriticalLock()
        {
            Interlocked.Increment(ref CriticalLock);
            Monitor.Enter(ThreadLock);
        }

        public void ReleaseCriticalLock()
        {
            Monitor.Exit(ThreadLock);
            Interlocked.Decrement(ref CriticalLock);
        }
    }

    public enum TextureWarp
    {
        GL_CLAMP_TO_EDGE = 0,
        GL_REPEAT = 1,
        GL_MIRRORED_REPEAT = 2,
        GL_CLAMP_TO_BORDER = 3
    }

    public enum TextureFiltering
    { 
        GL_NEAREST = 0,
        GL_LINEAR = 1
    }
}
