"""Export with a supplied matching template, while keeping the project portable."""
from pathlib import Path
import argparse
import subprocess

parser = argparse.ArgumentParser()
parser.add_argument('--godot', required=True, type=Path)
parser.add_argument('--template', required=True, type=Path)
args = parser.parse_args()
assert args.godot.is_file() and args.template.is_file()
root = Path(__file__).resolve().parents[1]
preset = root / 'export_presets.cfg'
original = preset.read_bytes()
text = original.decode('utf-8')
override = 'custom_template/release="' + args.template.resolve().as_posix() + '"'
assert 'custom_template/release=""' in text
try:
    preset.write_text(text.replace('custom_template/release=""', override), encoding='utf-8')
    result = subprocess.run(
        [str(args.godot), '--headless', '--path', str(root),
         '--export-release', 'Windows Desktop', str(root / 'build/Bianliang.exe')],
        capture_output=True, timeout=180,
    )
    log = result.stdout + result.stderr
    (root / 'build/export_v2.log').write_bytes(log)
    if result.returncode or b'ERROR:' in log or b'SCRIPT ERROR:' in log:
        print(log.decode('utf-8', errors='replace'))
        raise SystemExit(result.returncode or 1)
    print('EXPORT_PASS:', (root / 'build/Bianliang.exe').stat().st_size, 'bytes')
finally:
    preset.write_bytes(original)
