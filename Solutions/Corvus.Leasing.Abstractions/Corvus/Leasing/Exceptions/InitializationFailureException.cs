// <copyright file="InitializationFailureException.cs" company="Endjin Limited">
// Copyright (c) Endjin Limited. All rights reserved.
// </copyright>

namespace Corvus.Leasing
{
    using System;
    using System.Runtime.Serialization;

    /// <summary>
    /// An exception raised when leasing fails to initialize.
    /// </summary>
    public class InitializationFailureException : Exception
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="InitializationFailureException"/> class.
        /// </summary>
        public InitializationFailureException()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="InitializationFailureException"/> class.
        /// </summary>
        /// <param name="message">The exception message.</param>
        public InitializationFailureException(string message)
            : base(message)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="InitializationFailureException"/> class.
        /// </summary>
        /// <param name="message">The exception message.</param>
        /// <param name="innerException">The inner exception.</param>
        public InitializationFailureException(string message, Exception innerException)
            : base(message, innerException)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="InitializationFailureException"/> class.
        /// </summary>
        /// <param name="info">The serialization information.</param>
        /// <param name="context">The serialization context.</param>
        protected InitializationFailureException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }
}
