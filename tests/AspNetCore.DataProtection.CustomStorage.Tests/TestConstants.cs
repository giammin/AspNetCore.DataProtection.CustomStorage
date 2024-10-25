using System.Xml.Linq;

namespace AspNetCore.DataProtection.CustomStorage.Tests;

public static class TestConstants
{
    public static readonly string FriendlyName = "friendlyName";
    public static readonly string Xml = """
                                        <note>
                                        <to>Tove</to>
                                        <from>Jani</from>
                                        <heading>Reminder</heading>
                                        <body>Don't forget me this weekend!</body>
                                        </note>
                                        """;
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