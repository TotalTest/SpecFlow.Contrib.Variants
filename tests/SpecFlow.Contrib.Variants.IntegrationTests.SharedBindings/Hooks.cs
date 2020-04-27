using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Edge;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using TechTalk.SpecFlow;

namespace SpecFlow.Contrib.Variants.IntegrationTests.SharedBindings
{
    [Binding]
    public sealed class Hooks
    {
        private readonly ScenarioContext _scenarioContext;
        private string _baseDir;
        private IWebDriver _driver;

        public Hooks(ScenarioContext scenarioContext)
        {
            _scenarioContext = scenarioContext;
        }

        [BeforeScenario]
        public void BeforeScenario()
        {
            _scenarioContext.TryGetValue("Browser", out var browser);

            _baseDir = AppDomain.CurrentDomain.BaseDirectory.ToLowerInvariant();

            switch (browser)
            {
                case "Chrome":
                    _driver = SetupChromeDriver();
                    break;
                case "Edge":
                    _driver = SetupEdgeDriver();
                    break;
                default:
                    _driver = SetupChromeDriver();
                    break;
            }

            _driver.Manage().Timeouts().ImplicitWait = TimeSpan.FromSeconds(5);
            _scenarioContext.ScenarioContainer.RegisterInstanceAs(_driver);
        }

        private IWebDriver SetupChromeDriver()
        {
            var envChromeWebDriver = Environment.GetEnvironmentVariable("ChromeWebDriver");
            var co = new ChromeOptions();
            co.AddArgument("headless");
            return new ChromeDriver(envChromeWebDriver, co);
        }

        private IWebDriver SetupEdgeDriver()
        {
            var envEdgeWebDriver = Environment.GetEnvironmentVariable("EdgeWebDriver");
            var ed = new EdgeOptions { UseChromium = true };
            return new EdgeDriver(envEdgeWebDriver, ed);
        }

        [AfterScenario]
        public void After()
        {
            if (_scenarioContext.TestError != null)
            {
                var path = Path.Combine(_baseDir, "Screenshots");
                if (!Directory.Exists(path))
                    Directory.CreateDirectory(path);
                try
                {
                    ((ITakesScreenshot)_driver).GetScreenshot().SaveAsFile($@"{path}\{_scenarioContext.ScenarioInfo.Title}.jpg", ScreenshotImageFormat.Jpeg);
                }
                catch (Exception e)
                {
                    Console.WriteLine("Unexpected Error trying to take a screen shot");
                    Console.WriteLine($"Error message: {e.Message}");
                }
            }

            try
            {
                _driver.Dispose();
            }
            catch (Exception e)
            {
                Console.WriteLine("Unexpected error disposing driver");
                Console.WriteLine($"Error message: {e.Message}");
            }

            _scenarioContext.ScenarioContainer.Dispose();
        }

        [AfterTestRun]
        public static void AfterRun()
        {
            var processes = Process.GetProcessesByName("msedgedriver").ToList();
            processes.AddRange(Process.GetProcessesByName("chromedriver"));

            foreach (var process in processes)
                process.Kill();
        }
    }
}
