"""Rebuild CopyPasta.ico from the simple (16-64) and detailed (256) PNGs. Requires Pillow."""
from pathlib import Path
from PIL import Image
import io
import struct

assets = Path(__file__).resolve().parent
simple = Image.open(assets / "CopyPasta-simple.png").convert("RGBA")
detailed = Image.open(assets / "CopyPasta-detailed.png").convert("RGBA")


def fit(img: Image.Image, size: int) -> Image.Image:
    return img.resize((size, size), Image.Resampling.LANCZOS)


def png_bytes(im: Image.Image) -> bytes:
    buf = io.BytesIO()
    im.save(buf, format="PNG")
    return buf.getvalue()


frames = [(size, fit(simple, size)) for size in (16, 24, 32, 48, 64)]
frames.append((256, fit(detailed, 256)))
pngs = [(size, png_bytes(im)) for size, im in frames]

count = len(pngs)
header = struct.pack("<HHH", 0, 1, count)
entries = b""
offset = 6 + 16 * count
blob = b""
for size, data in pngs:
    w = 0 if size >= 256 else size
    h = 0 if size >= 256 else size
    entries += struct.pack("<BBBBHHII", w, h, 0, 0, 1, 32, len(data), offset)
    blob += data
    offset += len(data)

out = assets / "CopyPasta.ico"
out.write_bytes(header + entries + blob)
print(f"wrote {out} ({out.stat().st_size} bytes)")
ico = Image.open(out)
print("sizes", sorted(ico.ico.sizes()) if hasattr(ico, "ico") else ico.size)
