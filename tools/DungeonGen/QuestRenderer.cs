using System.Text;
using static Svg;

// One half-width card per pinned repo. Cards sit two per row; the left card draws the left rail,
// the right card draws the right rail, so together they continue the console window.
static class QuestRenderer
{
    public const int Slots_ = 6;
    const int W = 480, H = 200;

    /// Always 6 slots so the grid stays even; missing ones become "locked" quests.
    public static List<Repo?> Slots(Profile p) =>
        Enumerable.Range(0, Slots_).Select(i => i < p.Pinned.Count ? p.Pinned[i] : null).ToList();

    public static string Render(Repo? repo, int index, string login, string? role = null)
    {
        bool left = index % 2 == 0;
        int bx = left ? 40 : 16, bw = W - 56, by = 12, bh = H - 24;
        var sb = new StringBuilder(Open(W, H));
        sb.Append(Frame(W, H, leftRail: left, rightRail: !left));
        sb.Append($"""<rect x="{bx}" y="{by}" width="{bw}" height="{bh}" rx="5" fill="{Panel}" stroke="{Dim}" stroke-opacity=".8"/>""");
        sb.Append($"""<text class="lbl" x="{bx + 16}" y="{by + 24}" fill="{Label}">◆ QUEST {index + 1:00}</text>""");

        if (repo is null) return Locked(sb, bx, by, bw, bh);

        // status badge
        var days = (int)(DateTime.UtcNow - repo.PushedAt).TotalDays;
        var (status, color, pulse) = days <= 14 ? ("ACTIVE", Green, true)
                                   : days <= 90 ? ("IN PROGRESS", "#ffd84d", false)
                                   : ("ON HOLD", Label, false);
        int sw = status.Length * 8 + 20, sx = bx + bw - 16 - sw;
        sb.Append($"""<rect x="{sx}" y="{by + 10}" width="{sw}" height="20" rx="3" fill="none" stroke="{color}"/>""");
        if (pulse) sb.Append($"""<rect class="pulse" x="{sx}" y="{by + 10}" width="{sw}" height="20" rx="3" fill="{color}" fill-opacity=".18"/>""");
        sb.Append($"""<text class="lbl" x="{sx + sw / 2}" y="{by + 24}" fill="{color}" text-anchor="middle">{status}</text>""");

        bool contributor = !repo.NameWithOwner.StartsWith(login + "/", StringComparison.OrdinalIgnoreCase);
        if (contributor)
        {
            const string gold = "#ffd84d";
            int cw = 11 * 8 + 20, cx = sx - 8 - cw;
            sb.Append($"""<rect class="pulse" x="{cx}" y="{by + 10}" width="{cw}" height="20" rx="3" fill="{gold}" fill-opacity=".15"/>""");
            sb.Append($"""<rect x="{cx}" y="{by + 10}" width="{cw}" height="20" rx="3" fill="none" stroke="{gold}"/>""");
            sb.Append($"""<text class="lbl" x="{cx + cw / 2}" y="{by + 24}" fill="{gold}" text-anchor="middle" filter="url(#glow)">CONTRIBUTOR</text>""");
        }

        // title: show "owner/name" for repos that belong to an org or someone else
        var title = repo.NameWithOwner.StartsWith(login + "/", StringComparison.OrdinalIgnoreCase) ? repo.Name : repo.NameWithOwner;
        sb.Append($"""<text class="big" x="{bx + 16}" y="{by + 56}" fill="{Green}" filter="url(#glow)">{Esc(Trim(title, 30))}</text>""");
        sb.Append($"""<line x1="{bx + 16}" y1="{by + 66}" x2="{bx + bw - 16}" y2="{by + 66}" stroke="{Dim}" stroke-opacity=".35"/>""");

        // description, wrapped to two lines
        var desc = !string.IsNullOrWhiteSpace(repo.Description) ? repo.Description!
                 : contributor ? "Shared guild quest. Open the repo for the full log."
                 : "No quest log yet. Uncharted territory.";
        bool hasRole = !string.IsNullOrWhiteSpace(role);
        var lines = Wrap(desc, 50, hasRole ? 2 : 3);
        for (int i = 0; i < lines.Count; i++)
            sb.Append($"""<text class="sm" x="{bx + 16}" y="{by + 84 + i * 18}" fill="{Dim}">{Esc(lines[i])}</text>""");
        if (hasRole)
            sb.Append($"""<text class="sm" x="{bx + 16}" y="{by + 84 + lines.Count * 18 + 8}" fill="#ffd84d"><tspan fill="{Label}">my role: </tspan>{Esc(Trim(role!, 41))}</text>""");

        // footer: language · stars · forks · last played
        int fy = by + bh - 16;
        int x = bx + 16;
        if (repo.Language is not null)
        {
            sb.Append($"""<rect x="{x}" y="{fy - 9}" width="10" height="10" fill="{repo.LanguageColor ?? Green}"/>""");
            sb.Append($"""<text class="lbl" x="{x + 16}" y="{fy}" fill="{Green}">{Esc(repo.Language)}</text>""");
            x += 26 + repo.Language.Length * 8;
        }
        var stats = contributor && repo.MyPullRequests > 0
            ? $"""★ {Short(repo.Stars)}   <tspan fill="{Green}">my PRs {repo.MyPullRequests} · {repo.MyMerged} merged</tspan>"""
            : $"★ {Short(repo.Stars)}   forks {Short(repo.Forks)}   played {Ago(days)}";
        sb.Append($"""<text class="lbl" x="{x}" y="{fy}" fill="{Dim}">{stats}</text>""");
        sb.Append($"""<text class="lbl" x="{bx + bw - 16}" y="{fy}" fill="{Green}" text-anchor="end" filter="url(#glow)">OPEN &gt;&gt;</text>""");

        sb.Append(Close(W, H));
        return sb.ToString();
    }

    static string Locked(StringBuilder sb, int bx, int by, int bw, int bh)
    {
        int cx = bx + bw / 2, cy = by + bh / 2;
        sb.Append($"""<g opacity=".7"><rect x="{cx - 11}" y="{cy - 14}" width="22" height="18" fill="none" stroke="{Label}" stroke-width="2"/><path d="M{cx - 6} {cy - 14} v-6 a6 6 0 0 1 12 0 v6" fill="none" stroke="{Label}" stroke-width="2"/></g>""");
        sb.Append($"""<text class="sm" x="{cx}" y="{cy + 26}" fill="{Label}" text-anchor="middle">??? LOCKED QUEST — pin a repo to unlock</text>""");
        sb.Append(Close(W, H));
        return sb.ToString();
    }

    static string Short(int n) => n >= 1000 ? $"{n / 1000.0:0.#}k".Replace(",", ".") : n.ToString();

    static string Ago(int days) => days switch
    {
        <= 0 => "today",
        1 => "yesterday",
        < 30 => $"{days}d ago",
        < 365 => $"{days / 30}mo ago",
        _ => $"{days / 365}y ago",
    };
}
