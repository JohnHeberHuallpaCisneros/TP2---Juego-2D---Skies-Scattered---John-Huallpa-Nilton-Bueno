using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using MonoGameLibrary;
using MonoGameLibrary.Graphics;

namespace Skies_Scattered;

public class Game1 : Core
{
    // texture region that defines the slime sprite in the atlas.
    // Defines the slime animated sprite.
    private AnimatedSprite _Yoko;

    // Defines the bat animated sprite.
    private AnimatedSprite _Kyoko;
    public Game1() : base("Skies Scattered", 1280, 720, false)
    {

    }

    protected override void Initialize()
    {
        // TODO: Add your initialization logic here

        base.Initialize();
    }

    protected override void LoadContent()
    {
        // Create the texture atlas from the XML configuration file
        TextureAtlas atlas = TextureAtlas.FromFile(Content, "Image/atlas-definition.xml");

        // Create the slime animated sprite from the atlas.
        _Yoko = atlas.CreateAnimatedSprite("yoko-animation");
        _Yoko.Scale = new Vector2(4.0f, 4.0f);

        // Create the bat animated sprite from the atlas.
        _Kyoko = atlas.CreateAnimatedSprite("kyoko-animation");
        _Kyoko.Scale = new Vector2(4.0f, 4.0f);
    }

    protected override void Update(GameTime gameTime)
    {
        if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed || Keyboard.GetState().IsKeyDown(Keys.Escape))
            Exit();
                    // Update the slime animated sprite.
        _Yoko.Update(gameTime);

        // Update the bat animated sprite.
        _Kyoko.Update(gameTime);

        // TODO: Add your update logic here

        base.Update(gameTime);
    }

    protected override void Draw(GameTime gameTime)
    {
        // Clear the back buffer.
        GraphicsDevice.Clear(Color.CornflowerBlue);

        // Begin the sprite batch to prepare for rendering.
        SpriteBatch.Begin(samplerState: SamplerState.PointClamp);

        // Draw the slime sprite.
        _Yoko.Draw(SpriteBatch, Vector2.One);

        // Draw the bat sprite 10px to the right of the slime.
        _Kyoko.Draw(SpriteBatch, new Vector2(_Yoko.Width + 10, 0));

        // Always end the sprite batch when finished.
        SpriteBatch.End();

        base.Draw(gameTime);
    }
}
