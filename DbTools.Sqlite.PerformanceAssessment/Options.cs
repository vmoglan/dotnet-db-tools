using CommandLine;
using CommandLine.Text;

namespace DbTools.Sqlite.PerformanceAssessment
{
    class Options
    {
        [Option('s', "data-source", Required = true, HelpText = "The path where the database file will be created.")]
        public string DataSource { get; set; }

        [Option('k', "key", Required = false, HelpText = "The key used for opening/encrypting the database.")]
        public string Password { get; set; } = string.Empty;

        [Option('c', "record-count", Required = true, HelpText = "The number of records to execute the operation on.")]
        public int RecordCount { get; set; }

        [Option('m', "memory-security-off")]
        public bool IsMemorySecurityOff { get; set; } = true;

        [Option('o', "output-path", Required = false, HelpText = "The path to the log.")]
        public string OutputPath { get; set; } = string.Empty;
    }
}
