using OpenQA.Selenium;
using System.Linq;

namespace SpecFlow.Contrib.Variants.IntegrationTests.SharedBindings.Pages
{
    public class InputFormsPage
    {
        private readonly IWebDriver _driver;
        private readonly By _checkBox = By.Id("isAgeSelected");
        private readonly By _checkedText = By.Id("txtAge");
        private readonly By _checkAll = By.Id("check1");
        private readonly By _checkBoxes = By.ClassName("cb1-element");

        public InputFormsPage(IWebDriver driver)
        {
            _driver = driver;
        }

        public void Navigate()
        {
            _driver.Navigate().GoToUrl("https://www.seleniumeasy.com/test/basic-checkbox-demo.html");
        }

        public void CheckBox()
        {
            _driver.FindElement(_checkBox).Click();
        }

        public string CheckedText()
        {
            return _driver.FindElement(_checkedText).Text;
        }

        public void CheckAll()
        {
            _driver.FindElement(_checkAll).Click();
        }

        public bool CheckboxByName(string name)
        {
            var options = _driver.FindElements(_checkBoxes);
            var currentOption = options.First(a => a.FindElement(By.XPath("..")).Text == name);

            return currentOption.Selected;
        }
    }
}
