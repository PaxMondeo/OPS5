namespace OPS5.Engine
{
    public static class Utils
    {
        public static string SeparateFilePath(string platform, ref string? fileName)
        {
            if (fileName == null)
                return "";
            else
            {
                string folderPath = "";
                if (platform == "Windows")
                {
                    if (fileName.Contains("\\"))
                    {
                        int pos = fileName.LastIndexOf("\\") + 1;
                        folderPath = fileName.Substring(0, pos);
                        fileName = fileName.Substring(pos);
                    }
                }
                else
                {
                    if (fileName.Contains("/"))
                    {
                        int pos = fileName.LastIndexOf("/") + 1;
                        folderPath = fileName.Substring(0, pos);
                        fileName = fileName.Substring(pos);
                    }
                }
                return folderPath;
            }
        }

    }
}
