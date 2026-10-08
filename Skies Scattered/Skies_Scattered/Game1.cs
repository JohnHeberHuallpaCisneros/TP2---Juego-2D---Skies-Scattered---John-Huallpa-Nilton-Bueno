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

    private Texture2D _background;
    private Texture2D[] _backgrounds;
    private int _currentBackground = 0;
    
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

        _background = Content.Load<Texture2D>(
        "Image/backgrounds_battle/geometry/triangles01"
);
_backgrounds = new Texture2D[9];

_backgrounds[0] = Content.Load<Texture2D>(
    "Image/backgrounds_battle/geometry/triangles01"
);

_backgrounds[1] = Content.Load<Texture2D>(
    "Image/backgrounds_battle/geometry/squares01"
);

_backgrounds[2] = Content.Load<Texture2D>(
    "Image/backgrounds_battle/geometry/lines02"
);

_backgrounds[3] = Content.Load<Texture2D>(
    "Image/backgrounds_battle/ondulate/lines01"
);

_backgrounds[4] = Content.Load<Texture2D>(
    "Image/backgrounds_battle/ondulate/sea02"
);

    _backgrounds[5] = Content.Load<Texture2D>(
    "Image/backgrounds_battle/ondulate/tiles05"
);

    _backgrounds[6] = Content.Load<Texture2D>(
    "Image/backgrounds_battle/psycho/Degrade01transparent"
);

    _backgrounds[7] = Content.Load<Texture2D>(
    "Image/backgrounds_battle/psycho/kaleidoscope03"
);

    _backgrounds[8] = Content.Load<Texture2D>(
    "Image/backgrounds_battle/psycho/weird03"
);

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
        KeyboardState keyboard = Keyboard.GetState();

    if (keyboard.IsKeyDown(Keys.D1))
    _currentBackground = 0;

    if (keyboard.IsKeyDown(Keys.D2))
    _currentBackground = 1;

    if (keyboard.IsKeyDown(Keys.D3))
    _currentBackground = 2;

    if (keyboard.IsKeyDown(Keys.D4))
    _currentBackground = 3;

    if (keyboard.IsKeyDown(Keys.D5))
    _currentBackground = 4;

    if (keyboard.IsKeyDown(Keys.D6))
    _currentBackground = 5;

    if (keyboard.IsKeyDown(Keys.D7))
    _currentBackground = 6;

    if (keyboard.IsKeyDown(Keys.D8))
    _currentBackground = 7;

    if (keyboard.IsKeyDown(Keys.D9))
    _currentBackground = 8;
    }

protected override void Draw(GameTime gameTime)
{
    GraphicsDevice.Clear(Color.Black);

    SpriteBatch.Begin(
        samplerState: SamplerState.PointClamp
    );

    SpriteBatch.Draw(
        _backgrounds[_currentBackground],
        new Rectangle(
            0,
            0,
            GraphicsDevice.Viewport.Width,
            GraphicsDevice.Viewport.Height
        ),
        Color.White
    );

_Kyoko.Draw(
    SpriteBatch,
    new Vector2(550, 180)
);

_Yoko.Draw(
    SpriteBatch,
    new Vector2(550, 400)
);
    SpriteBatch.End();

    base.Draw(gameTime);
}
}
