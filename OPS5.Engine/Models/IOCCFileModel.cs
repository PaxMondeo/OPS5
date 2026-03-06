using OPS5.Engine.Contracts;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace OPS5.Engine.Models
{
    public class IOCCFileModel
    {
        public List<ClassModel> Classes { get; set; } = new List<ClassModel>();
    }

    public class ClassModel : FileModelBase
    {
        public string ClassName {get;set;} = string.Empty;
        public bool IsBase { get; set; }
        public string BaseClass { get; set; } = string.Empty;
        public List<string> InheritedAtts { get; set; } = new List<string>();
        public bool Disabled { get; set; } = false;
        public string Comment { get; set; } = "";
        public List<string> Atoms { get; set; } = new List<string>();
        public bool IsPersistent { get; set; } = false;
        public bool PersistIndividualObjects { get; set; } = false;

        public ClassModel(string line) : base(line)
        {

        }


        public void ValidateAtoms()
        {
            if (Atoms.Count == 0)
                throw new Exception("No attributes found");

            //Clean up comments and any junk
            for (int x = 0; x < Atoms.Count; x++)
            {
                string atom = Regex.Replace(Atoms[x], @"\r\n?|\n", "");
                atom = atom.Trim();
                if (atom.Contains(@"//"))
                    atom = atom.Substring(0, atom.IndexOf(@"//"));
                Atoms[x] = atom;
            }

            if (Atoms[0].ToUpper() == "DISABLED")
            {
                Disabled = true;
                Atoms.RemoveAt(0);
            }
            if (Atoms[0].ToUpper().StartsWith("COMMENT"))
            {
                Comment = Atoms[0].Substring(9);
                Comment = Comment.Substring(0, Comment.Length - 1);
                Atoms.RemoveAt(0);
            }
            IsValid = true;
            if (Atoms.Contains("PERSISTENT", StringComparer.OrdinalIgnoreCase))
            {
                IsPersistent = true;
                Atoms.RemoveAll(p => p.ToUpper() == "PERSISTENT");
            }
            if (Atoms.Contains("PERSISTOBJECT", StringComparer.OrdinalIgnoreCase))
            {
                IsPersistent = true;
                PersistIndividualObjects = true;
                Atoms.RemoveAll(p => p.ToUpper() == "PERSISTOBJECT");
            }
        }


    }
}
