namespace EAappEmulater.Helper;

public static class FileHelper
{
    /// <summary>
    /// Create folder
    /// </summary>
    public static void CreateDirectory(string dirPath)
    {
        if (!Directory.Exists(dirPath))
            Directory.CreateDirectory(dirPath);
    }

    /// <summary>
    /// Create file
    /// </summary>
    public static void CreateFile(string filePath)
    {
        if (!File.Exists(filePath))
            File.Create(filePath).Close();
    }

    /// <summary>
    /// Create file
    /// </summary>
    public static void CreateFile(string dirPath, string fileName)
    {
        var path = Path.Combine(dirPath, fileName);

        if (!File.Exists(path))
            File.Create(path).Close();
    }

    /// <summary>
    /// Get the embedded resource stream (automatically add prefix)
    /// </summary>
    public static Stream GetEmbeddedResourceStream(string resPath)
    {
        var assembly = Assembly.GetExecutingAssembly();
        return assembly.GetManifestResourceStream($"EAappEmulater.Assets.Files.{resPath}");
    }

    /// <summary>
    /// Get the text content of the embedded resource
    /// </summary>
    public static string GetEmbeddedResourceText(string resPath)
    {
        var stream = GetEmbeddedResourceStream(resPath);
        if (stream is null)
            return string.Empty;

        return new StreamReader(stream).ReadToEnd();
    }

    /// <summary>
    /// Clear the files and folders in the specified folder
    /// </summary>
    public static void ClearDirectory(string dirPath)
    {
        try
        {
            var dir = new DirectoryInfo(dirPath);
            var fileInfo = dir.GetFileSystemInfos();

            foreach (var file in fileInfo)
            {
                if (file is DirectoryInfo)
                {
                    var subdir = new DirectoryInfo(file.FullName);
                    subdir.Delete(true);
                }
                else
                {
                    File.Delete(file.FullName);
                }
            }

            LoggerHelper.Info($"Cleared folder successfully {dirPath}");
        }
        catch (Exception ex)
        {
            LoggerHelper.Error($"Clearing folder exception {dirPath}", ex);
        }
    }

    /// <summary>
    /// Extract resource files from resource files (overwrite source files by default)
    /// </summary>
    public static void ExtractResFile(string resPath, string outputPath, bool isOverride = true)
    {
        // If the output file exists and the file is not overwritten, exit
        if (!isOverride && File.Exists(outputPath))
            return;

        var stream = GetEmbeddedResourceStream(resPath);
        if (stream is null)
            return;

        BufferedStream inStream = null;
        FileStream outStream = null;

        try
        {
            inStream = new BufferedStream(stream);
            outStream = new FileStream(outputPath, FileMode.Create, FileAccess.Write);

            var buffer = new byte[1024];
            int length;

            while ((length = inStream.Read(buffer, 0, buffer.Length)) > 0)
                outStream.Write(buffer, 0, length);

            outStream.Flush();

            LoggerHelper.Info($"Resource file released successfully {outputPath}");
        }
        catch (Exception ex)
        {
            LoggerHelper.Error($"Exception in releasing resource file {outputPath}", ex);
        }
        finally
        {
            outStream?.Close();
            inStream?.Close();
        }
    }
}
