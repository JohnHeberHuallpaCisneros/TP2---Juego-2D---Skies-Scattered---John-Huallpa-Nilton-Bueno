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
    private SpellEffectLibrary _spellEffects;
    private RenderTarget2D _battleFrame;
    private RenderTarget2D _sceneFrame;
    private Effect _crtFilter;
    private bool _crtEnabled = true;

    private SpriteFont _retroFont;
    private Texture2D _earthboundHudTexture;
    private Texture2D _uiBordersTexture;
    private Texture2D _mapaTextura;                

    // =========================================
    // SISTEMAS
    // =========================================

    private readonly Encounter _encuentros = new Encounter();
    private readonly Camera2D _camera = new Camera2D();

    // Sistema de elecciones (cuadrados de color)
    private readonly ChoiceSystem _choices = new ChoiceSystem();
    private Texture2D _pixel;
    private Color? _colorFinal;

    // CAMBIO: sin readonly, el tamaño sale del mapa
    private Vector2 _worldSize = new Vector2(2048, 2560);

    private TileCollision _colisiones;              
    private bool _verColisiones;                        

    private List<BattleState.EnemyTemplate> _enemyTemplates;

    private readonly List<CharacterStats> _party = BattleState.CreateParty();

    private readonly OverworldUI _overworldUI = new OverworldUI();

    private IGameState _currentState;
    private GameOverState _gameOver;
    private readonly SaveGameService _saveGame = new SaveGameService();
    private Vector2? _savePointPosition = new Vector2(1440, 1700);
    private Dictionary<string, ZoneProgress> _zones = new() { ["mapa2"] = new ZoneProgress() };
    private ZoneProgress CurrentZone => _zones["mapa2"];
    private bool _saveDialogOpen;
    private bool _saveFromMenu;
    private string _saveNotice = "";
    private float _saveNoticeTime;
    private const string SavePointId = "inicio-mapa2";
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
        CargarMapa();                               

        TextureAtlas atlas =
            TextureAtlas.FromFile(
                Content,
                "atlas-definition.xml");

        CargarAnimaciones(atlas);
        CrearPersonajes();

        CrearFondoDeBatalla();
        CrearMenu();
        CrearElecciones();
    }

    // NUEVO
    private void CargarMapa()
    {
        _colisiones = new TileCollision(
            System.IO.Path.Combine(
                AppContext.BaseDirectory,
                "Content", "Image", "Maps", "mapa2.tmx"));

        _worldSize = _colisiones.TamanoMundo;

        string rutaMapa = System.IO.Path.Combine(
    AppContext.BaseDirectory,
    "Content",
    "Image",
    "Maps",
    "EarthboundMapLilliputSteps2x.png"
);

using (var archivo = System.IO.File.OpenRead(rutaMapa))
{
    _mapaTextura = Texture2D.FromStream(GraphicsDevice, archivo);
}
    }

    private void CargarRecursos()
    {
        _crtFilter = Content.Load<Effect>("CrtFilter");
        _spellEffects = new SpellEffectLibrary(GraphicsDevice);
        _earthboundHudTexture =
            Content.Load<Texture2D>(
                "Image/Boxes_UI/SNES - EarthBound - Miscellaneous - Battle HUD");

        _uiBordersTexture =
            Content.Load<Texture2D>(
                "Image/Boxes_UI/Pxiel Art UI borders");

        _retroFont =
            Content.Load<SpriteFont>("Fonts/RetroFont");

        // Textura de 1x1 para dibujar rectángulos de color.
        _pixel = new Texture2D(GraphicsDevice, 1, 1);
        _pixel.SetData(new[] { Color.White });
    }

    private void CargarEnemigos()
    {
        _enemyTemplates =
            new List<BattleState.EnemyTemplate>
            {
                new BattleState.EnemyTemplate
                {
                    Name = "Black Eye",
                    Floats = false,
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
                    Floats = true,
                    Texture = Content.Load<Texture2D>(
                        "Image/Enemies/mantle"),
                    HP = 70
                },

                new BattleState.EnemyTemplate
                {
                    Name = "Mothshum",
                    Floats = true,
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
                    Floats = true,
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

        animacion.Scale = new Vector2(2f, 2f);

        return animacion;
    }

    private void CargarAnimaciones(TextureAtlas atlas)
    {
        // YOKO
        _yokoFrente =
            CrearAnimacion(atlas, "yoko-animation");
        _yokoAtras =
            CrearAnimacion(atlas, "yoko-atras-animation");
        _yokoLado =
            CrearAnimacion(atlas, "yoko-lado-animation");
        _yokoIzquierda =
            CrearAnimacion(atlas, "yoko-izquierda-animation");
        _yokoIzquierdaAbajo =
            CrearAnimacion(atlas, "yoko-izquierdaFrente-animation");
        _yokoIzquierdaArriba =
            CrearAnimacion(atlas, "yoko-izquierdaAtras-animation");
        _yokoDerechaAbajo =
            CrearAnimacion(atlas, "yoko-DerechaFrente-animation");
        _yokoDerechaArriba =
            CrearAnimacion(atlas, "yoko-DerechaAtras-animation");

        // KYOKO
        _kyokoFrente =
            CrearAnimacion(atlas, "kyoko-Frente-animation");
        _kyokoAtras =
            CrearAnimacion(atlas, "kyoko-atras-animation");
        _kyokoLado =
            CrearAnimacion(atlas, "kyoko-lado-animation");
        _kyokoIzquierda =
            CrearAnimacion(atlas, "kyoko-izquierda-animation");
        _kyokoIzquierdaAbajo =
            CrearAnimacion(atlas, "kyoko-izquierdaFrente-animation");
        _kyokoIzquierdaArriba =
            CrearAnimacion(atlas, "kyoko-izquierdaAtras-animation");
        _kyokoDerechaAbajo =
            CrearAnimacion(atlas, "kyoko-DerechaFrente-animation");
        _kyokoDerechaArriba =
            CrearAnimacion(atlas, "kyoko-DerechaAtras-animation");
    }

    // =========================================
    // PERSONAJES
    // =========================================

    private void CrearPersonajes()
    {
        // CAMBIO: posiciones iniciales nuevas (zona de pasto).
        // Si caen en pared, ajustalas mirando con F3.
        _yoko = new Player(
            Keys.W,
            Keys.S,
            Keys.A,
            Keys.D,
            new Vector2(1500, 1700))
        {
            AnimFrente = _yokoFrente,
            AllowArrowKeys = true,
            AnimAtras = _yokoAtras,

            AnimIzquierda = _yokoLado,
            AnimDerecha = _yokoIzquierda,

            AnimArribaDerecha = _yokoDerechaArriba,
            AnimArribaIzquierda = _yokoIzquierdaArriba,

            AnimAbajoDerecha = _yokoDerechaAbajo,
            AnimAbajoIzquierda = _yokoIzquierdaAbajo
        };

        _kyoko = new Player(
            Keys.Up,
            Keys.Down,
            Keys.Left,
            Keys.Right,
            new Vector2(1550, 1700))
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
            _earthboundHudTexture, () => _saveGame.HasSave);

        _mainMenu.OnStartGame += () => ReiniciarPartida(false);
        _mainMenu.OnContinueGame += ContinuarPartida;
        _overworldUI.OnSaveRequested += AbrirGuardadoDesdeMenu;
        _mainMenu.OnExitGame += Exit;
    }

    private void CrearElecciones()
    {
        _choices.OnFinalSquareTouched += color =>
        {
            _colorFinal = color;
        };
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
        _saveNoticeTime = Math.Max(0, _saveNoticeTime - (float)gameTime.ElapsedGameTime.TotalSeconds);

        //F3 muestra/oculta las colisiones
        if (Input.IsKeyPressed(Keys.F3))
            _verColisiones = !_verColisiones;

        if (_enMenu)
        {
            _mainMenu.Update(gameTime);
            return;
        }

        if (_gameOver != null)
        {
            _gameOver.Update(gameTime);
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
            (CurrentZone.RemainingEnemies == 0 ? "No quedan enemigos en esta zona" :
                _encuentros.PasosSegurosRestantes > 0 ? $"Pasos seguros: {_encuentros.PasosSegurosRestantes}" :
                $"Probabilidad de encuentro por paso: {_encuentros.ProbabilidadActual:P1}") +
            (_colorFinal.HasValue ? $" | Color final: {_colorFinal.Value}" : "");
    }

    private void ActualizarPantalla()
    {
        if (Input.IsKeyPressed(Keys.F9)) _crtEnabled = !_crtEnabled;
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
        if (_saveDialogOpen)
        {
            if (Input.IsKeyPressed(Keys.X) || Input.IsKeyPressed(Keys.Escape)) _saveDialogOpen = false;
            else if (Input.IsKeyPressed(Keys.Z))
            {
                var data = new SaveGameData { Yoko = new SavedPosition(_yoko.Position),
                    Kyoko = new SavedPosition(_kyoko.Position), Party = _party, Choices = _choices.Capture(),
                    SavePointId = _saveFromMenu ? "menu-overworld" : SavePointId, SavedAt = DateTimeOffset.UtcNow, Zones = _zones };
                bool saved = _saveGame.TrySave(data);
                _saveNotice = saved ? "Partida guardada." : _saveGame.LastError;
                _saveNoticeTime = 4f;
                if (saved) _saveDialogOpen = false;
            }
            return;
        }
        if (!_overworldUI.IsOpen && !_choices.IsChoosing && NearSavePoint() && Input.IsKeyPressed(Keys.Z))
        {
            _saveDialogOpen = true;
            _saveFromMenu = false;
            _saveNoticeTime = 0;
            return;
        }

        if (!_choices.IsChoosing && Input.IsKeyPressed(Keys.C))
        {
            _overworldUI.Toggle();
            return;
        }
        if (_overworldUI.IsOpen)
        {
            _overworldUI.Update(_party);
            return;
        }

        if (_choices.IsChoosing)
        {
            _choices.Update(ObtenerHitbox(_yoko), _worldSize);
            return;
        }

        // CAMBIO: guardamos las posiciones de los dos personajes
        Vector2 yokoAntes = _yoko.Position;
        Vector2 kyokoAntes = _kyoko.Position;

        // Yoko se controla con WASD y choca con las paredes.
        _yoko.Update(gameTime);
        ResolverColision(_yoko, yokoAntes);

        
     _kyoko.Seguir(_yoko.Position, gameTime);

        _yoko.Position = LimitarPosicion(
            _yoko.Position,
            _yoko.CurrentAnimation);

        _kyoko.Position = LimitarPosicion(
            _kyoko.Position,
            _kyoko.CurrentAnimation);

        _camera.Follow(
            _yoko.Position,
            TamanoVista(),
            _worldSize);

        float distanciaRecorrida =
            Vector2.Distance(
                yokoAntes,
                _yoko.Position);

        _encuentros.Update(distanciaRecorrida, CurrentZone.RemainingEnemies > 0);

        _choices.Update(ObtenerHitbox(_yoko), _worldSize);
    }

    // NUEVO: mueve por ejes separados para deslizar contra las paredes
    private void ResolverColision(Player p, Vector2 anterior)
    {
        Vector2 destino = p.Position;
        Vector2 final = anterior;

        p.Position = new Vector2(destino.X, anterior.Y);
        if (!_colisiones.Choca(HitboxPies(p)))
            final.X = destino.X;

        p.Position = new Vector2(final.X, destino.Y);
        if (!_colisiones.Choca(HitboxPies(p)))
            final.Y = destino.Y;

        p.Position = final;
    }

    // NUEVO: hitbox de los pies (la parte baja del sprite)
    private Rectangle HitboxPies(Player p)
    {
        var a = p.CurrentAnimation;

        if (a == null)
            return new Rectangle((int)p.Position.X - 8, (int)p.Position.Y - 8, 16, 16);

        int izq = (int)(p.Position.X - a.Origin.X * a.Scale.X);
        int arr = (int)(p.Position.Y - a.Origin.Y * a.Scale.Y);

        return new Rectangle(
            izq + (int)a.Width / 4,
            arr + (int)a.Height - 14,
            (int)a.Width / 2,
            12);
    }

    private Rectangle ObtenerHitbox(Player p)
    {
        var a = p.CurrentAnimation;

        if (a == null)
            return new Rectangle((int)p.Position.X, (int)p.Position.Y, 32, 32);

        var rect = new Rectangle(
            (int)(p.Position.X - a.Origin.X * a.Scale.X),
            (int)(p.Position.Y - a.Origin.Y * a.Scale.Y),
            (int)a.Width,
            (int)a.Height);

        rect.Inflate(-8, -8);
        return rect;
    }

    // =========================================
    // BATALLA
    // =========================================

    private void ActualizarBatalla(GameTime gameTime)
    {
        if (_currentState is not BattleState)
        {
            if (CurrentZone.RemainingEnemies <= 0) { _encuentros.Reiniciar(); return; }
            var batalla = new BattleState(
                _retroFont,
                _uiBordersTexture,
                _earthboundHudTexture,
                _enemyTemplates,
                _party,
                _spellEffects,
                CurrentZone);

            batalla.OnBattleEnded += FinalizarBatalla;

            _currentState = batalla;
        }

        _currentState.Update(gameTime);
    }

    protected override void UnloadContent()
    {
        _spellEffects?.Dispose();
        _battleFrame?.Dispose();
        _sceneFrame?.Dispose();
        base.UnloadContent();
    }

    private void FinalizarBatalla()
    {
        _encuentros.Reiniciar();
        _currentState = null;
        if (_party.TrueForAll(hero => hero.HP <= 0))
        {
            _gameOver = new GameOverState(_retroFont, _earthboundHudTexture, _pixel, _saveGame.HasSave);
            _gameOver.OnRetry += () => { if (_saveGame.HasSave) ContinuarPartida(); else ReiniciarPartida(false); };
            _gameOver.OnMainMenu += () => ReiniciarPartida(true);
        }
    }

    private void AbrirGuardadoDesdeMenu()
    {
        if (_enMenu || _gameOver != null || _encuentros.Activo || _choices.IsChoosing || !_overworldUI.IsOpen) return;
        _saveFromMenu = true;
        _saveDialogOpen = true;
        _saveNoticeTime = 0;
    }

    private bool NearSavePoint() => _savePointPosition.HasValue &&
        Vector2.Distance(_yoko.Position, _savePointPosition.Value) <= 72f;

    private void ContinuarPartida()
    {
        if (!_saveGame.TryLoad(out var saved))
        {
            _mainMenu.StatusMessage = _saveGame.LastError;
            if (_gameOver != null) _gameOver.StatusMessage = _saveGame.LastError;
            return;
        }
        // Validar las posiciones contra las colisiones antes de cambiar la partida.
        Vector2 oldYoko = _yoko.Position, oldKyoko = _kyoko.Position;
        _yoko.Position = saved.Yoko.ToVector();
        _kyoko.Position = saved.Kyoko.ToVector();
        bool blocked = _colisiones.Choca(HitboxPies(_yoko));
        if (blocked)
        {
            _yoko.Position = oldYoko; _kyoko.Position = oldKyoko;
            _mainMenu.StatusMessage = "La posición del guardado no es válida.";
            if (_gameOver != null) _gameOver.StatusMessage = _mainMenu.StatusMessage;
            return;
        }
        _party.Clear(); _party.AddRange(saved.Party);
        _zones = saved.Zones;
        _choices.Restore(saved.Choices);
        _colorFinal = saved.Choices.Finished ? _choices.FinalColor : null;
        _encuentros.Reiniciar();
        _currentState = null; _gameOver = null; _saveDialogOpen = false;
        _saveNotice = "Partida cargada."; _saveNoticeTime = 3f;
        if (_overworldUI.IsOpen) _overworldUI.Toggle();
        IniciarJuego();
    }

    private void DrawSavePoint()
    {
        if (!_savePointPosition.HasValue) return;
        Vector2 point = _savePointPosition.Value;
        Color color = Color.Gold;
        SpriteBatch.Draw(_pixel, new Rectangle((int)point.X - 3, (int)point.Y - 16, 6, 32), color);
        SpriteBatch.Draw(_pixel, new Rectangle((int)point.X - 13, (int)point.Y - 3, 26, 6), color);
        SpriteBatch.Draw(_pixel, new Rectangle((int)point.X - 7, (int)point.Y - 7, 14, 14), Color.LightYellow);
    }

    private void DrawSavePanel()
    {
        if (!_saveDialogOpen && _saveNoticeTime <= 0 && !NearSavePoint()) return;
        var view = TamanoVista();
        Rectangle box = new Rectangle(20, (int)view.Y - 102, (int)view.X - 40, 82);
        EarthboundBox.Draw(SpriteBatch, _earthboundHudTexture, _pixel, box);
        string text = _saveNoticeTime > 0 ? _saveNotice : _saveDialogOpen
            ? (_saveGame.HasSave ? "¿Sobrescribir la partida guardada?" : "¿Guardar la partida?") + "\nZ: Confirmar    X: Volver"
            : "Punto de guardado - Z: Interactuar";
        Vector2 size = _retroFont.MeasureString(text);
        float scale = Math.Min(1f, (box.Width - 28) / Math.Max(1f, size.X));
        SpriteBatch.DrawString(_retroFont, text, new Vector2(box.X + 14, box.Y + 18), Color.White,
            0f, Vector2.Zero, scale, SpriteEffects.None, 0f);
    }

    private void ReiniciarPartida(bool volverAlMenu)
    {
        _gameOver = null;
        _saveDialogOpen = false;
        _saveNoticeTime = 0;
        _party.Clear();
        _party.AddRange(BattleState.CreateParty());
        _zones = new Dictionary<string, ZoneProgress> { ["mapa2"] = new ZoneProgress() };
        _encuentros.Reiniciar();
        _choices.Reiniciar();
        _colorFinal = null;
        if (_overworldUI.IsOpen) _overworldUI.Toggle();
        CrearPersonajes();
        IniciarJuego();
        _enMenu = volverAlMenu;
    }

    // =========================================
    // DIBUJADO
    // =========================================

    protected override void Draw(GameTime gameTime)
    {
        if (!_crtEnabled)
        {
            DrawScene(gameTime);
            return;
        }
        var previous = GraphicsDevice.GetRenderTargets();
        int width = GraphicsDevice.Viewport.Width, height = GraphicsDevice.Viewport.Height;
        if (_sceneFrame == null || _sceneFrame.Width != width || _sceneFrame.Height != height)
        {
            _sceneFrame?.Dispose();
            _sceneFrame = new RenderTarget2D(GraphicsDevice, width, height);
        }
        GraphicsDevice.SetRenderTarget(_sceneFrame);
        DrawScene(gameTime);
        GraphicsDevice.SetRenderTargets(previous);
        _crtFilter.Parameters["OutputSize"].SetValue(new Vector2(width, height));
        _crtFilter.Parameters["ScanlineStrength"].SetValue(0.28f);
        _crtFilter.Parameters["VignetteStrength"].SetValue(0.16f);
        _crtFilter.Parameters["MaskStrength"].SetValue(0.06f);
        SpriteBatch.Begin(samplerState: SamplerState.PointClamp, effect: _crtFilter);
        SpriteBatch.Draw(_sceneFrame, Vector2.Zero, Color.White);
        SpriteBatch.End();
    }

    private void DrawScene(GameTime gameTime)
    {
        if (_gameOver != null)
        {
            GraphicsDevice.Clear(Color.Black);
            SpriteBatch.Begin(samplerState: SamplerState.PointClamp);
            _gameOver.Draw(SpriteBatch, GraphicsDevice);
            SpriteBatch.End();
            base.Draw(gameTime);
            return;
        }

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
        var previous = GraphicsDevice.GetRenderTargets();
        int width = GraphicsDevice.Viewport.Width;
        int height = GraphicsDevice.Viewport.Height;
        if (_battleFrame == null || _battleFrame.Width != width || _battleFrame.Height != height)
        {
            _battleFrame?.Dispose();
            _battleFrame = new RenderTarget2D(GraphicsDevice, width, height);
        }
        GraphicsDevice.SetRenderTarget(_battleFrame);
        GraphicsDevice.Clear(Color.Black);
        _battleBackground.Draw(
            SpriteBatch,
            GraphicsDevice);

        SpriteBatch.Begin(
            samplerState: SamplerState.PointClamp);

        _currentState.Draw(
            SpriteBatch,
            GraphicsDevice);

        SpriteBatch.End();
        GraphicsDevice.SetRenderTargets(previous);
        GraphicsDevice.Clear(Color.Black);
        Vector2 offset = _currentState is BattleState battle ? battle.ShakeOffset : Vector2.Zero;
        SpriteBatch.Begin(samplerState: SamplerState.PointClamp);
        SpriteBatch.Draw(_battleFrame, offset, Color.White);
        if (_currentState is BattleState activeBattle && activeBattle.ScreenFlashOpacity > 0)
            SpriteBatch.Draw(_pixel, new Rectangle(0, 0, width, height),
                activeBattle.ScreenFlashColor * activeBattle.ScreenFlashOpacity);
        SpriteBatch.End();
    }

    private void DibujarExploracion(GameTime gameTime)
    {
        SpriteBatch.Begin(
            samplerState: SamplerState.PointClamp,
            transformMatrix: _camera.Transform);

        
        SpriteBatch.Draw(_mapaTextura, Vector2.Zero, Color.White);

        // NUEVO: capa de depuración (F3)
        if (_verColisiones)
        {
            _colisiones.DibujarDebug(
                SpriteBatch,
                _pixel,
                new Rectangle(
                    (int)_camera.Position.X,
                    (int)_camera.Position.Y,
                    (int)TamanoVista().X,
                    (int)TamanoVista().Y));
        }

        _choices.DrawSquares(SpriteBatch, _pixel);
        DrawSavePoint();

        _yoko.CurrentAnimation?.Draw(
            SpriteBatch,
            PixelPosition(_yoko.Position));

        _kyoko.CurrentAnimation?.Draw(
            SpriteBatch,
            PixelPosition(_kyoko.Position));

        SpriteBatch.End();

        SpriteBatch.Begin(
            samplerState: SamplerState.PointClamp);

        _choices.DrawMenu(
            SpriteBatch,
            _pixel,
            _retroFont,
            TamanoVista());

        _overworldUI.Draw(SpriteBatch, _pixel, _earthboundHudTexture, _retroFont, _party, TamanoVista());
        if (!_choices.IsChoosing && (_saveDialogOpen || _saveNoticeTime > 0 || !_overworldUI.IsOpen)) DrawSavePanel();
        SpriteBatch.End();
    }

    // =========================================
    // CÁMARA Y LÍMITES
    // =========================================

    private static Vector2 PixelPosition(Vector2 position) =>
        new Vector2((float)Math.Round(position.X), (float)Math.Round(position.Y));

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
