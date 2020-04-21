[![Build Status](https://dev.azure.com/totaltestltd/Total%20Test/_apis/build/status/TotalTest.SpecFlow.Contrib.Variants?branchName=release)](https://dev.azure.com/totaltestltd/Total%20Test/_build/latest?definitionId=5&branchName=release)
![Azure DevOps tests](https://img.shields.io/azure-devops/tests/totaltestltd/Total%20Test/5)
![Nuget (with prereleases)](https://img.shields.io/nuget/vpre/specflow.contrib.variants)

# SpecFlow.Contrib.Variants
SpecFlow plugin to allow variants of a test to be run using tags.
For example (but not limited to) running scenarios or features against different browsers if performing UI tests.
Supports MsTest, NUnit and xUnit

## 1. SpecFlow v3+ notes
In line with SpecFlow's docs, it is required that one of the following unit test providers package is installed (apart from SpecRun which is not supported by this plugin):

- SpecFlow.xUnit
- SpecFlow.MsTest
- SpecFlow.NUnit

It is also recommended that specflow.json is used over app.config. When using this plugin however, app.config is also supported for .net framework projects. Details about specific configuration is explained further below.
\
Note that only specflow.json is supported in .net core projects so app.config can't be used for those. Original docs can be found here: 
https://specflow.org/documentation/configuration/

## 2. SpecFlow v2.4 notes
As this version of SpecFlow only works with app.config, the details for configuration if using this version can be found below.

## 3. Usage

### 3.1 Installation

Install plugin using Nuget Package Manager

```powershell
PM> Install-Package SpecFlow.Contrib.Variants
```

### 3.2 Overview
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
i.e 3 test cases for the below scenario:
```gherkin
Feature: AnExampleFeature

@Browser:Chrome
@Browser:Firefox
@Browser:Edge
Scenario: Simple scenario
	Given something has happened
	When I do something
	Then the result should be something
```

### 3.3 Access the variant
The variant key/value can then be accessed via the ScenarioContext static or injected class. This decision was made to cater for all supported test frameworks (NUnit, MsTest and xUnit).

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

It's also possible to use the in built contexts per test framework if desired (doesn't apply to xUnit, which is why ScenarioContext is recommended):

__MsTest__
```csharp
var browser = TestContext.Properties["Browser"];
```
\
__NUnit__
```csharp
var categories = TestContext.CurrentContext.Test.Properties["Category"];
var browser = categories.First(a => a.ToString().StartsWith("Browser").ToString().Split(':')[1];
```

See the integration test projects for full example.

## 4. Configuration

### 4.1 SpecFlow v3+
__specflow.json__
\
The default variant key is 'Variant' if nothing specific is set. This means the tag `@Variant:Chrome` will be treated as a variant, where 'Chrome' is the variant value. However, the variant key can be customised in the specflow.json file:

```json
{
  "pluginparameters": {
    "variantkey": "Browser"
  }
}
```

The above means that only tags that begin with `@Browser:` will be treated as variants.

An example can be found [here](https://github.com/TotalTest/SpecFlow.Contrib.Variants/blob/master/tests/SpecFlow.Contrib.Variants.Core.MsTestProvider.IntegrationTests/specflow.json)

__app.config__
\
If using app.config (applicable only for .net framework), the custom variant key can be set in the following generator element and path attribute:

```XML
<configSections>
  <section name="specFlow" type="TechTalk.SpecFlow.Configuration.ConfigurationSectionHandler, TechTalk.SpecFlow" />
</configSections>
<specFlow>
  <generator path="VariantKey:Browser" />
</specFlow>
```
This isn't the ideal element to use but was the best possibility we had, the path value is only treated as a variant if it starts with 'VariantKey:' meaning the generator element can be still be used as originally intended.

An example can be found [here](https://github.com/TotalTest/SpecFlow.Contrib.Variants/blob/master/tests/SpecFlow.Contrib.Variants.MsTestProvider.IntegrationTests/App.config)

### 4.2 SpecFlow v2.4 (app.config)
Specify the plugin name and ensure the type is set to 'Generator'. The variant key can also be a custom value, the default key is 'Variant' if no parameters value is specified.

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



## License
This project uses the [MIT](https://choosealicense.com/licenses/mit/) license.
