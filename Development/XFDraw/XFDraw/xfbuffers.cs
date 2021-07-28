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
        public delegate void ReadPixelDelegate12(GLBytes12 output);


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

        public unsafe void LockPixels(ReadPixelDelegate12 del)
        {
            if (_stride != 12) throw new Exception("Stride is not 12!");

            RequestLock();

            GLBytes12 bytes = new GLBytes12(_width, _height, (xfcore.Extras.Vector3*)HEAP_ptr);
            del(bytes);
            bytes.Dispose();

            ReleaseLock();
        }


        public int this[int x, int y]
        {
            get { return 0; }
        }

        public void Clear()
        {
            GL.Clear(this);
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



    public unsafe class GLBuffer : IDisposable
    {
        internal IntPtr HEAP_ptr;
        internal int Size;
        float* fptr;

        static int Buffer_RAM_Usage = 0;

        bool disposed = false;
        internal object ThreadLock = new object();
        internal int stride;
        internal int elementSize;

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
            lock (ThreadLock)
                if (!disposed)
                {
                    Marshal.FreeHGlobal(HEAP_ptr);
                    disposed = true;
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
                if (i >= 0 || i < Size)
                    return fptr[i];
                else throw new IndexOutOfRangeException();
            }
            set
            {
                if (i >= 0 || i < Size)
                    fptr[i] = value;
                else throw new IndexOutOfRangeException();
            }
        }



        public GLBuffer(int size, int Stride = 3)
        {
            if (size <= 0) throw new Exception("Size must be bigger than zero!");
            if (size % Stride != 0) throw new Exception("Invalid Stride OR Size!");
            if (size % 4 != 0) throw new Exception("GLBuffer only supports FP32 numbers!");


            //  if (elementSize != 4) throw new Exception("Only FP32 Buffers are allowed!");

            stride = Stride;
            Size = size;
            Interlocked.Add(ref Buffer_RAM_Usage, Size);
            HEAP_ptr = Marshal.AllocHGlobal(size);
            fptr = (float*)HEAP_ptr;
        }

        public GLBuffer(float[] Source, int Stride = 3)
        {
            if (Source == null) throw new ArgumentNullException();

            if (Source.Length <= 0) throw new Exception("Size must be bigger than zero!");
            if (Source.Length % Stride != 0) throw new Exception("Invalid Stride OR Size!");
            
            stride = Stride;
            Size = Source.Length * 4;
            Interlocked.Add(ref Buffer_RAM_Usage, Size);
            HEAP_ptr = Marshal.AllocHGlobal(Size);
            fptr = (float*)HEAP_ptr;

            for (int i = 0; i < Source.Length; i++)
            {
                fptr[i] = Source[i];
            }
        }

        public void Resize(int size, int newStride = 3)
        {
            lock (ThreadLock)
            {
                Interlocked.Add(ref Buffer_RAM_Usage, -size);
                Size = size;

                HEAP_ptr = Marshal.ReAllocHGlobal(HEAP_ptr, (IntPtr)(Size));
                fptr = (float*)HEAP_ptr;
                stride = newStride;
                Interlocked.Add(ref Buffer_RAM_Usage, Size);
            }
        }
    }
}
