namespace Donatello.Rest;

using System;
using System.Linq;
using System.Net.Http.Headers;

internal sealed class RequestBucket
{
    private int _limit;
    private int _remaining;

    /// <summary>Creates a new <see cref="RequestBucket"/> from the provided <see cref="HttpResponseHeaders"/>.</summary>
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

        _limit = limit;
        _remaining = remaining;

        this.Id = id;
        this.ResetTime = resetTime;
    }

    /// <summary>Bucket ID.</summary>
    internal string Id { get; private init; }

    /// <summary></summary>
    internal DateTime ResetTime { get; private set; }

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
            var limitHeader = headers.GetValues("X-RateLimit-Limit");
            _limit = int.Parse(limitHeader.SingleOrDefault());

            var remainingHeader = headers.GetValues("X-RateLimit-Remaining");
            var remaining = int.Parse(remainingHeader.SingleOrDefault());
            _remaining = remaining;

            var resetHeader = headers.GetValues("X-RateLimit-Reset");
            var resetTimestamp = int.Parse(resetHeader.SingleOrDefault());
            this.ResetTime = DateTime.UnixEpoch + TimeSpan.FromSeconds(resetTimestamp);
        }
    }
}

