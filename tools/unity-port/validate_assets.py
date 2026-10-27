"""Validate exported geometry, node references, and lossless source asset copies."""
import hashlib
import json
import math
import struct
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
IMPORTED = ROOT / "UnityProject/Assets/Resources/Imported"

def sha(path):
    return hashlib.file_digest(path.open("rb"), "sha256").hexdigest()

def main():
    meta = json.loads((IMPORTED / "WorldMetadata.json").read_text(encoding="utf-8"))
    paths, meshes, vertices, labels = [], 0, 0, 0
    with (IMPORTED / "World.bytes").open("rb") as stream:
        def read(fmt):
            size = struct.calcsize(fmt)
            data = stream.read(size)
            assert len(data) == size, "Truncated geometry"
            return struct.unpack(fmt, data)
        def string():
            return stream.read(read("<I")[0]).decode("utf-8")
        assert read("<I")[0] == 0x514d5731
        count = read("<I")[0]
        for i in range(count):
            parent = read("<i")[0]
            assert -1 <= parent < i
            name = string()
            assert "/" not in name
            paths.append("." if parent == -1 else name if parent == 0 else paths[parent] + "/" + name)
            assert all(math.isfinite(v) for v in read("<10f"))
            assert read("<B")[0] in (0, 1)
            for _ in range(read("<I")[0]):
                read("<4f2B")
                n = read("<I")[0]
                coordinates = read("<" + str(n * 6) + "f")
                assert all(math.isfinite(v) for v in coordinates)
                ni = read("<I")[0]
                assert ni % 3 == 0
                assert all(v < n for v in read("<" + str(ni) + "I"))
                meshes += 1
                vertices += n
            if read("<B")[0]:
                string()
                read("<If4f")
                labels += 1
        assert not stream.read(1), "Unparsed trailing bytes"
    assert len(set(paths)) == count, "Duplicate transform paths"
    assert (count, meshes, vertices) == (meta["nodeCount"], meta["meshCount"], meta["vertices"])
    for key in ("player", "friend", "customer", "cargo_boat", "row_boat", "oar", "pear_display", "sun", "leaf_parent"):
        assert meta[key] in paths, (key, meta[key])
    for value in meta["targets"].values():
        assert value in paths
    for key in ("walkers", "willows", "banners", "wind_lanterns", "drifting_leaves"):
        for value in meta[key]:
            assert value["node"] in paths
    for key in ("lanterns", "boats", "actors", "night_lights"):
        for value in meta[key]:
            assert value in paths
    for prop in ("basket", "pear", "jug", "cup"):
        assert "HeldTemplates/" + prop in paths
    matches = []
    for source_dir, target_dir in (
        (ROOT / "assets/painting", ROOT / "UnityProject/Assets/StreamingAssets/Painting"),
        (ROOT / "assets/fonts", ROOT / "UnityProject/Assets/Resources/Fonts"),
    ):
        for target in target_dir.iterdir():
            if not target.is_file() or target.suffix == ".meta":
                continue
            source = source_dir / target.name
            assert source.is_file(), str(source)
            digest = sha(source)
            assert digest == sha(target), str(target)
            matches.append({"file": str(target.relative_to(ROOT)), "sha256": digest})
    report = {
        "result": "PASS",
        "scope": "Binary structure, hierarchy references, finite geometry, triangle indices, source-copy hashes. This does not verify Unity compilation or rendering.",
        "nodes": count, "meshSurfaces": meshes, "vertices": vertices, "labels": labels,
        "matchedAssets": matches,
    }
    (Path(__file__).parent / "AssetValidation.json").write_text(json.dumps(report, ensure_ascii=False, indent=2), encoding="utf-8")
    print(json.dumps({k: v for k, v in report.items() if k != "matchedAssets"}, ensure_ascii=False))
    print("Matched assets:", len(matches))

if __name__ == "__main__":
    main()
