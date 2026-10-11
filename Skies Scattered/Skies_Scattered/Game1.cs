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
    private AnimatedSprite _yokoAtras;
    private AnimatedSprite _yokoFrente;
    private AnimatedSprite _yokoLado;
    private AnimatedSprite _yokoizquierda;
    private AnimatedSprite _yokoDerechaArriba;
    private AnimatedSprite _yokoIzquierdaArriba;
    private AnimatedSprite _yokoDerechaAbajo;
    private AnimatedSprite _yokoIzquierdaAbajo;

    private AnimatedSprite _Kyoko;
    private AnimatedSprite _kyokoAtras;
    private AnimatedSprite _kyokoFrente;
    private AnimatedSprite _kyokoLado;
    private AnimatedSprite _kyokoizquierda;
    private Vector2 _direccion = Vector2.Zero;
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
        _yokoFrente = atlas.CreateAnimatedSprite("yoko-animation");
        _yokoFrente.Scale = new Vector2(4.0f, 4.0f);
        _yokoAtras = atlas.CreateAnimatedSprite("yoko-atras-animation");
        _yokoAtras.Scale = new Vector2(4.0f, 4.0f);
        _yokoLado = atlas.CreateAnimatedSprite("yoko-lado-animation");
        _yokoLado.Scale = new Vector2(4.0f, 4.0f);  
        _yokoizquierda = atlas.CreateAnimatedSprite("yoko-izquierda-animation");
        _yokoizquierda.Scale = new Vector2(4.0f, 4.0f);
        _yokoDerechaArriba = atlas.CreateAnimatedSprite("yoko-DerechaFrente-animation");
        _yokoDerechaArriba.Scale = new Vector2(4.0f, 4.0f);
        _yokoIzquierdaArriba = atlas.CreateAnimatedSprite("yoko-izquierdaFrente-animation");
        _yokoIzquierdaArriba.Scale = new Vector2(4.0f, 4.0f);
        _yokoDerechaAbajo = atlas.CreateAnimatedSprite("yoko-DerechaAtras-animation");
        _yokoDerechaAbajo.Scale = new Vector2(4.0f, 4.0f);
        _yokoIzquierdaAbajo = atlas.CreateAnimatedSprite("yoko-izquierdaAtras-animation");
        _yokoIzquierdaAbajo.Scale = new Vector2(4.0f, 4.0f);


        _Kyoko = atlas.CreateAnimatedSprite("kyoko-Frente-animation");
        _Kyoko.Scale = new Vector2(4.0f, 4.0f);
        _kyokoFrente = atlas.CreateAnimatedSprite("kyoko-Frente-animation");
        _kyokoFrente.Scale = new Vector2(4.0f, 4.0f);
        _kyokoAtras = atlas.CreateAnimatedSprite("kyoko-atras-animation");
        _kyokoAtras.Scale = new Vector2(4.0f, 4.0f);
        _kyokoLado = atlas.CreateAnimatedSprite("kyoko-lado-animation");
        _kyokoLado.Scale = new Vector2(4.0f, 4.0f);
        _kyokoizquierda = atlas.CreateAnimatedSprite("kyoko-izquierda-animation");
        _kyokoizquierda.Scale = new Vector2(4.0f, 4.0f);


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

             Vector2 NuevoDireccion = Vector2.Zero;
        if (Keyboard.GetState().IsKeyDown(Keys.W))
        {
            _yokoPosition.Y -= 5;
            NuevoDireccion.Y -=1;
            _Yoko = _yokoAtras;

        }
        if (Keyboard.GetState().IsKeyDown(Keys.S))
        {
            _yokoPosition.Y += 5;
            NuevoDireccion.Y +=1;
            _Yoko = _yokoFrente;
        }
        if (Keyboard.GetState().IsKeyDown(Keys.A))
        {
            _yokoPosition.X -= 5;
            _direccion.X -=1;
            _Yoko = _yokoLado;
        }
        if (Keyboard.GetState().IsKeyDown(Keys.D))
        {
            _yokoPosition.X += 5;
            NuevoDireccion.X +=1;
            _Yoko = _yokoizquierda;
        }
//---------------------------------------------------------------------------------
        if (Keyboard.GetState().IsKeyDown(Keys.W) && Keyboard.GetState().IsKeyDown(Keys.D))
        {
            _yokoPosition.X += 1;
            _yokoPosition.Y -= 1;
            _Yoko = _yokoDerechaAbajo;
        }
        if (Keyboard.GetState().IsKeyDown(Keys.W) && Keyboard.GetState().IsKeyDown(Keys.A))
        {
            _yokoPosition.X -= 1;
            _yokoPosition.Y -= 1;
            _Yoko = _yokoDerechaArriba;
        }
        if (Keyboard.GetState().IsKeyDown(Keys.S) && Keyboard.GetState().IsKeyDown(Keys.D))
        {
            _yokoPosition.X += 1;
            _yokoPosition.Y += 1;
            _Yoko = _yokoIzquierdaAbajo;
        }
        if (Keyboard.GetState().IsKeyDown(Keys.S) && Keyboard.GetState().IsKeyDown(Keys.A))
        {
            _yokoPosition.X -= 1;
            _yokoPosition.Y += 1;
            _Yoko = _yokoIzquierdaArriba;
        }


        if (NuevoDireccion != Vector2.Zero)
        {
            _direccion = NuevoDireccion;
            _direccion.Normalize();
        }
//---------------------------------------------------------------------------------
        
        if (Keyboard.GetState().IsKeyDown(Keys.Up))
        {
            _kyokoPosition.Y -= 5;
            NuevoDireccion.Y -=1;
            _Kyoko = _kyokoAtras;

        }
        if (Keyboard.GetState().IsKeyDown(Keys.Down))
        {
            _kyokoPosition.Y += 5;
            NuevoDireccion.Y +=1;
            _Kyoko = _kyokoFrente;
        }
        if (Keyboard.GetState().IsKeyDown(Keys.Left))
        {
            _kyokoPosition.X -= 5;
            _direccion.X -=1;
            _Kyoko = _kyokoLado;
        }
        if (Keyboard.GetState().IsKeyDown(Keys.Right))
        {
           _kyokoPosition.X += 5;
            NuevoDireccion.X +=1;
            _Kyoko = _kyokoizquierda;
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
