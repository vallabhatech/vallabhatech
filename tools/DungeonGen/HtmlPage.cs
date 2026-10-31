// Interactive dungeon page for GitHub Pages (hover any tile).
static class HtmlPage
{
    public static string Build(string user, string svg) => $$"""
    <!doctype html>
    <html lang="en">
    <head>
    <meta charset="utf-8">
    <meta name="viewport" content="width=device-width, initial-scale=1">
    <title>{{user}} :: Dungeon</title>
    <style>
      body{margin:0;min-height:100vh;background:#080A0F;color:#E8EDF2;font-family:Georgia,"Times New Roman",serif;display:flex;flex-direction:column;align-items:center;justify-content:center;gap:16px;padding:24px;box-sizing:border-box}
      .wrap{width:100%;max-width:1200px}
      svg{width:100%;height:auto;display:block}
      .day{cursor:pointer;outline:none}
      .day:hover,.day:focus{filter:brightness(1.35) drop-shadow(0 0 3px #63D8D0)}
      #tip{position:fixed;pointer-events:none;background:#111923;border:1px solid #63D8D0;color:#E8EDF2;padding:6px 10px;font-size:14px;border-radius:4px;box-shadow:0 0 12px #63D8D066;opacity:0;transition:opacity .1s;white-space:nowrap}
      a{color:#AAB5C4;text-decoration:none;border-bottom:1px solid #526A7B;padding:8px 12px} a:hover,a:focus-visible{color:#63D8D0;border-color:#63D8D0} a:focus-visible{outline:2px solid #8CB9F4;outline-offset:3px}
      @media (max-width:600px){body{padding:14px;gap:12px}.wrap{overflow-x:auto}#tip{font-size:12px;padding:8px}}
      @media (prefers-reduced-motion:reduce){*,*::before,*::after{animation:none!important;transition:none!important}}
    </style>
    </head>
    <body>
    <div class="wrap">{{svg}}</div>
    <a href="https://github.com/{{user}}">&lt; back to github.com/{{user}}</a>
    <div id="tip"></div>
    <script>
      const tip = document.getElementById('tip');
      document.querySelectorAll('.day').forEach(g => {
        g.querySelector('title')?.remove();               // use our styled tooltip instead
        g.setAttribute('tabindex', '0');\n        g.setAttribute('role', 'img');\n        g.addEventListener('focus', () => { tip.textContent = g.dataset.tip; tip.style.left = '16px'; tip.style.top = '16px'; tip.style.opacity = 1; });\n        g.addEventListener('blur', () => tip.style.opacity = 0);\n        g.addEventListener('mousemove', e => {
          tip.textContent = g.dataset.tip;
          tip.style.left = Math.min(e.clientX + 14, innerWidth - tip.offsetWidth - 8) + 'px';
          tip.style.top = (e.clientY - 40) + 'px';
          tip.style.opacity = 1;
        });
        g.addEventListener('mouseleave', () => tip.style.opacity = 0);
      });
    </script>
    </body>
    </html>
    """;
}
