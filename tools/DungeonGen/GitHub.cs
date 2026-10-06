using System.Globalization;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;

static class GitHub
{
    const string Query = """
    query($login: String!) {
      user(login: $login) {
        createdAt
        followers { totalCount }
        pullRequests { totalCount }
        issues { totalCount }
        repositoriesContributedTo(contributionTypes: [COMMIT, PULL_REQUEST, ISSUE, REPOSITORY]) { totalCount }
        pinnedItems(first: 6, types: REPOSITORY) {
          nodes { ... on Repository { ...RepoFields } }
        }
        repositories(ownerAffiliations: OWNER, isFork: false, privacy: PUBLIC, first: 100,
                     orderBy: { field: PUSHED_AT, direction: DESC }) {
          totalCount
          nodes {
            ...RepoFields
            languages(first: 10, orderBy: { field: SIZE, direction: DESC }) {
              edges { size node { name color } }
            }
          }
        }
        contributionsCollection {
          commitContributionsByRepository(maxRepositories: 100) {
            contributions { totalCount }
            repository { nameWithOwner primaryLanguage { name color } }
          }
          contributionCalendar {
            totalContributions
            weeks { contributionDays { date contributionCount weekday } }
          }
        }
      }
    }
    fragment RepoFields on Repository {
      name nameWithOwner url description stargazerCount forkCount pushedAt
      primaryLanguage { name color }
    }
    """;

    public static async Task<Profile> FetchAsync(string login, string token)
    {
        using var http = new HttpClient();
        http.DefaultRequestHeaders.UserAgent.ParseAdd("Vallabha-ProfileGen");
        http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("bearer", token);
        var json = await PostGraphQLAsync(http, Query, new { login });
        // GraphQL can return partial data: e.g. an organisation that blocks your token hides just
        // that one pinned repo. Keep going with whatever came back and only fail if the user is missing.
        if (json["errors"] is JsonArray errs)
            foreach (var e in errs)
                Console.WriteLine($"::warning::GitHub API: {e?["message"]} (path: {e?["path"]?.ToJsonString()})");

        var u = json["data"]?["user"]
            ?? throw new Exception("GitHub API returned no user data. Check the token in the PROFILE_TOKEN secret.");

        // --- Contribution calendar (API version; replaced by the profile page version below)
        var cal = u["contributionsCollection"]!["contributionCalendar"]!;
        var weeks = new List<List<Day?>>();
        foreach (var w in cal["weeks"]!.AsArray())
        {
            var col = new Day?[7];
            foreach (var d in w!["contributionDays"]!.AsArray())
                col[(int)d!["weekday"]!] = new Day(
                    DateOnly.Parse((string)d["date"]!, CultureInfo.InvariantCulture),
                    (int)d["contributionCount"]!);
            weeks.Add(col.ToList());
        }
        int total = (int)cal["totalContributions"]!;

        // The API only sees private contributions with a personal token. The profile page's own
        // calendar already includes them (if "private contributions" is on), so prefer it.
        try
        {
            var (pageWeeks, pageTotal) = await FetchProfileCalendarAsync(http, login);
            Console.WriteLine($"Calendar source: profile page ({pageTotal}) vs API ({total}).");
            if (pageTotal >= total) { weeks = pageWeeks; total = pageTotal; }
        }
        catch (Exception e) { Console.WriteLine($"Profile calendar unavailable, using API data: {e.Message}"); }

        // --- Repos, languages, pinned quests
        var repoNodes = u["repositories"]!["nodes"]!.AsArray().OfType<JsonNode>().ToList();
        // Languages = what you actually commit in: every repo you pushed commits to this year
        // (your own, org repos, open source like MetaCall), weighted by your commit count there,
        // using that repo's main language. Falls back to code size in your own repos.
        var byCommits = (u["contributionsCollection"]?["commitContributionsByRepository"]?.AsArray() ?? [])
            .OfType<JsonNode>()
            .Where(c => c["repository"]?["primaryLanguage"] is not null)
            .GroupBy(c => (string)c["repository"]!["primaryLanguage"]!["name"]!)
            .Select(g => new Language(g.Key, (string?)g.First()["repository"]!["primaryLanguage"]!["color"] ?? "#00FF00",
                                      g.Sum(c => (long)c["contributions"]!["totalCount"]!)))
            .OrderByDescending(l => l.Bytes)
            .ToList();
        var bySize = repoNodes
            .SelectMany(n => n["languages"]!["edges"]!.AsArray().OfType<JsonNode>())
            .GroupBy(e => (string)e["node"]!["name"]!)
            .Select(g => new Language(g.Key, (string?)g.First()["node"]!["color"] ?? "#00FF00",
                                      g.Sum(e => (long)e["size"]!)))
            .OrderByDescending(l => l.Bytes)
            .ToList();
        bool useCommits = byCommits.Sum(l => l.Bytes) > 0;
        var languages = useCommits ? byCommits : bySize;
        Console.WriteLine($"Languages by {(useCommits ? "commits" : "code size")}: " +
                          string.Join(", ", languages.Take(6).Select(l => $"{l.Name}={l.Bytes}")));

        var pinned = (u["pinnedItems"]?["nodes"]?.AsArray() ?? [])
            .OfType<JsonNode>()                       // blocked repos come back as null: skip them
            .Where(n => n["name"] is not null)
            .Select(ToRepo).ToList();
        if (pinned.Count == 0) pinned = repoNodes.Take(6).Select(ToRepo).ToList();   // nothing pinned: latest repos
        pinned = await AddMyPullRequestsAsync(http, login, pinned);

        return new Profile(weeks, total,
            Stars: repoNodes.Sum(n => (int)n["stargazerCount"]!),
            Repos: (int)u["repositories"]!["totalCount"]!,
            Followers: (int)u["followers"]!["totalCount"]!,
            PullRequests: (int?)u["pullRequests"]?["totalCount"] ?? 0,
            Issues: (int?)u["issues"]?["totalCount"] ?? 0,
            ContributedTo: (int?)u["repositoriesContributedTo"]?["totalCount"] ?? 0,
            CreatedAt: DateTime.Parse((string)u["createdAt"]!, CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal),
            Languages: languages,
            LanguagesByCommits: useCommits,
            Pinned: pinned);
    }

    static async Task<JsonNode> PostGraphQLAsync(HttpClient http, string query, object variables)
    {
        var body = JsonSerializer.Serialize(new { query, variables });
        var res = await http.PostAsync("https://api.github.com/graphql",
            new StringContent(body, Encoding.UTF8, "application/json"));
        res.EnsureSuccessStatusCode();
        return JsonNode.Parse(await res.Content.ReadAsStringAsync())!;
    }

    // For pinned repos you don't own (open source, org projects): count your PRs there, all-time.
    // One request, using an aliased search per repo.
    static async Task<List<Repo>> AddMyPullRequestsAsync(HttpClient http, string login, List<Repo> pinned)
    {
        var foreign = pinned.Select((r, i) => (r, i))
            .Where(x => !x.r.NameWithOwner.StartsWith(login + "/", StringComparison.OrdinalIgnoreCase)).ToList();
        if (foreign.Count == 0) return pinned;

        var q = new StringBuilder("query {");
        foreach (var (r, i) in foreign)
            q.Append($$"""
              all{{i}}: search(type: ISSUE, query: "repo:{{r.NameWithOwner}} author:{{login}} is:pr") { issueCount }
              merged{{i}}: search(type: ISSUE, query: "repo:{{r.NameWithOwner}} author:{{login}} is:pr is:merged") { issueCount }
            """);
        q.Append('}');

        try
        {
            var data = (await PostGraphQLAsync(http, q.ToString(), new { }))["data"];
            var result = pinned.ToList();
            foreach (var (r, i) in foreign)
                result[i] = r with
                {
                    MyPullRequests = (int?)data?[$"all{i}"]?["issueCount"] ?? 0,
                    MyMerged = (int?)data?[$"merged{i}"]?["issueCount"] ?? 0,
                };
            return result;
        }
        catch (Exception e)
        {
            Console.WriteLine($"::warning::Could not count your PRs on contributed repos: {e.Message}");
            return pinned;
        }
    }

    static Repo ToRepo(JsonNode n) => new(
        (string)n["name"]!, (string)n["nameWithOwner"]!, (string)n["url"]!, (string?)n["description"],
        (string?)n["primaryLanguage"]?["name"], (string?)n["primaryLanguage"]?["color"],
        (int)n["stargazerCount"]!, (int)n["forkCount"]!,
        DateTime.Parse((string)n["pushedAt"]!, CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal));

    // Reads https://github.com/users/<login>/contributions — the exact graph shown on the profile.
    static async Task<(List<List<Day?>> Weeks, int Total)> FetchProfileCalendarAsync(HttpClient http, string login)
    {
        using var req = new HttpRequestMessage(HttpMethod.Get, $"https://github.com/users/{login}/contributions");
        req.Headers.Add("X-Requested-With", "XMLHttpRequest");
        var res = await http.SendAsync(req);
        res.EnsureSuccessStatusCode();
        var html = await res.Content.ReadAsStringAsync();

        var tips = new Dictionary<string, int>();
        foreach (Match m in Regex.Matches(html, @"<tool-tip[^>]*\bfor=""([^""]+)""[^>]*>\s*(\d+|No) contribution"))
            tips[m.Groups[1].Value] = m.Groups[2].Value == "No" ? 0 : int.Parse(m.Groups[2].Value, CultureInfo.InvariantCulture);

        var days = new List<Day>();
        foreach (Match m in Regex.Matches(html, @"<td\b[^>]*>"))
        {
            var tag = m.Value;
            var date = Regex.Match(tag, @"data-date=""([\d-]+)""");
            var id = Regex.Match(tag, @"\bid=""([^""]+)""");
            if (!date.Success || !id.Success) continue;
            days.Add(new Day(DateOnly.Parse(date.Groups[1].Value, CultureInfo.InvariantCulture),
                             tips.GetValueOrDefault(id.Groups[1].Value)));
        }
        if (days.Count < 300) throw new Exception($"only parsed {days.Count} days");
        days.Sort((a, b) => a.Date.CompareTo(b.Date));

        var weeks = new List<List<Day?>>();
        List<Day?>? week = null;
        foreach (var d in days)
        {
            int wd = (int)d.Date.DayOfWeek;              // Sunday = 0, like GitHub's graph
            if (week is null || wd == 0) { week = Enumerable.Repeat<Day?>(null, 7).ToList(); weeks.Add(week); }
            week[wd] = d;
        }

        var header = Regex.Match(html, @"([\d,]+)\s+contributions?\s+in the last year");
        int total = header.Success ? int.Parse(header.Groups[1].Value.Replace(",", ""), CultureInfo.InvariantCulture)
                                   : days.Sum(d => d.Count);
        return (weeks, total);
    }
}
