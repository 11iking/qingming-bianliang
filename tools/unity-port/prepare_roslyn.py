"""Install the exact NuGet dependencies specified by MCPForUnity RoslynInstaller."""
import concurrent.futures
import hashlib
import io
import json
import urllib.request
import uuid
import zipfile
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
DEST = ROOT / "UnityProject/Assets/Plugins/Roslyn"
ENTRIES = [
    ("microsoft.codeanalysis.common", "4.12.0", "Microsoft.CodeAnalysis.dll"),
    ("microsoft.codeanalysis.csharp", "4.12.0", "Microsoft.CodeAnalysis.CSharp.dll"),
    ("system.collections.immutable", "8.0.0", "System.Collections.Immutable.dll"),
    ("system.reflection.metadata", "8.0.0", "System.Reflection.Metadata.dll"),
    ("system.runtime.compilerservices.unsafe", "6.0.0", "System.Runtime.CompilerServices.Unsafe.dll"),
]

def install(entry):
    package, version, name = entry
    url = f"https://api.nuget.org/v3-flatcontainer/{package}/{version}/{package}.{version}.nupkg"
    with urllib.request.urlopen(url, timeout=90) as response:
        data = response.read()
    with zipfile.ZipFile(io.BytesIO(data)) as archive:
        dll = archive.read("lib/netstandard2.0/" + name)
        license_files = [p for p in archive.namelist() if p.lower() in ("license.txt", "license.md", "thirdpartynotices.txt")]
        for p in license_files:
            (DEST / (package + "-" + p)).write_bytes(archive.read(p))
    path = DEST / name
    path.write_bytes(dll)
    meta = path.with_suffix(path.suffix + ".meta")
    if not meta.exists():
        meta.write_text(f"""fileFormatVersion: 2
guid: {uuid.uuid4().hex}
PluginImporter:
  externalObjects: {{}}
  serializedVersion: 2
  iconMap: {{}}
  executionOrder: {{}}
  defineConstraints: []
  isPreloaded: 0
  isOverridable: 0
  isExplicitlyReferenced: 1
  validateReferences: 1
  platformData:
  - first:
      Any:
    second:
      enabled: 0
      settings: {{}}
  - first:
      Editor: Editor
    second:
      enabled: 1
      settings:
        CPU: AnyCPU
        OS: AnyOS
  userData:
  assetBundleName:
  assetBundleVariant:
""", encoding="utf-8")
    return dict(package=package, version=version, url=url, dll=name, sha256=hashlib.sha256(dll).hexdigest(), bytes=len(dll))

if __name__ == "__main__":
    DEST.mkdir(parents=True, exist_ok=True)
    with concurrent.futures.ThreadPoolExecutor(max_workers=5) as executor:
        results = list(executor.map(install, ENTRIES))
    (Path(__file__).parent / "RoslynDependencies.json").write_text(json.dumps(results, indent=2), encoding="utf-8")
    for result in results:
        print(result["dll"], result["bytes"], "bytes")
