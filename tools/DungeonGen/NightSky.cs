using System.Text;
using static Svg;

// The surface above the dungeon: twinkling stars, a crescent moon, a shooting star,
// an airship drifting across, and a castle on the hill with the dungeon entrance.
// Everything is seeded/static, so the image only changes when the data changes.
static class NightSky
{
    const int Top = 56, Bottom = 140, Left = 17;

    public static string Render(int w)
    {
        int right = w - 17, h = Bottom - Top;
        var rng = new Random(2077);
        var sb = new StringBuilder();

        sb.Append($$"""
        <defs>
          <linearGradient id="skyG" x1="0" y1="0" x2="0" y2="1"><stop offset="0" stop-color="#00040a"/><stop offset="1" stop-color="#021a0c"/></linearGradient>
          <radialGradient id="moonG"><stop offset=".55" stop-color="#d9ffe0" stop-opacity=".35"/><stop offset="1" stop-color="#d9ffe0" stop-opacity="0"/></radialGradient>
          <linearGradient id="trail" x1="0" x2="1"><stop offset="0" stop-color="#fff" stop-opacity="0"/><stop offset="1" stop-color="#fff"/></linearGradient>
          <clipPath id="skyClip"><rect x="{{Left}}" y="{{Top}}" width="{{right - Left}}" height="{{h}}"/></clipPath>
        </defs>
        <style>
          .tw{animation:tw 3s ease-in-out infinite} @keyframes tw{0%,100%{opacity:1}50%{opacity:.15} }
          .shoot{animation:shoot 9s linear infinite;opacity:0} @keyframes shoot{0%{transform:translate(0,0);opacity:0}2%{opacity:1}9%{transform:translate(260px,46px);opacity:0}100%{transform:translate(260px,46px);opacity:0} }
          .ship{animation:ship 30s linear infinite} @keyframes ship{from{transform:translateX(-80px)}to{transform:translateX({{w + 40}}px)} }
          .bob2{animation:bob2 2s ease-in-out infinite} @keyframes bob2{0%,100%{transform:translateY(0)}50%{transform:translateY(2px)} }
          .lamp{animation:lamp 1s step-end infinite} @keyframes lamp{50%{opacity:0} }
          .win{animation:win 4s infinite} @keyframes win{0%,90%,100%{opacity:1}93%{opacity:.4} }
        </style>
        <g clip-path="url(#skyClip)" pointer-events="none">
          <rect x="{{Left}}" y="{{Top}}" width="{{right - Left}}" height="{{h}}" fill="url(#skyG)"/>

        """);

        // Stars: mostly tiny, a few bright crosses, each twinkling on its own schedule
        for (int i = 0; i < 70; i++)
        {
            int x = rng.Next(Left + 4, right - 4), y = rng.Next(Top + 4, Bottom - 30);
            double delay = rng.NextDouble() * 3;
            if (rng.NextDouble() < 0.12)
                sb.Append($"""<g class="tw" style="animation-delay:{delay:0.0}s" fill="#eaffea"><rect x="{x - 2}" y="{y}" width="5" height="1"/><rect x="{x}" y="{y - 2}" width="1" height="5"/></g>""");
            else
                sb.Append($"""<rect class="tw" style="animation-delay:{delay:0.0}s" x="{x}" y="{y}" width="{(rng.NextDouble() < .3 ? 2 : 1)}" height="{(rng.NextDouble() < .3 ? 2 : 1)}" fill="{(rng.NextDouble() < .2 ? "#9fffb0" : "#ffffff")}" opacity=".85"/>""");
        }

        // Crescent moon with a soft halo
        int mx = right - 90, my = Top + 26;
        sb.Append($"""<circle cx="{mx}" cy="{my}" r="34" fill="url(#moonG)"/><circle cx="{mx}" cy="{my}" r="14" fill="#e8ffe8" filter="url(#glow)"/><circle cx="{mx + 9}" cy="{my - 5}" r="11" fill="#00060c"/>""");

        // Shooting star
        sb.Append($"""<g class="shoot"><line x1="{Left + 180}" y1="{Top + 8}" x2="{Left + 230}" y2="{Top + 17}" stroke="url(#trail)" stroke-width="2"/></g>""");

        // Airship drifting across (with blinking nav lights)
        sb.Append($"""
        <g class="ship"><g transform="translate(0 {Top + 22})"><g class="bob2">
          <rect x="4" y="0" width="34" height="9" rx="4" fill="#2f8f45"/><rect x="8" y="2" width="26" height="2" fill="#7dff8f" opacity=".6"/>
          <rect x="16" y="10" width="12" height="5" fill="#0f4d1a"/><rect x="18" y="11" width="2" height="2" fill="#ffd84d"/><rect x="23" y="11" width="2" height="2" fill="#ffd84d"/>
          <path d="M0 2 l5 2 -5 2z" fill="#2f8f45"/>
          <rect class="lamp" x="39" y="3" width="2" height="2" fill="#ff4d4d"/><rect class="lamp" style="animation-delay:.5s" x="3" y="8" width="2" height="2" fill="#7dff8f"/>
        </g></g></g>

        """);

        // Hills + castle + dungeon entrance
        int g = Bottom;
        sb.Append($"""<path d="M{Left} {g} L{Left} {g - 14} Q{Left + 120} {g - 30} {Left + 240} {g - 16} T{Left + 480} {g - 12} T{Left + 720} {g - 18} T{right} {g - 10} L{right} {g} Z" fill="#031a0a"/>""");
        sb.Append($"""<path d="M{Left} {g - 14} Q{Left + 120} {g - 30} {Left + 240} {g - 16} T{Left + 480} {g - 12} T{Left + 720} {g - 18} T{right} {g - 10}" fill="none" stroke="{Dim}" stroke-opacity=".7"/>""");

        int cx = Left + 70, cb = g - 22;   // castle base
        sb.Append($"""
        <g fill="#062b10" stroke="{Dim}" stroke-opacity=".6">
          <rect x="{cx}" y="{cb - 30}" width="16" height="30"/><rect x="{cx + 16}" y="{cb - 18}" width="34" height="18"/><rect x="{cx + 50}" y="{cb - 38}" width="14" height="38"/>
          <path d="M{cx - 2} {cb - 30} h4 v-4 h4 v4 h4 v-4 h4 v4 h2" fill="none"/>
          <path d="M{cx + 48} {cb - 38} h4 v-4 h4 v4 h4 v-4 h4" fill="none"/>
          <path d="M{cx + 57} {cb - 38} v-10" fill="none"/>
        </g>
        <path d="M{cx + 57} {cb - 48} l9 3 -9 3z" fill="#ffd84d"/>
        <rect class="win" x="{cx + 6}" y="{cb - 22}" width="4" height="5" fill="#ffd84d"/>
        <rect class="win" style="animation-delay:1.3s" x="{cx + 54}" y="{cb - 28}" width="4" height="5" fill="#ffd84d"/>
        <path d="M{cx + 27} {cb} v-9 a6 6 0 0 1 12 0 v9z" fill="#000"/>

        """);

        // Stairs down into the dungeon, with a sign
        int ex = Left + 300;
        sb.Append($"""
        <path d="M{ex} {g} v-12 a10 10 0 0 1 20 0 v12z" fill="#000" stroke="{Dim}" stroke-opacity=".7"/>
        <rect class="win" x="{ex - 8}" y="{g - 22}" width="3" height="6" fill="#ffae2b" filter="url(#glow)"/>
        <rect x="{ex + 26}" y="{g - 24}" width="2" height="12" fill="#0f4d1a"/>
        <text class="lbl" x="{ex + 32}" y="{g - 16}" fill="{Label}" style="font-size:10px">DUNGEON ▼</text>

        """);

        sb.Append("</g>\n");
        return sb.ToString();
    }
}
