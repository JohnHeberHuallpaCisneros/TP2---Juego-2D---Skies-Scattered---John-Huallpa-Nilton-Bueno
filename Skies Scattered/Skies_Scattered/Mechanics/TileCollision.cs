using System;
using System.Collections.Generic;
using System.Xml.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Skies_Scattered.Mechanics;
public sealed class MapExit
{
    public Rectangle Area { get; init; }
    public string Destination { get; init; }
    public bool IsBlocked => string.IsNullOrEmpty(Destination);
}

public class TileCollision
{
    private readonly bool[, ] _solido;
    private readonly List<MapExit> _exits = new();
    private readonly List<MapGift> _gifts = new();
    private readonly int _ancho;
    private readonly int _alto;
    private readonly int _tam;
    public IReadOnlyList<MapGift> Gifts => _gifts;
    public IReadOnlyList<MapExit> Exits => _exits;
    public Vector2 TamanoMundo => new Vector2(_ancho * _tam, _alto * _tam);

    public TileCollision(string rutaTmx, string capa = "coliciones")
    {
        XElement mapa = XDocument.Load(rutaTmx).Root;
        _ancho = (int)mapa.Attribute("width");
        _alto = (int)mapa.Attribute("height");
        _tam = (int)mapa.Attribute("tilewidth");
        _solido = new bool[_ancho, _alto];
        CargarSuelo(mapa, capa);
        CargarObjetos(mapa);
    }

    private void CargarSuelo(XElement mapa, string nombreCapa)
    {
        XElement capa = null;
        foreach (XElement candidata in mapa.Elements("layer"))
        {
            if ((string)candidata.Attribute("name") == nombreCapa)
            {
                capa = candidata;
                break;
            }
        }

        if (capa == null)
            throw new InvalidOperationException("No se encontro la capa de colisiones.");
        string[] ids = capa.Element("data").Value.Split(new[] { ',', '\n', '\r', ' ' }, StringSplitOptions.RemoveEmptyEntries);
        if (ids.Length != _ancho * _alto)
            throw new InvalidOperationException("Capa de colisiones incompleta.");
        for (int i = 0; i < ids.Length; i++)
        {
            int x = i % _ancho;
            int y = i / _ancho;
            uint gid = uint.Parse(ids[i]) & 0x1FFFFFFF;
            _solido[x, y] = gid != 0;
        }
    }

    private void CargarObjetos(XElement mapa)
    {
        foreach (XElement grupo in mapa.Elements("objectgroup"))
        {
            string tipo = (string)grupo.Attribute("name");
            if (tipo != "salidas" && tipo != "regalos")
                continue;
            foreach (XElement objeto in grupo.Elements("object"))
            {
                Rectangle area = LeerRectangulo(objeto);
                if (tipo == "salidas")
                {
                    _exits.Add(new MapExit { Area = area, Destination = LeerPropiedad(objeto, "destino") });
                }
                else
                {
                    _gifts.Add(new MapGift { Id = (string)objeto.Attribute("name"), Item = LeerPropiedad(objeto, "item"), Bounds = area });
                }
            }
        }
    }

    private static Rectangle LeerRectangulo(XElement objeto)
    {
        return new Rectangle((int)(float)objeto.Attribute("x"), (int)(float)objeto.Attribute("y"), (int)(float)objeto.Attribute("width"), (int)(float)objeto.Attribute("height"));
    }

    private static string LeerPropiedad(XElement objeto, string nombre)
    {
        XElement propiedades = objeto.Element("properties");
        if (propiedades == null)
            return null;
        foreach (XElement propiedad in propiedades.Elements("property"))
        {
            if ((string)propiedad.Attribute("name") == nombre)
                return (string)propiedad.Attribute("value");
        }

        return null;
    }

    public MapExit ExitAt(Rectangle pies)
    {
        foreach (MapExit salida in _exits)
        {
            if (salida.Area.Intersects(pies))
                return salida;
        }

        return null;
    }

    public bool Choca(Rectangle r)
    {
        if (r.Left < 0 || r.Top < 0 || r.Right > _ancho * _tam || r.Bottom > _alto * _tam)
            return true;
        foreach (MapExit salida in _exits)
        {
            if (salida.IsBlocked && salida.Area.Intersects(r))
                return true;
        }

        foreach (MapGift regalo in _gifts)
        {
            if (regalo.Collision.Intersects(r))
                return true;
        }

        int x0 = r.Left / _tam;
        int x1 = (r.Right - 1) / _tam;
        int y0 = r.Top / _tam;
        int y1 = (r.Bottom - 1) / _tam;
        for (int y = y0; y <= y1; y++)
        {
            for (int x = x0; x <= x1; x++)
            {
                if (_solido[x, y])
                    return true;
            }
        }

        return false;
    }

    public void DibujarDebug(SpriteBatch sb, Texture2D pixel, Rectangle vista)
    {
        int x0 = Math.Max(0, vista.Left / _tam);
        int x1 = Math.Min(_ancho - 1, vista.Right / _tam);
        int y0 = Math.Max(0, vista.Top / _tam);
        int y1 = Math.Min(_alto - 1, vista.Bottom / _tam);
        for (int y = y0; y <= y1; y++)
        {
            for (int x = x0; x <= x1; x++)
            {
                if (_solido[x, y])
                    sb.Draw(pixel, new Rectangle(x * _tam, y * _tam, _tam, _tam), Color.Red * .4f);
            }
        }
    }
}
