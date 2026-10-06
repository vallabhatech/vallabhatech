using System.Text;
using static Svg;

// Link buttons. A README image can only link as a whole, so each button is its own SVG,
// placed side by side. Only the first/last button draws the console's side rail.
static class ConnectRenderer
{
    const int H = 80;

    public static int Width(int count) => 960 / Math.Max(1, count);

    public static string Render(LinkButton link, int index, int count)
    {
        int w = Width(count);
        bool first = index == 0, last = index == count - 1;
        // equal-width buttons with equal gaps across the row: 40px outer insets, and every button
        // still fits inside its own image with room for its glow
        double bw = w - 56, gap = count > 1 ? (960 - 80 - count * bw) / (count - 1) : 0;
        double bx = 40 + index * (bw + gap) - index * w;
        int by = 14, bh = 44;
        double delay = index * 0.4;   // staggered pulse so the row "breathes" left to right

        var sb = new StringBuilder(Open(w, H));
        sb.Append(Frame(w, H, leftRail: first, rightRail: last));
        sb.Append($$"""
        <filter id="bg" x="-30%" y="-80%" width="160%" height="260%"><feGaussianBlur stdDeviation="6"/></filter>
        <rect class="pulse" style="animation-delay:{{delay:0.0}}s" x="{{bx:0.##}}" y="{{by}}" width="{{bw:0.##}}" height="{{bh}}" rx="6" fill="{{Green}}" fill-opacity=".45" filter="url(#bg)"/>
        <rect x="{{bx:0.##}}" y="{{by}}" width="{{bw:0.##}}" height="{{bh}}" rx="6" fill="#002a0b" stroke="{{Green}}" stroke-width="2"/>
        <rect x="{{bx + 12:0.##}}" y="{{by + 10}}" width="26" height="24" rx="3" fill="#000" stroke="{{Green}}"/>
        <rect x="{{bx + 12:0.##}}" y="{{by + 30}}" width="26" height="4" fill="{{Green}}" fill-opacity=".5"/>
        <text class="sm" x="{{bx + 25:0.##}}" y="{{by + 27}}" fill="{{Green}}" text-anchor="middle" filter="url(#glow)">{{Esc(link.Key)}}</text>
        <text class="sm" x="{{bx + 50:0.##}}" y="{{by + 27}}" fill="#eaffea" filter="url(#glow)">{{Esc(Trim(link.Label, (int)(bw - 60) / 8))}}</text>

        """);
        sb.Append(Close(w, H));
        return sb.ToString();
    }
}
