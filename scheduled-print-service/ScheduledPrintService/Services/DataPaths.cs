using System;
using System.IO;
using Microsoft.Extensions.Configuration;

namespace ScheduledPrintService.Services;

public class DataPaths
{
    private const string EnvVar = "SCHEDULED_PRINT_DATA_ROOT";
    private static string? _configuredDataRoot;

    public static void Initialize(IConfiguration configuration)
    {
        _configuredDataRoot = configuration["DataRoot"];
    }

    public static string DataRoot
    {
        get
        {
            // Priority: 1) Environment variable (for dev override), 2) appsettings.json, 3) fallback to ProgramData
            var overrideRoot = Environment.GetEnvironmentVariable(EnvVar);
            string root;
            if (!string.IsNullOrWhiteSpace(overrideRoot))
            {
                root = Path.GetFullPath(overrideRoot);
            }
            else if (!string.IsNullOrWhiteSpace(_configuredDataRoot))
            {
                root = Path.GetFullPath(_configuredDataRoot);
            }
            else
            {
                // Fallback to ProgramData if not configured
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
