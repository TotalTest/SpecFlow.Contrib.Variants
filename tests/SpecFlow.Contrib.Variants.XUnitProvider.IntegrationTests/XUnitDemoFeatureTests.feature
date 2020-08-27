@Browser:Chrome
@Browser:Edge
Feature: XUnitDemoFeatureTests
	In order to verify the SpecFlow variants plugin for features
	As a developer
	I want to be able to run integration tests to validate the plugin

Background: 
	Given I am on the home page

Scenario Outline: A test with variant tags and examples
	When I drill into the '<Link>' link
	Then the page should be '<Site>'
	Examples:
	| Link       | Site                                         |
	| Checkboxes | http://the-internet.herokuapp.com/checkboxes |
	| Dropdown   | http://the-internet.herokuapp.com/dropdown   |