from pathlib import Path
from html.parser import HTMLParser
from urllib.parse import unquote
import base64, io
from PIL import Image

root=Path(__file__).resolve().parents[2]
class Check(HTMLParser):
    images=0
    links=0
    def handle_starttag(self,tag,attrs):
        a=dict(attrs)
        if tag=='img' and a.get('src','').startswith('data:'):
            Image.open(io.BytesIO(base64.b64decode(a['src'].split(',',1)[1]))).verify()
            assert a.get('alt')
            self.images+=1
        if tag=='a' and not a['href'].startswith('#'):
            assert (root/unquote(a['href'])).exists(), a['href']
            self.links+=1
c=Check();c.feed((root/'本周进度汇报_2026-10-21.html').read_text(encoding='utf-8'))
assert c.images==9 and c.links==6
print(f'PASS: {c.images} valid embedded images; {c.links} local deliverable links exist.')
