from pathlib import Path
import hashlib
import json
import shutil
import stat
import zipfile

ROOT = Path(__file__).resolve().parents[2]
OUT = ROOT / 'Mac开发交接'
STAGE = OUT / 'QingmingMacDev_2026-10-21'
ARCHIVE = OUT / 'Qingming_M4Max_Dev_2026-10-21.zip'
EXCLUDED_PARTS = {'.git','.venv','__pycache__','.pytest_cache','.mypy_cache','.ruff_cache','.DS_Store','node_modules'}
records = {}

def digest(path):
    h=hashlib.sha256()
    with path.open('rb') as f:
        for b in iter(lambda:f.read(1024*1024), b''): h.update(b)
    return h.hexdigest()

def copy_file(src, rel):
    if src.is_symlink(): raise RuntimeError(f'Unexpected symlink: {src}')
    dst=STAGE/rel
    dst.parent.mkdir(parents=True,exist_ok=True)
    shutil.copy2(src,dst)
    source_hash=digest(src)
    assert source_hash==digest(dst), str(rel)
    records[str(rel).replace('\\','/')]={'origin':src.relative_to(ROOT).as_posix(),'source_sha256':source_hash}

def copy_tree(name):
    source=ROOT/name
    for f in sorted(source.rglob('*')):
        if not f.is_file(): continue
        sub=f.relative_to(source)
        if any(p in EXCLUDED_PARTS for p in sub.parts): continue
        if f.suffix.lower() in {'.pyc','.pyo','.log','.tmp'} or f.name in {'.env','credentials.json'}: continue
        copy_file(f,Path(name)/sub)

if STAGE.exists() or ARCHIVE.exists(): raise SystemExit('Output already exists. Use a new dated package; do not overwrite an earlier handoff.')
OUT.mkdir(exist_ok=True)
assert shutil.disk_usage(OUT).free > 4*1024**3, 'Need at least 4 GB free.'
STAGE.mkdir()
for folder in ['UnityProject/Assets','UnityProject/Packages','UnityProject/ProjectSettings',
               'assets','scenes','scripts','tests','reference','pre','captures','剧本','美术',
               'tools/unity-mcp/MCPForUnity','tools/unity-mcp/Server','tools/unity-mcp/docs']:
    copy_tree(folder)
    print('Copied',folder,flush=True)
for name in ['project.godot','export_presets.cfg','.gitignore','THIRD_PARTY_NOTICES.txt','使用说明.txt',
             '阿安故事版体验说明.txt','Unity迁移说明.txt','文件入口.md','本周进度汇报_2026-10-21.html',
             'Mac开发说明.txt','迁移验证记录.txt','验证记录.txt',
             'tools/unity-mcp/README.md','tools/unity-mcp/LICENSE',
             'tools/unity-port/export_world.gd','tools/unity-port/validate_assets.py',
             'tools/unity-port/prepare_roslyn.py','tools/unity-port/RoslynDependencies.json',
             'tools/unity-port/AssetValidation.json','tools/unity-port/UnityRuntimeTest.txt']:
    copy_file(ROOT/name,Path(name))
for f in Path(__file__).parent.iterdir():
    if f.suffix in {'.txt','.command','.json'}: copy_file(f,Path(f.name))

# Preserve exact source configuration separately; only adapt this transfer copy.
for name in ['ProjectSettings/ProjectVersion.txt','Packages/manifest.json','Packages/packages-lock.json']:
    copy_file(ROOT/'UnityProject'/name,Path('HandoffReference')/Path(name).name)
version=STAGE/'UnityProject/ProjectSettings/ProjectVersion.txt'
version.write_text('m_EditorVersion: 2022.3.62f1\n',encoding='utf-8')
records[version.relative_to(STAGE).as_posix()]['change']='Hub version marker only; standard Unity import not yet validated.'
lock=STAGE/'UnityProject/Packages/packages-lock.json'
# This is a single known staged file, never a source file or a recursive deletion.
assert lock.resolve().is_relative_to(STAGE.resolve())
lock.unlink(); records.pop(lock.relative_to(STAGE).as_posix())
manifest_path=STAGE/'UnityProject/Packages/manifest.json'
manifest=json.loads(manifest_path.read_text(encoding='utf-8-sig'))
for module in ['animation','imageconversion','physics','screencapture','unitywebrequest','uielements']:
    manifest['dependencies']['com.unity.modules.'+module]='1.0.0'
manifest['dependencies']['com.unity.nuget.newtonsoft-json']='3.0.2'
manifest_path.write_text(json.dumps(manifest,ensure_ascii=False,indent=2)+'\n',encoding='utf-8')
records[manifest_path.relative_to(STAGE).as_posix()]['change']='Declare direct game dependencies explicitly; retain relative local MCP package.'

for script in STAGE.glob('*.command'):
    script.write_text(script.read_text(encoding='utf-8-sig').replace('\r\n','\n'),encoding='utf-8',newline='\n')

testlog=(ROOT/'tools/unity-port/PlayerPortraitTest.log').read_text(encoding='utf-8-sig',errors='replace')
summary='\n'.join(line for line in testlog.splitlines() if line.startswith('QINGMING_'))
(STAGE/'HandoffReference/Windows验证摘要.txt').write_text(
    '验证平台：Windows / 团结 1.10.4。不是 Mac 或 Vision Pro 验证结果。\n'+summary+'\n',encoding='utf-8')
(STAGE/'HandoffReference/本包调整说明.txt').write_text(
    '原工程源码与资源原样复制；仅交接副本版本标记与依赖清单作便携调整。\n'
    '原始 ProjectVersion.txt、manifest.json、packages-lock.json 在本目录。\n'
    '副本不沿用团结包仓库锁文件，标准 Unity 首次打开时重新解析。\n'
    '新增根目录 Mac 启动/MCP 脚本与开发说明；未安装 visionOS 包或宣称通过 Mac 验证。\n',encoding='utf-8')

rel=manifest['dependencies']['com.coplaydev.unity-mcp'].removeprefix('file:')
package_path=(STAGE/'UnityProject/Packages'/rel).resolve()
assert package_path.is_relative_to(STAGE.resolve()) and (package_path/'package.json').is_file()
assert len(list((STAGE/'UnityProject/Assets/Resources/Portraits').glob('*.png')))==13
assert (STAGE/'UnityProject/Assets/Scenes/Qingming.unity').is_file()
assert len(list((STAGE/'UnityProject/Assets/StreamingAssets/Painting').glob('tile_*.png')))==19
files=sorted(f for f in STAGE.rglob('*') if f.is_file())
case_names={}
for f in files:
    key=f.relative_to(STAGE).as_posix().casefold()
    assert key not in case_names, f'Case collision: {key}'
    case_names[key]=str(f)
entries=[]
for f in files:
    rel=f.relative_to(STAGE).as_posix()
    entry={'path':rel,'bytes':f.stat().st_size,'sha256':digest(f),**records.get(rel,{'origin':'generated handoff metadata'})}
    if 'source_sha256' in entry and 'change' not in entry:
        assert entry['sha256']==entry['source_sha256'], rel
    entries.append(entry)
report={'date':'2026-10-21','target':'Apple silicon M4 Max Mac development; future Apple Vision Pro',
        'verified':'Source-copy hashes, local package path, portrait/tile counts, path case, ZIP CRC and member hashes. Mac/visionOS runtime NOT verified.',
        'excluded':'Generated caches, editors, installers, venvs, account configuration, raw editor logs, old ZIPs and Windows builds.',
        'files':entries}
(STAGE/'TRANSFER_MANIFEST.json').write_text(json.dumps(report,ensure_ascii=False,indent=2)+'\n',encoding='utf-8')
files=sorted(f for f in STAGE.rglob('*') if f.is_file())
(STAGE/'MANIFEST.sha256').write_text(''.join(f'{digest(f)}  {f.relative_to(STAGE).as_posix()}\n' for f in files),encoding='utf-8')
print('Creating ZIP...',flush=True)
with zipfile.ZipFile(ARCHIVE,'w',zipfile.ZIP_DEFLATED,compresslevel=3,allowZip64=True) as z:
    for f in sorted(STAGE.rglob('*')):
        if not f.is_file(): continue
        info=zipfile.ZipInfo.from_file(f,arcname=STAGE.name+'/'+f.relative_to(STAGE).as_posix())
        info.create_system=3
        mode=0o755 if f.suffix=='.command' else 0o644
        info.external_attr=(stat.S_IFREG|mode)<<16
        info.compress_type=zipfile.ZIP_DEFLATED
        with f.open('rb') as src, z.open(info,'w',force_zip64=True) as dst: shutil.copyfileobj(src,dst,1024*1024)
print('Checking ZIP contents...',flush=True)
with zipfile.ZipFile(ARCHIVE) as z:
    assert z.testzip() is None
    for entry in entries:
        h=hashlib.sha256()
        with z.open(STAGE.name+'/'+entry['path']) as f:
            for block in iter(lambda:f.read(1024*1024),b''): h.update(block)
        assert h.hexdigest()==entry['sha256'],entry['path']
    assert len(z.namelist())==len(entries)+2
    assert z.getinfo(STAGE.name+'/OpenUnityOnMac.command').external_attr>>16 & 0o111
archive_hash=digest(ARCHIVE)
ARCHIVE.with_suffix('.sha256.txt').write_text(archive_hash+'  '+ARCHIVE.name+'\n',encoding='utf-8')
result={'result':'PASS','archive':str(ARCHIVE),'zip_bytes':ARCHIVE.stat().st_size,'files':len(entries)+2,
        'portraits':13,'painting_tiles':19,'sha256':archive_hash,'Mac_runtime_verified':False,'visionOS_verified':False}
(OUT/'交接包验证结果.json').write_text(json.dumps(result,ensure_ascii=False,indent=2)+'\n',encoding='utf-8')
print(json.dumps(result,ensure_ascii=True),flush=True)
