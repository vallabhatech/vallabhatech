using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

// Keeps README.md in sync with the generated images:
//   <!-- QUESTS:START --> ... <!-- QUESTS:END -->   quest cards linked to your pinned repos
//   <!-- CONNECT:START --> ... <!-- CONNECT:END --> link buttons from about.json
// and adds ?v=<hash> to every asset so GitHub's image cache never shows an old version.
static class ReadmeUpdater
{
    public static async Task UpdateAsync(string readmePath, string assetsDir, List<Repo?> quests, List<LinkButton> links)
    {
        var readme = await File.ReadAllTextAsync(readmePath);
        var rel = "./" + assetsDir.Replace('\\', '/').Trim('/');

        var questBlock = new StringBuilder();
        for (int i = 0; i < quests.Count; i += 2)
        {
            // two cards per line, no whitespace between them, so they sit side by side at 50% each
            questBlock.Append(Image(quests[i]?.Url, $"{rel}/quest-{i + 1}.svg", "50%", quests[i]?.Name ?? "Locked quest"));
            if (i + 1 < quests.Count)
                questBlock.Append(Image(quests[i + 1]?.Url, $"{rel}/quest-{i + 2}.svg", "50%", quests[i + 1]?.Name ?? "Locked quest"));
            questBlock.Append('\n');
        }

        var pct = links.Count == 0 ? "100%" : $"{100.0 / links.Count:0.##}%";
        var connectBlock = string.Concat(links.Select((l, i) => Image(l.Url, $"{rel}/connect-{i + 1}.svg", pct, l.Label))) + "\n";

        var updated = ReplaceBlock(readme, "QUESTS", questBlock.ToString());
        updated = ReplaceBlock(updated, "CONNECT", connectBlock);
        updated = BustCache(updated, rel, assetsDir);
        if (updated != readme) await File.WriteAllTextAsync(readmePath, updated);
    }

    static string ReplaceBlock(string readme, string name, string content)
    {
        string start = $"<!-- {name}:START -->", end = $"<!-- {name}:END -->";
        if (!readme.Contains(start) || !readme.Contains(end))
        {
            Console.WriteLine($"README has no {name} markers; skipping.");
            return readme;
        }
        return Regex.Replace(readme, Regex.Escape(start) + ".*?" + Regex.Escape(end),
            _ => $"{start}\n{content}{end}", RegexOptions.Singleline);
    }

    static string Image(string? url, string src, string width, string alt)
    {
        var img = $"<img src=\"{src}\" width=\"{width}\" align=\"top\" alt=\"{alt}\">";
        return url is null ? img : $"<a href=\"{url}\">{img}</a>";
    }

    // A changed SVG gets a new URL (new hash); unchanged files keep theirs, so no pointless commits.
    static string BustCache(string readme, string rel, string assetsDir) =>
        Regex.Replace(readme, Regex.Escape(rel) + @"/([\w.\-]+\.svg)(\?v=[0-9a-f]+)?", m =>
        {
            var file = Path.Combine(assetsDir, m.Groups[1].Value);
            if (!File.Exists(file)) return m.Value;
            var hash = Convert.ToHexString(SHA1.HashData(File.ReadAllBytes(file)))[..8].ToLowerInvariant();
            return $"{rel}/{m.Groups[1].Value}?v={hash}";
        });
}
