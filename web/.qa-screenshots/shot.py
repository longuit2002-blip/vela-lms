# web/.qa-screenshots/shot.py — usage: python shot.py <route> <name>
import sys, time, os
from playwright.sync_api import sync_playwright
route, name = sys.argv[1], sys.argv[2]
OUT = os.path.dirname(os.path.abspath(__file__))
with sync_playwright() as p:
    b = p.chromium.launch()
    for w, tag in [(1440, "desktop"), (390, "mobile")]:
        pg = b.new_page(viewport={"width": w, "height": 900}, device_scale_factor=2)
        pg.goto(f"http://127.0.0.1:3100{route}", wait_until="networkidle", timeout=90000)
        time.sleep(1.5)
        pg.screenshot(path=os.path.join(OUT, f"{name}-{tag}.png"), full_page=True)
    b.close()
print("shot", name)
