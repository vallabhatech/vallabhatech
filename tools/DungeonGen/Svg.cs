using System.Net;

// Shared pieces so every slice of the console looks identical and lines up.
static class Svg
{
    public const string Green = "#00FF00", Dim = "#00a83a", Label = "#2f8f45", Panel = "#001a06";

    public static string Esc(string? s) => WebUtility.HtmlEncode(s ?? "");

    public static string Open(int w, int h) => $$"""
    <svg xmlns="http://www.w3.org/2000/svg" width="{{w}}" height="{{h}}" viewBox="0 0 {{w}} {{h}}">
    <defs>
      <filter id="glow" x="-50%" y="-50%" width="200%" height="200%"><feGaussianBlur stdDeviation="2" result="b"/><feMerge><feMergeNode in="b"/><feMergeNode in="SourceGraphic"/></feMerge></filter>
      <pattern id="grid" width="40" height="40" patternUnits="userSpaceOnUse"><path d="M40 0H0V40" fill="none" stroke="{{Green}}" stroke-opacity=".07"/></pattern>
      <pattern id="scan" width="4" height="4" patternUnits="userSpaceOnUse"><rect width="4" height="2" fill="#000" fill-opacity=".3"/></pattern>
    </defs>
    <style>
      text{font-family:"Courier New",Consolas,"DejaVu Sans Mono",monospace;font-weight:700;white-space:pre}
      .ui{font-size:15px} .lbl{font-size:12px} .sm{font-size:13px} .big{font-size:20px} .xl{font-size:22px}
      .flick{animation:f 7s infinite} @keyframes f{0%,95%,100%{opacity:1}96%{opacity:.6}97%{opacity:1} }
      .pulse{animation:p 1.6s ease-in-out infinite} @keyframes p{0%,100%{opacity:.35}50%{opacity:1} }
      .blink{animation:b 1s step-end infinite} @keyframes b{50%{opacity:0} }
    </style>
    <rect width="{{w}}" height="{{h}}" fill="#000"/>

    """;

    /// Background grid + neon side rails. A half-width slice only draws the rail on its outer side.
    public static string Frame(int w, int h, bool leftRail = true, bool rightRail = true)
    {
        int gx = leftRail ? 12 : 0, gw = w - gx - (rightRail ? 12 : 0);
        var s = $"""<rect x="{gx}" width="{gw}" height="{h}" fill="url(#grid)"/><g class="flick" filter="url(#glow)" pointer-events="none">""";
        // rects, not lines: a glow filter on a zero-width line has an empty filter region and vanishes
        if (leftRail) s += $"""<rect x="12.5" y="0" width="3" height="{h}" fill="{Green}"/>""";
        if (rightRail) s += $"""<rect x="{w - 15.5}" y="0" width="3" height="{h}" fill="{Green}"/>""";
        return s + "</g>\n";
    }

    /// Section divider: "[ 02 ] INVENTORY ────────── ◆" across the console, gives every section a clear start.
    public static string Section(int w, int y, int num, string title)
    {
        int tx = 40, tw = (title.Length + 7) * 8 + 16;
        return $"""<g pointer-events="none"><line x1="{tx + tw + 8}" y1="{y - 4}" x2="{w - 52}" y2="{y - 4}" stroke="{Dim}" stroke-opacity=".55" stroke-dasharray="6 4"/>""" +
               $"""<rect x="{w - 46}" y="{y - 9}" width="6" height="6" transform="rotate(45 {w - 43} {y - 6})" fill="{Green}" filter="url(#glow)"/>""" +
               $"""<rect x="{tx}" y="{y - 15}" width="{tw}" height="22" rx="3" fill="{Panel}" stroke="{Dim}"/>""" +
               $"""<text class="lbl" x="{tx + 10}" y="{y}" fill="{Green}" filter="url(#glow)">[ {num:00} ] <tspan fill="#eaffea">{Esc(title)}</tspan></text></g>""";
    }

    public static string Trim(string s, int max) => s.Length <= max ? s : s[..(max - 1)] + "…";

    /// Word-wraps text to lines of at most `width` characters (monospace font).
    public static List<string> Wrap(string text, int width, int maxLines = int.MaxValue)
    {
        var lines = new List<string>();
        var line = "";
        foreach (var word in text.Split(' ', StringSplitOptions.RemoveEmptyEntries))
        {
            var next = line.Length == 0 ? word : line + " " + word;
            if (next.Length > width && line.Length > 0) { lines.Add(line); line = word; }
            else line = next;
        }
        if (line.Length > 0) lines.Add(line);
        if (lines.Count > maxLines)
        {
            var rest = string.Join(" ", lines.Skip(maxLines - 1));
            lines = lines.Take(maxLines - 1).Append(Trim(rest, width)).ToList();
        }
        return lines.Select(l => Trim(l, width)).ToList();
    }

    public static string Close(int w, int h) =>
        $"""<rect width="{w}" height="{h}" fill="url(#scan)" pointer-events="none"/>""" + "\n</svg>\n";
}
