using NUnit.Framework;
using OpenQA.Selenium.Appium;

namespace BioscoopMAUI.UITests;

[TestFixture]
public class MoviesListTests : BaseTest
{
    [Test]
    public void AuthenticatedUserSeesMoviesList()
    {
        TapTab("Movies");

        Assert.That(FindUIElement("PageTitle").Text, Is.EqualTo("Movies"));

        var movieCards = App.FindElements(MobileBy.Id("MovieListItem"));
        Assert.That(movieCards, Is.Not.Empty);
    }
}