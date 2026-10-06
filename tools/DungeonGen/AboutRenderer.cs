using System.Text;
using static Svg;

// "> cat character.txt": character sheet, active effects (learning, seeking, ...) and equipped gear.
static class AboutRenderer
{
    const int W = 960;
    const int CharW = 8;   // approx. width of one 13px monospace character

    public static string Render(AboutConfig a)
    {
        var body = new StringBuilder();
        int top = 84;

        // --- Character sheet (left)
        int lx = 40, lw = 520;
        var bio = Wrap(a.Bio, 62);
        (string K, string V)[] rows = [("NAME", $"{a.Name} ({a.Handle})"), ("CLASS", a.Class), ("ORIGIN", a.Origin), ("SPECIALTY", a.Specialty)];
        int leftH = 40 + rows.Length * 24 + 12 + bio.Count * 18 + 14;

        // --- Active effects (right)
        int rx = 580, rw = W - 40 - rx;
        var effects = a.Effects.Select(e => (e, Lines: Wrap(e.Text, 38, 2))).ToList();
        int rightH = 40 + effects.Sum(e => 18 + e.Lines.Count * 16 + 8) + 4;

        int panelH = Math.Max(leftH, rightH);
        // spread the left column so it fills the same height as the effects panel (no dead space)
        int extra = panelH - leftH;
        int rowStep = 24 + Math.Min(10, extra / 2 / rows.Length), bioStep = 18 + Math.Min(4, extra / 4 / Math.Max(1, bio.Count));
        int bioTop = top + 48 + rows.Length * rowStep + 6;
        bioTop += Math.Max(0, (top + panelH - 20) - (bioTop + (bio.Count - 1) * bioStep)) / 2;
        body.Append(PanelBox(lx, top, lw, panelH, "CHARACTER SHEET"));
        for (int i = 0; i < rows.Length; i++)
        {
            int y = top + 48 + i * rowStep;
            body.Append($"""<text class="lbl" x="{lx + 14}" y="{y}" fill="{Label}">{rows[i].K}</text>""");
            body.Append($"""<text class="sm" x="{lx + 110}" y="{y}" fill="{Green}"{(i < 2 ? " filter=\"url(#glow)\"" : "")}>{Esc(Trim(rows[i].V, 50))}</text>""");
        }
        body.Append($"""<line x1="{lx + 14}" y1="{bioTop - 20}" x2="{lx + lw - 14}" y2="{bioTop - 20}" stroke="{Dim}" stroke-opacity=".35"/>""");
        for (int i = 0; i < bio.Count; i++)
            body.Append($"""<text class="sm" x="{lx + 14}" y="{bioTop + i * bioStep}" fill="{Dim}">{Esc(bio[i])}</text>""");

        body.Append(PanelBox(rx, top, rw, panelH, "ACTIVE EFFECTS"));
        int ey = top + 46;
        foreach (var (e, lines) in effects)
        {
            body.Append($"""<text class="lbl" x="{rx + 14}" y="{ey}" fill="#ffd84d" filter="url(#glow)">[{Esc(e.Tag)}]</text>""");
            body.Append($"""<text class="lbl" x="{rx + 44}" y="{ey}" fill="{Label}">{Esc(e.Label)}</text>""");
            for (int i = 0; i < lines.Count; i++)
                body.Append($"""<text class="sm" x="{rx + 44}" y="{ey + 17 + i * 16}" fill="{Green}">{Esc(lines[i])}</text>""");
            ey += 18 + lines.Count * 16 + 8;
        }

        // --- Equipped gear + skills as chips (full width)
        int gy = top + panelH + 18, gx = 40, gw = W - 80;
        var chips = new StringBuilder();
        int cy = gy + 40;
        cy = Chips(chips, "GEAR", a.Gear, gx + 14, cy, gx + gw - 14, Green, true);
        cy = Chips(chips, "SKILLS", a.Skills, gx + 14, cy + 8, gx + gw - 14, Dim, false);
        int gearH = cy - gy + 12;
        body.Append(PanelBox(gx, gy, gw, gearH, "EQUIPPED GEAR & SKILLS"));
        body.Append(chips);

        // --- Lead-in to the link buttons below
        int need = gy + gearH + 96;
        int h = (need + 39) / 40 * 40;              // round up to the 40px grid so slices line up
        int ly = h - 18 - (h - need) / 2;           // split the rounding slack above/below the prompt
        body.Append(Section(W, ly - 38, 5, "PARTY"));
        body.Append($"""<text class="ui" x="40" y="{ly}" fill="{Green}" filter="url(#glow)">&gt; ./connect --party</text>""");
        body.Append($"""<text class="lbl" x="{W - 40}" y="{ly}" fill="{Dim}" text-anchor="end">pick a channel</text>""");

        var sb = new StringBuilder(Open(W, h));
        sb.Append(Frame(W, h));
        sb.Append(Section(W, 28, 4, "CHARACTER"));
        sb.Append($"""<text class="ui" x="40" y="66" fill="{Green}" filter="url(#glow)">&gt; cat character.txt</text>""");
        sb.Append($"""<text class="lbl" x="{W - 40}" y="66" fill="{Dim}" text-anchor="end">PLAYER 1 · {Esc(a.Handle.ToUpperInvariant())}</text>""");
        sb.Append(body);
        sb.Append(Close(W, h));
        return sb.ToString();
    }

    static string PanelBox(int x, int y, int w, int h, string title) =>
        $"""<rect x="{x}" y="{y}" width="{w}" height="{h}" rx="4" fill="{Panel}" stroke="{Dim}" stroke-opacity=".7"/>""" +
        $"""<text class="lbl" x="{x + 14}" y="{y + 22}" fill="{Label}">{Esc(title)}</text>""";

    /// Flows chips left to right, wrapping to new rows. Returns the y below the last row.
    static int Chips(StringBuilder sb, string label, List<string> items, int x0, int y, int maxX, string color, bool glow)
    {
        sb.Append($"""<text class="lbl" x="{x0}" y="{y + 15}" fill="{Label}">{label}</text>""");
        int start = x0 + 64, x = start;
        foreach (var item in items)
        {
            int w = item.Length * CharW + 22;
            if (x + w > maxX) { x = start; y += 30; }
            sb.Append($"""<rect x="{x}" y="{y}" width="{w}" height="22" rx="3" fill="#000" stroke="{color}" stroke-opacity=".9"/>""");
            sb.Append($"""<text class="sm" x="{x + w / 2}" y="{y + 15}" fill="{color}" text-anchor="middle"{(glow ? " filter=\"url(#glow)\"" : "")}>{Esc(item)}</text>""");
            x += w + 8;
        }
        return y + 22;
    }
}
