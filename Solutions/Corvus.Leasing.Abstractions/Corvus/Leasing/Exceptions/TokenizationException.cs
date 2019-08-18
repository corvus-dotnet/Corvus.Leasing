// <copyright file="TokenizationException.cs" company="Endjin Limited">
// Copyright (c) Endjin Limited. All rights reserved.
// </copyright>

namespace Corvus.Leasing
{
    using System;
    using System.Runtime.Serialization;

    /// <summary>
    /// An exception raised when a lease token fails to encode/decode.
    /// </summary>
    public class TokenizationException : Exception
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="TokenizationException"/> class.
        /// </summary>
        public TokenizationException()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="TokenizationException"/> class.
        /// </summary>
        /// <param name="message">The exception message.</param>
        public TokenizationException(string message)
            : base(message)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="TokenizationException"/> class.
        /// </summary>
        /// <param name="message">The exception message.</param>
        /// <param name="innerException">The inner exception.</param>
        public TokenizationException(string message, Exception innerException)
            : base(message, innerException)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="TokenizationException"/> class.
        /// </summary>
        /// <param name="info">The serialization information.</param>
        /// <param name="context">The serialization context.</param>
        protected TokenizationException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }
}
