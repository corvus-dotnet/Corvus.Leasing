// <copyright file="AzureLease.cs" company="Endjin Limited">
// Copyright (c) Endjin Limited. All rights reserved.
// </copyright>

namespace Corvus.Leasing.Internal
{
    using System;
    using System.Text;
    using Corvus.Extensions;

    /// <summary>
    /// An implementation of a lease for Azure blob leasing.
    /// </summary>
    public class AzureLease : Lease
    {
        private const string NullString = "<no value>";
        private const string LeaseTokenContentType = "application/vnd.endjin.azureleaseprovider.leasetoken";

        private DateTimeOffset? lastAcquired;

        /// <summary>
        /// Initializes a new instance of the <see cref="AzureLease"/> class.
        /// </summary>
        /// <param name="leaseProvider">The lease provider.</param>
        /// <param name="leasePolicy">The lease policy.</param>
        /// <param name="id">The id for the lease.</param>
        internal AzureLease(AzureLeaseProvider leaseProvider, LeasePolicy leasePolicy, string id)
            : base(leaseProvider, leasePolicy, id)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="AzureLease"/> class.
        /// </summary>
        /// <param name="leaseProvider">The lease provider.</param>
        /// <param name="leasePolicy">The lease policy.</param>
        /// <param name="id">The id for the lease.</param>
        /// <param name="lastAcquired">The time at which the lease was last acquired.</param>
        internal AzureLease(AzureLeaseProvider leaseProvider, LeasePolicy leasePolicy, string id, DateTimeOffset? lastAcquired)
            : base(leaseProvider, leasePolicy, id)
        {
            this.lastAcquired = lastAcquired;
        }

        /// <inheritdoc/>
        public override DateTimeOffset? LastAcquired
        {
            get
            {
                return this.lastAcquired;
            }
        }

        /// <summary>
        /// Creates an <see cref="AzureLease"/> from a lease token.
        /// </summary>
        /// <param name="leaseProvider">The lease provider.</param>
        /// <param name="token">The lease token from which to construct the lease.</param>
        /// <returns>An instance of an in-memory lease configured from the lease token.</returns>
        internal static AzureLease FromToken(AzureLeaseProvider leaseProvider, string token)
        {
            if (leaseProvider is null)
            {
                throw new ArgumentNullException(nameof(leaseProvider));
            }

            if (token is null)
            {
                throw new ArgumentNullException(nameof(token));
            }

            string tokenizedLease = token.Base64UrlDecode();
            string[] lines = tokenizedLease.Split(new[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries);
            if (lines[0] != LeaseTokenContentType)
            {
                throw new TokenizationException();
            }

            string id = lines[1];
            DateTimeOffset? lastAcquired = lines[2] != NullString ? (DateTimeOffset?)DateTimeOffset.FromUnixTimeMilliseconds(long.Parse(lines[2])) : null;
            var leasePolicy = new LeasePolicy
            {
                ActorName = lines[3],
                Duration = lines[4] != NullString ? (TimeSpan?)TimeSpan.FromMilliseconds(long.Parse(lines[4])) : null,
                Name = lines[5],
            };

            return new AzureLease(leaseProvider, leasePolicy, id, lastAcquired);
        }

        /// <summary>
        /// Create a token for this lease.
        /// </summary>
        /// <returns>The token for the lease.</returns>
        internal string GetToken()
        {
            var builder = new StringBuilder();
            builder.AppendLine(LeaseTokenContentType);
            builder.AppendLine(this.Id);
            builder.AppendLine(this.LastAcquired.HasValue ? this.LastAcquired.Value.ToUnixTimeMilliseconds().ToString() : NullString);
            builder.AppendLine(string.IsNullOrEmpty(this.LeasePolicy.ActorName) ? NullString : this.LeasePolicy.ActorName);
            builder.AppendLine(this.LeasePolicy.Duration.HasValue ? this.LeasePolicy.Duration.Value.TotalMilliseconds.ToString() : NullString);
            builder.AppendLine(this.LeasePolicy.Name);
            return builder.ToString().Base64UrlEncode();
        }

        /// <summary>
        /// Clears the id.
        /// </summary>
        internal void ClearId()
        {
            this.Id = null;
        }

        /// <summary>
        /// Sets the last acquired time.
        /// </summary>
        /// <param name="lastAcquired">The last time the lease was acquired, or null if the lease has been released.</param>
        internal void SetLastAcquired(DateTimeOffset? lastAcquired)
        {
            this.lastAcquired = lastAcquired;
        }
    }
}
