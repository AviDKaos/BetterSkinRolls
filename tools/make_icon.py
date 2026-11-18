"""Draw the Thunderstore icon (256x256): a slot machine reel where a completed prize (checked, faded) is skipped.

Usage: python -I tools/make_icon.py thunderstore/icon.png   (needs Pillow)
"""
import sys

from PIL import Image, ImageDraw

S = 1024  # draw large, downscale for smooth edges

BG_TOP, BG_BOTTOM = (70, 30, 90), (24, 10, 36)
FRAME, FRAME_EDGE = (40, 24, 52), (255, 210, 90)
SLOT, SLOT_EDGE = (30, 36, 48), (120, 130, 150)
COMMON, RARE, LEGENDARY = (150, 200, 230), (190, 110, 240), (255, 200, 60)
FADED = (70, 76, 88)
CHECK = (90, 220, 120)
POINTER = (255, 90, 90)


def gradient(size, top, bottom):
    img = Image.new("RGB", (size, size))
    d = ImageDraw.Draw(img)
    for y in range(size):
        t = y / (size - 1)
        d.line([(0, y), (size, y)], fill=tuple(int(top[i] + (bottom[i] - top[i]) * t) for i in range(3)))
    return img


def gem(d, cx, cy, r, color):
    top = (cx, cy - r)
    pts = [top, (cx + r, cy - r // 3), (cx, cy + r), (cx - r, cy - r // 3)]
    d.polygon(pts, fill=color)
    shade = tuple(max(0, c - 50) for c in color)
    d.polygon([(cx - r, cy - r // 3), (cx, cy - r // 3), (cx, cy + r)], fill=shade)
    d.line([(cx - r, cy - r // 3), (cx + r, cy - r // 3)], fill=(255, 255, 255), width=10)


def main(out):
    img = gradient(S, BG_TOP, BG_BOTTOM)
    d = ImageDraw.Draw(img)

    # Machine window
    d.rounded_rectangle([90, 300, S - 90, 760], radius=70, fill=FRAME, outline=FRAME_EDGE, width=24)

    w, gap, y = 230, 40, 380
    xs = [(S - 3 * w - 2 * gap) // 2 + i * (w + gap) for i in range(3)]
    colors = [COMMON, None, LEGENDARY]
    for i, x in enumerate(xs):
        d.rounded_rectangle([x, y, x + w, y + 300], radius=40, fill=SLOT, outline=SLOT_EDGE, width=10)
        cx, cy = x + w // 2, y + 150
        if colors[i]:
            gem(d, cx, cy, 80, colors[i])
        else:
            gem(d, cx, cy, 80, FADED)
            # Completed: check badge and a strike across the slot
            d.line([(x + 20, y + 280), (x + w - 20, y + 20)], fill=(200, 70, 70), width=22)
            d.ellipse([cx + 20, cy + 40, cx + 120, cy + 140], fill=CHECK)
            d.line([(cx + 42, cy + 92), (cx + 64, cy + 116), (cx + 102, cy + 66)], fill=(255, 255, 255), width=16)

    # Pointer above the reel, aimed at the legendary prize
    px = xs[2] + w // 2
    d.polygon([(px - 60, 200), (px + 60, 200), (px, 300)], fill=POINTER)

    # Lever
    d.rounded_rectangle([S - 70, 430, S - 40, 640], radius=14, fill=(160, 160, 170))
    d.ellipse([S - 95, 360, S - 15, 440], fill=POINTER)

    img.resize((256, 256), Image.LANCZOS).save(out)


if __name__ == "__main__":
    main(sys.argv[1])
