﻿#region License
// Copyright 2020 HDVP (https://github.com/skrysmanski/hdvp)
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//     http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
#endregion

using System;

using AppMotor.Core.Utils;

using JetBrains.Annotations;

namespace HDVP
{
    /// <summary>
    /// This class represents the "server side" in HDVP. It generates the verification codes
    /// to be displayed to the user (see <see cref="GetVerificationCode"/>). It also manages
    /// the verification code's supplemental data (e.g. the hash) as well as its lifetime
    /// (see <see cref="VerificationCodeValidUntil"/>).
    /// </summary>
    public class HdvpVerificationCodeProvider
    {
        /// <summary>
        /// The default time-to-live for a verification code.
        /// </summary>
        /// <seealso cref="VerificationCodeValidUntil"/>
        [PublicAPI]
        public static readonly TimeSpan DEFAULT_TIME_TO_LIVE = TimeSpan.FromMinutes(1);

        /// <summary>
        /// The default verification code length.
        /// </summary>
        [PublicAPI]
        public static readonly int DEFAULT_CODE_LENGTH = 12;

        /// <summary>
        /// How long the current verification code (as returned by <see cref="GetVerificationCode"/>)
        /// is still valid. As long as this time has not yet been reached, the verification code returned
        /// by <see cref="GetVerificationCode"/> does not change. Once this time has passed, a new
        /// verification will be created automatically.
        /// </summary>
        [PublicAPI]
        public DateTime VerificationCodeValidUntil { get; private set; }

        private HdvpVerificationCode? m_currentVerificationCode;

        private readonly HdvpVerifiableData m_data;

        private readonly int m_verificationCodeLength;

        private readonly TimeSpan m_verificationCodeTimeToLive;

        private readonly IDateTimeProvider m_dateTimeProvider;

        /// <summary>
        /// Constructor. Uses <see cref="DEFAULT_CODE_LENGTH"/> as code length and <see cref="DEFAULT_TIME_TO_LIVE"/>
        /// as time-to-live for verification codes.
        /// </summary>
        /// <param name="data">The binary data for which verification codes should be created.</param>
        /// <param name="dateTimeProvider">A <see cref="IDateTimeProvider"/>; mainly for unit testing
        /// purposes. If <c>null</c>, <see cref="DefaultDateTimeProvider"/> will be used.</param>
        public HdvpVerificationCodeProvider(
                HdvpVerifiableData data,
                IDateTimeProvider? dateTimeProvider = null
            )
                : this(data, verificationCodeLength: DEFAULT_CODE_LENGTH, DEFAULT_TIME_TO_LIVE, dateTimeProvider)
        {
        }

        /// <summary>
        /// Constructor. Uses <see cref="DEFAULT_TIME_TO_LIVE"/> as time-to-live for verification codes.
        /// </summary>
        /// <param name="data">The binary data for which verification codes should be created.</param>
        /// <param name="verificationCodeLength">The expected/desired length (in characters) of the
        /// verification codes generated by this instance.</param>
        /// <param name="dateTimeProvider">A <see cref="IDateTimeProvider"/>; mainly for unit testing
        /// purposes. If <c>null</c>, <see cref="DefaultDateTimeProvider"/> will be used.</param>
        public HdvpVerificationCodeProvider(
                HdvpVerifiableData data,
                int verificationCodeLength,
                IDateTimeProvider? dateTimeProvider = null
            )
                : this(data, verificationCodeLength, DEFAULT_TIME_TO_LIVE, dateTimeProvider)
        {
        }

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="data">The binary data for which verification codes should be created.</param>
        /// <param name="verificationCodeLength">The expected/desired length (in characters) of the
        /// verification codes generated by this instance.</param>
        /// <param name="verificationCodeTimeToLive">The time-to-live for each generated verification
        /// code. Shorter TTLs make the codes more secure; longer TTLs make the codes less secure.
        /// If possible, use <see cref="DEFAULT_TIME_TO_LIVE"/> (or shorter) for this value. Using a
        /// longer TTL is not recommended.</param>
        /// <param name="dateTimeProvider">A <see cref="IDateTimeProvider"/>; mainly for unit testing
        /// purposes. If <c>null</c>, <see cref="DefaultDateTimeProvider"/> will be used.</param>
        public HdvpVerificationCodeProvider(
                HdvpVerifiableData data,
                int verificationCodeLength,
                TimeSpan verificationCodeTimeToLive,
                IDateTimeProvider? dateTimeProvider = null
            )
        {
            Validate.Argument.IsNotNull(data, nameof(data));

            this.m_data = data;
            this.m_verificationCodeLength = verificationCodeLength;
            this.m_verificationCodeTimeToLive = verificationCodeTimeToLive;
            this.m_dateTimeProvider = dateTimeProvider ?? DefaultDateTimeProvider.Instance;

            this.VerificationCodeValidUntil = this.m_dateTimeProvider.UtcNow + verificationCodeTimeToLive;
        }

        /// <summary>
        /// Returns the current verification code. If the lifetime of the last verification code has expired,
        /// generates a new verification code automatically and updates <see cref="VerificationCodeValidUntil"/>.
        ///
        /// <para>Note: Whenever a new verification code needs to be generated, this method takes a "long" time.
        /// On a modern desktop system it roughly takes 0.5 seconds - on a Raspberry Pi 4 it roughly takes 3
        /// seconds. So you should not execute this method on the UI thread.</para>
        /// </summary>
        [PublicAPI]
        public HdvpVerificationCode GetVerificationCode()
        {
            if (this.m_currentVerificationCode != null && this.m_dateTimeProvider.UtcNow < this.VerificationCodeValidUntil)
            {
                return this.m_currentVerificationCode;
            }

            var salt = HdvpSalt.CreateNewSalt();
            this.m_currentVerificationCode = HdvpVerificationCode.Create(this.m_data, salt, this.m_verificationCodeLength);
            this.VerificationCodeValidUntil = this.m_dateTimeProvider.UtcNow + this.m_verificationCodeTimeToLive;

            return this.m_currentVerificationCode;
        }
    }
}
