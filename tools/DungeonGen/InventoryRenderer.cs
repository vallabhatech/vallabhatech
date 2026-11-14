using System.Globalization;
using System.Text;
using static Svg;

// "Backpack" of languages (by bytes of code) + a character sheet of stats.
static class InventoryRenderer
{
    const int W = 960, H = 400;

    public static string Render(Profile p)
    {
        var sb = new StringBuilder(Open(W, H));
        sb.Append(Frame(W, H));
        sb.Append(Section(W, 26, 2, "INVENTORY"));
        sb.Append($"""<text class="ui" x="40" y="64" fill="{Green}" filter="url(#glow)">&gt; ./inventory --open</text>""");
        sb.Append($"""<text class="lbl" x="{W - 40}" y="64" fill="{Dim}" text-anchor="end">ADVENTURER SINCE {p.CreatedAt:yyyy}</text>""");

        // --- Backpack (left)
        int lx = 40, lw = 540, top = 82;
        sb.Append($"""<rect x="{lx}" y="{top}" width="{lw}" height="226" rx="4" fill="{Panel}" stroke="{Dim}" stroke-opacity=".7"/>""");
        sb.Append($"""<text class="lbl" x="{lx + 14}" y="{top + 22}" fill="{Label}">BACKPACK · {(p.LanguagesByCommits ? "languages by my commits this year" : "languages by code written")}</text>""");

        long totalBytes = Math.Max(1, p.Languages.Sum(l => l.Bytes));
        var items = p.Languages.Take(6).ToList();
        for (int i = 0; i < 6; i++)
        {
            int y = top + 40 + i * 30;
            int barX = lx + 190, barW = 270;
            // inventory slot
            sb.Append($"""<rect x="{lx + 14}" y="{y}" width="20" height="20" fill="#000" stroke="{Dim}"/>""");
            if (i >= items.Count)
            {
                sb.Append($"""<text class="sm" x="{lx + 46}" y="{y + 15}" fill="#1d4d29">— empty slot —</text>""");
                continue;
            }
            var l = items[i];
            double pct = 100.0 * l.Bytes / totalBytes;
            sb.Append($"""<rect x="{lx + 18}" y="{y + 4}" width="12" height="12" fill="{l.Color}" filter="url(#glow)"/>""");
            sb.Append($"""<text class="sm" x="{lx + 46}" y="{y + 15}" fill="{Green}">{Esc(Trim(l.Name, 16))}</text>""");
            sb.Append($"""<rect x="{barX}" y="{y + 5}" width="{barW}" height="10" fill="#062b10"/>""");
            sb.Append($"""<rect x="{barX}" y="{y + 5}" width="{Math.Max(2, barW * pct / 100):0.#}" height="10" fill="{l.Color}"/>""");
            // pixel notches so it reads like a game bar
            for (int n = 1; n < 10; n++)
                sb.Append($"""<rect x="{barX + n * barW / 10}" y="{y + 5}" width="1" height="10" fill="#000" fill-opacity=".6"/>""");
            sb.Append($"""<text class="sm" x="{lx + lw - 14}" y="{y + 15}" fill="{Dim}" text-anchor="end">{pct.ToString("0.0", CultureInfo.InvariantCulture)}%</text>""");
        }

        // --- Character sheet (right)
        int rx = 600, rw = W - 40 - rx;
        sb.Append($"""<rect x="{rx}" y="{top}" width="{rw}" height="226" rx="4" fill="{Panel}" stroke="{Dim}" stroke-opacity=".7"/>""");
        sb.Append($"""<text class="lbl" x="{rx + 14}" y="{top + 22}" fill="{Label}">CHARACTER STATS</text>""");
        (string Label, string Value)[] stats =
        [
            ("GOLD · stars earned", $"★ {p.Stars}"),
            ("QUESTS · public repos", $"{p.Repos}"),
            ("PULL REQUESTS", $"{p.PullRequests}"),
            ("ISSUES OPENED", $"{p.Issues}"),
            ("GUILDS · contributed to", $"{p.ContributedTo}"),
            ("PARTY · followers", $"{p.Followers}"),
        ];
        for (int i = 0; i < stats.Length; i++)
        {
            int y = top + 55 + i * 30;
            sb.Append($"""<text class="sm" x="{rx + 14}" y="{y}" fill="{Dim}">{stats[i].Label}</text>""");
            sb.Append($"""<text class="big" x="{rx + rw - 14}" y="{y + 2}" fill="{Green}" text-anchor="end" filter="url(#glow)">{Esc(stats[i].Value)}</text>""");
        }

        // --- Lead-in to the quest cards below
        sb.Append(Section(W, 346, 3, "QUEST LOG"));
        sb.Append($"""<text class="ui" x="40" y="384" fill="{Green}" filter="url(#glow)">&gt; ./quests --pinned</text>""");
        sb.Append($"""<text class="lbl" x="{W - 44}" y="384" fill="{Dim}" text-anchor="end">click a quest to open it</text>""");
        sb.Append($"""<path class="blink" d="M{W - 34} 377 h10 l-5 7z" fill="{Green}"/>""");

        sb.Append(Close(W, H));
        return sb.ToString();
    }

}
