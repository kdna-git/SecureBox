"""Renders the SecureBox chest icon (PNG + ICO) with only the stdlib.

Usage: python make_icon.py <out_dir>   -> securebox.png / securebox.ico
"""
import math, os, struct, sys, zlib

S = 512.0  # design canvas

# Dark wood palette, flat colours
LID = (0x7A, 0x4C, 0x2B)
LID_HI = (0x8E, 0x5B, 0x35)
BODY = (0x66, 0x3F, 0x24)
BODY_LO = (0x52, 0x32, 0x1C)
LATCH = (0xE9, 0xB2, 0x3C)
LATCH_HI = (0xF5, 0xCE, 0x6A)
OUTLINE = (0x24, 0x13, 0x0A)
OUTLINE_W = 22.0

# Flat front view: a pill-shaped lid floating above a rounded body, joined by the latch.
# (x0, y0, x1, y1, radius)
LID_R = (52, 64, 460, 214, 70)
BODY_R = (52, 236, 460, 456, 84)
LATCH_R = (200, 160, 312, 300, 40)


def sd_rrect(px, py, r):
    x0, y0, x1, y1, rad = r
    cx, cy = (x0 + x1) / 2, (y0 + y1) / 2
    hx, hy = (x1 - x0) / 2 - rad, (y1 - y0) / 2 - rad
    qx, qy = abs(px - cx) - hx, abs(py - cy) - hy
    return math.hypot(max(qx, 0), max(qy, 0)) + min(max(qx, qy), 0) - rad


def layers():
    # (signed distance, colour, opacity, edge softness in design units)
    L = []

    def shape(sd, col):
        L.append((sd, OUTLINE, 1.0, 0))
        L.append((lambda x, y: sd(x, y) + OUTLINE_W, col, 1.0, 0))

    def inside(sd, extra):  # extra region clipped to the inner fill of sd
        return lambda x, y: max(sd(x, y) + OUTLINE_W, extra(x, y))

    body = lambda x, y: sd_rrect(x, y, BODY_R)
    shape(body, BODY)
    L.append((inside(body, lambda x, y: 392 - y), BODY_LO, 1.0, 0))
    lid = lambda x, y: sd_rrect(x, y, LID_R)
    shape(lid, LID)
    L.append((inside(lid, lambda x, y: max(y - 130, lid(x, y) + OUTLINE_W + 18)), LID_HI, 1.0, 0))
    latch = lambda x, y: sd_rrect(x, y, LATCH_R)
    shape(latch, LATCH)
    L.append((inside(latch, lambda x, y: max(y - 214, latch(x, y) + OUTLINE_W + 12)), LATCH_HI, 1.0, 0))
    return L


LAYERS = layers()


def render(size):
    k = size / S
    buf = bytearray(size * size * 4)
    for j in range(size):
        y = (j + 0.5) / k
        for i in range(size):
            x = (i + 0.5) / k
            r = g = b = a = 0.0
            for sd, col, alpha, soft in LAYERS:
                cov = min(max(0.5 - sd(x, y) / max(soft, 1 / k), 0), 1)
                if cov <= 0:
                    continue
                c = cov * alpha
                r = col[0] * c + r * (1 - c)
                g = col[1] * c + g * (1 - c)
                b = col[2] * c + b * (1 - c)
                a = c + a * (1 - c)
            if a > 0:
                o = (j * size + i) * 4
                buf[o:o + 4] = bytes((round(r / a), round(g / a), round(b / a), round(a * 255)))
    return bytes(buf)


def png(size, rgba):
    raw = b"".join(b"\0" + rgba[y * size * 4:(y + 1) * size * 4] for y in range(size))
    chunk = lambda t, d: struct.pack(">I", len(d)) + t + d + struct.pack(">I", zlib.crc32(t + d) & 0xFFFFFFFF)
    return (b"\x89PNG\r\n\x1a\n" + chunk(b"IHDR", struct.pack(">IIBBBBB", size, size, 8, 6, 0, 0, 0))
            + chunk(b"IDAT", zlib.compress(raw, 9)) + chunk(b"IEND", b""))


def ico(images):
    head = struct.pack("<HHH", 0, 1, len(images))
    off = 6 + 16 * len(images)
    dirs, data = b"", b""
    for size, blob in images:
        dirs += struct.pack("<BBBBHHII", size % 256, size % 256, 0, 0, 1, 32, len(blob), off + len(data))
        data += blob
    return head + dirs + data


def main():
    out = sys.argv[1]
    os.makedirs(out, exist_ok=True)
    pngs = {sz: png(sz, render(sz)) for sz in (16, 24, 32, 48, 64, 128, 256, 512)}
    with open(os.path.join(out, "securebox.png"), "wb") as f:
        f.write(pngs[512])
    with open(os.path.join(out, "securebox.ico"), "wb") as f:
        f.write(ico([(sz, pngs[sz]) for sz in (16, 24, 32, 48, 64, 128, 256)]))
    print("ok")


if __name__ == "__main__":
    main()
