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
            return;

            ShaderCompile sModule;

            if (!ShaderParser.Compile("basicVS.cpp", "basicFS.cpp", out sModule)){
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
            Console.Write("Compiling Shaders -> ");

            if (!ShaderParser.Compile("vignetteShader.cpp", out sModule, CompileOption.ForceRecompile))
            {
                Console.WriteLine("Failed to compile Shader!");
                return;
            }

            Console.WriteLine("Success!");



            Console.WriteLine("Screen Shader:\n" + sModule.PrintScreenSpaceShader());
            Console.ReadLine();
        }
        
    }
}
