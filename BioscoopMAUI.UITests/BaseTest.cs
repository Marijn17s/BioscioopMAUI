using OpenQA.Selenium.Appium;
using OpenQA.Selenium.Appium.Android;

namespace BioscoopMAUI.UITests;

public abstract class BaseTest
{
    protected AppiumDriver App => AppiumSetup.App;

    protected AppiumElement FindUiElement(string automationId)
    {
        return App.FindElement(MobileBy.Id(automationId));
    }

    // Tabs expose their Title as the Android content-desc
    protected void TapTab(string tabTitle)
    {
        App.FindElement(MobileBy.AccessibilityId(tabTitle)).Click();
    }

    // Trigger a SearchBar search action
    protected void PressEnterKey()
    {
        const int enterKeyCode = 66;
        ((AndroidDriver)App).PressKeyCode(enterKeyCode);
    }

    // Quick visibility probe without the implicit wait, used to make tests independent of any state left behind by a previous test in the session.
    protected bool IsElementVisible(string automationId)
    {
        App.Manage().Timeouts().ImplicitWait = TimeSpan.Zero;
        try
        {
            return App.FindElements(MobileBy.Id(automationId)).Any(element => element.Displayed);
        }
        finally
        {
            App.Manage().Timeouts().ImplicitWait = TimeSpan.FromSeconds(10);
        }
    }
}