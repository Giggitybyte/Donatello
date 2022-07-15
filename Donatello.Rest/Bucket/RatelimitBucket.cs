namespace Donatello.Rest.Bucket;

using System;
using System.Linq;
using System.Net.Http.Headers;

public sealed class RatelimitBucket
{
    private TimeSpan _resetIncrement;

    internal RatelimitBucket(string id, HttpResponseHeaders headers = null)
    {
        this.Id = id;
        this.Limit = uint.MaxValue;
        this.Remaining = uint.MaxValue;

        _resetIncrement = TimeSpan.FromSeconds(1);
        this.ResetDate = DateTimeOffset.UtcNow + _resetIncrement;

        if (headers is not null)
            Update(headers);
    }

    /// <summary>Bucket ID.</summary>
    public string Id { get; private init; }

    /// <summary>Number of requests alloted.</summary>
    public uint Limit { get; internal set; }

    /// <summary>Number of requests available for use.</summary>
    public uint Remaining { get; internal set; }

    /// <summary>Date when the requests remaining for this bucket will be reset.</summary>
    public DateTimeOffset ResetDate { get; private set; }

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
                    this.ResetDate += _resetIncrement;
                }
                else
                    return false;
            }

            this.Remaining--;
            return true;
        }
    }

    /// <summary>Updates ratelimit information using the provided instance of <see cref="HttpResponseHeaders"/>.</summary>
    internal void Update(HttpResponseHeaders headers)
    {
        lock (this)
        {
            var limitHeader = headers.GetValues("X-RateLimit-Limit");
            this.Limit = uint.Parse(limitHeader.Single());

            var remainingHeader = headers.GetValues("X-RateLimit-Remaining");
            this.Remaining = uint.Parse(remainingHeader.Single());

            var resetHeader = headers.GetValues("X-RateLimit-Reset");
            var resetTimestamp = uint.Parse(resetHeader.Single());
            this.ResetDate = DateTime.UnixEpoch + TimeSpan.FromSeconds(resetTimestamp);

            _resetIncrement = this.ResetDate - DateTimeOffset.UtcNow;
        }
    }
}