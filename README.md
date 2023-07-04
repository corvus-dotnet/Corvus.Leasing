# Corvus.Leasing

[![Build Status](https://dev.azure.com/endjin-labs/Corvus.Leasing/_apis/build/status/corvus-dotnet.Corvus.Leasing?branchName=main)](https://dev.azure.com/endjin-labs/Corvus.Leasing/_build/latest?definitionId=4&branchName=main)
[![GitHub license](https://img.shields.io/badge/License-Apache%202-blue.svg)](https://raw.githubusercontent.com/corvus-dotnet/Corvus.Leasing/main/LICENSE)
[![IMM](https://imm.endjin.com/api/imm/github/corvus-dotnet/Corvus.Leasing/total?cache=false)](https://imm.endjin.com/api/imm/github/corvus-dotnet/Corvus.Leasing/total?cache=false)

Leasing patterns for mediating access to exclusive resources in distributed processes. A generic abstraction, with an Azure blob-based implementation.

It is built for netstandard2.0.

## Features

Corvus.Leasing provides a means to acquire, release and extend exclusive leases to mediate resource access in distributed processing.

**Caution** - you should be aware that using an exclusive leasing pattern, while essential for some activities, can introduce bottlenecks and deadlocks in your distributed processing (rather similar to the use of a mutex in multithreaded programming on a single instance), and you should be careful to understand the implications of introducing this into your system.

The `ILeaseProvider` interface supports a cycle from `Acquire` (for a duration), through an optional `Extend` (renewing a lease before it expires for another lease period) to `Release` (relinquish the lease).

Standard implementations of this are provided for Azure Blob storage, and an "in memory" version which is intended for testing, rather than production code..

Typically, you use the lease provider through one of the extension methods in `LeaseProviderExtensions`.

These give you a mutex model, with try-once or try-until-acquire-or-timeout semantics, and auto-renewal behaviour.

At its simplest, you can execute an action with guaranteed distributed mutex semantics.

```
leaseProvider.ExecuteWithMutexAsync(() => { /* my action */ }, "myuniqueleasename");
```

This will block until the lease is acquired, and then execute the action, holding the lease until it is done.

The operation has a default timeout. If the lease cannot be acquired in time, it will throw a `LeaseAcquisitionUnsuccessfulException`.

You can also use the "try once" semantics.

```
leaseProvider.ExecuteWithMutexTryOnceAsync(() => { /* my action */ }, "myuniqueleasename");
```

This will try to acquire the lease, and then execute the action, holding the lease until it is done. If it cannot acquire the lease, it will immediately throw
a `LeaseAcquisitionUnsuccessfulException` without retrying.

If you do not want to see the `LeaseAcquisitionUnsuccessfulException`, you can suppress it and turn it into a boolean return value
with an extension method.

```
leaseProvider.ExecuteWithMutexAsync(() => { /* my action */ }, "myuniqueleasename").DoNotThrowIfLeaseNotAcquired();
```

There are various overloads that allow you to control the duration of the lease, and the strategies and policy for retrying in the event of failure.

Most providers also have constraints on the lease name and lease duration - e.g. special characters, minimum duration. If you use an invalid parameter,
it will raise an `InvalidOperationException`.

It is also possible that the leasing operation will fail during initialization. In that case you will see an `InitializationFailureException`

## Licenses

[![GitHub license](https://img.shields.io/badge/License-Apache%202-blue.svg)](https://raw.githubusercontent.com/corvus-dotnet/Corvus.Leasing/main/LICENSE)

Corvus.Leasing is available under the Apache 2.0 open source license.

For any licensing questions, please email [&#108;&#105;&#99;&#101;&#110;&#115;&#105;&#110;&#103;&#64;&#101;&#110;&#100;&#106;&#105;&#110;&#46;&#99;&#111;&#109;](&#109;&#97;&#105;&#108;&#116;&#111;&#58;&#108;&#105;&#99;&#101;&#110;&#115;&#105;&#110;&#103;&#64;&#101;&#110;&#100;&#106;&#105;&#110;&#46;&#99;&#111;&#109;)

## Project Sponsor

This project is sponsored by [endjin](https://endjin.com), a UK based Microsoft Gold Partner for Cloud Platform, Data Platform, Data Analytics, DevOps, and a Power BI Partner.

For more information about our products and services, or for commercial support of this project, please [contact us](https://endjin.com/contact-us). 

We produce two free weekly newsletters; [Azure Weekly](https://azureweekly.info) for all things about the Microsoft Azure Platform, and [Power BI Weekly](https://powerbiweekly.info).

Keep up with everything that's going on at endjin via our [blog](https://blogs.endjin.com/), follow us on [Twitter](https://twitter.com/endjin), or [LinkedIn](https://www.linkedin.com/company/1671851/).

Our other Open Source projects can be found on [GitHub](https://endjin.com/open-source)

## Code of conduct

This project has adopted a code of conduct adapted from the [Contributor Covenant](http://contributor-covenant.org/) to clarify expected behavior in our community. This code of conduct has been [adopted by many other projects](http://contributor-covenant.org/adopters/). For more information see the [Code of Conduct FAQ](https://opensource.microsoft.com/codeofconduct/faq/) or contact [&#104;&#101;&#108;&#108;&#111;&#064;&#101;&#110;&#100;&#106;&#105;&#110;&#046;&#099;&#111;&#109;](&#109;&#097;&#105;&#108;&#116;&#111;:&#104;&#101;&#108;&#108;&#111;&#064;&#101;&#110;&#100;&#106;&#105;&#110;&#046;&#099;&#111;&#109;) with any additional questions or comments.

## IP Maturity Matrix (IMM)

The IMM is endjin's IP quality framework.

[![Shared Engineering Standards](https://imm.endjin.com/api/imm/github/corvus-dotnet/Corvus.Leasing/rule/74e29f9b-6dca-4161-8fdd-b468a1eb185d?nocache=true)](https://imm.endjin.com/api/imm/github/corvus-dotnet/Corvus.Leasing/rule/74e29f9b-6dca-4161-8fdd-b468a1eb185d?cache=false)

[![Coding Standards](https://imm.endjin.com/api/imm/github/corvus-dotnet/Corvus.Leasing/rule/f6f6490f-9493-4dc3-a674-15584fa951d8?cache=false)](https://imm.endjin.com/api/imm/github/corvus-dotnet/Corvus.Leasing/rule/f6f6490f-9493-4dc3-a674-15584fa951d8?cache=false)

[![Executable Specifications](https://imm.endjin.com/api/imm/github/corvus-dotnet/Corvus.Leasing/rule/bb49fb94-6ab5-40c3-a6da-dfd2e9bc4b00?cache=false)](https://imm.endjin.com/api/imm/github/corvus-dotnet/Corvus.Leasing/rule/bb49fb94-6ab5-40c3-a6da-dfd2e9bc4b00?cache=false)

[![Code Coverage](https://imm.endjin.com/api/imm/github/corvus-dotnet/Corvus.Leasing/rule/0449cadc-0078-4094-b019-520d75cc6cbb?cache=false)](https://imm.endjin.com/api/imm/github/corvus-dotnet/Corvus.Leasing/rule/0449cadc-0078-4094-b019-520d75cc6cbb?cache=false)

[![Benchmarks](https://imm.endjin.com/api/imm/github/corvus-dotnet/Corvus.Leasing/rule/64ed80dc-d354-45a9-9a56-c32437306afa?cache=false)](https://imm.endjin.com/api/imm/github/corvus-dotnet/Corvus.Leasing/rule/64ed80dc-d354-45a9-9a56-c32437306afa?cache=false)

[![Reference Documentation](https://imm.endjin.com/api/imm/github/corvus-dotnet/Corvus.Leasing/rule/2a7fc206-d578-41b0-85f6-a28b6b0fec5f?cache=false)](https://imm.endjin.com/api/imm/github/corvus-dotnet/Corvus.Leasing/rule/2a7fc206-d578-41b0-85f6-a28b6b0fec5f?cache=false)

[![Design & Implementation Documentation](https://imm.endjin.com/api/imm/github/corvus-dotnet/Corvus.Leasing/rule/f026d5a2-ce1a-4e04-af15-5a35792b164b?cache=false)](https://imm.endjin.com/api/imm/github/corvus-dotnet/Corvus.Leasing/rule/f026d5a2-ce1a-4e04-af15-5a35792b164b?cache=false)

[![How-to Documentation](https://imm.endjin.com/api/imm/github/corvus-dotnet/Corvus.Leasing/rule/145f2e3d-bb05-4ced-989b-7fb218fc6705?cache=false)](https://imm.endjin.com/api/imm/github/corvus-dotnet/Corvus.Leasing/rule/145f2e3d-bb05-4ced-989b-7fb218fc6705?cache=false)

[![Date of Last IP Review](https://imm.endjin.com/api/imm/github/corvus-dotnet/Corvus.Leasing/rule/da4ed776-0365-4d8a-a297-c4e91a14d646?cache=false)](https://imm.endjin.com/api/imm/github/corvus-dotnet/Corvus.Leasing/rule/da4ed776-0365-4d8a-a297-c4e91a14d646?cache=false)

[![Framework Version](https://imm.endjin.com/api/imm/github/corvus-dotnet/Corvus.Leasing/rule/6c0402b3-f0e3-4bd7-83fe-04bb6dca7924?cache=false)](https://imm.endjin.com/api/imm/github/corvus-dotnet/Corvus.Leasing/rule/6c0402b3-f0e3-4bd7-83fe-04bb6dca7924?cache=false)

[![Associated Work Items](https://imm.endjin.com/api/imm/github/corvus-dotnet/Corvus.Leasing/rule/79b8ff50-7378-4f29-b07c-bcd80746bfd4?cache=false)](https://imm.endjin.com/api/imm/github/corvus-dotnet/Corvus.Leasing/rule/79b8ff50-7378-4f29-b07c-bcd80746bfd4?cache=false)

[![Source Code Availability](https://imm.endjin.com/api/imm/github/corvus-dotnet/Corvus.Leasing/rule/30e1b40b-b27d-4631-b38d-3172426593ca?cache=false)](https://imm.endjin.com/api/imm/github/corvus-dotnet/Corvus.Leasing/rule/30e1b40b-b27d-4631-b38d-3172426593ca?cache=false)

[![License](https://imm.endjin.com/api/imm/github/corvus-dotnet/Corvus.Leasing/rule/d96b5bdc-62c7-47b6-bcc4-de31127c08b7?cache=false)](https://imm.endjin.com/api/imm/github/corvus-dotnet/Corvus.Leasing/rule/d96b5bdc-62c7-47b6-bcc4-de31127c08b7?cache=false)

[![Production Use](https://imm.endjin.com/api/imm/github/corvus-dotnet/Corvus.Leasing/rule/87ee2c3e-b17a-4939-b969-2c9c034d05d7?cache=false)](https://imm.endjin.com/api/imm/github/corvus-dotnet/Corvus.Leasing/rule/87ee2c3e-b17a-4939-b969-2c9c034d05d7?cache=false)

[![Insights](https://imm.endjin.com/api/imm/github/corvus-dotnet/Corvus.Leasing/rule/71a02488-2dc9-4d25-94fa-8c2346169f8b?cache=false)](https://imm.endjin.com/api/imm/github/corvus-dotnet/Corvus.Leasing/rule/71a02488-2dc9-4d25-94fa-8c2346169f8b?cache=false)

[![Packaging](https://imm.endjin.com/api/imm/github/corvus-dotnet/Corvus.Leasing/rule/547fd9f5-9caf-449f-82d9-4fba9e7ce13a?cache=false)](https://imm.endjin.com/api/imm/github/corvus-dotnet/Corvus.Leasing/rule/547fd9f5-9caf-449f-82d9-4fba9e7ce13a?cache=false)

[![Deployment](https://imm.endjin.com/api/imm/github/corvus-dotnet/Corvus.Leasing/rule/edea4593-d2dd-485b-bc1b-aaaf18f098f9?cache=false)](https://imm.endjin.com/api/imm/github/corvus-dotnet/Corvus.Leasing/rule/edea4593-d2dd-485b-bc1b-aaaf18f098f9?cache=false)

[![OpenChain](https://imm.endjin.com/api/imm/github/corvus-dotnet/Corvus.Leasing/rule/66efac1a-662c-40cf-b4ec-8b34c29e9fd7?cache=false)](https://imm.endjin.com/api/imm/github/corvus-dotnet/Corvus.Leasing/rule/66efac1a-662c-40cf-b4ec-8b34c29e9fd7?cache=false)


