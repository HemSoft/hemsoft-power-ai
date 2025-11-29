namespace AgentDemo.Console.Tools;

using System.ComponentModel;

public static class FileTools
{
    [Description("Lists all files in the specified directory path and returns their names")]
    public static string[] ListFiles(string path)
    {
        System.Console.WriteLine($"[Tool] ListFiles called with path: {path}");

        if (!Directory.Exists(path))
            return [$"Directory not found: {path}"];

        var files = Directory.GetFiles(path)
            .Select(Path.GetFileName)
            .Where(f => f is not null)
            .Cast<string>()
            .ToArray();

        System.Console.WriteLine($"[Tool] Found {files.Length} files");
        return files;
    }

    [Description("Counts the number of files in the specified directory path")]
    public static int CountFiles(string path)
    {
        if (!Directory.Exists(path))
            return -1;

        return Directory.GetFiles(path).Length;
    }

    [Description("Creates a new folder at the specified path")]
    public static string CreateFolder(string path)
    {
        try
        {
            Directory.CreateDirectory(path);
            return $"Folder created: {path}";
        }
        catch (Exception ex)
        {
            return $"Failed to create folder: {ex.Message}";
        }
    }

    [Description("Gets information about a file including size and last modified date")]
    public static string GetFileInfo(string filePath)
    {
        if (!File.Exists(filePath))
            return $"File not found: {filePath}";

        var info = new FileInfo(filePath);
        return $"Name: {info.Name}, Size: {info.Length} bytes, Modified: {info.LastWriteTime}";
    }
}
