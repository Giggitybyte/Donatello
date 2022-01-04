namespace Donatello.Rest;

using System;
using System.Linq;
using System.Net.Http.Headers;

internal sealed class RequestBucket
{
    private int _limit;
    private int _remaining;

    internal RequestBucket(string bucketId, int limit, int remaining, DateTime resetTime)
    {
        _limit = limit;
        _remaining = remaining;

        this.Id = bucketId;
        this.ResetTime = resetTime;
    }

    /// <summary>Bucket ID.</summary>
    internal string Id { get; private init; }

    /// <summary></summary>
    internal DateTime ResetTime { get; private set; }

    /// <summary>Attempts to create a new <see cref="RequestBucket"/> from the provided <see cref="HttpResponseHeaders"/>.</summary>
    internal static bool TryParse(HttpResponseHeaders headers, out RequestBucket bucket)
    {
        bucket = null;

        // Bucket ID
        if (!headers.TryGetValues("X-RateLimit-Bucket", out var bucketIdHeader))
            return false;

        var id = bucketIdHeader.SingleOrDefault();
        if (id is null)
            return false;

        // Limit
        if (!headers.TryGetValues("X-RateLimit-Limit", out var limitHeader))
            return false;

        if (!int.TryParse(limitHeader.SingleOrDefault(), out var limit))
            return false;

        // Remaining
        if (!headers.TryGetValues("X-RateLimit-Remaining", out var remainingHeader))
            return false;

        if (!int.TryParse(remainingHeader.SingleOrDefault(), out var remaining))
            return false;

        // Reset time
        if (!headers.TryGetValues("X-RateLimit-Reset", out var resetHeader))
            return false;

        if (!int.TryParse(resetHeader.SingleOrDefault(), out var resetTimestamp))
            return false;

        var resetTime = DateTime.UnixEpoch + TimeSpan.FromSeconds(resetTimestamp);

        bucket = new RequestBucket(id, limit, remaining, resetTime);
        return true;
    }

    /// <summary>Decrements the number of requests remaining for this bucket.</summary>
    internal bool TryUse()
    {
        lock (this)
        {
            if (_remaining <= 0)
            {
                if (this.ResetTime >= DateTime.Now)
                    _remaining = _limit;
                else
                    return false;
            }

            _remaining--;
            return true;
        }
    }

    /// <summary></summary>
    internal void Update(HttpResponseHeaders headers)
    {
        lock (this)
        {
            if (headers.TryGetValues("X-RateLimit-Limit", out var limitHeader))
                if (!int.TryParse(limitHeader.SingleOrDefault(), out var limit))
                    _limit = limit;

            if (headers.TryGetValues("X-RateLimit-Remaining", out var remainingHeader))
                if (!int.TryParse(remainingHeader.SingleOrDefault(), out var remaining))
                    _remaining = remaining;

            if (!headers.TryGetValues("X-RateLimit-Reset", out var resetHeader))
                if (!int.TryParse(resetHeader.SingleOrDefault(), out var resetTimestamp))
                    this.ResetTime = DateTime.UnixEpoch + TimeSpan.FromSeconds(resetTimestamp);
        }
    }
}

