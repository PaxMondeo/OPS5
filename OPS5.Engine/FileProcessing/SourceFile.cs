namespace OPS5.Engine
{
    internal class SourceFile
    {
        public string FileName { get; set; }
        public string FilePath { get; set; }
        public string Comment { get; set; }
        public string Text { get; set; }
        public bool Loaded { get; set; }
        public bool Saved { get; set; }

        public SourceFile(string fileName, string filePath, string comment, string text, bool loaded, bool saved)
        {
            FileName = fileName;
            FilePath = filePath;
            Comment = comment;
            Loaded = loaded;
            Text = text;
            Saved = saved;
        }
    }

}
