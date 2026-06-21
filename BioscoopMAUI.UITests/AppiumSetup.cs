using NUnit.Framework;
using OpenQA.Selenium.Appium;
using OpenQA.Selenium.Appium.Android;
using OpenQA.Selenium.Appium.Enums;

namespace BioscoopMAUI.UITests;

[SetUpFixture]
public class AppiumSetup
{
    private const string AppPackage = "com.marijn17s.bioscoop_maui";
    private const string AppActivity = "com.marijn17s.bioscoop_maui.MainActivity";

    private static AppiumDriver? _driver;

    public static AppiumDriver App => _driver ?? throw new InvalidOperationException("Appium driver is not initialized.");

    [OneTimeSetUp]
    public void RunBeforeAnyTests()
    {
        AndroidSdkEnvironment.EnsureConfigured();
        AppiumServerHelper.StartAppiumLocalServer();

        var androidOptions = new AppiumOptions
        {
            AutomationName = "UIAutomator2",
            PlatformName = "Android"
        };

        androidOptions.AddAdditionalAppiumOption(MobileCapabilityType.NoReset, "true");
        androidOptions.AddAdditionalAppiumOption(AndroidMobileCapabilityType.AppPackage, AppPackage);
        androidOptions.AddAdditionalAppiumOption(AndroidMobileCapabilityType.AppActivity, AppActivity);

        _driver = new AndroidDriver(AppiumServerHelper.GetServerUri(), androidOptions);
        _driver.Manage().Timeouts().ImplicitWait = TimeSpan.FromSeconds(10);
    }

    [OneTimeTearDown]
    public void RunAfterAnyTests()
    {
        _driver?.Quit();
        _driver = null;
        AppiumServerHelper.DisposeAppiumLocalServer();
    }
}