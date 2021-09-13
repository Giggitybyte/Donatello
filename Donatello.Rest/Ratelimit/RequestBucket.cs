using System;
using System.Linq;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;

namespace Donatello.Rest.Ratelimit
{
    internal sealed class RequestBucket
    {
        private int _limit, _remaining;
        private DateTime _resetTime;
        private SemaphoreSlim _semaphore;

        internal RequestBucket(string bucketId, int limit, int remaining, DateTime resetTime, bool isGlobal)
        {
            _limit = limit;
            _remaining = remaining;
            _resetTime = resetTime;
            _semaphore = new SemaphoreSlim(1);

            this.Id = bucketId;
            this.IsGlobal = isGlobal;
        }

        /// <summary>Bucket ID.</summary>
        internal string Id { get; init; }

        /// <summary>Whether this bucket is the global bucket.</summary>
        internal bool IsGlobal { get; init; }

        /// <summary>Decrements the number of requests remaining for this bucket.</summary>
        internal async ValueTask<bool> TryUseAsync()
        {
            try
            {
                await _semaphore.WaitAsync().ConfigureAwait(false);

                if (_remaining <= 0)
                    return _resetTime < DateTime.Now;

                _remaining--;
                return true;
            }
            finally
            {
                _semaphore.Release();
            }
        }

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

            // Global
            var isGlobal = headers.Contains("X-RateLimit-Global");

            bucket = new RequestBucket(id, limit, remaining, resetTime, isGlobal);
            return true;
        }
    }
}
