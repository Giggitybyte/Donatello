namespace Donatello.Rest;

using System;
using System.Linq;
using System.Net.Http.Headers;

internal sealed class RequestBucket
{
    /// <summary>Creates a new <see cref="RequestBucket"/> using an existing instance of <see cref="HttpResponseHeaders"/>.</summary>
    internal RequestBucket(HttpResponseHeaders headers)
    {
        var id = headers.GetValues("X-RateLimit-Bucket").SingleOrDefault();

        var limitHeader = headers.GetValues("X-RateLimit-Limit");
        var limit = int.Parse(limitHeader.SingleOrDefault());

        var remainingHeader = headers.GetValues("X-RateLimit-Remaining");
        var remaining = int.Parse(remainingHeader.SingleOrDefault());

        var resetHeader = headers.GetValues("X-RateLimit-Reset");
        var resetTimestamp = int.Parse(resetHeader.SingleOrDefault());
        var resetTime = DateTime.UnixEpoch + TimeSpan.FromSeconds(resetTimestamp);

        this.Id = id;
        this.Limit = limit;
        this.Remaining = remaining;
        this.ResetDate = resetTime;
    }

    /// <summary>Bucket ID.</summary>
    internal string Id { get; private init; }

    /// <summary>Number of requests alloted.</summary>
    internal int Limit { get; private set; }

    /// <summary>Number of requests available for use.</summary>
    internal int Remaining { get; private set; }

    /// <summary>Date when the requests remaining for this bucket will be reset.</summary>
    internal DateTime ResetDate { get; private set; }

    /// <summary>Updates ratelimit information using the provided instance of <see cref="HttpResponseHeaders"/>.</summary>
    internal void Update(HttpResponseHeaders headers)
    {
        lock (this)
        {
            var limitHeader = headers.GetValues("X-RateLimit-Limit");
            this.Limit = int.Parse(limitHeader.SingleOrDefault());

            var remainingHeader = headers.GetValues("X-RateLimit-Remaining");
            var remaining = int.Parse(remainingHeader.SingleOrDefault());
            this.Remaining = remaining;

            var resetHeader = headers.GetValues("X-RateLimit-Reset");
            var resetTimestamp = int.Parse(resetHeader.SingleOrDefault());
            this.ResetDate = DateTime.UnixEpoch + TimeSpan.FromSeconds(resetTimestamp);
        }
    }

    /// <summary>Attempts to decrement the number of requests available for this bucket.</summary>
    internal bool TryUse()
    {
        lock (this)
        {
            if (DateTime.Now >= this.ResetDate)
                this.Remaining = this.Limit;

            if (this.Remaining - 1 < 0)
                return false;
            else
            {
                this.Remaining--;
                return true;
            }
        }
    }
}

