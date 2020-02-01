[![Build Status](https://dev.azure.com/totaltestltd/Total%20Test/_apis/build/status/TotalTest.SpecFlow.Contrib.Variants?branchName=release)](https://dev.azure.com/totaltestltd/Total%20Test/_build/latest?definitionId=5&branchName=release)
![Azure DevOps tests (branch)](https://img.shields.io/azure-devops/tests/totaltestltd/Total%20Test/5/release)
[![NuGet](https://img.shields.io/nuget/v/specflow.contrib.variants.svg)](https://nuget.org/packages/specflow.contrib.variants)

# SpecFlow.Contrib.Variants
SpecFlow plugin to allow variants of a test to be run using tags.
For example (but not limited to) running scenarios or features against different browsers if performing UI tests. 

## Usage - SpecFlow v3+
_Coming soon_

## Usage - SpecFlow v2.4

### 1. Installation

Install plugin using Nuget Package Manager

```powershell
PM> Install-Package SpecFlow.Contrib.Variants
```

### 2. Overview
Feature variant tags mean each scenario within that feature is run for each variant.
\
i.e 4 test cases for the below two scenarios:
```gherkin
@Browser:Chrome
@Browser:Firefox
Feature: AnExampleFeature

Scenario: Simple scenario
	Given something has happened
	When I do something
	Then the result should be something else

Scenario: Simple scenario two
	Given something has happened
	When I do something
	Then the result should be something else
```
\
Scenario variant tags mean the scenario is run for each of its variants.
\
i.e 4 test cases for the below scenario:
```gherkin
Feature: AnExampleFeature

@Browser:Chrome
@Browser:Firefox
Scenario: Simple scenario
	Given something has happened
	When I do something
	Then the result should be something
```

### 3. App.config
Specify the plugin name and ensure the type is set to 'Generator'. The variant key can also be a custom value, the default is 'Variant' if none is specified.

e.g. 
```XML
<specFlow>
  <unitTestProvider name="xunit" />
  <plugins>
    <add name="SpecFlow.Contrib.Variants" type="Generator" parameters="Browser" />
  </plugins>
</specFlow>
 ```
The above will ensure the plugin is used and that 'Browser' is set as the variant key. This means any tags starting with `@Browser:` will be treated as variants. 

A colon should be used as the seperator between the variant key and value. For example `@Browser:Chrome` will mean 'Chrome' is the variant value.

The unitTestProvider can either be xunit, mstest or nunit.

## 4. Access the variant
The variant key/value can then be accessed via the ScenarioContext static or injected class. This decision was made to cater for all supported test frameworks (NUnit, MsTest and XUnit).

```csharp
[Binding]
public sealed class Hooks
{
    private readonly ScenarioContext _scenarioContext;
    private IWebDriver _driver;

    public Hooks(ScenarioContext scenarioContext)
    {
        _scenarioContext = scenarioContext;
    }

    [BeforeScenario]
    public void BeforeScenario()
    {
        _scenarioContext.TryGetValue("Browser", out var browser);

        switch (browser)
        {
            case "Chrome":
                _driver = SetupChromeDriver();
                break;
            case "Firefox":
                _driver = SetupFirefoxDriver();
                break;
            default:
                _driver = SetupChromeDriver();
                break;
        }
        _scenarioContext.ScenarioContainer.RegisterInstanceAs(_driver);
    }
    ...
}
```

It's also possible to use the in built contexts per test framework if desired (doesn't apply to XUnit, which is why ScenarioContext is recommended):

__MsTest__
```csharp
var browser = TestContext.Properties["Browser"];
```

__NUnit__
```csharp
var categories = TestContext.CurrentContext.Test.Properties["Category"];
var browser = categories.First(a => a.ToString().StartsWith("Browser").ToString().Split(':')[1];
```

See the integration test projects for full example.

## License
This project uses the [MIT](https://choosealicense.com/licenses/mit/) license.