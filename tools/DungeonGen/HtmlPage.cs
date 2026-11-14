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
      body{margin:0;min-height:100vh;background:#000;color:#00FF00;font-family:"Courier New",Consolas,monospace;display:flex;flex-direction:column;align-items:center;justify-content:center;gap:16px;padding:24px;box-sizing:border-box}
      .wrap{width:100%;max-width:1200px}
      svg{width:100%;height:auto;display:block}
      .day{cursor:crosshair}
      .day:hover{filter:brightness(2.2) drop-shadow(0 0 3px #00FF00)}
      #tip{position:fixed;pointer-events:none;background:#001a06;border:1px solid #00FF00;color:#00FF00;padding:6px 10px;font-size:14px;border-radius:4px;box-shadow:0 0 12px #00FF0066;opacity:0;transition:opacity .1s;white-space:nowrap}
      a{color:#00a83a} a:hover{color:#00FF00}
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
        g.addEventListener('mousemove', e => {
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
