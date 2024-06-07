// See https://aka.ms/new-console-template for more information
using System.Diagnostics;
using System.Text;
using MacAddressSearch;

Console.WriteLine("Hello, World!");




var vendorMappings = VendorMappingParser.ParseXML();
// Console.WriteLine(vendorMappings.VendorMappings.Count);

var macAddressSearcher = new MacAddressSearcher();

foreach (var vendorMapping in vendorMappings.VendorMappings)
{
    macAddressSearcher.Insert(vendorMapping);
}

var listFindSw = Stopwatch.StartNew();
var listFindSb = new StringBuilder();
foreach (var mac in TestData.MacAddresses)
{
    var vendor = vendorMappings.VendorMappings.Find(x => mac.StartsWith(x.MacPrefix));
    listFindSb.AppendLine(vendor?.VendorName);
}
listFindSw.Stop();
Console.WriteLine($"List find: {listFindSw.ElapsedMilliseconds} ms");

var searchSw = Stopwatch.StartNew();
var searchSb = new StringBuilder();
foreach (var mac in TestData.MacAddresses)
{
    var vendorName = macAddressSearcher.Search(mac);
    searchSb.AppendLine(vendorName);
}
searchSw.Stop();

Console.WriteLine($"Searcher find: {searchSw.ElapsedMilliseconds} ms");
if (listFindSb.ToString() != searchSb.ToString())
{
    Console.WriteLine("results did not match!");
    Console.WriteLine($"find: {listFindSb}");
    Console.WriteLine($"trie: {searchSb}");
}


