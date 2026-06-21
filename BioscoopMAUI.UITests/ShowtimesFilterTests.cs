using NUnit.Framework;

namespace BioscoopMAUI.UITests;

[TestFixture]
public class ShowtimesFilterTests : BaseTest
{
    [Test]
    public void MovieSearchFilterShowsNoMatchesForUnknownTitle()
    {
        TapTab("Screenings");
        Assert.That(FindUIElement("PageTitle").Text, Is.EqualTo("Screenings"));

        if (!IsElementVisible("ShowtimesSearchBar"))
            FindUIElement("ShowtimesMovieFilterToggle").Click();

        var searchBar = FindUIElement("ShowtimesSearchBar");
        searchBar.Clear();
        searchBar.SendKeys("zzzz-no-matching-movie-title");
        searchBar.Click();
        PressEnterKey();

        Assert.That(FindUIElement("ShowtimesNoMatchesPanel").Displayed, Is.True);
    }
}