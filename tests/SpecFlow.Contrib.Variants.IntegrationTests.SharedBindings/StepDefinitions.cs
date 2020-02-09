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

        [Given("I am on the input forms page")]
        public void GivenIAmOnTheInputFormsPage()
        {
            _inputFormsPage.Navigate();
        }

        [When("check the checkbox")]
        public void WhenCheckTheCheckbox()
        {
            _inputFormsPage.CheckBox();
        }

        [Then("the checkbox text is '(.*)'")]
        public void ThenTheCheckboxTextIs(string text)
        {
            if (!string.Equals(_inputFormsPage.CheckedText(), text, StringComparison.InvariantCultureIgnoreCase))
                throw new Exception("Checked text was incorrect");
        }

        [When("I check all the option check boxes")]
        public void WhenICheckAllTheOptionCheckBoxes()
        {
            _inputFormsPage.CheckAll();
        }

        [Then("the tags check boxes should be checked")]
        public void ThenTheTagsCheckBoxesShouldBeChecked()
        {
            var tags = _scenarioContext.ScenarioInfo.Tags.Select(a => a.Replace("_", " ")).ToList();
            if (!tags.All(a => _inputFormsPage.CheckboxByName(a)))
                throw new Exception("One or more checkboxes were not checked");
        }

        [Given("I drill into the '(.*)' link")]
        [When("I drill into the '(.*)' link")]
        public void GivenIDrillIntoTheLink(string link)
        {
            _commonsPage.NavigateSidebar(link);
        }

        [Then(@"the page should be '(.*)'")]
        public void ThenThePageShouldBe(string site)
        {
            if (!string.Equals(_commonsPage.Url, site, StringComparison.InvariantCultureIgnoreCase))
                throw new Exception("The expected url was wrong");
        }

    }
}
