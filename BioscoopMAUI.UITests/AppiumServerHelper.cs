using OpenQA.Selenium.Appium.Service;
using OpenQA.Selenium.Appium.Service.Exceptions;

namespace BioscoopMAUI.UITests;

public static class AppiumServerHelper
{
    private static AppiumLocalService? _appiumLocalService;

    public const string DefaultHostAddress = "127.0.0.1";
    public const int DefaultHostPort = 4723;

    public static Uri? ExternalServerUrl { get; private set; }

    public static bool UseExternalServer => ExternalServerUrl is not null;

    static AppiumServerHelper()
    {
        var externalUrl = Environment.GetEnvironmentVariable("APPIUM_SERVER_URL");
        if (!string.IsNullOrWhiteSpace(externalUrl))
            ExternalServerUrl = new Uri(externalUrl);
    }

    public static void StartAppiumLocalServer(string host = DefaultHostAddress, int port = DefaultHostPort)
    {
        if (UseExternalServer || _appiumLocalService is not null)
            return;

        AndroidSdkEnvironment.EnsureConfigured();

        var builder = new AppiumServiceBuilder()
            .WithIPAddress(host)
            .UsingPort(port)
            .WithEnvironment(AndroidSdkEnvironment.CreateProcessEnvironment());

        var nodeBinaryPath = ResolveNodeBinaryPath();
        if (nodeBinaryPath is not null)
            builder.UsingDriverExecutable(new FileInfo(nodeBinaryPath));

        try
        {
            _appiumLocalService = builder.Build();
            _appiumLocalService.Start();
        }
        catch (InvalidNodeJSInstanceException)
        {
            throw new InvalidOperationException(
                "Appium requires Node.js. Install it (e.g. `brew install node`), ensure `node` is on PATH, " +
                "or set NODE_BINARY_PATH to the node executable. Alternatively, start Appium manually " +
                "(`appium`) and set APPIUM_SERVER_URL=http://127.0.0.1:4723/ to skip auto-start.");
        }
    }

    public static void DisposeAppiumLocalServer()
    {
        _appiumLocalService?.Dispose();
        _appiumLocalService = null;
    }

    public static Uri GetServerUri()
    {
        if (ExternalServerUrl is not null)
            return ExternalServerUrl;

        if (_appiumLocalService is not null)
            return _appiumLocalService.ServiceUrl;

        return new Uri($"http://{DefaultHostAddress}:{DefaultHostPort}/");
    }

    private static string? ResolveNodeBinaryPath()
    {
        var configuredPath = Environment.GetEnvironmentVariable(AppiumServiceConstants.NodeBinaryPath);
        if (!string.IsNullOrWhiteSpace(configuredPath) && File.Exists(configuredPath))
            return configuredPath;

        var pathEnvironmentVariable = Environment.GetEnvironmentVariable("PATH");
        if (!string.IsNullOrWhiteSpace(pathEnvironmentVariable))
        {
            foreach (var folder in pathEnvironmentVariable.Split(':', StringSplitOptions.RemoveEmptyEntries))
            {
                var candidate = Path.Combine(folder, "node");
                if (File.Exists(candidate))
                    return candidate;
            }
        }

        string[] defaultCandidates =
        [
            "/opt/homebrew/bin/node",
            "/usr/local/bin/node"
        ];

        foreach (var candidate in defaultCandidates)
        {
            if (File.Exists(candidate))
                return candidate;
        }

        return null;
    }
}