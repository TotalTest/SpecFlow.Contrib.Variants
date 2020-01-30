using SpecFlow.Contrib.Variants.IntegrationTests.SharedBindings.Pages;
using System;
using System.Linq;
using TechTalk.SpecFlow;

namespace SpecFlow.Contrib.Variants.IntegrationTests.SharedBindings
{
    [Binding]
    public sealed class StepDefinitions
    {
        private readonly ScenarioContext _scenarioContext;
        private readonly GoogleHomePage _googleHomePage;
        private readonly GoogleSearchPage _googleSearchPage;
        private readonly GitHubAccountPage _gitHubPage;

        public StepDefinitions(ScenarioContext scenarioContext, GoogleHomePage googleHomePage, GoogleSearchPage googleSearchPage, GitHubAccountPage gitHubPage)
        {
            _googleHomePage = googleHomePage;
            _gitHubPage = gitHubPage;
            _scenarioContext = scenarioContext;
            _googleSearchPage = googleSearchPage;
        }

        [Given(@"I am on the Google home page")]
        public void GivenIAmOnTheGoogleHomePage()
        {
            _googleHomePage.Navigate();
        }

        [Given(@"I navigate to the '(.*)' Github page")]
        public void GivenINavigateToTheTotalTestGithubPage(string account)
        {
            _gitHubPage.Navigate(account);
        }

        [When(@"I search for '(.*)'")]
        public void WhenISearchFor(string searchTerm)
        {
            _googleHomePage.SearchFor(searchTerm);
        }

        [When(@"I select the result '(.*)'")]
        public void WhenISelectTheResult(string result)
        {
            _googleSearchPage.SelectResult(result);
        }

        [When(@"I drill into the '(.*)' repository")]
        public void WhenIDrillIntoTheRepository(string repo)
        {
            _gitHubPage.SelectRepo(repo);
        }

        [Then(@"the following result should be listed:")]
        public void ThenTheFollowingResultShouldBeListed(string multilineText)
        {
            var searchResult = _googleSearchPage.GetSearchResults();
            var result = searchResult.Any(a => a.IndexOf(multilineText, StringComparison.InvariantCultureIgnoreCase) >= 0);

            if (!result) { throw new Exception("Test Failed"); }
        }

        [Then(@"there should be links to the tags specified")]
        public void ThenThereShouldBeLinksToTheTagsSpecified()
        {
            var tags = _scenarioContext.ScenarioInfo.Tags.ToList();
            var pageLinks = _googleSearchPage.GetLinks();

            if (tags.Except(pageLinks).Any()) { throw new Exception("Test Failed"); }
        }

        [Then(@"I should be on the website '(.*)'")]
        public void ThenIShouldBeOnTheWebsite(string site)
        {
            var page = _gitHubPage.CurrentUrl;

            if (page != site) { throw new Exception($"Expected '{site}'. Actual '{page}'"); }
        }
    }
}
