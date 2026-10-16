using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using MonoGameLibrary;
using MonoGameLibrary.Graphics;
using Skies_Scattered.Entities;
using Skies_Scattered.Graphics;
using Skies_Scattered.Mechanics;
using Skies_Scattered.States;
using Skies_Scattered.UI;

namespace Skies_Scattered;

public class Game1 : Core  
{  
    // Jugadores con la clase Player
    private Player _yoko;  
    private Player _kyoko;

    // Animaciones de Yoko.  
    private AnimatedSprite _yokoAtras;  
    private AnimatedSprite _yokoFrente;  
    private AnimatedSprite _yokoLado;  
    private AnimatedSprite _yokoizquierda;  
    private AnimatedSprite _yokoDerechaArriba;  
    private AnimatedSprite _yokoIzquierdaArriba;  
    private AnimatedSprite _yokoDerechaAbajo;  
    private AnimatedSprite _yokoIzquierdaAbajo;

    // Animaciones de Kyoko.  
    private AnimatedSprite _kyokoAtras;  
    private AnimatedSprite _kyokoFrente;  
    private AnimatedSprite _kyokoLado;  
    private AnimatedSprite _kyokoizquierda;

    // Fondo de batalla.
    private BattleBackground _battleBackground;  
     
    // Recursos generales y UI  
    private SpriteFont _retroFont;  
    private Texture2D _earthboundHudTexture;  
    private Texture2D _uiBordersTexture;  
    
    // Lista de plantillas de enemigos para el combate aleatorio
    private List<BattleState.EnemyTemplate> _enemyTemplates;
    
    // Sistema de encuentros en la carpeta Mechanics
    private readonly Encounter _encuentros = new Encounter();  
    private IGameState _currentState; // Estado activo (Exploración o Batalla)

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
         
        // Cargar fuente
        _retroFont = Content.Load<SpriteFont>("Fonts/RetroFont");  

        // Cargar todos los enemigos de la carpeta Enemies/
        _enemyTemplates = new List<BattleState.EnemyTemplate>
        {
            new BattleState.EnemyTemplate { Name = "Black Eye", Texture = Content.Load<Texture2D>("Image/Enemies/black_eye"), HP = 40 },
            new BattleState.EnemyTemplate { Name = "Caged", Texture = Content.Load<Texture2D>("Image/Enemies/caged"), HP = 55 },
            new BattleState.EnemyTemplate { Name = "Crypted", Texture = Content.Load<Texture2D>("Image/Enemies/crypted"), HP = 50 },
            new BattleState.EnemyTemplate { Name = "Mantle", Texture = Content.Load<Texture2D>("Image/Enemies/mantle"), HP = 70 },
            new BattleState.EnemyTemplate { Name = "Mothshum", Texture = Content.Load<Texture2D>("Image/Enemies/mothshum"), HP = 35 },
            new BattleState.EnemyTemplate { Name = "Reaper Blossom", Texture = Content.Load<Texture2D>("Image/Enemies/reaper_blossom"), HP = 60 },
            new BattleState.EnemyTemplate { Name = "Snow Twins", Texture = Content.Load<Texture2D>("Image/Enemies/snow_twins"), HP = 45 }
        };

        TextureAtlas atlas = TextureAtlas.FromFile(Content, "Image/atlas-definition.xml");

        // Animaciones de Yoko  
        _yokoFrente = CrearAnimacion(atlas, "yoko-animation");  
        _yokoAtras = CrearAnimacion(atlas, "yoko-atras-animation");  
        _yokoLado = CrearAnimacion(atlas, "yoko-lado-animation");  
        _yokoizquierda = CrearAnimacion(atlas, "yoko-izquierda-animation");  
        _yokoDerechaArriba = CrearAnimacion(atlas, "yoko-DerechaFrente-animation");  
        _yokoIzquierdaArriba = CrearAnimacion(atlas, "yoko-izquierdaFrente-animation");  
        _yokoDerechaAbajo = CrearAnimacion(atlas, "yoko-DerechaAtras-animation");  
        _yokoIzquierdaAbajo = CrearAnimacion(atlas, "yoko-izquierdaAtras-animation");

        // Animaciones de Kyoko  
        _kyokoFrente = CrearAnimacion(atlas, "kyoko-Frente-animation");  
        _kyokoAtras = CrearAnimacion(atlas, "kyoko-atras-animation");  
        _kyokoLado = CrearAnimacion(atlas, "kyoko-lado-animation");  
        _kyokoizquierda = CrearAnimacion(atlas, "kyoko-izquierda-animation");

        // Fondo de batalla  
        _battleBackground = new BattleBackground(Content);  
        _battleBackground.LoadContent();  
        
        // Inicializar a Yoko con la clase Player
        _yoko = new Player(Keys.W, Keys.S, Keys.A, Keys.D, new Vector2(400, 300))  
        {  
            AnimFrente = _yokoFrente,  
            AnimAtras = _yokoAtras,  
            AnimIzquierda = _yokoLado,  
            AnimDerecha = _yokoizquierda,  
            AnimArribaDerecha = _yokoDerechaAbajo,  
            AnimArribaIzquierda = _yokoDerechaArriba,  
            AnimAbajoDerecha = _yokoIzquierdaAbajo,  
            AnimAbajoIzquierda = _yokoIzquierdaArriba  
        };

        // Inicializar a Kyoko con la clase Player
        _kyoko = new Player(Keys.Up, Keys.Down, Keys.Left, Keys.Right, new Vector2(800, 300))  
        {  
            AnimFrente = _kyokoFrente,  
            AnimAtras = _kyokoAtras,  
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

        if (_encuentros.Activo)  
        {  
            if (_currentState is not BattleState)
            {
                var battleState = new BattleState(_retroFont, _uiBordersTexture, _earthboundHudTexture, _enemyTemplates);
                
                battleState.OnBattleEnded += () => {
                    _encuentros.Reiniciar();
                    _currentState = null; 
                };

                _currentState = battleState;
            }

            _currentState.Update(gameTime);
        }  
        else  
        {  
            // Lógica de exploración del mapa con los objetos Player
            Vector2 posAntes = _yoko.Position;

            _yoko.Update(gameTime);  
            _kyoko.Update(gameTime);

            _yoko.Position = LimitarPosicion(_yoko.Position, _yoko.CurrentAnimation);  
            _kyoko.Position = LimitarPosicion(_kyoko.Position, _kyoko.CurrentAnimation);

            float distancia = Vector2.Distance(posAntes, _yoko.Position);  
            _encuentros.Update(distancia);  
        }  

        Window.Title = $"Pasos para el combate: {_encuentros.PasosRestantes}";
    }

    protected override void Draw(GameTime gameTime)  
    {  
        GraphicsDevice.Clear(Color.Black);

        // 1. Dibujar fondo de batalla  
        _battleBackground.Draw(SpriteBatch, GraphicsDevice);

        // 2. Dibujar UI y sprites según el estado activo  
        SpriteBatch.Begin(samplerState: SamplerState.PointClamp);

        if (_encuentros.Activo && _currentState != null)  
        {  
            _currentState.Draw(SpriteBatch, GraphicsDevice);  
        }  
        else  
        {  
            _yoko.CurrentAnimation?.Draw(SpriteBatch, _yoko.Position);  
            _kyoko.CurrentAnimation?.Draw(SpriteBatch, _kyoko.Position);  
        }  

        SpriteBatch.End();  
        base.Draw(gameTime);  
    }

    private Vector2 LimitarPosicion(Vector2 posicion, AnimatedSprite personaje)  
    {  
        if (personaje == null) return posicion;

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