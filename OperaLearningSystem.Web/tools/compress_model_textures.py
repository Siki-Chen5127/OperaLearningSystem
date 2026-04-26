# 脚本功能：读取3D文件夹，找出里面的贴图，将其分辨率、画质压缩，生成全新的文件夹
import argparse
import json
import shutil
from pathlib import Path

from PIL import Image


def parse_args() -> argparse.Namespace:
    parser = argparse.ArgumentParser(description="Create a lite glTF model by resizing referenced textures.")
    parser.add_argument("--src", required=True, help="Source model directory containing scene.gltf")
    parser.add_argument("--dst", required=True, help="Destination model directory")
    parser.add_argument("--max-size", type=int, default=1024, help="Max width/height for textures")
    parser.add_argument("--jpg-quality", type=int, default=78, help="JPEG quality")
    return parser.parse_args()


def load_gltf(gltf_path: Path) -> dict:
    with gltf_path.open("r", encoding="utf-8") as f:
        return json.load(f)


def referenced_uris(gltf: dict) -> set[str]:
    uris: set[str] = set()
    for buf in gltf.get("buffers", []):
        uri = buf.get("uri")
        if isinstance(uri, str) and uri:
            uris.add(uri)
    for img in gltf.get("images", []):
        uri = img.get("uri")
        if isinstance(uri, str) and uri:
            uris.add(uri)
    return uris


def resize_texture(path: Path, max_size: int, jpg_quality: int) -> tuple[int, int, int]:
    before = path.stat().st_size
    with Image.open(path) as im:
        im.load()
        w, h = im.size
        if max(w, h) > max_size:
            scale = max_size / float(max(w, h))
            new_size = (max(1, int(w * scale)), max(1, int(h * scale)))
            im = im.resize(new_size, Image.Resampling.LANCZOS)

        suffix = path.suffix.lower()
        if suffix in (".jpg", ".jpeg"):
            if im.mode not in ("RGB", "L"):
                im = im.convert("RGB")
            im.save(path, quality=jpg_quality, optimize=True, progressive=True)
        elif suffix == ".png":
            if im.mode == "P":
                im = im.convert("RGBA")
            im.save(path, optimize=True, compress_level=9)
        else:
            # Unhandled texture format, skip recompressing.
            return before, before, 0

    after = path.stat().st_size
    return before, after, 1


def main() -> None:
    args = parse_args()
    src = Path(args.src).resolve()
    dst = Path(args.dst).resolve()
    src_gltf = src / "scene.gltf"
    if not src_gltf.exists():
        raise FileNotFoundError(f"scene.gltf not found in: {src}")

    if dst.exists():
        shutil.rmtree(dst)
    shutil.copytree(src, dst)

    gltf = load_gltf(dst / "scene.gltf")
    uris = referenced_uris(gltf)

    tex_files: list[Path] = []
    for uri in uris:
        if uri.startswith("data:"):
            continue
        p = (dst / uri).resolve()
        if p.exists() and p.is_file() and p.suffix.lower() in (".png", ".jpg", ".jpeg"):
            tex_files.append(p)

    total_before = 0
    total_after = 0
    processed = 0
    for p in tex_files:
        b, a, flag = resize_texture(p, args.max_size, args.jpg_quality)
        total_before += b
        total_after += a
        processed += flag

    print(f"Processed textures: {processed}")
    print(f"Texture bytes: {total_before} -> {total_after}")
    print(f"Saved bytes: {max(0, total_before - total_after)}")
    print(f"Output: {dst}")


if __name__ == "__main__":
    main()
