// <copyright file="IResultStorageService.cs" company="HemSoft">
// Copyright © 2025 HemSoft
// </copyright>

namespace HemSoft.PowerAI.Common.Services;

using System.Text.Json;

/// <summary>
/// Defines operations for storing and retrieving large task result payloads.
/// Used to avoid sending large data through pub/sub channels which have size limitations.
/// </summary>
public interface IResultStorageService
{
    /// <summary>
    /// Stores a result payload with default expiration and returns a storage key.
    /// </summary>
    /// <param name="taskId">The task ID associated with this result.</param>
    /// <param name="data">The JSON data to store.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The storage key that can be used to retrieve the data.</returns>
    Task<string> StoreResultAsync(
        string taskId,
        JsonDocument data,
        CancellationToken cancellationToken);

    /// <summary>
    /// Stores a result payload with custom expiration and returns a storage key.
    /// </summary>
    /// <param name="taskId">The task ID associated with this result.</param>
    /// <param name="data">The JSON data to store.</param>
    /// <param name="expiry">Expiration time for the stored data.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The storage key that can be used to retrieve the data.</returns>
    Task<string> StoreResultAsync(
        string taskId,
        JsonDocument data,
        TimeSpan expiry,
        CancellationToken cancellationToken);

    /// <summary>
    /// Retrieves a stored result payload by its storage key.
    /// </summary>
    /// <param name="storageKey">The storage key returned from <see cref="StoreResultAsync(string, JsonDocument, CancellationToken)"/>.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The stored JSON data, or null if not found or expired.</returns>
    Task<JsonDocument?> RetrieveResultAsync(
        string storageKey,
        CancellationToken cancellationToken);

    /// <summary>
    /// Deletes a stored result payload.
    /// </summary>
    /// <param name="storageKey">The storage key to delete.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if the key was deleted, false if it didn't exist.</returns>
    Task<bool> DeleteResultAsync(
        string storageKey,
        CancellationToken cancellationToken);
}
