from pathlib import Path
import shutil, hashlib, json, re, subprocess

ROOT=Path(__file__).resolve().parents[2]
DEST=ROOT/'GitHub发布'/'qingming-bianliang'
if DEST.exists(): raise SystemExit('Publish directory already exists; inspect it instead of overwriting.')
DEST.mkdir(parents=True)
SKIP={'.git','.venv','__pycache__','node_modules','.pytest_cache','.mypy_cache','.ruff_cache'}
OMIT={'.log','.pyc','.pyo','.tmp','.zip','.exe','.msi','.pdb','.rsp'}
records=[]
def sha(p):
 h=hashlib.sha256()
 with p.open('rb') as f:
  for b in iter(lambda:f.read(1048576),b''):h.update(b)
 return h.hexdigest()
def copy(f,rel=None):
 rel=Path(rel) if rel else f.relative_to(ROOT)
 if f.is_symlink():raise RuntimeError(f'Symlink: {rel}')
 if any(p in SKIP for p in rel.parts) or f.suffix.lower() in OMIT or f.name in {'.env','.DS_Store','credentials.json'}:return
 d=DEST/rel;d.parent.mkdir(parents=True,exist_ok=True);shutil.copy2(f,d)
 a=sha(f);assert a==sha(d),str(rel)
 records.append({'path':rel.as_posix(),'sha256':a,'bytes':d.stat().st_size})
for folder in ['UnityProject/Assets','UnityProject/Packages','UnityProject/ProjectSettings',
               'assets','scenes','scripts','tests','reference','pre','剧本','美术',
               'tools/unity-mcp/MCPForUnity','tools/unity-mcp/Server','tools/unity-mcp/docs',
               'tools/unity-port/before-aan-story','tools/unity-port/before-hands-on',
               'tools/mac-handoff',
               'captures/aan-story','captures/hands-on','captures/portraits','captures/street-environment','captures/unity']:
 for f in sorted((ROOT/folder).rglob('*')):
  if f.is_file():copy(f)
for f in ROOT.iterdir():
 if f.is_file() and f.name!='.gitignore':copy(f)
for f in (ROOT/'tools/unity-port').iterdir():
 if f.is_file() and (f.suffix in {'.py','.ps1','.gd'} or f.name in {'AssetValidation.json','UnityRuntimeTest.txt','RoslynDependencies.json','refresh.json','rebuild.json'}):copy(f)
for f in (ROOT/'tools/reports').glob('*.py'):copy(f)
copy(ROOT/'.tools/pre_check.js',Path('tools/legacy/pre_check.js'))
for name in ['README.md','LICENSE'] :copy(ROOT/'tools/unity-mcp'/name)
revision=subprocess.check_output(['git','-C',str(ROOT/'tools/unity-mcp'),'rev-parse','HEAD'],text=True).strip()
(DEST/'tools/unity-mcp/UPSTREAM.txt').write_text('Source: https://github.com/CoplayDev/unity-mcp\nCommit: '+revision+'\nVendored editor package, server and documentation. Nested Git metadata and virtual environments excluded.\n',encoding='utf-8')
(DEST/'.gitignore').write_text('''# Generated engine caches and local state
.godot/
[Ll]ibrary/
[Tt]emp/
[Oo]bj/
[Ll]ogs/
[Uu]ser[Ss]ettings/
.vs/
.idea/
.venv/
venv/
__pycache__/
*.py[cod]
node_modules/
.pytest_cache/
.mypy_cache/
.ruff_cache/
.DS_Store
Thumbs.db
*.tmp
*.log
*.pdb
*.rsp
# Installers, compiled demos and duplicate handoff archives
/build/
/build-unity/
/.tools/
/交付包/
/Mac迁移包/
/Mac开发交接/
/GitHub发布/
/tools/reports/qa/
/tools/unity-setup/
*.zip
*.exe
*.msi
# Personal authentication/signing material
.env
.env.*
!.env.example
*.p12
*.pfx
*.mobileprovision
*.key
*.pem
credentials.json
''',encoding='utf-8')
(DEST/'.gitattributes').write_text('''* text=auto
*.command text eol=lf
*.sh text eol=lf
*.cmd text eol=crlf
*.png binary
*.jpg binary
*.jpeg binary
*.mp4 binary
*.ttf binary
*.woff2 binary
*.dll binary
*.bytes binary
''',encoding='utf-8')
readme='''# 清明上河图 · 汴梁一日

基于《清明上河图》的市井生活体验 Demo。玩家扮演卖梨的阿安，经历进货、买卖、送货、回访和夜归对账。

## 当前内容

- Unity / 团结引擎主工程，以及原 Godot 工程。
- 十四场连续故事、四类结局、钱货账目与人物约定。
- 拿取、翻面验货、分类、数钱、提篓步行送货和人物接收/拒收反馈。
- 画卷进入三维的色调过渡、细化街道与河岸环境。
- 十三位人物的原创淡彩立绘及对话界面。
- Unity MCP 编辑器包、服务端源码、历史研究和迁移工具。

![人物对话](captures/portraits/00_人物对话.png)

## 打开工程

### 当前可复现基线

Unity 工程：`UnityProject`，主场景：`Assets/Scenes/Qingming.unity`。
原验证编辑器为团结引擎 1.10.4（内部版本 2022.3.62t16），验证平台为 Windows。
在 Hub 中添加 `UnityProject`，等待首次导入后打开主场景并播放。

请保留 `UnityProject` 与 `tools` 的相对位置：MCP 是相对路径本地包依赖。
首次导入需要下载包依赖、重建缓存。编译后的 Windows 程序和安装器不在源码仓库内。
编辑器菜单 `Qingming > Build Windows Demo` 可重新构建 Windows 版。

原 Godot 工程入口为根目录 `project.godot`。该版本用于参考，最新剧情与立绘在 Unity 工程中。

### M4 Max / Mac 与 Vision Pro

最终目标是 Apple Vision Pro；目前仓库是桌面体验基线，尚未通过 Mac 或 visionOS 实机验证。
Apple silicon Mac 开发交接说明及头显改造清单在 `tools/mac-handoff/`。
Windows 上执行 `python tools/mac-handoff/package_mac.py` 可生成带完整性校验的 Mac 开发交接包；不要直接把本机 Windows 缓存或虚拟环境复制到 Mac。
Mac 打开步骤、版本标记调整和原配置备份均在交接说明中明确记录。
Vision Pro 仍需改造 XR 相机、注视/捏合输入、空间 UI，并完成对应渲染路线及真机验证。

## 主要目录

| 目录 | 内容 |
|---|---|
| `UnityProject/Assets` | 最新场景、脚本、材质、字体、原画分块与人物立绘 |
| `tools/unity-mcp` | 上游 MCP 代码副本，版本见 `UPSTREAM.txt` |
| `tools/unity-port` | 场景导出、依赖准备与资源验证工具 |
| `tools/mac-handoff` | Mac 打包工具、启动脚本和 Vision Pro 开发清单 |
| `scripts`、`scenes`、`assets` | 原 Godot Demo 代码与素材 |
| `剧本`、`美术`、`captures`、`pre` | 剧本研究、人物图鉴、截图与已有展示材料 |

## 操作与验证

WASD 走动，右键环顾；E 操作/确认，F 展开对话，R 翻面，Q 放回；Tab 账本，Esc 暂停。
详细规则见 [故事版体验说明](阿安故事版体验说明.txt)。
2026-10-21 的 Windows 验证记录覆盖十四场、四类结局、12 条定向路线和 240 条固定种子路线，另有实物交互和立绘资源检查。
结果见 `tools/unity-port/UnityRuntimeTest.txt`。这些结果不代表已通过 Mac 或 Vision Pro 测试。

## 依赖与来源

第三方素材来源见 [THIRD_PARTY_NOTICES.txt](THIRD_PARTY_NOTICES.txt)，字体许可在 `assets/fonts`，原画来源在 `assets/painting/source.json`。
MCP 采用上游 MIT 许可，原文保存在 `tools/unity-mcp/LICENSE`。本仓库没有为原创项目内容额外指定开源许可。

本仓库不包含编辑器、安装器、缓存、账号凭据、旧压缩包和生成后的应用。源码文件复制校验记录见 `SOURCE_SNAPSHOT.json`。
'''
(DEST/'README.md').write_text(readme,encoding='utf-8')
# One Windows packaging helper expects this previous Godot Mac launcher.
assert (DEST/'在Mac上开发.command').exists()
assert (DEST/'tools/unity-mcp/MCPForUnity/package.json').exists()
assert len(list((DEST/'UnityProject/Assets/Resources/Portraits').glob('*.png')))==13
too_large=[r for r in records if r['bytes']>=100*1024*1024]
assert not too_large, 'Files exceed GitHub per-file limit: '+str([r['path'] for r in too_large])
patterns=[re.compile(rb'gh[pousr]_[A-Za-z0-9]{30,}'),re.compile(rb'github_pat_[A-Za-z0-9_]{40,}'),re.compile(rb'-----BEGIN (?:RSA |OPENSSH |EC )?PRIVATE KEY-----')]
hits=[]
for f in DEST.rglob('*'):
 if f.is_file() and f.suffix.lower() not in {'.png','.jpg','.jpeg','.mp4','.ttf','.woff2','.dll','.bytes'}:
  data=f.read_bytes()
  if any(p.search(data) for p in patterns):hits.append(f.relative_to(DEST).as_posix())
if hits:raise RuntimeError('Review credential-like content in: '+str(hits))
(DEST/'SOURCE_SNAPSHOT.json').write_text(json.dumps({'source_date':'2026-10-27','verified':'Source copies SHA-256 match; no file >=100 MiB; 13 portraits; local MCP source included. Runtime not re-tested.','files':records},ensure_ascii=False,indent=2)+'\n',encoding='utf-8')
print(json.dumps({'destination':str(DEST),'copied_files':len(records),'total_MiB':round(sum(r['bytes'] for r in records)/1048576,1),'largest_MiB':round(max(r['bytes'] for r in records)/1048576,1),'mcp_revision':revision},ensure_ascii=True))
