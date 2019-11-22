using OpenQA.Selenium;
using System.Collections.Generic;
using System.Linq;

namespace SpecFlow.Variants.IntegrationTests.SharedBindings.Pages
{
    public class GoogleSearchPage
    {
        private readonly IWebDriver _driver;
        private readonly By _searchResults = By.CssSelector("#search .S3Uucc");
        private readonly By _links = By.CssSelector("#hdtb-msb a");

        public GoogleSearchPage(IWebDriver driver)
        {
            _driver = driver;
        }

        public List<string> GetSearchResults()
        {
            return _driver.FindElements(_searchResults).Select(a => a.Text).ToList();
        }

        public List<string> GetLinks()
        {
            return _driver.FindElements(_links).Select(a => a.Text).ToList();
        }

        public void SelectResult(string result)
        {
            _driver.FindElements(_searchResults)
                .First(a => a.Text.Contains(result)).Click();

            if (_driver.FindElements(_searchResults).Count > 0)
            {
                _driver.FindElements(_searchResults)
                .FirstOrDefault(a => a.Text.Contains(result))?.Click();
            }
        }
    }
}
