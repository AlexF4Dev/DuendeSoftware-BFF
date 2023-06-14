// Copyright (c) Duende Software. All rights reserved.
// See LICENSE in the project root for license information.

using Duende.Bff;
using Microsoft.Extensions.Options;
using Duende.AccessTokenManagement.OpenIdConnect;

namespace Microsoft.AspNetCore.Builder;

public class ConfigureUserTokenManagmentOptions : IConfigureOptions<UserTokenManagementOptions>
{
    private readonly BffOptions _bffOptions;

    public ConfigureUserTokenManagmentOptions(IOptions<BffOptions> bffOptions)
    {
        _bffOptions = bffOptions.Value;
    }
    public void Configure(UserTokenManagementOptions options)
    {
        options.DPoPJsonWebKey = _bffOptions.DPoPJsonWebKey;
    }
}