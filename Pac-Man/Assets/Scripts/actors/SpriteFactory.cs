using UnityEngine;

public static class SpriteFactory
{
    private static Sprite _square;
    private static Sprite _circle;
    private static Sprite _pacman;
    private static Sprite _ghost;

    public static Sprite Square
    {
        get
        {
            if (_square == null)
            {
                var tex = new Texture2D(8, 8) { filterMode = FilterMode.Point };
                var px = new Color[64];
                for (int i = 0; i < px.Length; i++) px[i] = Color.white;
                tex.SetPixels(px);
                tex.Apply();
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
                var tex = new Texture2D(size, size) { filterMode = FilterMode.Bilinear };
                var px = new Color[size * size];
                float r = size / 2f - 2f;
                float c = size / 2f - 0.5f;
                for (int y = 0; y < size; y++)
                {
                    for (int x = 0; x < size; x++)
                    {
                        float dx = x - c, dy = y - c;
                        px[y * size + x] = (dx * dx + dy * dy <= r * r) ? Color.white : Color.clear;
                    }
                }
                tex.SetPixels(px);
                tex.Apply();
                _circle = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
            }
            return _circle;
        }
    }

    public static Sprite PacmanSprite
    {
        get
        {
            if (_pacman == null)
            {
                int size = 64;
                var tex = new Texture2D(size, size) { filterMode = FilterMode.Bilinear };
                var px = new Color[size * size];
                float r = size / 2f - 2f;
                float c = size / 2f - 0.5f;
                float mouthHalf = 30f * Mathf.Deg2Rad;
                for (int y = 0; y < size; y++)
                {
                    for (int x = 0; x < size; x++)
                    {
                        float dx = x - c, dy = y - c;
                        if (dx * dx + dy * dy > r * r) { px[y * size + x] = Color.clear; continue; }
                        float ang = Mathf.Atan2(dy, dx);
                        // mouth opens to the right (0 rad)
                        if (Mathf.Abs(Mathf.DeltaAngle(ang * Mathf.Rad2Deg, 0f)) < mouthHalf * Mathf.Rad2Deg)
                            px[y * size + x] = Color.clear;
                        else
                            px[y * size + x] = Color.white;
                    }
                }
                tex.SetPixels(px);
                tex.Apply();
                _pacman = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
            }
            return _pacman;
        }
    }

    public static Sprite GhostSprite
    {
        get
        {
            if (_ghost == null)
            {
                int size = 64;
                var tex = new Texture2D(size, size) { filterMode = FilterMode.Bilinear };
                var px = new Color[size * size];

                float cx = size / 2f - 0.5f;        // центр по X
                float cy = size * 0.62f;            // центр «купола» (верхняя полусфера)
                float radius = size * 0.36f;        // радиус купола = половина ширины тела
                float bodyHalfWidth = radius;
                float bodyBottom = size * 0.20f;    // где заканчиваются прямые бока
                float footHeight = size * 0.12f;    // насколько ножки выступают вниз
                int numFeet = 3;
                float footWidth = (2f * bodyHalfWidth) / numFeet;

                for (int y = 0; y < size; y++)
                {
                    for (int x = 0; x < size; x++)
                    {
                        float dx = x - cx;
                        float dy = y - cy;
                        bool inside = false;

                        if (y >= cy)
                        {
                            // Верхняя полусфера
                            inside = dx * dx + dy * dy <= radius * radius;
                        }
                        else if (Mathf.Abs(dx) <= bodyHalfWidth)
                        {
                            if (y >= bodyBottom)
                            {
                                // Прямые бока
                                inside = true;
                            }
                            else
                            {
                                // Ножки-треугольники
                                float localX = dx + bodyHalfWidth;
                                int footIndex = Mathf.Clamp(Mathf.FloorToInt(localX / footWidth), 0, numFeet - 1);
                                float localInFoot = localX - footIndex * footWidth;
                                // треугольник: 1 в центре ножки, 0 на её краю
                                float triangle = 1f - Mathf.Abs((localInFoot / footWidth) * 2f - 1f);
                                float edgeY = bodyBottom - footHeight * triangle;
                                inside = y >= edgeY;
                            }
                        }

                        px[y * size + x] = inside ? Color.white : Color.clear;
                    }
                }

                tex.SetPixels(px);
                tex.Apply();
                _ghost = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
            }
            return _ghost;
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
}