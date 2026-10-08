using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using MonoGameLibrary;
using MonoGameLibrary.Graphics;

namespace Skies_Scattered;

public class Game1 : Core
{
    private AnimatedSprite _Yoko;
    private AnimatedSprite _Kyoko;

    // Fondos
    private Texture2D _background1;
    private Texture2D _background2;
    private Texture2D _background3;

    // Shader
    private Effect _battleEffect;

    // Tiempo
    private float _time;

    // Cambio de color
    private float _colorTimer;

    private Color _currentColor = Color.Red;
    private Color _targetColor = Color.Blue;

    public Game1() : base("Skies Scattered", 1280, 720, false)
    {
    }

    protected override void Initialize()
    {
        base.Initialize();
    }

    protected override void LoadContent()
    {
        TextureAtlas atlas = TextureAtlas.FromFile(
            Content,
            "Image/atlas-definition.xml"
        );

        _Yoko = atlas.CreateAnimatedSprite("yoko-animation");
        _Yoko.Scale = new Vector2(4.0f, 4.0f);

        _Kyoko = atlas.CreateAnimatedSprite("kyoko-animation");
        _Kyoko.Scale = new Vector2(4.0f, 4.0f);

        // =========================
        // FONDOS
        // =========================

        _background1 = Content.Load<Texture2D>(
            "Image/backgrounds_battle/geometry/triangles01"
        );

        _background2 = Content.Load<Texture2D>(
            "Image/backgrounds_battle/ondulate/sea02"
        );

        _background3 = Content.Load<Texture2D>(
            "Image/backgrounds_battle/psycho/kaleidoscope03"
        );

        // =========================
        // SHADER
        // =========================

        _battleEffect = Content.Load<Effect>(
            "BattleBackground"
        );
    }

    private void DrawTiledBackground(
        Texture2D texture,
        float offsetX,
        Color color)
    {
        int textureWidth = texture.Width * 4;
        int textureHeight = texture.Height * 4;

        offsetX %= textureWidth;

        for (int y = 0; y < 720 + textureHeight; y += textureHeight)
        {
            for (int x = -textureWidth; x < 1280 + textureWidth; x += textureWidth)
            {
                SpriteBatch.Draw(
                    texture,
                    new Vector2(x + offsetX, y),
                    null,
                    color,
                    0f,
                    Vector2.Zero,
                    4f,
                    SpriteEffects.None,
                    0f
                );
            }
        }
    }

    protected override void Update(GameTime gameTime)
    {
        float deltaTime =
            (float)gameTime.ElapsedGameTime.TotalSeconds;

        _time += deltaTime;

        // Animaciones
        _Yoko.Update(gameTime);
        _Kyoko.Update(gameTime);

        // =========================
        // CAMBIO DE COLOR
        // =========================

        _colorTimer += deltaTime;

        // Cada 6 segundos cambia el color objetivo
        if (_colorTimer >= 6f)
        {
            _colorTimer = 0f;

            if (_targetColor == Color.Blue)
            {
                _targetColor = Color.Red;
            }
            else
            {
                _targetColor = Color.Blue;
            }
        }

        // Transición lenta entre colores
        _currentColor = Color.Lerp(
            _currentColor,
            _targetColor,
            deltaTime * 0.5f
        );

        // Salir
        if (Keyboard.GetState().IsKeyDown(Keys.Escape))
        {
            Exit();
        }

        base.Update(gameTime);
    }

    protected override void Draw(GameTime gameTime)
    {
        GraphicsDevice.Clear(Color.Black);

        // =================================================
        // CAPA 1 - GEOMETRY
        // =================================================

        float x1 = _time * 20f;

        _battleEffect.Parameters["Time"].SetValue(
            _time
        );

        _battleEffect.Parameters["Strength"].SetValue(
            0.015f
        );

        _battleEffect.Parameters["Frequency"].SetValue(
            8.0f
        );

        _battleEffect.Parameters["Tint"].SetValue(
            _currentColor.ToVector4()
        );

        SpriteBatch.Begin(
            samplerState: SamplerState.PointWrap,
            effect: _battleEffect
        );

        DrawTiledBackground(
            _background1,
            x1,
            Color.White
        );

        SpriteBatch.End();


        // =================================================
        // CAPA 2 - ONDULATE
        // =================================================

        float x2 = _time * -35f;

        _battleEffect.Parameters["Time"].SetValue(
            _time * 1.2f
        );

        _battleEffect.Parameters["Strength"].SetValue(
            0.025f
        );

        _battleEffect.Parameters["Frequency"].SetValue(
            10.0f
        );

        _battleEffect.Parameters["Tint"].SetValue(
            _currentColor.ToVector4()
        );

        SpriteBatch.Begin(
            samplerState: SamplerState.PointWrap,
            effect: _battleEffect
        );

        DrawTiledBackground(
            _background2,
            x2,
            Color.White * 0.5f
        );

        SpriteBatch.End();


        // =================================================
        // CAPA 3 - PSYCHO
        // =================================================

        float x3 = _time * 50f;

        _battleEffect.Parameters["Time"].SetValue(
            _time * 1.8f
        );

        _battleEffect.Parameters["Strength"].SetValue(
            0.040f
        );

        _battleEffect.Parameters["Frequency"].SetValue(
            14.0f
        );

        _battleEffect.Parameters["Tint"].SetValue(
            _currentColor.ToVector4()
        );

        SpriteBatch.Begin(
            samplerState: SamplerState.PointWrap,
            effect: _battleEffect
        );

        DrawTiledBackground(
            _background3,
            x3,
            Color.White * 0.4f
        );

        SpriteBatch.End();

        base.Draw(gameTime);
    }
}