using System.Collections.Generic;
using UnityEngine;

public static class SpriteFactory
{
    private static Sprite _square;
    private static Sprite _circle;
    private static Sprite _ghost;
    private static Sprite _cherry;
    private static readonly Dictionary<int, Sprite> PacmanCache = new Dictionary<int, Sprite>();

    public static Sprite Square
    {
        get
        {
            if (_square == null)
            {
                var tex = new Texture2D(8, 8) { filterMode = FilterMode.Point };
                var px = new Color[64];
                for (int i = 0; i < px.Length; i++) px[i] = Color.white;
                tex.SetPixels(px); tex.Apply();
                _square = Sprite.Create(tex, new Rect(0, 0, 8, 8), new Vector2(0.5f, 0.5f), 8);
            }
            return _square;
        }
    }

    public static Sprite Circle
    {
        get
        {
            if (_circle == null)
            {
                int size = 64;
                var tex = new Texture2D(size, size) { filterMode = FilterMode.Point };
                var px = new Color[size * size];
                DrawCircle(px, size, size / 2f - 0.5f, size / 2f - 0.5f, size / 2f - 2f, Color.white);
                tex.SetPixels(px); tex.Apply();
                _circle = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
            }
            return _circle;
        }
    }

    public static Sprite GetPacmanSprite(int mouthAngleDeg)
    {
        mouthAngleDeg = Mathf.Clamp(mouthAngleDeg, 0, 60);
        if (PacmanCache.TryGetValue(mouthAngleDeg, out var cached)) return cached;

        int size = 64;
        var tex = new Texture2D(size, size) { filterMode = FilterMode.Point };
        var px = new Color[size * size];
        float r = size / 2f - 2f;
        float c = size / 2f - 0.5f;
        float mouthHalfDeg = mouthAngleDeg * 0.5f;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dx = x - c, dy = y - c;
                if (dx * dx + dy * dy > r * r) { px[y * size + x] = Color.clear; continue; }
                float angDeg = Mathf.Atan2(dy, dx) * Mathf.Rad2Deg;
                if (mouthAngleDeg > 0 && Mathf.Abs(Mathf.DeltaAngle(angDeg, 0f)) < mouthHalfDeg)
                    px[y * size + x] = Color.clear;
                else
                    px[y * size + x] = Color.white;
            }
        }
        tex.SetPixels(px); tex.Apply();
        var sprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
        PacmanCache[mouthAngleDeg] = sprite;
        return sprite;
    }

    public static Sprite PacmanSprite => GetPacmanSprite(30);

    public static Sprite GhostSprite
    {
        get
        {
            if (_ghost == null)
            {
                int size = 64;
                var tex = new Texture2D(size, size) { filterMode = FilterMode.Point };
                var px = new Color[size * size];
                float cx = size / 2f - 0.5f;
                float cy = size * 0.62f;
                float radius = size * 0.36f;
                float bodyHalfWidth = radius;
                float bodyBottom = size * 0.20f;
                float footHeight = size * 0.12f;
                int numFeet = 4;
                float footWidth = (2f * bodyHalfWidth) / numFeet;

                for (int y = 0; y < size; y++)
                {
                    for (int x = 0; x < size; x++)
                    {
                        float dx = x - cx, dy = y - cy;
                        bool inside = false;
                        if (y >= cy) inside = dx * dx + dy * dy <= radius * radius;
                        else if (Mathf.Abs(dx) <= bodyHalfWidth)
                        {
                            if (y >= bodyBottom) inside = true;
                            else
                            {
                                float localX = dx + bodyHalfWidth;
                                int footIndex = Mathf.Clamp(Mathf.FloorToInt(localX / footWidth), 0, numFeet - 1);
                                float localInFoot = localX - footIndex * footWidth;
                                float triangle = 1f - Mathf.Abs((localInFoot / footWidth) * 2f - 1f);
                                inside = y >= bodyBottom - footHeight * triangle;
                            }
                        }
                        px[y * size + x] = inside ? Color.white : Color.clear;
                    }
                }
                tex.SetPixels(px); tex.Apply();
                _ghost = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
            }
            return _ghost;
        }
    }

    public static Sprite CherrySprite
    {
        get
        {
            if (_cherry == null)
            {
                int size = 64;
                var tex = new Texture2D(size, size) { filterMode = FilterMode.Point };
                var px = new Color[size * size];
                for (int i = 0; i < px.Length; i++) px[i] = Color.clear;

                var red = new Color(0.92f, 0.10f, 0.10f);
                var green = new Color(0.20f, 0.75f, 0.20f);
                var shine = new Color(1f, 0.75f, 0.75f);

                DrawCircle(px, size, 20f, 20f, 14f, red);
                DrawCircle(px, size, 44f, 22f, 14f, red);
                DrawCircle(px, size, 15f, 25f, 3f, shine);
                DrawCircle(px, size, 39f, 27f, 3f, shine);

                for (int i = 0; i < 24; i++)
                {
                    float t = i / 23f;
                    float y = Mathf.Lerp(34f, 56f, t);
                    DrawCircle(px, size, Mathf.Lerp(20f, 32f, t), y, 2.2f, green);
                    DrawCircle(px, size, Mathf.Lerp(44f, 32f, t), y, 2.2f, green);
                }

                tex.SetPixels(px); tex.Apply();
                _cherry = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
            }
            return _cherry;
        }
    }

    public static GameObject CreateSprite(string name, Sprite sprite, Color color, int order = 0)
    {
        var go = new GameObject(name);
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = sprite;
        sr.color = color;
        sr.sortingOrder = order;
        return go;
    }

    private static void DrawCircle(Color[] px, int size, float cx, float cy, float r, Color color)
    {
        float r2 = r * r;
        for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float dx = x - cx, dy = y - cy;
                if (dx * dx + dy * dy <= r2) px[y * size + x] = color;
            }
    }
}