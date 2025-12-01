// <copyright file="FileTools.cs" company="HemSoft">
// Copyright © 2025 HemSoft
// </copyright>

namespace HemSoft.PowerAI.Console.Tools;

using System.ComponentModel;
using System.IO;

/// <summary>
/// Provides file system tools for the AI agent.
/// </summary>
internal static class FileTools
{
    /// <summary>
    /// Queries the file system for information.
    /// </summary>
    /// <param name="mode">Operation mode: 'list' (files in dir), 'count' (file count), 'info' (file details).</param>
    /// <param name="path">The file or directory path.</param>
    /// <returns>Query result or error message.</returns>
    [Description("Query file system. Modes: 'list' (files in directory), 'count' (file count in directory), 'info' (file size/date)")]
    public static string QueryFileSystem(string mode, string path)
    {
        System.Console.WriteLine($"[Tool] QueryFileSystem: {mode} {path}");

        return mode?.ToUpperInvariant() switch
        {
            "LIST" => ListFiles(path),
            "COUNT" => CountFiles(path),
            "INFO" => GetFileInfo(path),
            _ => $"Unknown mode '{mode}'. Use: list, count, info",
        };
    }

    /// <summary>
    /// Modifies the file system.
    /// </summary>
    /// <param name="mode">Operation mode: 'mkdir' (create folder), 'delete' (remove file/folder), 'move', 'copy'.</param>
    /// <param name="path">The source path.</param>
    /// <param name="destination">The destination path (for move/copy operations).</param>
    /// <returns>Result message or error.</returns>
    [Description(
        "Modify file system. Modes: 'mkdir' (create folder), 'delete' (remove), 'move', 'copy'. " +
        "Destination required for move/copy.")]
    public static string ModifyFileSystem(string mode, string path, string? destination = null)
    {
        System.Console.WriteLine($"[Tool] ModifyFileSystem: {mode} {path} -> {destination}");

        return mode?.ToUpperInvariant() switch
        {
            "MKDIR" => CreateFolder(path),
            "DELETE" => Delete(path),
            "MOVE" => Move(path, destination),
            "COPY" => Copy(path, destination),
            _ => $"Unknown mode '{mode}'. Use: mkdir, delete, move, copy",
        };
    }

    /// <summary>
    /// Formats a file operation error message.
    /// </summary>
    /// <param name="operation">The operation that failed.</param>
    /// <param name="path">The path involved.</param>
    /// <param name="ex">The exception that occurred.</param>
    /// <returns>Formatted error message.</returns>
    internal static string FormatError(string operation, string path, Exception ex) =>
        ex is UnauthorizedAccessException
            ? $"Access denied: {path}"
            : $"Error {operation} {path}: {ex.Message}";

    private static string ListFiles(string path)
    {
        if (!Directory.Exists(path))
        {
            return $"Directory not found: {path}";
        }

        var files = Directory.EnumerateFiles(path, "*", SearchOption.TopDirectoryOnly)
            .Take(100)
            .Select(Path.GetFileName)
            .Where(f => f is not null);

        var fileList = string.Join(", ", files);
        return string.IsNullOrEmpty(fileList) ? "Empty directory" : fileList;
    }

    private static string CountFiles(string path) =>
        !Directory.Exists(path)
            ? $"Directory not found: {path}"
            : $"{Directory.EnumerateFiles(path, "*", SearchOption.TopDirectoryOnly).Count()} files";

    private static string GetFileInfo(string path)
    {
        if (!File.Exists(path))
        {
            return $"File not found: {path}";
        }

        var info = new FileInfo(path);
        return $"{info.Name} | {info.Length:N0} bytes | Modified: {info.LastWriteTime:g}";
    }

    private static string CreateFolder(string path)
    {
        try
        {
            Directory.CreateDirectory(path);
            return $"Created: {path}";
        }
        catch (Exception ex) when (ex is UnauthorizedAccessException or IOException)
        {
            return FormatError("creating", path, ex);
        }
    }

    private static string Delete(string path)
    {
        try
        {
            if (File.Exists(path))
            {
                File.Delete(path);
                return $"Deleted file: {path}";
            }

            if (Directory.Exists(path))
            {
                Directory.Delete(path, recursive: true);
                return $"Deleted directory: {path}";
            }

            return $"Not found: {path}";
        }
        catch (Exception ex) when (ex is UnauthorizedAccessException or IOException)
        {
            return FormatError("deleting", path, ex);
        }
    }

    private static string Move(string source, string? destination)
    {
        if (string.IsNullOrEmpty(destination))
        {
            return "Destination required for move operation";
        }

        try
        {
            if (File.Exists(source))
            {
                File.Move(source, destination);
                return $"Moved file: {source} -> {destination}";
            }

            if (Directory.Exists(source))
            {
                Directory.Move(source, destination);
                return $"Moved directory: {source} -> {destination}";
            }

            return $"Source not found: {source}";
        }
        catch (Exception ex) when (ex is UnauthorizedAccessException or IOException)
        {
            return FormatError("moving", source, ex);
        }
    }

    private static string Copy(string source, string? destination)
    {
        if (string.IsNullOrEmpty(destination))
        {
            return "Destination required for copy operation";
        }

        try
        {
            if (File.Exists(source))
            {
                File.Copy(source, destination, overwrite: true);
                return $"Copied file: {source} -> {destination}";
            }

            return Directory.Exists(source)
                ? "Directory copy not supported. Use move or copy individual files."
                : $"Source not found: {source}";
        }
        catch (Exception ex) when (ex is UnauthorizedAccessException or IOException)
        {
            return FormatError("copying", source, ex);
        }
    }
}
