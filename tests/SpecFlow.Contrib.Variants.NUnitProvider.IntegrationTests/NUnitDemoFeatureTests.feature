@Variant:Chrome
@Variant:Firefox
Feature: NUnitDemoFeatureTests
	In order to verify the SpecFlow variants plugin for features
	As a developer
	I want to be able to run integration tests to validate the plugin

Background: 
	Given I am on the input forms page

Scenario Outline: A test with variant tags and examples
	And I drill into the '<Link>' link
	When I drill into the '<Sublink>' link
	Then the page should be '<Site>'
	Examples:
	| Link         | Sublink               | Site                                                              |
	| Input Forms  | Simple Form Demo      | https://www.seleniumeasy.com/test/basic-first-form-demo.html      |
	| Date pickers | Bootstrap Date Picker | https://www.seleniumeasy.com/test/bootstrap-date-picker-demo.html |