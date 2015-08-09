using CommandLine;
using CommandLine.Text;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ImageImporter
{
    class Options
    {
        [Option('s', "source", Required = true,  HelpText = "Source path to import images from.")]
        public string Source { get; set; }

        [Option('d', "destination", Required = true, HelpText = "Root destination to import images to. A folder with today's date will be created there if necessary")]
        public string Destination { get; set; }

        [ParserState]
        public IParserState LastParserState { get; set; }

        [HelpOption]
        public string GetUsage()
        {
            return HelpText.AutoBuild(this,
              (HelpText current) => HelpText.DefaultParsingErrorsHandler(this, current));
        }
    }
}
