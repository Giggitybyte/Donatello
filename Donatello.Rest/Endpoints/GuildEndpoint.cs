using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

namespace Donatello.Rest.Endpoints
{
    public class GuildEndpoint : ApiEndpoint
    {
        internal GuildEndpoint(HttpClient httpClient) : base(httpClient) { }

        public ValueTask<JsonElement> Get(ulong id)
        {
            
        }
    }
}
