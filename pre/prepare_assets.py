"""Prepare offline presentation media from the project's existing source files."""
from pathlib import Path
import json
import shutil
import subprocess
from PIL import Image
from fontTools import subset
from fontTools.ttLib import TTFont
import fontTools.ttLib.woff2 as woff2

ROOT = Path(__file__).resolve().parents[1]
OUT = ROOT / "pre" / "assets"
OUT.mkdir(parents=True, exist_ok=True)
NODE = shutil.which("node")


class NodeBrotli:
    """Use Node's bundled Brotli implementation when Python lacks the binding."""
    MODE_FONT = 2
    MODE_TEXT = 1

    @staticmethod
    def compress(data, mode=0):
        js = "const fs=require('fs'),z=require('zlib');process.stdout.write(z.brotliCompressSync(fs.readFileSync(0),{params:{[z.constants.BROTLI_PARAM_MODE]:Number(process.argv[1]),[z.constants.BROTLI_PARAM_QUALITY]:11}}));"
        return subprocess.run([str(NODE), "-e", js, str(mode)], input=data, capture_output=True, check=True).stdout

    @staticmethod
    def decompress(data):
        js = "const fs=require('fs'),z=require('zlib');process.stdout.write(z.brotliDecompressSync(fs.readFileSync(0)));"
        return subprocess.run([str(NODE), "-e", js], input=data, capture_output=True, check=True).stdout


def jpeg(source, dest, width, quality=86, box=None):
    with Image.open(source) as im:
        im = im.convert("RGB")
        if box:
            im = im.crop(box)
        h = round(im.height * width / im.width)
        im = im.resize((width, h), Image.Resampling.LANCZOS)
        im.save(OUT / dest, "JPEG", quality=quality, optimize=True, progressive=True)
        print(dest, im.size, (OUT / dest).stat().st_size, flush=True)


original = ROOT / "reference/Alongtheriver_QingMing_38414x1800.jpg"
jpeg(original, "painting-full.jpg", 12000, quality=90)
jpeg(original, "painting-bridge.jpg", 2000, quality=90, box=(16800, 0, 22300, 1800))

screens = {
    "game-panorama.jpg": "14_scroll_start.png",
    "game-bargain.jpg": "03_bargain.png",
    "game-bridge.jpg": "04_bridge.png",
    "game-boat.jpg": "05_boat.png",
    "game-tavern-first.jpg": "06_tavern.png",
    "game-tavern-second.jpg": "07_second_person.png",
    "game-theatre.jpg": "08_theatre.png",
    "game-summary.jpg": "09_summary.png",
}
for dest, source in screens.items():
    jpeg(ROOT / "captures/release_v4" / source, dest, 1440)
shutil.copy2(ROOT / "captures/v4/画卷展开与交互.mp4", OUT / "demo.mp4")

if not woff2.haveBrotli:
    if not NODE:
        raise RuntimeError("Install Python Brotli first: python3 -m pip install brotli")
    woff2.brotli = NodeBrotli
    woff2.haveBrotli = True

# Keep common Chinese, ASCII, punctuation, presentation symbols and fullwidth forms.
unicodes = set(range(0x20, 0x7F)) | set(range(0x2000, 0x2070)) | set(range(0x2190, 0x2600)) | set(range(0x3000, 0x3040)) | set(range(0x3400, 0xA000)) | set(range(0xFF00, 0xFFF0))
for family in ("NotoSansSC", "NotoSerifSC"):
    font = TTFont(ROOT / "assets/fonts" / (family + ".ttf"))
    opts = subset.Options()
    opts.layout_features = ["*"]
    opts.name_IDs = ["*"]
    opts.name_legacy = True
    opts.name_languages = ["*"]
    sub = subset.Subsetter(options=opts)
    sub.populate(unicodes=unicodes)
    sub.subset(font)
    font.flavor = "woff2"
    target = OUT / (family + ".woff2")
    font.save(target)
    # Decode the actual output once to verify the Brotli stream and WOFF2 tables.
    check = TTFont(target)
    assert ord("清") in check.getBestCmap()
    print(target.name, target.stat().st_size, flush=True)
shutil.copy2(ROOT / "assets/fonts/OFL.txt", OUT / "OFL.txt")
for source in ROOT.joinpath("assets/fonts").glob("*-copyright.txt"):
    shutil.copy2(source, OUT / source.name)

metadata = {
    "original": {
        "work": "张择端《清明上河图》原作画心",
        "source_file": "reference/Alongtheriver_QingMing_38414x1800.jpg",
        "source_pixels": [38414, 1800],
        "source_url": "https://commons.wikimedia.org/wiki/File:Alongtheriver_QingMing.jpg",
        "rights": "Public domain faithful reproduction; see the Wikimedia Commons source page.",
        "painting-full.jpg": "完整画心等比缩小，无裁切、无 AI 改绘",
        "painting-bridge.jpg": {"description": "真实原画虹桥区域裁图", "crop_xyxy": [16800, 0, 22300, 1800]},
    },
    "screenshots": {dest: "captures/release_v4/" + source for dest, source in screens.items()},
    "video": {"demo.mp4": "captures/v4/画卷展开与交互.mp4；Godot v4 实机录屏"},
    "fonts": "Noto Sans SC / Noto Serif SC, SIL Open Font License 1.1; locally embedded WOFF2 subsets.",
}
(OUT / "source.json").write_text(json.dumps(metadata, ensure_ascii=False, indent=2), encoding="utf-8")
total = sum(p.stat().st_size for p in OUT.iterdir() if p.is_file())
print("TOTAL", total, "bytes", round(total / 1024**2, 2), "MiB", flush=True)
