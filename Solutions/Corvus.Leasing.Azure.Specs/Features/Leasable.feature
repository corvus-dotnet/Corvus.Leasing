@setupContainer

Feature: Leasable
	In order to avoid concurrency issues
	As an actor in the system
	I want to have an exclusive lease on a long running task

Scenario: A single actor executes a long running task with duration less than the lease policy
	Given the long running task takes 0.5 seconds to complete
	And the lease name is "long-running-task"
	When I execute the task
	Then it should not throw any exceptions
	And it should return successfully
	And 1 action(s) should have completed successfully

Scenario: A single actor executes a long running task with a result with duration less than the lease policy
	Given the long running task takes 0.5 seconds to complete
	And the lease name is "long-running-task"
	When I execute the task with a result
	Then it should not throw any exceptions
	And it should return successfully
	And 1 action(s) should have completed successfully
	And the task result should be correct

Scenario: A single actor executes a long running task with duration more than the lease policy
	Given the long running task takes 18 seconds to complete
	And the lease name is "long-running-task"
	And the lease duration is 15 seconds
	When I execute the task
	Then it should not throw any exceptions
	And it should return successfully
	And 1 action(s) should have completed successfully

Scenario: Actor A attempts to execute a long running task whilst Actor B is currently running the task
Given the long running task takes 0.5 seconds to complete
	And the lease name is "long-running-task"
	And actor B is currently running the task
	And actor A executes the task
	When the tasks have completed
	Then it should not throw any exceptions
	And it should return successfully
	And 2 action(s) should have completed successfully

Scenario: Actor A attempts to execute a long running task with a do not retry policy and a linear retry strategy, whilst Actor B is currently running the task
	Given the long running task takes 0.5 seconds to complete
	And the lease name is "long-running-task"
	And the lease duration is 15 seconds
	And we use a do not retry policy
	And we use a linear retry strategy with periodicity of 2 seconds and 10 max retries
	And actor B is currently running the task
	And actor A executes the task with options
	When the tasks have completed
	Then it should throw an AggregateException containing LeaseAcquisitionUnsuccessfulException
	And 1 action(s) should have completed successfully

Scenario: Actor A attempts to execute a long running task with a try once mutex, whilst Actor B is currently running the task
	Given the long running task takes 0.5 seconds to complete
	And the lease name is "long-running-task"
	And actor B is currently running the task
	And actor A executes the task with a try once mutex
	When the tasks have completed
	Then it should throw an AggregateException containing LeaseAcquisitionUnsuccessfulException
	And 1 action(s) should have completed successfully

Scenario: Actor A attempts to execute a long running task with a do not retry on lease acquisition unsuccessful policy and a linear retry strategy, whilst Actor B is currently running the task
	Given the long running task takes 0.5 seconds to complete
	And the lease name is "long-running-task"
	And the lease duration is 15 seconds
	And we use a do not retry on lease acquisition unsuccessful policy
	And we use a linear retry strategy with periodicity of 2 seconds and 10 max retries
	And actor B is currently running the task
	And actor A executes the task with options
	When the tasks have completed
	Then it should throw an AggregateException containing LeaseAcquisitionUnsuccessfulException
	And 1 action(s) should have completed successfully

Scenario: A single actor executes a long running task with a retry until lease acquired policy and no retry strategy
	Given the long running task takes 0.5 seconds to complete
	And the lease name is "long-running-task"
	And the lease duration is 40 seconds
	And we use a do not retry on lease acquisition unsuccessful policy
	And we use no retry strategy
	When I execute the task with options
	Then it should not throw any exceptions
	And it should return successfully
	And 1 action(s) should have completed successfully

Scenario: A single actor executes a long running task with no retry policy and a linear retry strategy
	Given the long running task takes 0.5 seconds to complete
	And the lease name is "long-running-task"
	And the lease duration is 40 seconds
	And we use no lease policy
	And we use a linear retry strategy with periodicity of 1 seconds and 10 max retries
	When I execute the task with options
	Then it should not throw any exceptions
	And it should return successfully
	And 1 action(s) should have completed successfully

Scenario: A single actor executes a long running task with no lease policy
	Given we use a linear retry strategy with periodicity of 10 seconds and 10 max retries
	And we use a do not retry policy
	And the long running task takes 2 seconds to complete
	When I execute the task with options
	Then it should not throw any exceptions
	And it should return successfully
	And 1 action(s) should have completed successfully

Scenario: A single actor executes a long running task using a multi-leasable with 3 leases
	Given the long running task takes 0.5 seconds to complete
	And the lease names are
	| Name     |
	| "lease1" |
	| "lease2" |
	| "lease3" |
	When I execute the task using all the leases
	Then 1 action(s) should have completed successfully

Scenario: A single actor executes a long running task with duration longer than the lease period using a multi-leasable with 3 leases
	Given the long running task takes 18 seconds to complete
	And the lease duration is 15 seconds
	And the lease names are
	| Name     |
	| "lease1" |
	| "lease2" |
	| "lease3" |
	When I execute the task using all the leases
	Then 1 action(s) should have completed successfully
#	