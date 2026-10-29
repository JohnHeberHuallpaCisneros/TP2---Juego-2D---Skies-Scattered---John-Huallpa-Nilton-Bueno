using System;
using System.Linq;
using System.Xml.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Skies_Scattered.Mechanics;

public class TileCollision
{
    private readonly bool[,] _solido;
    private readonly int _ancho, _alto, _tam;

    public Vector2 TamanoMundo => new Vector2(_ancho * _tam, _alto * _tam);

    public TileCollision(string rutaTmx, string capa = "coliciones")
    {
        var mapa = XDocument.Load(rutaTmx).Root;

        _ancho = (int)mapa.Attribute("width");
        _alto = (int)mapa.Attribute("height");
        _tam = (int)mapa.Attribute("tilewidth");
        _solido = new bool[_ancho, _alto];

        var layer = mapa.Elements("layer")
            .First(l => (string)l.Attribute("name") == capa);

        var ids = layer.Element("data").Value
            .Split(new[] { ',', '\n', '\r', ' ' },
                   StringSplitOptions.RemoveEmptyEntries);

        for (int i = 0; i < ids.Length; i++)
        {
            // Quitar los bits de espejado/rotación que guarda Tiled
            uint gid = uint.Parse(ids[i]) & 0x1FFFFFFF;
            _solido[i % _ancho, i / _ancho] = gid != 0;
        }
    }

    public bool Choca(Rectangle r)
    {
        // Fuera del mapa cuenta como pared
        if (r.Left < 0 || r.Top < 0 ||
            r.Right > _ancho * _tam || r.Bottom > _alto * _tam)
            return true;

        int x0 = r.Left / _tam;
        int x1 = (r.Right - 1) / _tam;
        int y0 = r.Top / _tam;
        int y1 = (r.Bottom - 1) / _tam;

        for (int y = y0; y <= y1; y++)
            for (int x = x0; x <= x1; x++)
                if (_solido[x, y]) return true;

        return false;
    }

    // Para depurar: dibuja los bloques sólidos en rojo translúcido.
    public void DibujarDebug(SpriteBatch sb, Texture2D pixel, Rectangle vista)
    {
        int x0 = Math.Max(0, vista.Left / _tam);
        int x1 = Math.Min(_ancho - 1, vista.Right / _tam);
        int y0 = Math.Max(0, vista.Top / _tam);
        int y1 = Math.Min(_alto - 1, vista.Bottom / _tam);

        for (int y = y0; y <= y1; y++)
            for (int x = x0; x <= x1; x++)
                if (_solido[x, y])
                    sb.Draw(pixel,
                        new Rectangle(x * _tam, y * _tam, _tam, _tam),
                        Color.Red * 0.4f);
    }
}