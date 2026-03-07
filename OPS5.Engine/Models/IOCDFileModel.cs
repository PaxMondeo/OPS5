using System;
using System.Collections.Generic;
using System.Security.Cryptography.X509Certificates;
using System.Text;

namespace OPS5.Engine.Models
{
    public class DataFileModel
    {
        public List<DataActionModel> Actions { get; set; } = new List<DataActionModel>();
    }

    public class DataActionModel
    {
        public string Command { get; set; }
        public string FileName { get; set; }
        public List<string> Atoms { get; set; } = new List<string>();
        public DataActionModel(string command, string fileName)
        {
            Command = command.ToUpper();
            FileName = fileName;
        }
    }
}
