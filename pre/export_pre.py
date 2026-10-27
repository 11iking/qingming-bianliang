"""Build a portable, offline, single-file presentation from the editable source."""
from pathlib import Path
import ast
import base64
import json
import mimetypes
import re
from zipfile import ZipFile, ZIP_DEFLATED

root = Path(__file__).resolve().parent
source = (root / 'index.html').read_text(encoding='utf-8')
files = sorted({p for p in (root / 'assets').iterdir() if p.suffix in ('.jpg', '.mp4', '.woff2')})
data = {}
for path in files:
    mime = {'.woff2': 'font/woff2', '.jpg': 'image/jpeg', '.mp4': 'video/mp4'}[path.suffix]
    data[path.name] = 'data:' + mime + ';base64,' + base64.b64encode(path.read_bytes()).decode('ascii')

# Static image, video and CSS font references are embedded in their original slots.
html = source
for name, uri in data.items():
    html = html.replace('assets/' + name, uri)
# Switched images use the same map; they must not fall back to a missing assets folder.
image_map = {name: uri for name, uri in data.items() if name.endswith('.jpg')}
html = html.replace("'use strict';", "'use strict';\nconst BUNDLED=" + json.dumps(image_map, separators=(',', ':')) + ';', 1)
html = html.replace("'assets/'+d[4]", 'BUNDLED[d[4]]').replace("'assets/'+d[0]", 'BUNDLED[d[0]]')
assert "'assets/'+" not in html
assert not re.search(r'(?:src|poster)=[\"\']assets/', html)
licenses = '\n\n'.join(p.read_text(encoding='utf-8-sig') for p in (root / 'assets').glob('*.txt'))
html = html.replace('</body>', '<!-- Bundled font licenses\n' + licenses.replace('--', '—') + '\n-->\n</body>')
output = root / '汴梁一日_PRE.html'
output.write_text(html, encoding='utf-8')

notes = ast.literal_eval(re.search(r'const notes=(\[[\s\S]*?\]);', source).group(1))
titles = re.findall(r'data-title="([^"]+)" data-time="([^"]+)"', source)
assert len(notes) == len(titles) == 15
text = '汴梁一日 · 空间计算与交互设计\n第一版逐页讲稿 · 约 8–10 分钟\n\n'
for i, ((title, duration), note) in enumerate(zip(titles, notes), 1):
    text += f'{i:02d}　{title}（建议 {duration}）\n{note}\n\n'
(root / '逐页讲稿.txt').write_text(text, encoding='utf-8-sig')
readme = '''汴梁一日 · HTML 演示第一版

双击“汴梁一日_PRE.html”，用 Edge / Chrome 等桌面浏览器打开。
这是独立文件，图片、字体和实机视频均已内置，离线也能演示。
建议使用电脑横屏和投影；15 页，约 8–10 分钟，最后一页是答疑附录。

操作：
方向键 / 空格 / PageUp、PageDown：翻页。
F：全屏（也可使用浏览器 F11）。
O：目录，点击跳转。
N：本页讲稿。Esc：关闭讲稿或目录。
Home / End：第一页 / 最后一页。

可操作页面：
第 2 页：左右拖动完整原画。
第 3 页：选择阿安一天的不同章节。
第 4 页：比较亲历、对坐和观卷。
第 5 页：拖动滑杆解释头部姿态映射。
第 6 页：把梨拖进竹篮；选中梨后按回车也可完成。
第 7 页：倾角超过阈值并对准杯口后斟酒，松手即停。
第 10 页：演示角色淡出换位。
第 11 页：点击播放 18.7 秒真实 Godot Demo 录屏。

逐页讲稿.txt 可以单独打印，或在演示中按 N 查看。
index.html + assets 是可编辑源文件；export_pre.py 用于重新生成独立版。

内容边界：目前完成的是 Godot 键鼠桌面原型。
VR 的 OpenXR、6DoF 头手追踪、触觉与空间UI属于后续设计方案。
HTML 的拿梨、斟酒与角色换位为演示逻辑示意，没有连接头显。
原画出处与 VR 官方技术链接位于第 15 页。
'''
(root / '展示使用说明.txt').write_text(readme, encoding='utf-8-sig')
archive = root / '汴梁一日_PRE第一版.zip'
with ZipFile(archive, 'w', ZIP_DEFLATED, compresslevel=6) as z:
    for path in [output, root / '逐页讲稿.txt', root / '展示使用说明.txt']:
        z.write(path, path.name)
with ZipFile(archive) as z:
    assert z.testzip() is None
print(f'PRE_EXPORT_PASS: 15 slides; offline HTML {output.stat().st_size:,} bytes; ZIP {archive.stat().st_size:,} bytes')
