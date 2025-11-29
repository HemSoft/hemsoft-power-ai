// <copyright file="FileTools.cs" company="HemSoft">
// Copyright © 2025 HemSoft
// </copyright>

namespace AgentDemo.Console.Tools;

using System.ComponentModel;
using System.IO;

/// <summary>
/// Provides file system tools for the AI agent.
/// </summary>
internal static class FileTools
{
    /// <summary>
    /// Lists all files in the specified directory path.
    /// </summary>
    /// <param name="path">The directory path to list files from.</param>
    /// <returns>An array of file names in the directory.</returns>
    [Description("Lists all files in the specified directory path and returns their names")]
    public static string[] ListFiles(string path)
    {
        System.Console.WriteLine($"[Tool] ListFiles called with path: {path}");

        if (!Directory.Exists(path))
        {
            return [$"Directory not found: {path}"];
        }

        var files = Directory.GetFiles(path)
            .Select(Path.GetFileName)
            .Where(f => f is not null)
            .Cast<string>()
            .ToArray();

        System.Console.WriteLine($"[Tool] Found {files.Length} files");
        return files;
    }

    /// <summary>
    /// Counts the number of files in the specified directory.
    /// </summary>
    /// <param name="path">The directory path to count files in.</param>
    /// <returns>The number of files, or -1 if the directory does not exist.</returns>
    [Description("Counts the number of files in the specified directory path")]
    public static int CountFiles(string path) =>
        !Directory.Exists(path) ? -1 : Directory.GetFiles(path).Length;

    /// <summary>
    /// Creates a new folder at the specified path.
    /// </summary>
    /// <param name="path">The path where the folder should be created.</param>
    /// <returns>A message indicating success or failure.</returns>
    [Description("Creates a new folder at the specified path")]
    public static string CreateFolder(string path)
    {
        try
        {
            Directory.CreateDirectory(path);
            return $"Folder created: {path}";
        }
        catch (UnauthorizedAccessException ex)
        {
            return $"Access denied: {ex.Message}";
        }
        catch (IOException ex)
        {
            return $"IO error: {ex.Message}";
        }
    }

    /// <summary>
    /// Gets information about a file.
    /// </summary>
    /// <param name="filePath">The path to the file.</param>
    /// <returns>A string containing file name, size, and last modified date.</returns>
    [Description("Gets information about a file including size and last modified date")]
    public static string GetFileInfo(string filePath)
    {
        if (!File.Exists(filePath))
        {
            return $"File not found: {filePath}";
        }

        var info = new FileInfo(filePath);
        return $"Name: {info.Name}, Size: {info.Length} bytes, Modified: {info.LastWriteTime}";
    }
}
