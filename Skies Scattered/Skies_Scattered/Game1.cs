using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using MonoGameLibrary;
using MonoGameLibrary.Graphics;
using Skies_Scattered.Graphics;
using Skies_Scattered.UI;

namespace Skies_Scattered;

public class Game1 : Core
{
    // Animaciones de Yoko.
    private AnimatedSprite _Yoko;
    private AnimatedSprite _yokoAtras;
    private AnimatedSprite _yokoFrente;
    private AnimatedSprite _yokoLado;
    private AnimatedSprite _yokoizquierda;
    private AnimatedSprite _yokoDerechaArriba;
    private AnimatedSprite _yokoIzquierdaArriba;
    private AnimatedSprite _yokoDerechaAbajo;
    private AnimatedSprite _yokoIzquierdaAbajo;

    // Animaciones de Kyoko.
    private AnimatedSprite _Kyoko;
    private AnimatedSprite _kyokoAtras;
    private AnimatedSprite _kyokoFrente;
    private AnimatedSprite _kyokoLado;
    private AnimatedSprite _kyokoizquierda;

    // Posiciones y fondo.
    private Vector2 _yokoPosition = new Vector2(400, 300);
    private Vector2 _kyokoPosition = new Vector2(800, 300);

    private BattleBackground _battleBackground;
    
    // Recursos de UI y Combate
    private SpriteFont _retroFont;
    private Texture2D _enemyTexture;
    private Texture2D _earthboundHudTexture;
    private Texture2D _uiBordersTexture;
    
    private BattleUI _battleUI;
    private BattleUIState _battleState;
    

    public Game1() : base("Skies Scattered", 640, 480, false)
    {
    }

    protected override void Initialize()
    {
        base.Initialize();
    }

    private AnimatedSprite CrearAnimacion(TextureAtlas atlas, string nombre)
    {
        AnimatedSprite animacion = atlas.CreateAnimatedSprite(nombre);
        animacion.Scale = new Vector2(3.0f, 3.0f);
        return animacion;
    }

    protected override void LoadContent()
    {
        _earthboundHudTexture = Content.Load<Texture2D>("Image/SNES - EarthBound - Miscellaneous - Battle HUD");
        _uiBordersTexture = Content.Load<Texture2D>("Image/Pxiel Art UI borders"); 
        
        // Cargar fuente y enemigo de la carpeta Enemies
        _retroFont = Content.Load<SpriteFont>("Fonts/RetroFont");
        _enemyTexture = Content.Load<Texture2D>("Image/Enemies/enemy_approaching"); // Cambia por el nombre real de tu archivo de enemigo

        // Inicializar UI con la fuente
        _battleUI = new BattleUI(_retroFont); 

        _battleState = new BattleUIState
        {
            TopMessage = "Here They Are",
            SharedPP = 50,
            PartyMembers = new System.Collections.Generic.List<CharacterStats>
            {
                new CharacterStats { Name = "Yoko", AP = 10, HP = 50, CT = 5 },
                new CharacterStats { Name = "Kyoko", AP = 12, HP = 50, CT = 4 }
            }
        };

        TextureAtlas atlas = TextureAtlas.FromFile(Content, "Image/atlas-definition.xml");

        // Animaciones de Yoko.
        _Yoko = CrearAnimacion(atlas, "yoko-animation");
        _yokoFrente = CrearAnimacion(atlas, "yoko-animation");
        _yokoAtras = CrearAnimacion(atlas, "yoko-atras-animation");
        _yokoLado = CrearAnimacion(atlas, "yoko-lado-animation");
        _yokoizquierda = CrearAnimacion(atlas, "yoko-izquierda-animation");
        _yokoDerechaArriba = CrearAnimacion(atlas, "yoko-DerechaFrente-animation");
        _yokoIzquierdaArriba = CrearAnimacion(atlas, "yoko-izquierdaFrente-animation");
        _yokoDerechaAbajo = CrearAnimacion(atlas, "yoko-DerechaAtras-animation");
        _yokoIzquierdaAbajo = CrearAnimacion(atlas, "yoko-izquierdaAtras-animation");

        // Animaciones de Kyoko.
        _Kyoko = CrearAnimacion(atlas, "kyoko-Frente-animation");
        _kyokoFrente = CrearAnimacion(atlas, "kyoko-Frente-animation");
        _kyokoAtras = CrearAnimacion(atlas, "kyoko-atras-animation");
        _kyokoLado = CrearAnimacion(atlas, "kyoko-lado-animation");
        _kyokoizquierda = CrearAnimacion(atlas, "kyoko-izquierda-animation");

        // Fondo de batalla.
        _battleBackground = new BattleBackground(Content);
        _battleBackground.LoadContent();
    }

    protected override void Update(GameTime gameTime)
    {
        
        base.Update(gameTime);

        _Yoko.Update(gameTime);
        _Kyoko.Update(gameTime);
        _battleBackground.Update(gameTime);
        
        bool yokoSeMovio = false;
        bool kyokoSeMovio = false;

        if (Input.IsKeyPressed(Keys.F11))
        {
            Core.Graphics.IsFullScreen = !Core.Graphics.IsFullScreen;
            Core.Graphics.ApplyChanges();
        }

        if (Input.IsKeyDown(Keys.Escape))
        {
            Exit();
        }

        // Movimiento de Yoko.
        if (Input.IsKeyDown(Keys.W)) { yokoSeMovio = true; _yokoPosition.Y -= 5; _Yoko = _yokoAtras; }
        if (Input.IsKeyDown(Keys.S)) { yokoSeMovio = true; _yokoPosition.Y += 5; _Yoko = _yokoFrente; }
        if (Input.IsKeyDown(Keys.A)) { yokoSeMovio = true; _yokoPosition.X -= 5; _Yoko = _yokoLado; }
        if (Input.IsKeyDown(Keys.D)) { yokoSeMovio = true; _yokoPosition.X += 5; _Yoko = _yokoizquierda; }

        if (!yokoSeMovio) { _Yoko.Reset(); }

        // Movimiento de Kyoko.
        if (Input.IsKeyDown(Keys.Up)) { kyokoSeMovio = true; _kyokoPosition.Y -= 5; _Kyoko = _kyokoAtras; }
        if (Input.IsKeyDown(Keys.Down)) { kyokoSeMovio = true; _kyokoPosition.Y += 5; _Kyoko = _kyokoFrente; }
        if (Input.IsKeyDown(Keys.Left)) { kyokoSeMovio = true; _kyokoPosition.X -= 5; _Kyoko = _kyokoLado; }
        if (Input.IsKeyDown(Keys.Right)) { kyokoSeMovio = true; _kyokoPosition.X += 5; _Kyoko = _kyokoizquierda; }

        if (!kyokoSeMovio) { _Kyoko.Reset(); }

        _yokoPosition = LimitarPosicion(_yokoPosition, _Yoko);
        _kyokoPosition = LimitarPosicion(_kyokoPosition, _Kyoko);
    }

    protected override void Draw(GameTime gameTime)
    {
        GraphicsDevice.Clear(Color.Black);

        // 1. Dibujar fondo de batalla
        _battleBackground.Draw(SpriteBatch, GraphicsDevice);

        // 2. Iniciar SpriteBatch para UI y personajes
        SpriteBatch.Begin(samplerState: SamplerState.PointClamp);

        Vector2 screenSize = new Vector2(GraphicsDevice.Viewport.Width, GraphicsDevice.Viewport.Height);
        if (_battleUI != null && _battleState != null)
        {
            _battleUI.Draw(SpriteBatch, _battleState, _enemyTexture, screenSize);
        }

        _Yoko.Draw(SpriteBatch, _yokoPosition);
        _Kyoko.Draw(SpriteBatch, _kyokoPosition);

        SpriteBatch.End();

        base.Draw(gameTime);
    }

    private Vector2 LimitarPosicion(Vector2 posicion, AnimatedSprite personaje)
    {
        float anchoPantalla = GraphicsDevice.Viewport.Width;
        float altoPantalla = GraphicsDevice.Viewport.Height;

        float ancho = personaje.Width;
        float alto = personaje.Height;

        float izquierda = personaje.Origin.X * personaje.Scale.X;
        float arriba = personaje.Origin.Y * personaje.Scale.Y;

        posicion.X = MathHelper.Clamp(posicion.X, izquierda, anchoPantalla - ancho + izquierda);
        posicion.Y = MathHelper.Clamp(posicion.Y, arriba, altoPantalla - alto + arriba);

        return posicion;
    }
}