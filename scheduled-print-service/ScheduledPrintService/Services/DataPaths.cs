using System;
using System.IO;

namespace ScheduledPrintService.Services;

public static class DataPaths
{
    private const string EnvVar = "SCHEDULED_PRINT_DATA_ROOT";

    public static string DataRoot
    {
        get
        {
            // Allow override for development/local runs via environment variable
            var overrideRoot = Environment.GetEnvironmentVariable(EnvVar);
            string root;
            if (!string.IsNullOrWhiteSpace(overrideRoot))
            {
                root = Path.GetFullPath(overrideRoot);
            }
            else
            {
                root = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "ScheduledPrintService");
            }

            Directory.CreateDirectory(root);
            return root;
        }
    }

    public static string EnsureDir(string name)
    {
        var dir = Path.Combine(DataRoot, name);
        Directory.CreateDirectory(dir);
        return dir;
    }

    public static string EnsureFile(string name)
    {
        var filePath = Path.Combine(DataRoot, name);
        var dir = Path.GetDirectoryName(filePath);
        if (!string.IsNullOrEmpty(dir))
        {
            Directory.CreateDirectory(dir);
        }
        return filePath;
    }
}
