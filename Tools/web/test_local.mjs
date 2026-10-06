// Serves a packaged web build locally (wrapped in the same skeleton the Artifact host adds)
// and opens it in headless Chromium; waits for the game to boot and saves a screenshot.
// Usage: node Tools/web/test_local.mjs <packaged dir> <screenshot.png>
import http from "node:http";
import fs from "node:fs";
import path from "node:path";
import { createRequire } from "node:module";
const require = createRequire(import.meta.url);
const { chromium } = require(process.env.PLAYWRIGHT_MODULE || "playwright");

const dir = path.resolve(process.argv[2] || "Builds/WebArtifact");
const shot = process.argv[3] || "web_test.png";
const types = { ".html": "text/html", ".js": "application/javascript", ".gz": "application/octet-stream", ".txt": "text/plain", ".png": "image/png" };
const server = http.createServer((req, res) => {
  let p = decodeURIComponent(req.url.split("?")[0]);
  if (p === "/") {
    const page = fs.readFileSync(path.join(dir, "index.html"), "utf8");
    res.writeHead(200, { "Content-Type": "text/html" });
    res.end('<!doctype html><html><head><meta charset="utf-8"><meta name="viewport" content="width=device-width, initial-scale=1, viewport-fit=cover"><style>:root{color-scheme:light;padding-top:env(safe-area-inset-top,0px);padding-bottom:env(safe-area-inset-bottom,0px)}body{margin:0;font:14px system-ui}img{max-width:100%}[hidden]{display:none!important}</style></head><body>' + page + "</body></html>");
    return;
  }
  const f = path.join(dir, p);
  if (!f.startsWith(dir) || !fs.existsSync(f)) { res.writeHead(404); res.end(); return; }
  res.writeHead(200, { "Content-Type": types[path.extname(f)] || "application/octet-stream" });
  fs.createReadStream(f).pipe(res);
});
await new Promise(r => server.listen(8765, r));
const browser = await chromium.launch({ args: ["--use-angle=swiftshader", "--enable-unsafe-swiftshader", "--ignore-gpu-blocklist"] });
const page = await browser.newPage({ viewport: { width: 412, height: 892 }, deviceScaleFactor: 1, isMobile: true, hasTouch: true });
const logs = [];
page.on("console", m => logs.push(`[${m.type()}] ${m.text()}`));
page.on("pageerror", e => logs.push(`[pageerror] ${e.message}`));
await page.goto("http://127.0.0.1:8765/");
const t0 = Date.now();
let state = "timeout";
while (Date.now() - t0 < 900000) {
  state = await page.evaluate(() => {
    if (!document.getElementById("error").hidden) return "error: " + document.getElementById("error").textContent;
    if (document.getElementById("boot").classList.contains("done")) return "ready";
    return "loading: " + document.getElementById("status").textContent;
  });
  if (state === "ready" || state.startsWith("error")) break;
  await new Promise(r => setTimeout(r, 2000));
}
console.log("state after", Math.round((Date.now() - t0) / 1000), "s:", state);
if (state === "ready") await new Promise(r => setTimeout(r, 20000));
await page.screenshot({ path: shot });
if (state === "ready" && process.argv[4] === "play") {
  // Tap NEW RUN, then TEE OFF, then drag a shot back and release, like a finger would.
  const box = await page.evaluate(() => { const r = document.querySelector(".frame").getBoundingClientRect(); return { x: r.x, y: r.y, w: r.width, h: r.height }; });
  const at = (u, v) => [box.x + u * box.w, box.y + v * box.h];
  const tap = async (u, v) => { const [x, y] = at(u, v); await page.mouse.move(x, y); await page.mouse.down(); await page.waitForTimeout(80); await page.mouse.up(); };
  await tap(0.5, 0.832);
  await page.waitForTimeout(6000);
  await page.screenshot({ path: shot.replace(".png", "_intro.png") });
  await tap(0.5, 0.835);
  await page.waitForTimeout(5000);
  const [sx, sy] = at(0.55, 0.42);
  await page.mouse.move(sx, sy);
  await page.mouse.down();
  for (let i = 1; i <= 15; i++) { await page.mouse.move(sx - i * 1.2, sy + i * 7); await page.waitForTimeout(40); }
  await page.waitForTimeout(500);
  await page.screenshot({ path: shot.replace(".png", "_aim.png") });
  await page.mouse.up();
  await page.waitForTimeout(2500);
  await page.screenshot({ path: shot.replace(".png", "_shot.png") });
  await page.waitForTimeout(9000);
  await page.screenshot({ path: shot.replace(".png", "_rest.png") });
}
console.log(logs.filter(l => !l.includes("[debug]")).slice(-40).join("\n"));
await browser.close();
server.close();
