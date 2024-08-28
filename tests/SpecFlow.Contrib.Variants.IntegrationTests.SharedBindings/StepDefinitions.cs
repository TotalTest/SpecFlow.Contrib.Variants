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
        private readonly InputFormsPage _inputFormsPage;
        private readonly CommonsPage _commonsPage;

        public StepDefinitions(ScenarioContext scenarioContext, InputFormsPage inputFormsPage, CommonsPage commonsPage)
        {
            _scenarioContext = scenarioContext;
            _inputFormsPage = inputFormsPage;
            _commonsPage = commonsPage;
        }

        [Given("I am on the home page")]
        public void GivenIAmOnTheHomePage()
        {
            _commonsPage.Navigate();
        }

        [When("I add element")]
        public void WhenCheckTheCheckbox()
        {
            _inputFormsPage.AddElement();
        }

        [Then("the element is added")]
        public void ThenTheElementIsAdded()
        {
            if (!_inputFormsPage.ElementAdded())
                throw new Exception("Checked text was incorrect");
        }

        [Then("the tags match the menu items")]
        public void ThenTheTagsMatchTheMenuItems()
        {
            var tags = _scenarioContext.ScenarioInfo.Tags.Select(a => a.Replace("_", " ")).ToList();
            var items = _inputFormsPage.GetMenuItems();
            if (!tags.All(a => items.Contains(a)))
                throw new Exception("One or more tags were'nt displayed");
        }

        [Given("I drill into the '(.*)' link")]
        [When("I drill into the '(.*)' link")]
        public void GivenIDrillIntoTheLink(string link)
        {
            _commonsPage.NavigateSidebar(link);
        }

        [Then("the page should be '(.*)'")]
        public void ThenThePageShouldBe(string site)
        {
            if (!string.Equals(_commonsPage.Url, site, StringComparison.InvariantCultureIgnoreCase))
                throw new Exception($"The expected url was wrong");
        }

    }
}
