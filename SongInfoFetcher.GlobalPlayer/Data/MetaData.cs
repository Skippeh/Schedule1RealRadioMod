using System.Collections.Generic;
using System.Runtime.Serialization;
using Newtonsoft.Json;

namespace SongInfoFetcher.GlobalPlayer.Data;

internal record MetaData(Props Props);
internal record Props(PageProps PageProps);
internal record PageProps(Feature Feature);
internal record Feature(List<Block> Blocks);
internal record Block([property: JsonConverter(typeof(SafeStringEnumConverter<BlockType>))] BlockType Type, List<Brand>? Brands);
internal enum BlockType
{
    Unknown,
    [EnumMember(Value = "live_radio")]
    LiveRadio,
}
internal record Brand(string Slug, NationalStation NationalStation);
internal record NationalStation(string Name, int HeraldId);
