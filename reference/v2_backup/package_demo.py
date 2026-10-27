"""Package the playable Windows build, editable Godot source, original and credits."""
from pathlib import Path
from zipfile import ZipFile, ZIP_DEFLATED
import hashlib

root = Path(__file__).resolve().parents[1]
out = root / '交付包/汴梁一日_高清原画与三渲二_v2.zip'
out.parent.mkdir(exist_ok=True)
files = []
for name in ['project.godot', 'export_presets.cfg', '开始体验.cmd', '使用说明.txt',
             '验证记录.txt', 'THIRD_PARTY_NOTICES.txt', '.gitignore']:
    files.append(root / name)
for folder in ['assets', 'scenes', 'scripts', 'tests', 'tools', 'build']:
    files.extend(p for p in (root / folder).rglob('*')
                 if p.is_file() and '__pycache__' not in p.parts and p.suffix != '.log')
files.extend([root / 'reference/.gdignore',
              root / 'reference/Alongtheriver_QingMing_38414x1800.jpg',
              root / 'captures/.gdignore'])
for folder in ['captures/v2', 'captures/release_v2']:
    files.extend((root / folder).glob('*.png'))
with ZipFile(out, 'w', ZIP_DEFLATED, compresslevel=6) as archive:
    for file in sorted(set(files)):
        assert file.resolve().is_relative_to(root)
        archive.write(file, file.relative_to(root).as_posix())
with ZipFile(out) as archive:
    assert archive.testzip() is None
    assert archive.read('build/Bianliang.exe') == (root / 'build/Bianliang.exe').read_bytes()
    assert 'custom_template/release=""' in archive.read('export_presets.cfg').decode('utf-8')
sha = hashlib.sha256(out.read_bytes()).hexdigest()
out.with_suffix('.sha256.txt').write_text(sha + '  ' + out.name + '\n', encoding='utf-8')
print(f'PACKAGE_PASS: {out.name}; {out.stat().st_size:,} bytes; {len(set(files))} files; ZIP integrity and EXE verified')
