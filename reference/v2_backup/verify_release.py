"""Run only the exported EXE from an isolated directory, including real rendering."""
from pathlib import Path
import shutil
import subprocess
import tempfile

root = Path(__file__).resolve().parents[1]
temp_root = Path(tempfile.gettempdir()).resolve()
check_dir = Path(tempfile.mkdtemp(prefix='bianliang-release-check-')).resolve()
assert check_dir.parent == temp_root
exe = check_dir / 'Bianliang.exe'
shutil.copy2(root / 'build/Bianliang.exe', exe)
logs = []
try:
    for name, arguments, marker in [
        ('standalone', ['--headless', '--', '--self-test'], 'SELF_TEST_PASS'),
        ('render', ['--resolution', '1440x900', '--', '--capture'], 'CAPTURE_PASS'),
    ]:
        result = subprocess.run([str(exe), *arguments], cwd=check_dir,
                                capture_output=True, timeout=80)
        log = (result.stdout + result.stderr).decode('utf-8', errors='replace')
        logs.append(name.upper() + '\n' + log)
        assert result.returncode == 0 and marker in log and 'ERROR:' not in log, log
        print(name.upper() + '_PASS')
    actual = check_dir / 'captures/v2'
    assert len(list(actual.glob('*.png'))) == 13, str(actual)
    output = root / 'captures/release_v2'
    output.mkdir(parents=True, exist_ok=True)
    for screenshot in actual.glob('*.png'):
        shutil.copy2(screenshot, output / screenshot.name)
    print('RELEASE_PASS: isolated executable, offline scroll assets, complete flow, 13 rendered captures')
finally:
    (root / 'build/verify_release_v2.log').write_text('\n'.join(logs), encoding='utf-8')
    # Remove only this newly created temporary directory's verified contents.
    assert check_dir.parent == temp_root and check_dir.name.startswith('bianliang-release-check-')
    for item in sorted(check_dir.rglob('*'), key=lambda p: len(p.parts), reverse=True):
        assert item.resolve().is_relative_to(check_dir)
        if item.is_file():
            item.unlink()
        elif item.is_dir():
            item.rmdir()
    check_dir.rmdir()
