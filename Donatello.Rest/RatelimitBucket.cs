namespace Donatello.Rest;

using System;
using System.Linq;
using System.Net.Http.Headers;

public sealed class RatelimitBucket
{
    private TimeSpan _fallbackResetInterval;

    internal RatelimitBucket(HttpResponseHeaders headers)
    {
        this.Id = headers.GetValues("X-RateLimit-Bucket").Single();
        Update(headers);
    }

    /// <summary>Bucket ID.</summary>
    public string Id { get; private init; }

    /// <summary>Number of requests alloted.</summary>
    public int Limit { get; private set; }

    /// <summary>Number of requests available for use.</summary>
    public int Remaining { get; private set; }

    /// <summary>Date when the requests remaining for this bucket will be reset.</summary>
    public DateTimeOffset ResetDate { get; private set; }

    /// <summary>Updates ratelimit information using the provided instance of <see cref="HttpResponseHeaders"/>.</summary>
    internal void Update(HttpResponseHeaders headers)
    {
        lock (this)
        {
            var limitHeader = headers.GetValues("X-RateLimit-Limit");
            this.Limit = int.Parse(limitHeader.Single());

            var remainingHeader = headers.GetValues("X-RateLimit-Remaining");
            this.Remaining = int.Parse(remainingHeader.Single());

            var resetHeader = headers.GetValues("X-RateLimit-Reset");
            var resetTimestamp = int.Parse(resetHeader.Single());
            this.ResetDate = DateTime.UnixEpoch + TimeSpan.FromSeconds(resetTimestamp);

            _fallbackResetInterval = this.ResetDate - DateTimeOffset.UtcNow;
        }
    }

    /// <summary>Attempts to decrement the number of requests available for this bucket.</summary>
    internal bool TryUse()
    {
        lock (this)
        {
            if (this.Remaining is 0)
            {
                if (DateTimeOffset.UtcNow > this.ResetDate)
                {
                    this.Remaining = this.Limit;
                    this.ResetDate += _fallbackResetInterval;
                }
                else
                {
                    return false;
                }
            }

            this.Remaining--;
            return true;
        }
    }
}