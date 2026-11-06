using System;
using Skies_Scattered.UI;
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
    private readonly RasterizerState _rasterizerState = new RasterizerState
    {
        CullMode = CullMode.None,
        ScissorTestEnable = true
    };
    public BattleBackground(ContentManager content)
    {
        _content = content;
    }

    public void LoadContent()
    {
        _background1 = _content.Load<Texture2D>("Image/backgrounds_battle/geometry/triangles01");
        _background2 = _content.Load<Texture2D>("Image/backgrounds_battle/ondulate/sea02");
        _background3 = _content.Load<Texture2D>("Image/backgrounds_battle/psycho/kaleidoscope03");
        _battleEffect = _content.Load<Effect>("BattleBackground");
    }

    public void Update(GameTime gameTime)
    {
        float deltaTime = (float)gameTime.ElapsedGameTime.TotalSeconds;
        _time += deltaTime;
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

    public void Draw(SpriteBatch spriteBatch, GraphicsDevice graphicsDevice)
    {
        Rectangle previousScissor = graphicsDevice.ScissorRectangle;
        graphicsDevice.ScissorRectangle = new Rectangle(0, BattleUI.TopBandHeight, graphicsDevice.Viewport.Width, Math.Max(1, graphicsDevice.Viewport.Height - BattleUI.TopBandHeight - BattleUI.BottomBandHeight));
        Color darkPlum = new Color(20, 15, 29);
        Color darkNavy = new Color(12, 20, 34);
        Color lightPlum = new Color(100, 72, 104);
        Color lightBlue = new Color(65, 83, 111);
        Color darkPalette = Color.Lerp(darkPlum, darkNavy, _paletteMix);
        Color lightPalette = Color.Lerp(lightPlum, lightBlue, _paletteMix);
        int ancho = graphicsDevice.Viewport.Width;
        int alto = graphicsDevice.Viewport.Height;
        DrawLayer(spriteBatch, _background1, _time * 10f, _time * 0.8f, 0.015f, 8f, Color.White, ancho, alto, darkPalette, lightPalette);
        DrawLayer(spriteBatch, _background2, _time * -16f, _time * 0.9f, 0.025f, 10f, Color.White * 0.5f, ancho, alto, darkPalette, lightPalette);
        DrawLayer(spriteBatch, _background3, _time * 22f, _time * 1.1f, 0.040f, 14f, Color.White * 0.4f, ancho, alto, darkPalette, lightPalette);
        graphicsDevice.ScissorRectangle = previousScissor;
    }

    private void DrawLayer(SpriteBatch spriteBatch, Texture2D texture, float offsetX, float effectTime, float strength, float frequency, Color color, int ancho, int alto, Color darkPalette, Color lightPalette)
    {
        _battleEffect.Parameters["Time"].SetValue(effectTime);
        _battleEffect.Parameters["Strength"].SetValue(strength);
        _battleEffect.Parameters["Frequency"].SetValue(frequency);
        _battleEffect.Parameters["PaletteDark"].SetValue(darkPalette.ToVector4());
        _battleEffect.Parameters["PaletteLight"].SetValue(lightPalette.ToVector4());
        _battleEffect.Parameters["PaletteStrength"].SetValue(0.85f);
        spriteBatch.Begin(samplerState: SamplerState.PointWrap, rasterizerState: _rasterizerState, effect: _battleEffect);
        DrawTiledBackground(spriteBatch, texture, offsetX, color, ancho, alto);
        spriteBatch.End();
    }

    private void DrawTiledBackground(SpriteBatch spriteBatch, Texture2D texture, float offsetX, Color color, int ancho, int alto)
    {
        int textureWidth = texture.Width * 4;
        int textureHeight = texture.Height * 4;
        offsetX %= textureWidth;
        for (int y = -textureHeight; y < alto + textureHeight; y += textureHeight)
        {
            for (int x = -textureWidth; x < ancho + textureWidth; x += textureWidth)
            {
                spriteBatch.Draw(texture, new Vector2(x + offsetX, y), null, color, 0f, Vector2.Zero, 4f, SpriteEffects.None, 0f);
            }
        }
    }
}
