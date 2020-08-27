Feature: XUnitDemoScenarioTests
	In order to verify the SpecFlow variants plugin for scenarios
	As a developer
	I want to be able to run integration tests to validate the plugin

Background:
	Given I am on the home page

Scenario: A single test without examples or tags
	And I drill into the 'Add/Remove Elements' link
	When I add element
	Then the element is added

@Browser:Chrome
@Browser:Edge
Scenario: A test with variant tags
	And I drill into the 'Add/Remove Elements' link
	When I add element
	Then the element is added