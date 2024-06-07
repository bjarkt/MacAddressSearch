using System.Xml;
using System.Xml.Serialization;

namespace MacAddressSearch;

[XmlRoot(ElementName = "MacAddressVendorMappings", Namespace = "http://www.cisco.com/server/spt")]
public class MacAddressVendorMappings
{
    [XmlElement(ElementName = "VendorMapping")]
    public List<VendorMapping> VendorMappings { get; set; } = [];
}

public class VendorMapping
{
    [XmlAttribute(AttributeName = "mac_prefix")]
    public string MacPrefix { get; set; } = "";

    [XmlAttribute(AttributeName = "vendor_name")]
    public string VendorName { get; set; } = "";
}

public class VendorMappingParser
{
    public static MacAddressVendorMappings ParseXML()
    {
        using var filestream = File.OpenRead("vendorMacs.xml");
        using var reader = XmlReader.Create(filestream);
        var serializer = new XmlSerializer(typeof(MacAddressVendorMappings));
        if (serializer.Deserialize(reader) is not MacAddressVendorMappings result) throw new Exception("failed to parse xml");
        result.VendorMappings = result.VendorMappings.OrderByDescending(x => x.MacPrefix.Length).ToList();
        return result;
    }
}


public class MacAddressSearcher
{
    private readonly Dictionary<byte, MacNode> rootNodes = [];

    public void Insert(VendorMapping vendorMapping)
    {
        var macPrefixParts = vendorMapping.MacPrefix.Split(":");
        MacNode? node = null;
        for (var i = 0; i < macPrefixParts.Length; i++)
        {
            var isLastPart = i == macPrefixParts.Length - 1;
            var part = macPrefixParts[i];
            if (part.Length == 2)
            {
                var partByte = Convert.ToByte(part, 16);

                if (node == null)
                {
                    if (rootNodes.TryGetValue(partByte, out var rootNode))
                    {
                        node = rootNode;
                    }
                    else
                    {
                        node = new MacNode();
                        rootNodes[partByte] = node;
                    }
                }
                else
                {
                    // A root node was found earlier. Add children
                    if (node.GetChildren().TryGetValue(partByte, out var childNode))
                    {
                        node = childNode;
                    }
                    else
                    {
                        var newChildNode = new MacNode();
                        if (isLastPart)
                        {
                            newChildNode.VendorName = vendorMapping.VendorName;
                        }
                        node.AddChild(partByte, newChildNode);
                        node = newChildNode;
                    }
                }
            }
            else if (part.Length == 1)
            {
                if (node == null) throw new Exception($"Found single with no node? MacPrefix={vendorMapping.MacPrefix}");
                node.AddSingle(part[0], vendorMapping.VendorName);
            }
            else
            {
                throw new Exception($"Found VendorMapping with mac address part with length not equal to 1 or 2. MacPrefix={vendorMapping.MacPrefix}. Part={part}");
            }
        }
    }

    public string? Search(string macAddress)
    {
        var macAddressPartsStr = macAddress.Split(":").ToList();
        var macAddressParts = macAddressPartsStr.Select(x => Convert.ToByte(x, 16)).ToList();
        MacNode? node = null;
        for (var i = 0; i < macAddressParts.Count; i++)
        {
            var macPart = macAddressParts[i];
            var hex = macAddressPartsStr[i];
            if (node == null)
            {
                if (!rootNodes.TryGetValue(macPart, out var _rootNode))
                {
                    // TODO: return null
                    throw new Exception($"root node not found for mac address={macAddress}");
                }
                node = _rootNode;
            }
            else
            {
                var children = node.GetChildren();
                if (children.Count > 0 && children.TryGetValue(macPart, out var childNode))
                {
                    node = childNode;
                }
                else if (node.GetSingles().TryGetValue(hex[0], out var single))
                {
                    return single;
                }
                else if (node.VendorName != null)
                {
                    return node.VendorName;
                }
                else
                {
                    // TODO: return null
                    throw new Exception($"value not found for mac address={macAddress}");
                }
            }
        }
        return null;
    }
}

public class MacNode
{
    private readonly Dictionary<byte, MacNode> children = [];
    private readonly Dictionary<char, string> singles = [];

    public string? VendorName { get; set; }

    public IDictionary<byte, MacNode> GetChildren() => children;
    public IDictionary<char, string> GetSingles() => singles;

    public void AddChild(byte _byte, MacNode node)
    {
        children[_byte] = node;
    }
    public void AddSingle(char ch, string vendorName)
    {
        singles[ch] = vendorName;
    }


}