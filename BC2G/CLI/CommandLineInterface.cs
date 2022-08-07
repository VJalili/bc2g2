using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.CommandLine;

namespace BC2G.CLI
{
    internal class CommandLineInterface
    {
        private Options<int> 
        public CommandLineInterface(Action<DirectoryInfo, int> SampleCmdHandler)
        {

        }
        public void Invoke()
        {
            var rootCommand = new RootCommand("...");


            var opt1 = new Option<int>(name: "--opt1", description: "description ... ", getDefaultValue: () => 0);
            var sampleCmd = new Command("sample", "sample the graph")
            {
                opt1
            };
            sampleCmd.SetHandler()


            rootCommand.AddCommand(sampleCmd);

        }
    }


    public class T1
    {
        public void X(Func<int, string> method)
        {
            var result = method(10);
        }
    }

    public class T2
    {
        string aa = "00";
        public string Y(int a)
        {
            return aa + a;
        }

        public void test()
        {
            var t1 = new T1();
            t1.X(Y);
            
        }
    }
}
