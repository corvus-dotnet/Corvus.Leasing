// <copyright file="LeaseSteps.cs" company="Endjin Limited">
// Copyright (c) Endjin Limited. All rights reserved.
// </copyright>

#pragma warning disable

namespace Corvus.Leasing.Azure.Specs.Steps
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using Corvus.Leasing;
    using Corvus.Leasing.Internal;
    using Corvus.Testing.SpecFlow;
    using Microsoft.Extensions.DependencyInjection;
    using NUnit.Framework;
    using TechTalk.SpecFlow;

    [Binding]
    public class LeaseSteps
    {
        private readonly FeatureContext featureContext;
        private readonly ScenarioContext scenarioContext;

        public LeaseSteps(FeatureContext featureContext, ScenarioContext scenarioContext)
        {
            this.featureContext = featureContext;
            this.scenarioContext = scenarioContext;
        }

        [Given(@"Actor A has already acquired a lease for an operation with the same name")]
        public async Task GivenActorAHasAlreadyAcquiredALeaseForAnOperationWithTheSameName()
        {
            var otherPolicy = this.scenarioContext.Get<LeasePolicy>();
            var policy = new LeasePolicy { Name = otherPolicy.Name, Duration = TimeSpan.FromSeconds(15) };
            var leaseProvider = ContainerBindings.GetServiceProvider(this.featureContext).GetRequiredService<ILeaseProvider>();
            var lease = await leaseProvider.AcquireAsync(policy);
            this.scenarioContext.Set(lease, "ActorALease");
        }

        [Given(@"I am actor B trying to perform an operation called ""(.*)""")]
        public void GivenIAmActorBTryingToPerformAnOperationCalled(string leaseName)
        {
            leaseName += $"_{Guid.NewGuid()}";
            var policy = new LeasePolicy { Name = leaseName };
            this.scenarioContext.Set(policy);
        }

        [Given(@"I am the only actor trying to perform an operation called ""(.*)""")]
        public void GivenIAmTheOnlyActorTryingToPerformAnOperationCalled(string leaseName)
        {
            if (leaseName.EndsWith("."))
            {
                leaseName = leaseName.Remove(leaseName.Length - 1, 1);
                leaseName += $"_{Guid.NewGuid()}.";
            }
            else
            {
                leaseName += $"_{Guid.NewGuid()}";
            }

            var policy = new LeasePolicy { Name = leaseName };
            this.scenarioContext.Set(policy);
        }

        [Given(@"I have already acquired the lease")]
        public async Task GivenIHaveAlreadyAcquiredTheLease()
        {
            var leaseProvider = ContainerBindings.GetServiceProvider(this.featureContext).GetRequiredService<ILeaseProvider>();
            var policy = this.scenarioContext.Get<LeasePolicy>();
            var lease = await leaseProvider.AcquireAsync(policy);
            this.scenarioContext.Set(lease);
        }

        [Given(@"I want to acquire a lease for (.*) seconds")]
        public void GivenIWantToAcquireALeaseForSeconds(int leaseDurationInSeconds)
        {
            var policy = this.scenarioContext.Get<LeasePolicy>();
            policy.Duration = TimeSpan.FromSeconds(leaseDurationInSeconds);
            this.scenarioContext.Set(policy);
        }

        [Given(@"the lease name in the policy is (.*)")]
        public void GivenTheLeaseNameInThePolicyIs(string leaseName)
        {
            var policy = this.scenarioContext.Get<LeasePolicy>();
            policy.Name = leaseName;
            this.scenarioContext.Set(policy);
        }

        [Given(@"I tokenize the lease")]
        public void GivenITokenizeTheLease()
        {
            var leaseProvider = ContainerBindings.GetServiceProvider(this.featureContext).GetRequiredService<ILeaseProvider>();
            var lease = this.scenarioContext.Get<Lease>();
            string leaseToken = leaseProvider.ToLeaseToken(lease);
            this.scenarioContext.Set<string>(leaseToken, "LeaseToken");
        }

        [Given(@"I detokenize the lease")]
        public void GivenIDetokenizeTheLease()
        {
            var leaseToken = this.scenarioContext.Get<string>("LeaseToken");
            var leaseProvider = ContainerBindings.GetServiceProvider(this.featureContext).GetRequiredService<ILeaseProvider>();
            Lease lease = leaseProvider.FromLeaseToken(leaseToken);
            this.scenarioContext.Set<Lease>(lease);
        }


        [Given(@"the lease has expired")]
        public async Task GivenTheLeaseHasExpired()
        {
            var leaseProvider = ContainerBindings.GetServiceProvider(this.featureContext).GetRequiredService<ILeaseProvider>();
            var policy = this.scenarioContext.Get<LeasePolicy>();
            var lease = await leaseProvider.AcquireAsync(policy);
            Thread.Sleep(policy.Duration.Value.Add(TimeSpan.FromSeconds(2)));
            this.scenarioContext.Set(lease);
        }

        [Then(@"I should not have acquired the lease")]
        public void ThenIShouldNotHaveAcquiredTheLease()
        {
            var lease = this.scenarioContext.Get<Lease>();
            Assert.IsNull(lease);
        }

        [Then(@"it should not throw an exception")]
        public void ThenItShouldNotThrowAnException()
        {
            Exception exception;
            AggregateException aggregateException;
            Assert.False(this.scenarioContext.TryGetValue("Exception", out exception));
            Assert.False(this.scenarioContext.TryGetValue("AggregateException", out aggregateException));
        }

        [Then(@"it should retain the lease for (.*) seconds")]
        public void ThenItShouldRetainTheLeaseForSeconds(int leaseDurationInSeconds)
        {
            var lease = this.scenarioContext.Get<Lease>();

            Assert.True(lease.HasLease);
            Thread.Sleep(TimeSpan.FromSeconds(leaseDurationInSeconds - 5));
            Assert.True(lease.HasLease);
        }

        [Then(@"the lease acquired date should not be set")]
        public void ThenTheLeaseAcquiredDateShouldNotBeSet()
        {
            var lease = this.scenarioContext.Get<Lease>();
            Assert.Null(lease.LastAcquired);
        }

        [Then(@"the lease expiration date should be null")]
        public void ThenTheLeaseExpirationDateShouldBeNull()
        {
            var lease = this.scenarioContext.Get<Lease>();
            Assert.Null(lease.Expires);
        }

        [Then(@"the lease expires date should not be set")]
        public void ThenTheLeaseExpiresDateShouldNotBeSet()
        {
            var lease = this.scenarioContext.Get<Lease>();
            Assert.Null(lease.Expires);
        }

        [Then(@"the lease last acquired date should be null")]
        public void ThenTheLeaseLastAcquiredDateShouldBeNull()
        {
            var lease = this.scenarioContext.Get<Lease>();
            Assert.Null(lease.LastAcquired);
        }

        [Then(@"the lease should be expired")]
        public void ThenTheLeaseShouldBeExpired()
        {
            var lease = this.scenarioContext.Get<Lease>();
            Assert.False(lease.HasLease);
        }

        [Then(@"the lease should be expired after (.*) seconds")]
        public void ThenTheLeaseShouldBeExpiredAfterSeconds(int leaseDurationInSeconds)
        {
            var lease = this.scenarioContext.Get<Lease>();
            Thread.Sleep(TimeSpan.FromSeconds(5));
            Assert.False(lease.HasLease);
        }

        [Then(@"the lease should expire in the future")]
        public void ThenTheLeaseShouldExpireInTheFuture()
        {
            var lease = this.scenarioContext.Get<Lease>();
            Assert.Greater(lease.Expires, DateTimeOffset.UtcNow);
        }

        [Then(@"the lease should no longer be acquired")]
        public void ThenTheLeaseShouldNoLongerBeAcquired()
        {
            var lease = this.scenarioContext.Get<Lease>();
            Assert.IsTrue(lease.Released);
        }

        [When(@"I acquire the lease")]
        public async Task WhenIAcquireTheLease()
        {
            var policy = this.scenarioContext.Get<LeasePolicy>();

            Lease lease = null;
            var leaseProvider = ContainerBindings.GetServiceProvider(this.featureContext).GetRequiredService<ILeaseProvider>();
           
            try
            {
                if (!this.scenarioContext.TryGetValue(out lease))
                {
                    lease = await leaseProvider.AcquireAsync(policy);
                    this.scenarioContext.Set(lease);
                }
                else
                {
                   lease = await leaseProvider.AcquireAsync(policy);
                }
            }
            catch (AggregateException ex)
            {
                this.scenarioContext.Add("AggregateException", ex);
            }
            catch (Exception ex)
            {
                this.scenarioContext.Add("Exception", ex);
            }

            this.scenarioContext.Set(lease);
        }

        [When(@"I dispose the lease")]
        public async Task WhenIDisposeTheLease()
        {
            var lease = this.scenarioContext.Get<Lease>();
            try
            {
                await lease.ReleaseAsync();
            }
            catch (AggregateException ex)
            {
                this.scenarioContext.Add("AggregateException", ex);
            }
            catch (Exception ex)
            {
                this.scenarioContext.Add("Exception", ex);
            }

            this.scenarioContext.Set(lease);
        }

        [When(@"I release the lease")]
        public async Task WhenIReleaseTheLease()
        {
            var lease = this.scenarioContext.Get<Lease>();
            try
            {
                await lease.ReleaseAsync();
            }
            catch (AggregateException ex)
            {
                this.scenarioContext.Add("AggregateException", ex);
            }
            catch (Exception ex)
            {
                this.scenarioContext.Add("Exception", ex);
            }

            this.scenarioContext.Set(lease);
        }

        [When(@"I renew the lease")]
        public async Task WhenIRenewTheLease()
        {
            var lease = this.scenarioContext.Get<Lease>();
            try
            {
                await lease.ExtendAsync();
            }
            catch (AggregateException ex)
            {
                this.scenarioContext.Add("AggregateException", ex);
            }
            catch (Exception ex)
            {
                this.scenarioContext.Add("Exception", ex);
            }

            this.scenarioContext.Set(lease);
        }

        [Given(@"I create a token for a lease with an InMemoryLeaseProvider")]
        public async Task GivenICreateATokenForALeaseWithAnInMemoryLeaseProvider()
        {
            var leaseProvider = new InMemoryLeaseProvider();
            var policy = this.scenarioContext.Get<LeasePolicy>();
            var lease = await leaseProvider.AcquireAsync(policy);
            string leaseToken = leaseProvider.ToLeaseToken(lease);
            this.scenarioContext.Set(lease);
            this.scenarioContext.Set(leaseToken, "LeaseToken");
        }

        [When(@"I ask an AzureLeaseProvider to detokenize the token")]
        public void WhenIAskAnAzureLeaseProviderToDetokenizeTheToken()
        {
            string token = this.scenarioContext.Get<string>("LeaseToken");
            var lp = ContainerBindings.GetServiceProvider(this.featureContext).GetRequiredService<ILeaseProvider>();
            try
            {
                this.scenarioContext.Set(lp.FromLeaseToken(token));
            }
            catch (AggregateException ex)
            {
                this.scenarioContext.Add("AggregateException", ex);
            }
            catch (Exception ex)
            {
                this.scenarioContext.Add("Exception", ex);
            }
        }

        [Given(@"I create a lease with an InMemoryLeaseProvider")]
        public async Task GivenICreateALeaseWithAnInMemoryLeaseProvider()
        {
            var leaseProvider = new InMemoryLeaseProvider();
            var policy = this.scenarioContext.Get<LeasePolicy>();
            var lease = await leaseProvider.AcquireAsync(policy);
            this.scenarioContext.Set(lease);
        }

        [When(@"I ask an AzureLeaseProvider to tokenize the token")]
        public void WhenIAskAnAzureLeaseProviderToTokenizeTheToken()
        {
            var lp = ContainerBindings.GetServiceProvider(this.featureContext).GetRequiredService<ILeaseProvider>();
            Lease lease = this.scenarioContext.Get<Lease>();
            try
            {
                this.scenarioContext.Set(lp.ToLeaseToken(lease), "LeaseToken");
            }
            catch (AggregateException ex)
            {
                this.scenarioContext.Add("AggregateException", ex);
            }
            catch (Exception ex)
            {
                this.scenarioContext.Add("Exception", ex);
            }
        }


        [Given(@"I create a token for a lease with an AzureLeaseProvider")]
        public async Task GivenICreateATokenForALeaseWithAnAzureLeaseProvider()
        {
            var leaseProvider = ContainerBindings.GetServiceProvider(this.featureContext).GetRequiredService<ILeaseProvider>();
            var policy = this.scenarioContext.Get<LeasePolicy>();
            var lease = await leaseProvider.AcquireAsync(policy);
            string leaseToken = leaseProvider.ToLeaseToken(lease);
            this.scenarioContext.Set(lease);
            this.scenarioContext.Set(leaseToken, "LeaseToken");
        }

        [Given(@"I create a lease with an AzureLeaseProvider")]
        public async Task GivenICreateALeaseWithAnAzureLeaseProvider()
        {
            var leaseProvider = ContainerBindings.GetServiceProvider(this.featureContext).GetRequiredService<ILeaseProvider>();
            var policy = this.scenarioContext.Get<LeasePolicy>();
            var lease = await leaseProvider.AcquireAsync(policy);
            this.scenarioContext.Set(lease);
        }

        [When(@"I ask an InMemoryLeaseProvider to detokenize the token")]
        public void WhenIAskAnInMemoryLeaseProviderToDetokenizeTheToken()
        {
            string token = this.scenarioContext.Get<string>("LeaseToken");
            var lp = new InMemoryLeaseProvider();
            try
            {
                this.scenarioContext.Set(lp.FromLeaseToken(token));
            }
            catch (AggregateException ex)
            {
                this.scenarioContext.Add("AggregateException", ex);
            }
            catch (Exception ex)
            {
                this.scenarioContext.Add("Exception", ex);
            }
        }

        [When(@"I ask an InMemoryLeaseProvider to tokenize the token")]
        public void WhenIAskAnInMemoryLeaseProviderToTokenizeTheToken()
        {
            var lp = new InMemoryLeaseProvider();
            Lease lease = this.scenarioContext.Get<Lease>();
            try
            {
                this.scenarioContext.Set(lp.ToLeaseToken(lease), "LeaseToken");
            }
            catch (AggregateException ex)
            {
                this.scenarioContext.Add("AggregateException", ex);
            }
            catch (Exception ex)
            {
                this.scenarioContext.Add("Exception", ex);
            }
        }
    }
}

#pragma warning restore