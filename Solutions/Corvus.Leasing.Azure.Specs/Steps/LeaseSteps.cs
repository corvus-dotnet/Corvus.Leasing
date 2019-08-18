// <copyright file="LeaseSteps.cs" company="Endjin">
// Copyright (c) Endjin. All rights reserved.
// </copyright>

#pragma warning disable

namespace Endjin.Leasing.Azure.Specs.Steps
{
    using System;
    using System.Threading;
    using Endjin.Composition;
    using Endjin.Leasing.Internal;
    using Microsoft.Extensions.DependencyInjection;
    using NUnit.Framework;
    using TechTalk.SpecFlow;

    [Binding]
    public class LeaseSteps
    {
        [Given(@"Actor A has already acquired a lease for an operation with the same name")]
        public void GivenActorAHasAlreadyAcquiredALeaseForAnOperationWithTheSameName()
        {
            var otherPolicy = ScenarioContext.Current.Get<LeasePolicy>();
            var policy = new LeasePolicy { Name = otherPolicy.Name, Duration = TimeSpan.FromSeconds(15) };
            var leaseProvider = ServiceRoot.ServiceProvider.GetRequiredService<ILeaseProvider>();
            var lease = leaseProvider.AcquireAsync(policy).Result;
            ScenarioContext.Current.Set(lease, "ActorALease");
        }

        [Given(@"I am actor B trying to perform an operation called ""(.*)""")]
        public void GivenIAmActorBTryingToPerformAnOperationCalled(string leaseName)
        {
            leaseName += $"_{Guid.NewGuid()}";
            var policy = new LeasePolicy { Name = leaseName };
            ScenarioContext.Current.Set(policy);
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
            ScenarioContext.Current.Set(policy);
        }

        [Given(@"I have already acquired the lease")]
        public void GivenIHaveAlreadyAcquiredTheLease()
        {
            var leaseProvider = ServiceRoot.ServiceProvider.GetRequiredService<ILeaseProvider>();
            var policy = ScenarioContext.Current.Get<LeasePolicy>();
            var lease = leaseProvider.AcquireAsync(policy).Result;
            ScenarioContext.Current.Set(lease);
        }

        [Given(@"I want to acquire a lease for (.*) seconds")]
        public void GivenIWantToAcquireALeaseForSeconds(int leaseDurationInSeconds)
        {
            var policy = ScenarioContext.Current.Get<LeasePolicy>();
            policy.Duration = TimeSpan.FromSeconds(leaseDurationInSeconds);
            ScenarioContext.Current.Set(policy);
        }

        [Given(@"the lease name in the policy is (.*)")]
        public void GivenTheLeaseNameInThePolicyIs(string leaseName)
        {
            var policy = ScenarioContext.Current.Get<LeasePolicy>();
            policy.Name = leaseName;
            ScenarioContext.Current.Set(policy);
        }

        [Given(@"I tokenize the lease")]
        public void GivenITokenizeTheLease()
        {
            var leaseProvider = ServiceRoot.ServiceProvider.GetRequiredService<ILeaseProvider>();
            var lease = ScenarioContext.Current.Get<Lease>();
            string leaseToken = leaseProvider.ToLeaseToken(lease);
            ScenarioContext.Current.Set<string>(leaseToken, "LeaseToken");
        }

        [Given(@"I detokenize the lease")]
        public void GivenIDetokenizeTheLease()
        {
            var leaseToken = ScenarioContext.Current.Get<string>("LeaseToken");
            var leaseProvider = ServiceRoot.ServiceProvider.GetRequiredService<ILeaseProvider>();
            Lease lease = leaseProvider.FromLeaseToken(leaseToken);
            ScenarioContext.Current.Set<Lease>(lease);
        }


        [Given(@"the lease has expired")]
        public void GivenTheLeaseHasExpired()
        {
            var leaseProvider = ServiceRoot.ServiceProvider.GetRequiredService<ILeaseProvider>();
            var policy = ScenarioContext.Current.Get<LeasePolicy>();
            var lease = leaseProvider.AcquireAsync(policy).Result;
            Thread.Sleep(policy.Duration.Value.Add(TimeSpan.FromSeconds(2)));
            ScenarioContext.Current.Set(lease);
        }

        [Then(@"I should not have acquired the lease")]
        public void ThenIShouldNotHaveAcquiredTheLease()
        {
            var lease = ScenarioContext.Current.Get<Lease>();
            Assert.IsNull(lease);
        }

        [Then(@"it should not throw an exception")]
        public void ThenItShouldNotThrowAnException()
        {
            Exception exception;
            AggregateException aggregateException;
            Assert.False(ScenarioContext.Current.TryGetValue("Exception", out exception));
            Assert.False(ScenarioContext.Current.TryGetValue("AggregateException", out aggregateException));
        }

        [Then(@"it should retain the lease for (.*) seconds")]
        public void ThenItShouldRetainTheLeaseForSeconds(int leaseDurationInSeconds)
        {
            var lease = ScenarioContext.Current.Get<Lease>();

            Assert.True(lease.HasLease);
            Thread.Sleep(TimeSpan.FromSeconds(leaseDurationInSeconds - 5));
            Assert.True(lease.HasLease);
        }

        [Then(@"the lease acquired date should not be set")]
        public void ThenTheLeaseAcquiredDateShouldNotBeSet()
        {
            var lease = ScenarioContext.Current.Get<Lease>();
            Assert.Null(lease.LastAcquired);
        }

        [Then(@"the lease expiration date should be null")]
        public void ThenTheLeaseExpirationDateShouldBeNull()
        {
            var lease = ScenarioContext.Current.Get<Lease>();
            Assert.Null(lease.Expires);
        }

        [Then(@"the lease expires date should not be set")]
        public void ThenTheLeaseExpiresDateShouldNotBeSet()
        {
            var lease = ScenarioContext.Current.Get<Lease>();
            Assert.Null(lease.Expires);
        }

        [Then(@"the lease ID should not be set")]
        public void ThenTheLeaseIDShouldNotBeSet()
        {
            var lease = ScenarioContext.Current.Get<Lease>();
            Assert.Null(lease.Id);
        }

        [Then(@"the lease last acquired date should be null")]
        public void ThenTheLeaseLastAcquiredDateShouldBeNull()
        {
            var lease = ScenarioContext.Current.Get<Lease>();
            Assert.Null(lease.LastAcquired);
        }

        [Then(@"the lease should be expired")]
        public void ThenTheLeaseShouldBeExpired()
        {
            var lease = ScenarioContext.Current.Get<Lease>();
            Assert.False(lease.HasLease);
        }

        [Then(@"the lease should be expired after (.*) seconds")]
        public void ThenTheLeaseShouldBeExpiredAfterSeconds(int leaseDurationInSeconds)
        {
            var lease = ScenarioContext.Current.Get<Lease>();
            Thread.Sleep(TimeSpan.FromSeconds(5));
            Assert.False(lease.HasLease);
        }

        [Then(@"the lease should expire in the future")]
        public void ThenTheLeaseShouldExpireInTheFuture()
        {
            var lease = ScenarioContext.Current.Get<Lease>();
            Assert.Greater(lease.Expires, DateTimeOffset.UtcNow);
        }

        [Then(@"the lease should no longer be acquired")]
        public void ThenTheLeaseShouldNoLongerBeAcquired()
        {
            var lease = ScenarioContext.Current.Get<Lease>();
            Assert.Null(lease.Id);
        }

        [When(@"I acquire the lease")]
        public void WhenIAcquireTheLease()
        {
            var policy = ScenarioContext.Current.Get<LeasePolicy>();

            Lease lease = null;
            var leaseProvider = ServiceRoot.ServiceProvider.GetRequiredService<ILeaseProvider>();
           

            try
            {
                if (!ScenarioContext.Current.TryGetValue(out lease))
                {
                    lease = leaseProvider.AcquireAsync(policy).Result;
                    ScenarioContext.Current.Set(lease);
                }
                else
                {
                   lease = leaseProvider.AcquireAsync(policy).Result;
                }
            }
            catch (AggregateException ex)
            {
                ScenarioContext.Current.Add("AggregateException", ex);
            }
            catch (Exception ex)
            {
                ScenarioContext.Current.Add("Exception", ex);
            }

            ScenarioContext.Current.Set(lease);
        }

        [When(@"I dispose the lease")]
        public void WhenIDisposeTheLease()
        {
            var lease = ScenarioContext.Current.Get<Lease>();
            try
            {
                lease.ReleaseAsync().Wait();
            }
            catch (AggregateException ex)
            {
                ScenarioContext.Current.Add("AggregateException", ex);
            }
            catch (Exception ex)
            {
                ScenarioContext.Current.Add("Exception", ex);
            }

            ScenarioContext.Current.Set(lease);
        }

        [When(@"I release the lease")]
        public void WhenIReleaseTheLease()
        {
            var lease = ScenarioContext.Current.Get<Lease>();
            try
            {
                lease.ReleaseAsync().Wait();
            }
            catch (AggregateException ex)
            {
                ScenarioContext.Current.Add("AggregateException", ex);
            }
            catch (Exception ex)
            {
                ScenarioContext.Current.Add("Exception", ex);
            }

            ScenarioContext.Current.Set(lease);
        }

        [When(@"I renew the lease")]
        public void WhenIRenewTheLease()
        {
            var lease = ScenarioContext.Current.Get<Lease>();
            try
            {
                lease.ExtendAsync().Wait();
            }
            catch (AggregateException ex)
            {
                ScenarioContext.Current.Add("AggregateException", ex);
            }
            catch (Exception ex)
            {
                ScenarioContext.Current.Add("Exception", ex);
            }

            ScenarioContext.Current.Set(lease);
        }

        [Given(@"I create a token for a lease with an InMemoryLeaseProvider")]
        public void GivenICreateATokenForALeaseWithAnInMemoryLeaseProvider()
        {
            var leaseProvider = new InMemoryLeaseProvider();
            var policy = ScenarioContext.Current.Get<LeasePolicy>();
            var lease = leaseProvider.AcquireAsync(policy).Result;
            string leaseToken = leaseProvider.ToLeaseToken(lease);
            ScenarioContext.Current.Set(lease);
            ScenarioContext.Current.Set(leaseToken, "LeaseToken");
        }

        [When(@"I ask an AzureLeaseProvider to detokenize the token")]
        public void WhenIAskAnAzureLeaseProviderToDetokenizeTheToken()
        {
            string token = ScenarioContext.Current.Get<string>("LeaseToken");
            var lp = ServiceRoot.ServiceProvider.GetRequiredService<ILeaseProvider>();
            try
            {
                ScenarioContext.Current.Set(lp.FromLeaseToken(token));
            }
            catch (AggregateException ex)
            {
                ScenarioContext.Current.Add("AggregateException", ex);
            }
            catch (Exception ex)
            {
                ScenarioContext.Current.Add("Exception", ex);
            }
        }

        [Given(@"I create a lease with an InMemoryLeaseProvider")]
        public void GivenICreateALeaseWithAnInMemoryLeaseProvider()
        {
            var leaseProvider = new InMemoryLeaseProvider();
            var policy = ScenarioContext.Current.Get<LeasePolicy>();
            var lease = leaseProvider.AcquireAsync(policy).Result;
            ScenarioContext.Current.Set(lease);
        }

        [When(@"I ask an AzureLeaseProvider to tokenize the token")]
        public void WhenIAskAnAzureLeaseProviderToTokenizeTheToken()
        {
            var lp = ServiceRoot.ServiceProvider.GetRequiredService<ILeaseProvider>();
            Lease lease = ScenarioContext.Current.Get<Lease>();
            try
            {
                ScenarioContext.Current.Set(lp.ToLeaseToken(lease), "LeaseToken");
            }
            catch (AggregateException ex)
            {
                ScenarioContext.Current.Add("AggregateException", ex);
            }
            catch (Exception ex)
            {
                ScenarioContext.Current.Add("Exception", ex);
            }
        }


        [Given(@"I create a token for a lease with an AzureLeaseProvider")]
        public void GivenICreateATokenForALeaseWithAnAzureLeaseProvider()
        {
            var leaseProvider = ServiceRoot.ServiceProvider.GetRequiredService<ILeaseProvider>();
            var policy = ScenarioContext.Current.Get<LeasePolicy>();
            var lease = leaseProvider.AcquireAsync(policy).Result;
            string leaseToken = leaseProvider.ToLeaseToken(lease);
            ScenarioContext.Current.Set(lease);
            ScenarioContext.Current.Set(leaseToken, "LeaseToken");
        }

        [Given(@"I create a lease with an AzureLeaseProvider")]
        public void GivenICreateALeaseWithAnAzureLeaseProvider()
        {
            var leaseProvider = ServiceRoot.ServiceProvider.GetRequiredService<ILeaseProvider>();
            var policy = ScenarioContext.Current.Get<LeasePolicy>();
            var lease = leaseProvider.AcquireAsync(policy).Result;
            ScenarioContext.Current.Set(lease);
        }

        [When(@"I ask an InMemoryLeaseProvider to detokenize the token")]
        public void WhenIAskAnInMemoryLeaseProviderToDetokenizeTheToken()
        {
            string token = ScenarioContext.Current.Get<string>("LeaseToken");
            var lp = new InMemoryLeaseProvider();
            try
            {
                ScenarioContext.Current.Set(lp.FromLeaseToken(token));
            }
            catch (AggregateException ex)
            {
                ScenarioContext.Current.Add("AggregateException", ex);
            }
            catch (Exception ex)
            {
                ScenarioContext.Current.Add("Exception", ex);
            }
        }

        [When(@"I ask an InMemoryLeaseProvider to tokenize the token")]
        public void WhenIAskAnInMemoryLeaseProviderToTokenizeTheToken()
        {
            var lp = new InMemoryLeaseProvider();
            Lease lease = ScenarioContext.Current.Get<Lease>();
            try
            {
                ScenarioContext.Current.Set(lp.ToLeaseToken(lease), "LeaseToken");
            }
            catch (AggregateException ex)
            {
                ScenarioContext.Current.Add("AggregateException", ex);
            }
            catch (Exception ex)
            {
                ScenarioContext.Current.Add("Exception", ex);
            }
        }
    }
}

#pragma warning restore