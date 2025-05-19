using Newtonsoft.Json;

namespace SongInfoFetcher.OneFM.Data;

public record EventData<T>
{
    [JsonProperty("__data")]
    public T? Data { get; set; }
}
