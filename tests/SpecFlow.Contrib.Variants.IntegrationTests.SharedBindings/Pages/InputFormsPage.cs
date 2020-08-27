using OpenQA.Selenium;
using System.Collections.Generic;
using System.Linq;

namespace SpecFlow.Contrib.Variants.IntegrationTests.SharedBindings.Pages
{
    public class InputFormsPage
    {
        private readonly IWebDriver _driver;
        private readonly By _button = By.TagName("button");
        private readonly By _addedButton = By.ClassName("added-manually");
        private readonly By _items = By.CssSelector("ul>li>a");

        public InputFormsPage(IWebDriver driver)
        {
            _driver = driver;
        }

        public void AddElement()
        {
            _driver.FindElement(_button).Click();
        }

        public bool ElementAdded()
        {
            return _driver.FindElement(_addedButton).Displayed;
        }

        public IList<string> GetMenuItems()
        {
            return _driver.FindElements(_items).Select(a => a.Text).ToList();
        }
    }
}
