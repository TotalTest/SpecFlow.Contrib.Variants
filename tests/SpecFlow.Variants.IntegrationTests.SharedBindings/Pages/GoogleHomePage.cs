using OpenQA.Selenium;

namespace SpecFlow.Variants.IntegrationTests.SharedBindings.Pages
{
    public class GoogleHomePage
    {
        private readonly IWebDriver _driver;
        private readonly By _searchBox = By.CssSelector(".gLFyf.gsfi");
        private readonly By _searchButton = By.CssSelector("[value='Google Search']");

        public GoogleHomePage(IWebDriver driver)
        {
            _driver = driver;
        }

        public void Navigate()
        {
            _driver.Navigate().GoToUrl("https://google.co.uk");
        }

        public void SearchFor(string searchTerm)
        {
            _driver.FindElement(_searchBox).SendKeys(searchTerm);
            _driver.FindElement(_searchBox).SendKeys(Keys.Enter);

            int i = 2;
            while (_driver.FindElements(_searchButton).Count > 0 && i > 0)
            {
                _driver.FindElements(_searchButton)[1].Click();
                i--;
            }
        }
    }
}
