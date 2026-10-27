"""Check that the entire original is represented, with no missing or altered pixels."""
from pathlib import Path
from PIL import Image, ImageChops
import hashlib
import json

root = Path(__file__).resolve().parents[1]
manifest = json.loads((root / 'assets/painting/source.json').read_text(encoding='utf-8'))
original = root / 'reference/Alongtheriver_QingMing_38414x1800.jpg'
assert hashlib.sha1(original.read_bytes()).hexdigest() == manifest['sha1']
source = Image.open(original).convert('RGB')
assert source.size == (38414, 1800)
cursor = 0
for info in manifest['tiles']:
    assert info['x'] == cursor
    path = root / 'assets/painting' / info['file']
    tile = Image.open(path).convert('RGB')
    assert tile.size == (info['width'], 1800)
    expected = source.crop((cursor, 0, cursor + tile.width, 1800))
    assert ImageChops.difference(tile, expected).getbbox() is None, path.name
    settings = path.with_suffix('.png.import').read_text(encoding='utf-8')
    assert 'compress/mode=0' in settings and 'mipmaps/generate=true' in settings
    cursor += tile.width
assert cursor == source.width and len(manifest['tiles']) == 19
print('PAINTING_ASSETS_PASS: SHA-1, 38,414 x 1,800; all 19 tiles pixel-identical; complete width, lossless imports, mipmaps')
