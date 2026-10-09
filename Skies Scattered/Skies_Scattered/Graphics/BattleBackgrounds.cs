
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;

namespace Skies_Scattered.Graphics;

public class BattleBackground
{
    private readonly ContentManager _content;

    private Texture2D _background1;
    private Texture2D _background2;
    private Texture2D _background3;
    private Effect _battleEffect;

    private float _time;
    private float _paletteMix;
    private float _paletteDirection = 1f;

    public BattleBackground(ContentManager content)
    {
        _content = content;
    }

    public void LoadContent()
    {
        _background1 = _content.Load<Texture2D>(
            "Image/backgrounds_battle/geometry/triangles01");

        _background2 = _content.Load<Texture2D>(
            "Image/backgrounds_battle/ondulate/sea02");

        _background3 = _content.Load<Texture2D>(
            "Image/backgrounds_battle/psycho/kaleidoscope03");

        _battleEffect = _content.Load<Effect>("BattleBackground");
    }

    public void Update(GameTime gameTime)
    {
        float deltaTime =
            (float)gameTime.ElapsedGameTime.TotalSeconds;

        _time += deltaTime;

        // Alterna suavemente entre la paleta roja y la azul.
        _paletteMix += deltaTime * 0.12f * _paletteDirection;

        if (_paletteMix >= 1f)
        {
            _paletteMix = 1f;
            _paletteDirection = -1f;
        }
        else if (_paletteMix <= 0f)
        {
            _paletteMix = 0f;
            _paletteDirection = 1f;
        }
    }

    public void Draw(SpriteBatch spriteBatch)
    {
        Color darkRed = new Color(22, 0, 10);
        Color darkBlue = new Color(0, 3, 28);

        Color lightRed = new Color(145, 12, 38);
        Color lightBlue = new Color(22, 65, 150);

        Color darkPalette =
            Color.Lerp(darkRed, darkBlue, _paletteMix);

        Color lightPalette =
            Color.Lerp(lightRed, lightBlue, _paletteMix);

        DrawLayer(
            spriteBatch, _background1,
            _time * 20f, _time,
            0.015f, 8f, Color.White);

        DrawLayer(
            spriteBatch, _background2,
            _time * -35f, _time * 1.2f,
            0.025f, 10f, Color.White * 0.5f);

        DrawLayer(
            spriteBatch, _background3,
            _time * 50f, _time * 1.8f,
            0.040f, 14f, Color.White * 0.4f,
            darkPalette, lightPalette);
    }

    private void DrawLayer(
        SpriteBatch spriteBatch,
        Texture2D texture,
        float offsetX,
        float effectTime,
        float strength,
        float frequency,
        Color color,
        Color? darkPalette = null,
        Color? lightPalette = null)
    {
        _battleEffect.Parameters["Time"].SetValue(effectTime);
        _battleEffect.Parameters["Strength"].SetValue(strength);
        _battleEffect.Parameters["Frequency"].SetValue(frequency);

        // Si no se especifica una paleta, usa la actual igualmente.
        if (darkPalette.HasValue && lightPalette.HasValue)
        {
            _battleEffect.Parameters["PaletteDark"]
                .SetValue(darkPalette.Value.ToVector4());

            _battleEffect.Parameters["PaletteLight"]
                .SetValue(lightPalette.Value.ToVector4());
        }
        else
        {
            Color darkRed = new Color(22, 0, 10);
            Color darkBlue = new Color(0, 3, 28);
            Color lightRed = new Color(145, 12, 38);
            Color lightBlue = new Color(22, 65, 150);

            _battleEffect.Parameters["PaletteDark"].SetValue(
                Color.Lerp(darkRed, darkBlue, _paletteMix).ToVector4());

            _battleEffect.Parameters["PaletteLight"].SetValue(
                Color.Lerp(lightRed, lightBlue, _paletteMix).ToVector4());
        }

        _battleEffect.Parameters["PaletteStrength"].SetValue(0.70f);

        spriteBatch.Begin(
            samplerState: SamplerState.PointWrap,
            effect: _battleEffect);

        DrawTiledBackground(spriteBatch, texture, offsetX, color);

        spriteBatch.End();
    }

    private void DrawTiledBackground(
        SpriteBatch spriteBatch,
        Texture2D texture,
        float offsetX,
        Color color)
    {
        int textureWidth = texture.Width * 4;
        int textureHeight = texture.Height * 4;

        offsetX %= textureWidth;

        for (int y = 0; y < 720 + textureHeight; y += textureHeight)
        {
            for (int x = -textureWidth;
                x < 1280 + textureWidth;
                x += textureWidth)
            {
                spriteBatch.Draw(
                    texture,
                    new Vector2(x + offsetX, y),
                    null,
                    color,
                    0f,
                    Vector2.Zero,
                    4f,
                    SpriteEffects.None,
                    0f);
            }
        }
    }
}