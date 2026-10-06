// Vallabha Profile Generator
// Turns your GitHub activity into a cyberpunk/roguelike console for your profile README.
//
//   dotnet run -- <username> [--assets assets] [--html docs/index.html] [--readme README.md]
//
// Writes:
//   assets/dungeon.svg      contribution dungeon (README image)
//   assets/inventory.svg    languages + character stats
//   assets/quest-1..6.svg   one card per pinned repo
//   assets/about.svg        character sheet, effects, gear (from about.json)
//   assets/connect-N.svg    one link button per entry in about.json
//   docs/index.html         interactive dungeon (GitHub Pages, hover any tile)
//   README.md               refreshes the quest links between <!-- QUESTS:START/END -->
//
// Uses PROFILE_TOKEN (personal token) or GITHUB_TOKEN for the GitHub GraphQL API.

var user = args.FirstOrDefault(a => !a.StartsWith("--")) ?? "vallabhatech";
string Opt(string name, string fallback)
{
    var i = Array.IndexOf(args, name);
    return i >= 0 && i + 1 < args.Length ? args[i + 1] : fallback;
}
var assets = Opt("--assets", "assets");
var htmlPath = Opt("--html", "docs/index.html");
var readmePath = Opt("--readme", "README.md");
var aboutPath = Opt("--about", "tools/DungeonGen/about.json");

var token = Environment.GetEnvironmentVariable("PROFILE_TOKEN")
         ?? Environment.GetEnvironmentVariable("GITHUB_TOKEN")
         ?? throw new InvalidOperationException("Set PROFILE_TOKEN or GITHUB_TOKEN.");

var profile = await GitHub.FetchAsync(user, token);

var dungeon = new DungeonRenderer(profile);
await Write(Path.Combine(assets, "dungeon.svg"), dungeon.Render(interactive: false));
await Write(htmlPath, HtmlPage.Build(user, dungeon.Render(interactive: true)));
await Write(Path.Combine(assets, "inventory.svg"), InventoryRenderer.Render(profile));

// About / gear / connect buttons / roles come from about.json (plain text, edit it any time)
AboutConfig? about = null;
if (File.Exists(aboutPath))
    about = System.Text.Json.JsonSerializer.Deserialize<AboutConfig>(await File.ReadAllTextAsync(aboutPath),
        new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true })!;
else Console.WriteLine($"No {aboutPath}; skipping about/connect.");

var links = about?.Links ?? [];
if (about is not null)
{
    await Write(Path.Combine(assets, "about.svg"), AboutRenderer.Render(about));
    for (int i = 0; i < links.Count; i++)
        await Write(Path.Combine(assets, $"connect-{i + 1}.svg"), ConnectRenderer.Render(links[i], i, links.Count));
}

var quests = QuestRenderer.Slots(profile);
for (int i = 0; i < quests.Count; i++)
    await Write(Path.Combine(assets, $"quest-{i + 1}.svg"), QuestRenderer.Render(quests[i], i, user, RoleFor(quests[i])));

if (File.Exists(readmePath))
    await ReadmeUpdater.UpdateAsync(readmePath, assets, quests, links);

Console.WriteLine($"Done: {profile.Total} XP, {profile.Languages.Count} languages, {profile.Pinned.Count} quests.");

string? RoleFor(Repo? r) =>
    r is null || about?.Roles is null ? null
    : about.Roles.FirstOrDefault(kv => kv.Key.Equals(r.NameWithOwner, StringComparison.OrdinalIgnoreCase)).Value;

static async Task Write(string path, string content)
{
    Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(path))!);
    await File.WriteAllTextAsync(path, content);
}
