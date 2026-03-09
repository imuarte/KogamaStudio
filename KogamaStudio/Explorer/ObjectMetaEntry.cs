using Newtonsoft.Json;

namespace KogamaStudio.Explorer;

internal class ObjectMetaEntry
{
    [JsonProperty("name")]
    public string Name { get; set; } = "";
}
