namespace OPS5.Engine
{
    internal class SourceFile
    {
        public string FileName { get; set; }
        public string FilePath { get; set; }

        public SourceFile(string fileName, string filePath)
        {
            FileName = fileName;
            FilePath = filePath;
        }
    }

}
