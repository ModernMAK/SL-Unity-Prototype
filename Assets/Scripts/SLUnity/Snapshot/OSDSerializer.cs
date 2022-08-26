using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using OpenMetaverse;
using OpenMetaverse.StructuredData;
using SLUnity.Serialization;

namespace SLUnity.Snapshot
{
    public class OSDSerializer : ISerializer<OSD>
    {
        public void Write(BinaryWriter writer, OSD value)
        {
            writer.Write((byte)value.Type);
            switch (value.Type)
            {
                case OSDType.Unknown:
                    throw new Exception();
                case OSDType.Boolean:
                    writer.Write(value.AsBoolean());
                    break;
                case OSDType.Integer:
                    writer.Write(value.AsInteger()); // It doesn't matter if it's uint or int, so long as we always treat it the same during serialization; 
                    break;
                case OSDType.Real:
                    writer.Write(value.AsReal());
                    break;
                case OSDType.String:
                    writer.Write(value.AsString());
                    break;
                case OSDType.UUID:
                    writer.Write(value.AsUUID().Guid);
                    break;
                case OSDType.Date:
                    //Again; doesn't matter how we serialize; just as long we do it consistently
                    writer.Write(value.AsDate().ToFileTimeUtc());
                    break;
                case OSDType.URI:
                    writer.Write(value.AsUri().AbsoluteUri);
                    break;
                case OSDType.Binary:
                    writer.WriteByteArray(value.AsBinary());
                    break;
                case OSDType.Map:
                    var map = (OSDMap)value;
                    // OSDMap doesn't support GetEnumerator<TKey,TValue>, but does support GetEnumerator
                    //  Even though GetEnumerator uses the underlying implementatino; and it would be so easy to just return
                    //      Enumerator from internal dict
                    //          What the hell Libre? 
                    var dict = (IEnumerable)map; 
                    writer.Write(map.Count);
                    foreach (var kvpObj in dict)
                    {
                        var kvp = (KeyValuePair<string, OSD>)kvpObj;
                        writer.Write(kvp.Key);
                        Write(writer,kvp.Value);
                    }
                    break;
                case OSDType.Array:
                    var arr = (OSDArray)value;
                    writer.Write(arr.Count);
                    foreach (var arrItem in arr)
                        Write(writer, arrItem);
                    break;
                case OSDType.LlsdXml:
                    var xml = (OSDLlsdXml)value;
                    writer.Write(xml.value);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        public OSD Read(BinaryReader reader)
        {
            var type = (OSDType)reader.ReadByte();
            switch (type)
            {
                case OSDType.Unknown:        
                    throw new Exception();
                case OSDType.Boolean:
                    var osdBool = reader.ReadBoolean();
                    return OSD.FromBoolean(osdBool);
                case OSDType.Integer:
                    var osdInt = reader.ReadInt32();
                    return OSD.FromInteger(osdInt);
                case OSDType.Real:
                    var osdReal = reader.ReadDouble();
                    return OSD.FromReal(osdReal);
                case OSDType.String:
                    var osdStr = reader.ReadString();
                    return OSD.FromString(osdStr);
                case OSDType.UUID:
                    var osdGuid = reader.ReadGuid();
                    return OSD.FromUUID(new UUID(osdGuid));
                case OSDType.Date:
                    var osdDate = reader.ReadInt64();
                    return OSD.FromDate(DateTime.FromFileTimeUtc(osdDate));
                case OSDType.URI:
                    var osdUri = new Uri(reader.ReadString());
                    return OSD.FromUri(osdUri);
                case OSDType.Binary:
                    var osdBin = reader.ReadByteArray();
                    return OSD.FromBinary(osdBin);
                case OSDType.Map:
                    var osdMap = new Dictionary<string, OSD>();
                    var mapCount = reader.ReadInt32();
                    for (var i = 0; i < mapCount; i++)
                    {
                        var key = reader.ReadString();
                        var value = Read(reader);
                        osdMap[key] = value;
                    }
                    return new OSDMap(osdMap);
                case OSDType.Array:
                    var arrCount = reader.ReadInt32();
                    var osdArr = new OSD[arrCount];
                    for (var i = 0; i < arrCount; i++)
                        osdArr[i] = Read(reader);
                    return new OSDArray(osdArr.ToList()); // Why is it an array if its a list?
                case OSDType.LlsdXml:
                    var xml = reader.ReadString();
                    return new OSDLlsdXml(xml);
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
    }
}