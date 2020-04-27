@Browser:Chrome
@Browser:Edge
Feature: NUnitDemoFeatureTests
	In order to verify the SpecFlow variants plugin for features
	As a developer
	I want to be able to run integration tests to validate the plugin

Background: 
	Given I am on the input forms page

Scenario: A single test without examples or tags
	When check the checkbox
	Then the checkbox text is 'Success - Check box is checked'

@Option_1
@Option_2
@Option_3
@Option_4
Scenario: A test with non-variant tags
	When I check all the option check boxes
	Then the tags check boxes should be checked

Scenario Outline: A test with variant tags and examples
	And I drill into the '<Link>' link
	When I drill into the '<Sublink>' link
	Then the page should be '<Site>'
	Examples:
	| Link         | Sublink               | Site                                                              |
	| Input Forms  | Simple Form Demo      | https://www.seleniumeasy.com/test/basic-first-form-demo.html      |
	| Date pickers | Bootstrap Date Picker | https://www.seleniumeasy.com/test/bootstrap-date-picker-demo.html |