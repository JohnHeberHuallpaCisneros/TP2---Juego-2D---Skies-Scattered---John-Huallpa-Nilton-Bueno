using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using MonoGameLibrary;
using MonoGameLibrary.Graphics;
using MonoGameLibrary.Mecanicas;
using Skies_Scattered.Graphics;
using Skies_Scattered.UI;

namespace Skies_Scattered;

public class Game1 : Core
{
    // Animaciones de Yoko.
    private Teclado _controlYoko;
    private AnimatedSprite _yokoAtras;
    private AnimatedSprite _yokoFrente;
    private AnimatedSprite _yokoLado;
    private AnimatedSprite _yokoizquierda;
    private AnimatedSprite _yokoDerechaArriba;
    private AnimatedSprite _yokoIzquierdaArriba;
    private AnimatedSprite _yokoDerechaAbajo;
    private AnimatedSprite _yokoIzquierdaAbajo;

    // Animaciones de Kyoko.
    private Teclado _controlKyoko;
    private AnimatedSprite _kyokoAtras;
    private AnimatedSprite _kyokoFrente;
    private AnimatedSprite _kyokoLado;
    private AnimatedSprite _kyokoizquierda;

    // Posiciones y fondo.

    private BattleBackground _battleBackground;
    
    // Recursos de UI y Combate
    private SpriteFont _retroFont;
    private Texture2D _enemyTexture;
    private Texture2D _earthboundHudTexture;
    private Texture2D _uiBordersTexture;
    private readonly Encuentro _encuentros = new Encuentro();
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
        _yokoFrente = CrearAnimacion(atlas, "yoko-animation");
        _yokoAtras = CrearAnimacion(atlas, "yoko-atras-animation");
        _yokoLado = CrearAnimacion(atlas, "yoko-lado-animation");
        _yokoizquierda = CrearAnimacion(atlas, "yoko-izquierda-animation");
        _yokoDerechaArriba = CrearAnimacion(atlas, "yoko-DerechaFrente-animation");
        _yokoIzquierdaArriba = CrearAnimacion(atlas, "yoko-izquierdaFrente-animation");
        _yokoDerechaAbajo = CrearAnimacion(atlas, "yoko-DerechaAtras-animation");
        _yokoIzquierdaAbajo = CrearAnimacion(atlas, "yoko-izquierdaAtras-animation");

        // Animaciones de Kyoko.
        _kyokoFrente = CrearAnimacion(atlas, "kyoko-Frente-animation");
        _kyokoAtras = CrearAnimacion(atlas, "kyoko-atras-animation");
        _kyokoLado = CrearAnimacion(atlas, "kyoko-lado-animation");
        _kyokoizquierda = CrearAnimacion(atlas, "kyoko-izquierda-animation");

        // Fondo de batalla.
        _battleBackground = new BattleBackground(Content);
        _battleBackground.LoadContent();
        
 _controlYoko = new Teclado(Keys.W, Keys.S, Keys.A, Keys.D)
        {
            Posicion = new Vector2(400, 300),
            Actual = _yokoFrente,

            AnimArriba = _yokoAtras,
            AnimAbajo = _yokoFrente,
            AnimIzquierda = _yokoLado,
            AnimDerecha = _yokoizquierda,

            AnimArribaDerecha = _yokoDerechaAbajo,
            AnimArribaIzquierda = _yokoDerechaArriba,
            AnimAbajoDerecha = _yokoIzquierdaAbajo,
            AnimAbajoIzquierda = _yokoIzquierdaArriba
        };

        _controlKyoko = new Teclado(Keys.Up, Keys.Down, Keys.Left, Keys.Right)
        {
            Posicion = new Vector2(800, 300),
            Actual = _kyokoFrente,

            AnimArriba = _kyokoAtras,
            AnimAbajo = _kyokoFrente,
            AnimIzquierda = _kyokoLado,
            AnimDerecha = _kyokoizquierda
        };
        
    }

    protected override void Update(GameTime gameTime)
    {
        
        base.Update(gameTime);

        _battleBackground.Update(gameTime);

        if (Input.IsKeyPressed(Keys.F11))
        {
            Core.Graphics.IsFullScreen = !Core.Graphics.IsFullScreen;
            Core.Graphics.ApplyChanges();
        }

        if (!_encuentros.Activo)
{
    Vector2 posAntes = _controlYoko.Posicion;

    _controlYoko.Update(gameTime);
    _controlKyoko.Update(gameTime);

    _controlYoko.Posicion  = LimitarPosicion(_controlYoko.Posicion,  _controlYoko.Actual);
    _controlKyoko.Posicion = LimitarPosicion(_controlKyoko.Posicion, _controlKyoko.Actual);

    float distancia = Vector2.Distance(posAntes, _controlYoko.Posicion);
    _encuentros.Update(distancia);
}
else if (Input.IsKeyPressed(Keys.Enter))
{
    _encuentros.Reiniciar();   // por ahora, Enter "termina" el encuentro
}
    Window.Title = $"Probabilidad: {_encuentros.Probabilidad:P1}";
    }

    protected override void Draw(GameTime gameTime)
    {
        GraphicsDevice.Clear(Color.Black);

        // 1. Dibujar fondo de batalla
        _battleBackground.Draw(SpriteBatch, GraphicsDevice);

        // 2. Iniciar SpriteBatch para UI y personajes
        SpriteBatch.Begin(samplerState: SamplerState.PointClamp);

        if (_encuentros.Activo)
        {
            Vector2 screenSize = new Vector2(GraphicsDevice.Viewport.Width, GraphicsDevice.Viewport.Height);
            _battleUI.Draw(SpriteBatch, _battleState, _enemyTexture, screenSize);
        }
        else
        {
            _controlYoko.Actual.Draw(SpriteBatch, _controlYoko.Posicion);
            _controlKyoko.Actual.Draw(SpriteBatch, _controlKyoko.Posicion);
        }
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