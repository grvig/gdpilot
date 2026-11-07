using System.Text.RegularExpressions;
using GDPilot.Core;

namespace GDPilot.Core.Tests;

/// <summary>
/// Real report lines with their values mangled, as a different build of the in-game
/// part or a damaged line could send them. Whatever comes, a line is either read or
/// refused as a bad line, and nothing read can break what takes it: the link reads
/// on, and the bags are navigated on whatever survived.
/// </summary>
public sealed class GameReportFuzzTests
{
    private static readonly string[] Lines =
    {
        """{"protocol":1,"type":"layout","grid":"Inventory.Bag2","columns":8,"rows":8,"items":[{"id":1,"column":0,"row":4,"width":1,"height":1},{"id":3,"column":1,"row":0,"width":2,"height":3}]}""",
        """{"protocol":1,"type":"layout","grid":"Stash","tab":1,"tabs":2,"columns":10,"rows":19,"items":[{"id":7,"column":9,"row":18,"width":1,"height":1}]}""",
        """{"protocol":1,"type":"panels","open":["Inventory","Confirm"]}""",
        """{"protocol":1,"type":"health","percent":42}""",
        """{"protocol":1,"type":"camera","third":true}""",
        """{"protocol":1,"type":"lockpoint","id":5,"x":0.5,"y":0.4}""",
        """{"protocol":1,"type":"targets","list":[{"id":11,"x":0.5,"y":0.4,"distance":6,"health":100,"seen":true}],"feet":{"x":0.5,"y":0.7},"ring":[{"x":0.5,"y":0.45},null,null,null,null,null,null,null]}""",
        """{"protocol":1,"type":"interactables","list":[{"id":9,"kind":"travel","x":0.6,"y":0.3,"distance":4.1,"rarity":0}]}""",
        """{"protocol":1,"type":"character","name":"Semitarded"}""",
        """{"protocol":1,"type":"abilities","analog":true}""",
    };

    /// <summary>Numbers that read as numbers but make no sense: the likeliest damage, and the hardest to catch.</summary>
    private static readonly string[] Numbers = { "-1", "0", "-7", "3", "2147483647", "-2147483648", "100000", "64" };

    private static readonly string[] Replacements =
    {
        "-1", "0", "2147483648", "-2147483649", "1e308", "-1e308", "0.5", "\"\"", "\"x\"", "null", "true", "[]", "{}", "[1,2]", "999999",
    };

    [Fact]
    public void AnyMangledLineIsReadOrRefusedAndNothingReadBreaksTheBags()
    {
        Random random = new(20261108);
        GridSpec bag = new(1044, 645, 32, 32, 8, 8);
        GridSpec stash = new(529, 293, 32, 32, 10, 19);
        int read = 0;
        int refused = 0;
        int layouts = 0;

        for (int round = 0; round < 20000; round++)
        {
            // Half the rounds go to the layouts, which hold the most numbers and feed the most code.
            string original = Lines[random.Next(Lines.Length)];

            if (random.Next(2) == 0)
            {
                original = Lines[random.Next(2)];
            }

            string line = Mangle(original, random);
            GameReport? report;

            try
            {
                report = GameReport.Parse(line);
            }
            catch (FormatException)
            {
                refused++;
                continue;
            }

            read++;

            if (report is LayoutReport layout)
            {
                layouts++;
                ReportedOccupancy occupancy = new(null);
                occupancy.Accept(layout);
                occupancy.Read("Inventory.Bag2", bag, null);
                occupancy.Read("Stash", stash, null);
                occupancy.TabOf("Stash");
                occupancy.TabsOf("Stash");
            }
        }

        // Both ways out are taken many times over, so the mangling reaches what reads the lines as well as what refuses them.
        Assert.True(read > 1000 && refused > 1000 && layouts > 500, $"read {read}, refused {refused}, layouts {layouts}");
    }

    /// <summary>One to three values swapped for something else, or a field cut, or the line cut short.</summary>
    private static string Mangle(string line, Random random)
    {
        MatchCollection values = Regex.Matches(line, @"(?<=:)(""[^""]*""|-?[0-9.e]+|true|false|null|\[[^\[\]]*\])");
        int changes = 1 + random.Next(3);

        for (int change = 0; change < changes && values.Count > 0; change++)
        {
            Match value = values[random.Next(values.Count)];

            if (value.Index + value.Length > line.Length)
            {
                break;
            }

            string[] from = Replacements;

            if (random.Next(10) < 7)
            {
                from = Numbers;
            }

            line = line[..value.Index] + from[random.Next(from.Length)] + line[(value.Index + value.Length)..];
            values = Regex.Matches(line, @"(?<=:)(""[^""]*""|-?[0-9.e]+|true|false|null|\[[^\[\]]*\])");
        }

        if (random.Next(10) == 0)
        {
            line = line[..random.Next(line.Length)];
        }

        return line;
    }
}
