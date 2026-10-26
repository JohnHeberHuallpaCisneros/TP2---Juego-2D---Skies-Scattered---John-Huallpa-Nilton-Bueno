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

namespace Skies_Scattered;

public class Game1 : Core
{
    // =========================================
    // PERSONAJES
    // =========================================

    private Player _yoko;
    private Player _kyoko;

    // Animaciones de Yoko
    private AnimatedSprite _yokoAtras;
    private AnimatedSprite _yokoFrente;
    private AnimatedSprite _yokoLado;
    private AnimatedSprite _yokoIzquierda;

    private AnimatedSprite _yokoDerechaArriba;
    private AnimatedSprite _yokoIzquierdaArriba;
    private AnimatedSprite _yokoDerechaAbajo;
    private AnimatedSprite _yokoIzquierdaAbajo;

    // Animaciones de Kyoko
    private AnimatedSprite _kyokoAtras;
    private AnimatedSprite _kyokoFrente;
    private AnimatedSprite _kyokoLado;
    private AnimatedSprite _kyokoIzquierda;

    private AnimatedSprite _kyokoDerechaArriba;
    private AnimatedSprite _kyokoIzquierdaArriba;
    private AnimatedSprite _kyokoDerechaAbajo;
    private AnimatedSprite _kyokoIzquierdaAbajo;

    // =========================================
    // RECURSOS
    // =========================================

    private BattleBackground _battleBackground;

    private SpriteFont _retroFont;
    private Texture2D _earthboundHudTexture;
    private Texture2D _uiBordersTexture;

    // =========================================
    // SISTEMAS
    // =========================================

    private readonly Encounter _encuentros = new Encounter();
    private readonly Camera2D _camera = new Camera2D();

    private readonly Vector2 _worldSize =
        new Vector2(1920, 1440);

    private List<BattleState.EnemyTemplate> _enemyTemplates;

    private IGameState _currentState;
    private MainMenuState _mainMenu;

    private bool _enMenu = true;

    private static readonly Color ColorCielo =
        new Color(135, 206, 235);

    // =========================================
    // CONSTRUCTOR
    // =========================================

    public Game1()
        : base("Skies Scattered", 640, 480, false)
    {
    }

    // =========================================
    // CARGA
    // =========================================

    protected override void LoadContent()
    {
        base.LoadContent();

        CargarRecursos();
        CargarEnemigos();

        TextureAtlas atlas =
            TextureAtlas.FromFile(
                Content,
                "atlas-definition.xml");

        CargarAnimaciones(atlas);
        CrearPersonajes();

        CrearFondoDeBatalla();
        CrearMenu();
    }

    private void CargarRecursos()
    {
        _earthboundHudTexture =
            Content.Load<Texture2D>(
                "Image/Boxes_UI/SNES - EarthBound - Miscellaneous - Battle HUD");

        _uiBordersTexture =
            Content.Load<Texture2D>(
                "Image/Boxes_UI/Pxiel Art UI borders");

        _retroFont =
            Content.Load<SpriteFont>("Fonts/RetroFont");
    }

    private void CargarEnemigos()
    {
        _enemyTemplates =
            new List<BattleState.EnemyTemplate>
            {
                new BattleState.EnemyTemplate
                {
                    Name = "Black Eye",
                    Texture = Content.Load<Texture2D>(
                        "Image/Enemies/black_eye"),
                    HP = 40
                },

                new BattleState.EnemyTemplate
                {
                    Name = "Caged",
                    Texture = Content.Load<Texture2D>(
                        "Image/Enemies/caged"),
                    HP = 55
                },

                new BattleState.EnemyTemplate
                {
                    Name = "Crypted",
                    Texture = Content.Load<Texture2D>(
                        "Image/Enemies/crypted"),
                    HP = 50
                },

                new BattleState.EnemyTemplate
                {
                    Name = "Mantle",
                    Texture = Content.Load<Texture2D>(
                        "Image/Enemies/mantle"),
                    HP = 70
                },

                new BattleState.EnemyTemplate
                {
                    Name = "Mothshum",
                    Texture = Content.Load<Texture2D>(
                        "Image/Enemies/mothshum"),
                    HP = 35
                },

                new BattleState.EnemyTemplate
                {
                    Name = "Reaper Blossom",
                    Texture = Content.Load<Texture2D>(
                        "Image/Enemies/reaper_blossom"),
                    HP = 60
                },

                new BattleState.EnemyTemplate
                {
                    Name = "Snow Twins",
                    Texture = Content.Load<Texture2D>(
                        "Image/Enemies/snow_twins"),
                    HP = 45
                }
            };
    }

    // =========================================
    // ANIMACIONES
    // =========================================

    private AnimatedSprite CrearAnimacion(
        TextureAtlas atlas,
        string nombre)
    {
        AnimatedSprite animacion =
            atlas.CreateAnimatedSprite(nombre);

        animacion.Scale = new Vector2(3f, 3f);

        return animacion;
    }

    private void CargarAnimaciones(TextureAtlas atlas)
    {
        // =====================================
        // YOKO
        // Se conserva el orden original
        // =====================================

        // 1. Frente
        _yokoFrente =
            CrearAnimacion(atlas, "yoko-animation");

        // 2. Atrás
        _yokoAtras =
            CrearAnimacion(atlas, "yoko-atras-animation");

        // 3. Lado
        _yokoLado =
            CrearAnimacion(atlas, "yoko-lado-animation");

        // 4. Izquierda
        _yokoIzquierda =
            CrearAnimacion(atlas, "yoko-izquierda-animation");

        // 5. Izquierda abajo
        _yokoIzquierdaAbajo =
            CrearAnimacion(atlas, "yoko-izquierdaFrente-animation");

        // 6. Izquierda arriba
        _yokoIzquierdaArriba =
            CrearAnimacion(atlas, "yoko-izquierdaAtras-animation");

        // 7. Derecha abajo
        _yokoDerechaAbajo =
            CrearAnimacion(atlas, "yoko-DerechaFrente-animation");

        // 8. Derecha arriba
        _yokoDerechaArriba =
            CrearAnimacion(atlas, "yoko-DerechaAtras-animation");


        // =====================================
        // KYOKO
        // Se conserva el mismo orden
        // =====================================

        // 1. Frente
        _kyokoFrente =
            CrearAnimacion(atlas, "kyoko-Frente-animation");

        // 2. Atrás
        _kyokoAtras =
            CrearAnimacion(atlas, "kyoko-atras-animation");

        // 3. Lado
        _kyokoLado =
            CrearAnimacion(atlas, "kyoko-lado-animation");

        // 4. Izquierda
        _kyokoIzquierda =
            CrearAnimacion(atlas, "kyoko-izquierda-animation");

        // 5. Izquierda abajo
        _kyokoIzquierdaAbajo =
            CrearAnimacion(atlas, "kyoko-izquierdaFrente-animation");

        // 6. Izquierda arriba
        _kyokoIzquierdaArriba =
            CrearAnimacion(atlas, "kyoko-izquierdaAtras-animation");

        // 7. Derecha abajo
        _kyokoDerechaAbajo =
            CrearAnimacion(atlas, "kyoko-DerechaFrente-animation");

        // 8. Derecha arriba
        _kyokoDerechaArriba =
            CrearAnimacion(atlas, "kyoko-DerechaAtras-animation");
    }

    // =========================================
    // PERSONAJES
    // =========================================

    private void CrearPersonajes()
    {
        // =====================================
        // YOKO
        // =====================================

        _yoko = new Player(
            Keys.W,
            Keys.S,
            Keys.A,
            Keys.D,
            new Vector2(400, 300))
        {
            AnimFrente = _yokoFrente,
            AnimAtras = _yokoAtras,

            AnimIzquierda = _yokoLado,
            AnimDerecha = _yokoIzquierda,

            AnimArribaDerecha = _yokoDerechaArriba,
            AnimArribaIzquierda = _yokoIzquierdaArriba,

            AnimAbajoDerecha = _yokoDerechaAbajo,
            AnimAbajoIzquierda = _yokoIzquierdaAbajo
        };

        // =====================================
        // KYOKO
        // =====================================

        _kyoko = new Player(
            Keys.Up,
            Keys.Down,
            Keys.Left,
            Keys.Right,
            new Vector2(500, 300))
        {
            AnimFrente = _kyokoFrente,
            AnimAtras = _kyokoAtras,

            AnimIzquierda = _kyokoLado,
            AnimDerecha = _kyokoIzquierda,

            AnimArribaDerecha = _kyokoDerechaArriba,
            AnimArribaIzquierda = _kyokoIzquierdaArriba,

            AnimAbajoDerecha = _kyokoDerechaAbajo,
            AnimAbajoIzquierda = _kyokoIzquierdaAbajo
        };

        _yoko.CurrentAnimation = _yokoFrente;
        _kyoko.CurrentAnimation = _kyokoFrente;
    }

    // =========================================
    // MENÚ Y BATALLA
    // =========================================

    private void CrearFondoDeBatalla()
    {
        _battleBackground =
            new BattleBackground(Content);

        _battleBackground.LoadContent();
    }

    private void CrearMenu()
    {
        _mainMenu = new MainMenuState(
            _retroFont,
            _earthboundHudTexture);

        _mainMenu.OnStartGame += IniciarJuego;
        _mainMenu.OnExitGame += Exit;
    }

    private void IniciarJuego()
    {
        _enMenu = false;

        _camera.Follow(
            _yoko.Position,
            TamanoVista(),
            _worldSize,
            snap: true);
    }

    // =========================================
    // ACTUALIZACIÓN GENERAL
    // =========================================

    protected override void Update(GameTime gameTime)
    {
        base.Update(gameTime);

        ActualizarPantalla();

        if (_enMenu)
        {
            _mainMenu.Update(gameTime);
            return;
        }

        _battleBackground.Update(gameTime);

        if (_encuentros.Activo)
        {
            ActualizarBatalla(gameTime);
        }
        else
        {
            ActualizarExploracion(gameTime);
        }

        Window.Title =
            $"Pasos para el combate: {_encuentros.PasosRestantes}";
    }

    private void ActualizarPantalla()
    {
        if (Input.IsKeyPressed(Keys.F11))
        {
            Core.Graphics.IsFullScreen =
                !Core.Graphics.IsFullScreen;

            Core.Graphics.ApplyChanges();
        }
    }

    // =========================================
    // EXPLORACIÓN
    // =========================================

    private void ActualizarExploracion(GameTime gameTime)
    {
        Vector2 posicionAnterior = _yoko.Position;

        // Yoko se controla con WASD.
        _yoko.Update(gameTime);

        // Kyoko sigue a Yoko y actualiza su animación.
        _kyoko.Seguir(_yoko.Position, gameTime);

        // Limitar la posición de Yoko.
        _yoko.Position = LimitarPosicion(
            _yoko.Position,
            _yoko.CurrentAnimation);

        // Limitar la posición de Kyoko.
        _kyoko.Position = LimitarPosicion(
            _kyoko.Position,
            _kyoko.CurrentAnimation);

        // Actualizar la cámara.
        _camera.Follow(
            _yoko.Position,
            TamanoVista(),
            _worldSize);

        // Calcular la distancia recorrida.
        float distanciaRecorrida =
            Vector2.Distance(
                posicionAnterior,
                _yoko.Position);

        // Actualizar el sistema de encuentros.
        _encuentros.Update(distanciaRecorrida);
    }

    // =========================================
    // BATALLA
    // =========================================

    private void ActualizarBatalla(GameTime gameTime)
    {
        if (_currentState is not BattleState)
        {
            var batalla = new BattleState(
                _retroFont,
                _uiBordersTexture,
                _earthboundHudTexture,
                _enemyTemplates);

            batalla.OnBattleEnded += FinalizarBatalla;

            _currentState = batalla;
        }

        _currentState.Update(gameTime);
    }

    private void FinalizarBatalla()
    {
        _encuentros.Reiniciar();
        _currentState = null;
    }

    // =========================================
    // DIBUJADO
    // =========================================

    protected override void Draw(GameTime gameTime)
    {
        bool enBatalla =
            !_enMenu &&
            _encuentros.Activo &&
            _currentState != null;

        GraphicsDevice.Clear(
            enBatalla ? Color.Black : ColorCielo);

        if (_enMenu)
        {
            DibujarMenu(gameTime);
            return;
        }

        if (enBatalla)
        {
            DibujarBatalla(gameTime);
        }
        else
        {
            DibujarExploracion(gameTime);
        }

        base.Draw(gameTime);
    }

    private void DibujarMenu(GameTime gameTime)
    {
        SpriteBatch.Begin(
            samplerState: SamplerState.PointClamp);

        _mainMenu.Draw(
            SpriteBatch,
            GraphicsDevice);

        SpriteBatch.End();

        base.Draw(gameTime);
    }

    private void DibujarBatalla(GameTime gameTime)
    {
        _battleBackground.Draw(
            SpriteBatch,
            GraphicsDevice);

        SpriteBatch.Begin(
            samplerState: SamplerState.PointClamp);

        _currentState.Draw(
            SpriteBatch,
            GraphicsDevice);

        SpriteBatch.End();
    }

    private void DibujarExploracion(GameTime gameTime)
    {
        SpriteBatch.Begin(
            samplerState: SamplerState.PointClamp,
            transformMatrix: _camera.Transform);

        _yoko.CurrentAnimation?.Draw(
            SpriteBatch,
            _yoko.Position);

        _kyoko.CurrentAnimation?.Draw(
            SpriteBatch,
            _kyoko.Position);

        SpriteBatch.End();
    }

    // =========================================
    // CÁMARA Y LÍMITES
    // =========================================

    private Vector2 TamanoVista()
    {
        return new Vector2(
            GraphicsDevice.Viewport.Width,
            GraphicsDevice.Viewport.Height);
    }

    private Vector2 LimitarPosicion(
        Vector2 posicion,
        AnimatedSprite personaje)
    {
        if (personaje == null)
            return posicion;

        float ancho = personaje.Width;
        float alto = personaje.Height;

        float izquierda =
            personaje.Origin.X * personaje.Scale.X;

        float arriba =
            personaje.Origin.Y * personaje.Scale.Y;

        posicion.X = MathHelper.Clamp(
            posicion.X,
            izquierda,
            _worldSize.X - ancho + izquierda);

        posicion.Y = MathHelper.Clamp(
            posicion.Y,
            arriba,
            _worldSize.Y - alto + arriba);

        return posicion;
    }
}