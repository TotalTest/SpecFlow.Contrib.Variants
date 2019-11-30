using OpenQA.Selenium;
using OpenQA.Selenium.Support.UI;
using System;
using System.Linq;

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
            var wait = new WebDriverWait(_driver, TimeSpan.FromSeconds(5));
            wait.Until((a) =>
            {
                var gs = a.FindElements(_searchButton).FirstOrDefault(el => el.Displayed);
                if (gs == null) { return gs; }
                return SeleniumExtras.WaitHelpers.ExpectedConditions.ElementToBeClickable(gs).Invoke(a);
            }).Click();
        }
    }
}
