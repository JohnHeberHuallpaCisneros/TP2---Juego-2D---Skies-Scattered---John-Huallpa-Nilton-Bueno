
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using MonoGameLibrary;
using MonoGameLibrary.Graphics;
using Skies_Scattered.Graphics;

namespace Skies_Scattered;

public class Game1 : Core
{
    private AnimatedSprite _Yoko;
    private AnimatedSprite _Kyoko;

    private BattleBackground _battleBackground;

    private Vector2 _yokoPosition = new Vector2(400, 300);
    private Vector2 _kyokoPosition = new Vector2(800, 300);

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
            "Image/atlas-definition.xml");

        _Yoko = atlas.CreateAnimatedSprite("yoko-animation");
        _Yoko.Scale = new Vector2(4.0f, 4.0f);

        _Kyoko = atlas.CreateAnimatedSprite("kyoko-animation");
        _Kyoko.Scale = new Vector2(4.0f, 4.0f);

        _battleBackground = new BattleBackground(Content);
        _battleBackground.LoadContent();
    }

    protected override void Update(GameTime gameTime)
    {
        _Yoko.Update(gameTime);
        _Kyoko.Update(gameTime);

        _battleBackground.Update(gameTime);

        if (Keyboard.GetState().IsKeyDown(Keys.Escape))
        {
            Exit();
        }

        base.Update(gameTime);
    }

    protected override void Draw(GameTime gameTime)
    {
        GraphicsDevice.Clear(Color.Black);

        // Primero dibujamos el fondo.
        _battleBackground.Draw(SpriteBatch);

        // Después dibujamos los personajes encima.
        SpriteBatch.Begin(samplerState: SamplerState.PointClamp);

        _Yoko.Draw(SpriteBatch, _yokoPosition);
        _Kyoko.Draw(SpriteBatch, _kyokoPosition);

        SpriteBatch.End();

        base.Draw(gameTime);
    }
}