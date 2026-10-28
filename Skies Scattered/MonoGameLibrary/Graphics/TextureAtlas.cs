using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Xml;
using System.Xml.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;

namespace MonoGameLibrary.Graphics;

public class TextureAtlas
{
    private readonly Dictionary<string, TextureRegion> _regions;
    private readonly Dictionary<string, Animation> _animations;

    public Texture2D Texture { get; set; }

    public TextureAtlas()
    {
        _regions = new Dictionary<string, TextureRegion>();
        _animations = new Dictionary<string, Animation>();
    }

    public TextureAtlas(Texture2D texture)
    {
        Texture = texture;
        _regions = new Dictionary<string, TextureRegion>();
        _animations = new Dictionary<string, Animation>();
    }

    public void AddRegion(
        string name,
        int x,
        int y,
        int width,
        int height)
    {
        TextureRegion region = new TextureRegion(
            Texture,
            x,
            y,
            width,
            height
        );

        _regions.Add(name, region);
    }

    public TextureRegion GetRegion(string name)
    {
        if (!_regions.TryGetValue(name, out TextureRegion region))
        {
            throw new KeyNotFoundException(
                $"No existe la región '{name}' en el atlas. " +
                $"Regiones disponibles: {string.Join(", ", _regions.Keys)}"
            );
        }

        return region;
    }

    public bool RemoveRegion(string name)
    {
        return _regions.Remove(name);
    }

    public void Clear()
    {
        _regions.Clear();
    }

    public static TextureAtlas FromFile(
        ContentManager content,
        string fileName)
    {
        if (content == null)
        {
            throw new ArgumentNullException(nameof(content));
        }

        TextureAtlas atlas = new TextureAtlas();

        string filePath = Path.Combine(
            content.RootDirectory,
            fileName
        );

        Console.WriteLine(
            $"ATLAS CARGADO DESDE: {Path.GetFullPath(filePath)}"
        );

        using Stream stream = TitleContainer.OpenStream(filePath);
        using XmlReader reader = XmlReader.Create(stream);

        XDocument doc = XDocument.Load(reader);

        XElement root = doc.Root
            ?? throw new InvalidDataException(
                $"El atlas XML está vacío: {filePath}"
            );

        if (root.Name.LocalName != "TextureAtlas")
        {
            throw new InvalidDataException(
                $"La raíz del XML debe ser <TextureAtlas>: {filePath}"
            );
        }

        // CARGAR TEXTURA
        XElement textureElement = root.Element("Texture")
            ?? throw new InvalidDataException(
                $"Falta <Texture> en el atlas: {filePath}"
            );

        string texturePath = textureElement.Value.Trim();

        if (string.IsNullOrWhiteSpace(texturePath))
        {
            throw new InvalidDataException(
                $"La ruta de la textura está vacía: {filePath}"
            );
        }

        atlas.Texture = content.Load<Texture2D>(texturePath);

        // CARGAR REGIONES
        XElement regionsElement = root.Element("Regions")
            ?? throw new InvalidDataException(
                $"Falta <Regions> en el atlas: {filePath}"
            );

        foreach (XElement regionElement
                 in regionsElement.Elements("Region"))
        {
            string name = RequiredAttribute(
                regionElement,
                "name"
            );

            int x = ParseInt(regionElement, "x");
            int y = ParseInt(regionElement, "y");
            int width = ParseInt(regionElement, "width");
            int height = ParseInt(regionElement, "height");

            if (width <= 0 || height <= 0)
            {
                throw new InvalidDataException(
                    $"La región '{name}' tiene dimensiones inválidas."
                );
            }

            atlas.AddRegion(
                name,
                x,
                y,
                width,
                height
            );
        }

        Console.WriteLine(
            $"REGIONES CARGADAS: {atlas._regions.Count}"
        );

        // CARGAR ANIMACIONES
        XElement animationsElement = root.Element("Animations")
            ?? throw new InvalidDataException(
                $"Falta <Animations> en el atlas: {filePath}"
            );

        foreach (XElement animationElement
                 in animationsElement.Elements("Animation"))
        {
            string name = RequiredAttribute(
                animationElement,
                "name"
            );

            string delayText = RequiredAttribute(
                animationElement,
                "delay"
            );

            double delayMilliseconds = double.Parse(
                delayText,
                CultureInfo.InvariantCulture
            );

            TimeSpan delay = TimeSpan.FromMilliseconds(
                delayMilliseconds
            );

            List<TextureRegion> frames =
                new List<TextureRegion>();

            foreach (XElement frameElement
                     in animationElement.Elements("Frame"))
            {
                string regionName = RequiredAttribute(
                    frameElement,
                    "region"
                );

                TextureRegion region = atlas.GetRegion(
                    regionName
                );

                frames.Add(region);
            }

            if (frames.Count == 0)
            {
                throw new InvalidDataException(
                    $"La animación '{name}' no contiene frames."
                );
            }

            Animation animation = new Animation(
                frames,
                delay
            );

            atlas.AddAnimation(name, animation);
        }

        Console.WriteLine(
            $"ANIMACIONES CARGADAS: {atlas._animations.Count}"
        );

        Console.WriteLine(
            "NOMBRES DE ANIMACIONES: " +
            string.Join(", ", atlas._animations.Keys)
        );

        return atlas;
    }

    private static string RequiredAttribute(
        XElement element,
        string attributeName)
    {
        string value = element.Attribute(attributeName)?.Value;

        if (string.IsNullOrWhiteSpace(value))
        {
            throw new InvalidDataException(
                $"Falta el atributo '{attributeName}' " +
                $"en el elemento <{element.Name.LocalName}>."
            );
        }

        return value;
    }

    private static int ParseInt(
        XElement element,
        string attributeName)
    {
        string value = RequiredAttribute(
            element,
            attributeName
        );

        return int.Parse(
            value,
            CultureInfo.InvariantCulture
        );
    }

    public Sprite CreateSprite(string regionName)
    {
        TextureRegion region = GetRegion(regionName);
        return new Sprite(region);
    }

    public void AddAnimation(
        string animationName,
        Animation animation)
    {
        _animations.Add(animationName, animation);
    }

    public Animation GetAnimation(string animationName)
    {
        if (!_animations.TryGetValue(
            animationName,
            out Animation animation))
        {
            throw new KeyNotFoundException(
                $"No se encontró la animación '{animationName}'. " +
                $"Animaciones disponibles: " +
                string.Join(", ", _animations.Keys)
            );
        }

        return animation;
    }

    public bool RemoveAnimation(string animationName)
    {
        return _animations.Remove(animationName);
    }

    public AnimatedSprite CreateAnimatedSprite(
        string animationName)
    {
        Animation animation = GetAnimation(animationName);

        return new AnimatedSprite(animation);
    }
}
