using System.Xml.Linq;

namespace AspNetCore.DataProtection.CustomStorage.Tests;

public static class TestConstants
{
    public static readonly string FriendlyName = "friendlyName";
    public const string KeyMaterial = "c2VjcmV0LWtleS1tYXRlcmlhbA==";
    public static readonly XElement XElement = new("root",
        new XElement("child",
            new XAttribute("id", "1"),
            "First child content"
        ),
        new XElement("child",
            new XAttribute("id", "2"),
            "Second child content"
        )
    );
}