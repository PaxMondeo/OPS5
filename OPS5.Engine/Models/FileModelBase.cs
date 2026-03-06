namespace OPS5.Engine.Models
{
    public class FileModelBase
    {
        public string Line { get; set; }
        public bool IsValid { get; set; }
        public FileModelBase(string line)
        {
            Line = line;
            IsValid = false;
        }
    }
}
