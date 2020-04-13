Feature: XUnitDemoScenarioTests
	In order to verify the SpecFlow variants plugin for scenarios
	As a developer
	I want to be able to run integration tests to validate the plugin

Background:
	Given I am on the input forms page

Scenario: A single test without examples or tags
	When check the checkbox
	Then the checkbox text is 'Success - Check box is checked'

@Browser:Chrome
@Browser:Firefox
Scenario: A test with variant tags
	When check the checkbox
	Then the checkbox text is 'Success - Check box is checked'