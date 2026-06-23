using NUnit.Framework;

namespace BioscoopMAUI.UITests;

[TestFixture]
public class ShowtimesFilterTests : BaseTest
{
    [Test]
    public void MovieSearchFilterShowsNoMatchesForUnknownTitle()
    {
        TapTab("Screenings");
        Assert.That(FindUiElement("PageTitle").Text, Is.EqualTo("Screenings"));

        if (!IsElementVisible("ShowtimesSearchBar"))
            FindUiElement("ShowtimesMovieFilterToggle").Click();

        var searchBar = FindUiElement("ShowtimesSearchBar");
        searchBar.Clear();
        searchBar.SendKeys("random-movie-title");
        searchBar.Click();
        PressEnterKey();

        Assert.That(FindUiElement("ShowtimesNoMatchesPanel").Displayed, Is.True);
    }
}