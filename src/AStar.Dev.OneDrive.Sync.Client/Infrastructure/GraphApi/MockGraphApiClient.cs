// <copyright file="MockGraphApiClient.cs" company="AStar.Dev">
// Copyright (c) AStar.Dev. All rights reserved.
// </copyright>

namespace AStar.Dev.OneDrive.Sync.Client.Infrastructure.GraphApi;

using System.Threading;
using System.Threading.Tasks;
using AStar.Dev.OneDrive.Sync.Client.Features.Authentication.Models;

/// <summary>
/// Mock implementation of Graph API client for development/testing.
/// TODO: Phase 3 - Replace with Kiota-generated client.
/// </summary>
public class MockGraphApiClient : IGraphApiClient
{
    /// <inheritdoc/>
    public Task<UserProfile> GetUserProfileAsync(AuthToken authToken, CancellationToken cancellationToken)
    {
        // Return mock data for development - use positional constructor parameters
        var mockProfile = new UserProfile(
            Email: "mock.user@example.com",
            AccountId: "mock-account-id-" + Guid.NewGuid().ToString()[..8]);
            
        return Task.FromResult(mockProfile);
    }
}
