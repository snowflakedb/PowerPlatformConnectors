// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

namespace SnowflakeV2CoreLogic.Models
{
    /// <summary>
    /// Enum representing types of authentications
    /// </summary>
    public enum AuthenticationType
    {
        /// <summary>
        /// Microsoft Entra ID (Service Principal - Client Credentials)
        /// </summary>
        AAD,

        /// <summary>
        /// User Delegated (Requires manual OAuth app configuration)
        /// </summary>
        AADUserDelegated,

        /// <summary>
        /// OAuth Same Tenant (Simplified user authentication for same-tenant scenarios)
        /// </summary>
        OAuthSameTenant,
    }
}
