using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ImageImporter
{
    class Program
    {
        static void Main(string[] args)
        {
            string source;
            string destination;
            Options options = new Options();

            if (CommandLine.Parser.Default.ParseArguments(args, options))
            {
                source = options.Source;
                destination = options.Destination;
            }
            else
            {
                Console.WriteLine("Source: ");
                source = Console.ReadLine();
                Console.WriteLine("Destination: ");
                destination = Console.ReadLine();
            }

            Importer importer = new Importer();
            importer.ImportFiles(source, destination);

            Console.WriteLine("Press any key to exit...");
            Console.ReadLine();
        }
    }
}
