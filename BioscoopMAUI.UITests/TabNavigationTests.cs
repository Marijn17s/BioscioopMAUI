using NUnit.Framework;

namespace BioscoopMAUI.UITests;

[TestFixture]
public class TabNavigationTests : BaseTest
{
    [Test]
    public void AuthenticatedUserCanSwitchNavigationTabs()
    {
        TapTab("Home");
        Assert.That(FindUiElement("PageTitle").Text, Is.EqualTo("Home"));

        TapTab("Movies");
        Assert.That(FindUiElement("PageTitle").Text, Is.EqualTo("Movies"));

        TapTab("Screenings");
        Assert.That(FindUiElement("PageTitle").Text, Is.EqualTo("Screenings"));

        TapTab("Settings");

        Assert.Multiple(() =>
        {
            Assert.That(FindUiElement("PageTitle").Text, Is.EqualTo("Settings"));
            Assert.That(FindUiElement("LogoutButton").Displayed, Is.True);
        });
    }
}