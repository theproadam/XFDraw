using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace RemoteCompile
{
    class Program
    {
        static void Main(string[] args)
        {
            CompileSS();
            Console.ReadLine();
            return;

            ShaderCompile sModule;

            if (!ShaderParser.Parse("basicVS.cpp", "basicFS.cpp", out sModule)){
                Console.WriteLine("Failed to compile Shader!");
                Console.ReadLine();
                return;
            }

            Console.WriteLine("Shader Compile Success: ");

            Console.WriteLine("Vertex Shader:\n" + sModule.PrintVertexShader());
            Console.WriteLine("\nFragment Shader:\n" + sModule.PrintFragmentShader());


            //throw new Exception();
            Console.ReadLine();
        }

        static void CompileSS()
        {
            ShaderCompile sModule;
            Console.Write("Parsing Shader -> ");
            if (!ShaderParser.Parse("vignetteShader.cpp", out sModule))
            {
                Console.WriteLine("Failed to parse Shader!");
                return;
            }
            Console.WriteLine("Success!");


            Shader vignetteShader;
            Console.Write("Compiling Shader -> ");
            if (!sModule.Compile(out vignetteShader))
            {
                Console.WriteLine("Failed to compile Shader!");
                return;
            }
            Console.WriteLine("Success!");


            GLTexture vignetteBuffer = new GLTexture(1024, 768, typeof(float));
            vignetteShader.SetValue("viewportMod", new Vector2(10, 10));
            vignetteShader.AssignBuffer("outMultiplier", vignetteBuffer);

            vignetteShader.Pass();


            Console.ReadLine();
        }
        
    }
}
