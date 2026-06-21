namespace BioscoopMAUI.UITests;

public static class AndroidSdkEnvironment
{
    public static string EnsureConfigured()
    {
        var sdkRoot = ResolveSdkRoot();
        if (sdkRoot is null)
        {
            throw new InvalidOperationException(
                "Android SDK not found. Install Android Studio or the .NET Android workload, then set ANDROID_HOME " +
                "to your SDK path (typically ~/Library/Android/sdk on macOS). " +
                "See https://developer.android.com/studio/command-line/variables");
        }

        Environment.SetEnvironmentVariable("ANDROID_HOME", sdkRoot);
        Environment.SetEnvironmentVariable("ANDROID_SDK_ROOT", sdkRoot);

        var platformTools = Path.Combine(sdkRoot, "platform-tools");
        if (Directory.Exists(platformTools))
            PrependPath(platformTools);

        var emulatorPath = Path.Combine(sdkRoot, "emulator");
        if (Directory.Exists(emulatorPath))
            PrependPath(emulatorPath);

        return sdkRoot;
    }

    public static Dictionary<string, string> CreateProcessEnvironment()
    {
        var sdkRoot = EnsureConfigured();
        var environment = new Dictionary<string, string>(StringComparer.Ordinal);

        foreach (System.Collections.DictionaryEntry entry in Environment.GetEnvironmentVariables())
        {
            if (entry.Key is string key && entry.Value is string value)
                environment[key] = value;
        }

        environment["ANDROID_HOME"] = sdkRoot;
        environment["ANDROID_SDK_ROOT"] = sdkRoot;
        return environment;
    }

    private static string? ResolveSdkRoot()
    {
        string?[] configuredCandidates =
        [
            Environment.GetEnvironmentVariable("ANDROID_HOME"),
            Environment.GetEnvironmentVariable("ANDROID_SDK_ROOT")
        ];

        foreach (var candidate in configuredCandidates)
        {
            if (!string.IsNullOrWhiteSpace(candidate) && Directory.Exists(candidate))
                return candidate;
        }

        var defaultSdkRoot = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
            "Library",
            "Android",
            "sdk");

        return Directory.Exists(defaultSdkRoot) ? defaultSdkRoot : null;
    }

    private static void PrependPath(string folder)
    {
        var pathVariable = Environment.GetEnvironmentVariable("PATH") ?? string.Empty;
        var pathEntries = pathVariable.Split(':', StringSplitOptions.RemoveEmptyEntries);

        if (pathEntries.Contains(folder))
            return;

        Environment.SetEnvironmentVariable("PATH", string.IsNullOrEmpty(pathVariable)
            ? folder
            : $"{folder}:{pathVariable}");
    }
}