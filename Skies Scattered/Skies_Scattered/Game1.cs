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
using Skies_Scattered.Audio;
using Microsoft.Xna.Framework.Audio;

namespace Skies_Scattered;

public class Game1 : Core
{
    // =========================================
    // PERSONAJES
    // =========================================

    private Player _yoko;
    private Player _kyoko;
    private readonly PartyTrail _partyTrail = new PartyTrail();
    private readonly CaveLighting _caveLighting = new CaveLighting();

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
    private Dictionary<string, ZoneProgress> _zones = new() { ["mapa2"] = new ZoneProgress() };
    private string _mapId = "mapa2";
    private HashSet<string> _openedGifts = new();
    private MapExit _pendingExit;
    private ZoneProgress CurrentZone
    {
        get
        {
            if (!_zones.TryGetValue(_mapId, out var zone)) _zones.Add(_mapId, zone = new ZoneProgress());
            return zone;
        }
    }
    private bool _saveDialogOpen;
    private readonly OverworldDialogue _overworldDialogue = new OverworldDialogue();
    private BattleMusic _battleMusic;
    private bool _endingIntroSeen;
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
        string folder = System.IO.Path.Combine(AppContext.BaseDirectory, "Content", "Image", "Maps");
        var collision = new TileCollision(System.IO.Path.Combine(folder, _mapId + ".tmx"));
        string image = _mapId == "mapa3" ? "EarthboundMapCaveToThePast2X.png" : "EarthboundMapLilliputSteps2x.png";
        using var stream = System.IO.File.OpenRead(System.IO.Path.Combine(folder, image));
        var texture = Texture2D.FromStream(GraphicsDevice, stream);
        _mapaTextura?.Dispose();
        _mapaTextura = texture;
        _colisiones = collision;
        _worldSize = collision.TamanoMundo;
    }

    private void CambiarMapa(string destination)
    {
        _mapId = destination;
        CargarMapa();
        _yoko.Position = new Vector2(1344, 716);
        _kyoko.Position = new Vector2(1296, 716);
        _yoko.CurrentAnimation = _yokoFrente;
        _kyoko.CurrentAnimation = _kyokoFrente;
        ReiniciarSeguimiento();
        _pendingExit = null;
        _encuentros.Reiniciar();
        _overworldDialogue.Close();
        _ = CurrentZone;
        _camera.Follow(_yoko.Position, TamanoVista(), _worldSize);
        if (_mapId == "mapa3" && !_endingIntroSeen) IniciarEscenaFinal();
    }

    private void CargarRecursos()
    {
        SoundEffects.Load(Content);
        _battleMusic = new BattleMusic(Content.Load<SoundEffect>("Audio/OurWill"));
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
                    HP = 40,
                    Attack = 9,
                    Defense = 0
                },

                new BattleState.EnemyTemplate
                {
                    Name = "Caged",
                    Texture = Content.Load<Texture2D>(
                        "Image/Enemies/caged"),
                    HP = 55,
                    Attack = 7,
                    Defense = 2
                },

                new BattleState.EnemyTemplate
                {
                    Name = "Crypted",
                    Texture = Content.Load<Texture2D>(
                        "Image/Enemies/crypted"),
                    HP = 90,
                    Attack = 12,
                    Defense = 1
                },

                new BattleState.EnemyTemplate
                {
                    Name = "Mantle",
                    Floats = true,
                    Texture = Content.Load<Texture2D>(
                        "Image/Enemies/mantle"),
                    HP = 70,
                    Attack = 8,
                    Defense = 3
                },

                new BattleState.EnemyTemplate
                {
                    Name = "Mothshum",
                    Floats = true,
                    Texture = Content.Load<Texture2D>(
                        "Image/Enemies/mothshum"),
                    HP = 35,
                    Attack = 7,
                    Defense = 0
                },

                new BattleState.EnemyTemplate
                {
                    Name = "Reaper Blossom",
                    Texture = Content.Load<Texture2D>(
                        "Image/Enemies/reaper_blossom"),
                    HP = 60,
                    Attack = 11,
                    Defense = 1
                },

                new BattleState.EnemyTemplate
                {
                    Name = "Snow Twins",
                    Floats = true,
                    Texture = Content.Load<Texture2D>(
                        "Image/Enemies/snow_twins"),
                    HP = 45,
                    Attack = 10,
                    Defense = 0
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
        // Abertura lateral del extremo izquierdo de la cueva, indicada por el jugador.
        _yoko = new Player(
            Keys.W,
            Keys.S,
            Keys.A,
            Keys.D,
            new Vector2(144, 1744))
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
            new Vector2(192, 1744))
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

        _yoko.CurrentAnimation = _yoko.AnimDerecha;
        _kyoko.CurrentAnimation = _kyoko.AnimDerecha;
        ReiniciarSeguimiento();
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
        SoundEffects.Update((float)gameTime.ElapsedGameTime.TotalSeconds);
        _battleMusic?.Update((float)gameTime.ElapsedGameTime.TotalSeconds, CurrentZone.RemainingEnemies);

        if (!_overworldDialogue.IsOpen)
        {
            ActualizarPantalla();
            if (Input.IsKeyPressed(Keys.F3)) _verColisiones = !_verColisiones;
        }

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

        ActualizarTituloVentana();
    }

    private void ActualizarTituloVentana()
    {
        if (_mapId == "mapa3")
        {
            Window.Title = "Skies Scattered";
            return;
        }

        string titulo;
        if (CurrentZone.RemainingEnemies == 0)
            titulo = "No quedan enemigos en esta zona";
        else if (_encuentros.PasosSegurosRestantes > 0)
            titulo = $"Pasos seguros: {_encuentros.PasosSegurosRestantes}";
        else
            titulo = $"Probabilidad de encuentro por paso: {_encuentros.ProbabilidadActual:P1}";

        if (_colorFinal.HasValue) titulo += $" | Color final: {_colorFinal.Value}";
        Window.Title = titulo;
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
        if (_overworldDialogue.IsOpen)
        {
            _overworldDialogue.Update((float)gameTime.ElapsedGameTime.TotalSeconds, Input.IsKeyPressed(Keys.Z), Input.IsKeyPressed(Keys.X));
            return;
        }
        if (_mapId == "mapa3" && !_endingIntroSeen)
        {
            IniciarEscenaFinal();
            return;
        }
        if (_saveDialogOpen)
        {
            ActualizarConfirmacionGuardado();
            return;
        }
        if (!_choices.IsChoosing && Input.IsKeyPressed(Keys.C))
        {
            SoundEffects.Play(SoundCue.Select);
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

        if (Input.IsKeyPressed(Keys.Z) && InteractuarRegalo()) return;

        ActualizarMovimientoExploracion(gameTime);
    }

    private void ActualizarConfirmacionGuardado()
    {
        if (Input.IsKeyPressed(Keys.X) || Input.IsKeyPressed(Keys.Escape))
        {
            SoundEffects.Play(SoundCue.Select);
            _saveDialogOpen = false;
        }
        else if (Input.IsKeyPressed(Keys.Z))
        {
            SoundEffects.Play(SoundCue.Select);
            SaveGameData data = CrearDatosGuardado();
            bool saved = _saveGame.TrySave(data);
            MostrarDialogo(saved ? "Partida guardada." : _saveGame.LastError);
            if (saved) _saveDialogOpen = false;
        }
    }

    private SaveGameData CrearDatosGuardado()
    {
        return new SaveGameData
        {
            Yoko = new SavedPosition(_yoko.Position),
            Kyoko = new SavedPosition(_kyoko.Position),
            Party = _party,
            Choices = _choices.Capture(),
            EndingIntroSeen = _endingIntroSeen,
            OpenedGifts = _openedGifts,
            MapId = _mapId,
            SavePointId = "menu-overworld",
            SavedAt = DateTimeOffset.UtcNow,
            Zones = _zones
        };
    }

    private void ActualizarMovimientoExploracion(GameTime gameTime)
    {
        Vector2 yokoAntes = _yoko.Position;

        // Primero se mueve Yoko; después Kyo sigue el camino aceptado.
        _yoko.Update(gameTime);
        ResolverColision(_yoko, yokoAntes);
        if (_overworldDialogue.IsOpen) return;
        if (_pendingExit != null)
        {
            CambiarMapa(_pendingExit.Destination);
            return;
        }

        _kyoko.AplicarSeguimiento(_partyTrail.Advance(
            (float)gameTime.ElapsedGameTime.TotalSeconds, _kyoko.Speed * 60f, PuedeOcuparKyoko));

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

        _encuentros.Update(distanciaRecorrida, _mapId == "mapa2" && CurrentZone.RemainingEnemies > 0);

        _choices.Update(ObtenerHitbox(_yoko), _worldSize);
    }

    // NUEVO: mueve por ejes separados para deslizar contra las paredes
    private void ResolverColision(Player p, Vector2 anterior)
    {
        Vector2 delta = p.Position - anterior;
        p.Position = anterior;
        // Pasos pequeños evitan atravesar paredes durante un cuadro largo.
        int steps = Math.Max(1, (int)Math.Ceiling(Math.Max(Math.Abs(delta.X), Math.Abs(delta.Y)) / 4f));
        Vector2 step = delta / steps;
        for (int i = 0; i < steps; i++)
        {
            Vector2 previous = p.Position;
            p.Position += new Vector2(step.X, 0);
            if (!PuedeMoverse(p)) p.Position = previous;
            Vector2 corner = p.Position;
            if (_pendingExit != null || _overworldDialogue.IsOpen) return;
            previous = p.Position;
            p.Position += new Vector2(0, step.Y);
            if (!PuedeMoverse(p)) p.Position = previous;
            if (p == _yoko) _partyTrail.RecordStep(corner, p.Position, PuedeOcuparKyoko);
            if (_pendingExit != null || _overworldDialogue.IsOpen) return;
        }
    }

    private bool PuedeOcuparKyoko(Vector2 position) => !_colisiones.Choca(HitboxPiesEn(_kyoko, position));

    private bool AjustarASuelo(Player player)
    {
        Vector2 original = player.Position;
        if (!_colisiones.Choca(HitboxPies(player))) return true;
        for (int radius = 8; radius <= 96; radius += 8)
            for (int y = -radius; y <= radius; y += 8)
                for (int x = -radius; x <= radius; x += 8)
                {
                    if (Math.Abs(x) != radius && Math.Abs(y) != radius) continue;
                    Vector2 candidate = original + new Vector2(x, y);
                    if (!_colisiones.Choca(HitboxPiesEn(player, candidate)))
                    { player.Position = candidate; return true; }
                }
        return false;
    }

    private void ReiniciarSeguimiento()
    {
        AjustarASuelo(_kyoko);
        if (!_partyTrail.Reset(_kyoko.Position, _yoko.Position, PuedeOcuparKyoko))
        {
            // Recuperación al cargar un acompañante aislado por colisiones antiguas.
            _kyoko.Position = _yoko.Position;
            _partyTrail.Reset(_kyoko.Position, _yoko.Position, PuedeOcuparKyoko);
        }
    }

    private bool PuedeMoverse(Player p)
    {
        Rectangle feet = HitboxPies(p);
        var exit = _colisiones.ExitAt(feet);
        if (p == _yoko && exit != null)
        {
            if (exit.IsBlocked)
            {
                MostrarDialogo("No puedes retroceder");
            }
            else _pendingExit = exit;
            return false;
        }
        return !_colisiones.Choca(feet);
    }

    // NUEVO: hitbox de los pies (la parte baja del sprite)
    private Rectangle HitboxPies(Player p) => HitboxPiesEn(p, p.Position);

    private Rectangle HitboxPiesEn(Player p, Vector2 position)
    {
        var a = p.CurrentAnimation;

        if (a == null)
            return new Rectangle((int)position.X - 8, (int)position.Y - 8, 16, 16);

        int izq = (int)(position.X - a.Origin.X * a.Scale.X);
        int arr = (int)(position.Y - a.Origin.Y * a.Scale.Y);

        return new Rectangle(
            izq + (int)a.Width / 2 - 8,
            arr + (int)a.Height - 8,
            16,
            6);
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
        if (_mapId != "mapa2") { _encuentros.Reiniciar(); _battleMusic.Stop(); _currentState = null; return; }
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
            batalla.OnBattleResolved += () => _battleMusic.FadeOut();

            _currentState = batalla;
            SoundEffects.Play(SoundCue.Encounter);
            _battleMusic.Start(CurrentZone.RemainingEnemies);
        }

        _currentState.Update(gameTime);
    }

    protected override void UnloadContent()
    {
        SoundEffects.Unload();
        _battleMusic?.Dispose();
        _spellEffects?.Dispose();
        _caveLighting.Dispose();
        _battleFrame?.Dispose();
        _sceneFrame?.Dispose();
        base.UnloadContent();
    }

    private void FinalizarBatalla()
    {
        _battleMusic.FadeOut();
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
        _saveDialogOpen = true;
        _overworldDialogue.Close();
    }

    private void ContinuarPartida()
    {
        _battleMusic.Stop();
        _overworldDialogue.Close();
        if (!_saveGame.TryLoad(out var saved))
        {
            _mainMenu.StatusMessage = _saveGame.LastError;
            if (_gameOver != null) _gameOver.StatusMessage = _saveGame.LastError;
            return;
        }
        // Validar las posiciones contra las colisiones antes de cambiar la partida.
        Vector2 oldYoko = _yoko.Position, oldKyoko = _kyoko.Position;
        string oldMap = _mapId;
        if (_mapId != saved.MapId) { _mapId = saved.MapId; CargarMapa(); }
        _yoko.Position = saved.Yoko.ToVector();
        _kyoko.Position = saved.Kyoko.ToVector();
        bool blocked = !AjustarASuelo(_yoko);
        if (blocked)
        {
            _yoko.Position = oldYoko; _kyoko.Position = oldKyoko;
            if (_mapId != oldMap) { _mapId = oldMap; CargarMapa(); }
            _mainMenu.StatusMessage = "La posición del guardado no es válida.";
            if (_gameOver != null) _gameOver.StatusMessage = _mainMenu.StatusMessage;
            return;
        }
        ReiniciarSeguimiento();
        _party.Clear(); _party.AddRange(saved.Party);
        _zones = saved.Zones;
        _openedGifts = new HashSet<string>(saved.OpenedGifts);
        _endingIntroSeen = saved.EndingIntroSeen;
        _choices.Restore(saved.Choices);
        _colorFinal = saved.Choices.Finished ? _choices.FinalColor : null;
        _encuentros.Reiniciar();
        _currentState = null; _gameOver = null; _saveDialogOpen = false;
        MostrarDialogo("Partida cargada.");
        if (_overworldUI.IsOpen) _overworldUI.Toggle();
        IniciarJuego();
    }

    private bool InteractuarRegalo()
    {
        MapGift nearest = null;
        float distance = 49f;
        var feet = HitboxPies(_yoko).Center.ToVector2();
        foreach (var gift in _colisiones.Gifts)
        {
            float candidate = gift.DistanceTo(feet);
            if (candidate < distance) { nearest = gift; distance = candidate; }
        }
        if (nearest == null) return false;
        if (_openedGifts.Contains(nearest.Id)) MostrarDialogo("Este regalo ya está vacío.");
        else if (_party[0].Items.Count >= 200) MostrarDialogo("No tienes espacio para más objetos.");
        else
        {
            _party[0].Items.Add(nearest.Item);
            _openedGifts.Add(nearest.Id);
            MostrarDialogo($"Encontraste {GameItem.Description(nearest.Item)}.\nGuardado en el inventario compartido.");
        }
        return true;
    }

    private void DibujarRegalosAbiertos()
    {
        foreach (var gift in _colisiones.Gifts)
        {
            if (!_openedGifts.Contains(gift.Id)) continue;
            var box = gift.Bounds;
            // Tapa levantada y hueco oscuro sobre el regalo dibujado en el mapa.
            SpriteBatch.Draw(_mapaTextura, new Vector2(box.X, box.Y - 6),
                new Rectangle(box.X, box.Y, box.Width, 12), Color.White);
            SpriteBatch.Draw(_pixel, new Rectangle(box.X + 6, box.Y + 8, box.Width - 12, 8),
                new Color(48, 32, 24));
        }
    }

    private void MostrarDialogo(string text)
    {
        _yoko.AplicarSeguimiento(_yoko.Position);
        _kyoko.AplicarSeguimiento(_kyoko.Position);
        _overworldDialogue.Show(false, text);
    }

    private void IniciarEscenaFinal()
    {
        _endingIntroSeen = true;
        _yoko.AplicarSeguimiento(_yoko.Position);
        _kyoko.AplicarSeguimiento(_kyoko.Position);
        _yoko.CurrentAnimation = _yoko.AnimIzquierda;
        _kyoko.CurrentAnimation = _kyoko.AnimDerecha;
        _overworldDialogue.Show(true,
            "Kyo: Por fin salimos... Ya estamos cerca, ¿no?",
            "Kyo: Cuando lleguemos al prado podremos descansar y mirar el cielo juntas.",
            "Yoko: ...Podemos quedarnos un rato más aquí.",
            "Kyo: ¿Aquí? Pero vinimos hasta tan lejos...",
            "Yoko: Lo sé. Es que, si seguimos... llegaremos al final.",
            "Kyo: Yoko... ¿Eso es lo que te preocupa?",
            "Yoko: Cuando todo termine, ¿qué va a quedar de nosotras?",
            "Kyo: Llegar al final no borra lo que vivimos juntas.",
            "Yoko: No quiero volver a perderte.");
    }

    private void DrawSavePanel()
    {
        if (!_saveDialogOpen) return;
        var view = TamanoVista();
        Rectangle box = new Rectangle(20, (int)view.Y - 102, (int)view.X - 40, 82);
        EarthboundBox.Draw(SpriteBatch, _earthboundHudTexture, _pixel, box);
        string text = (_saveGame.HasSave ? "¿Sobrescribir la partida guardada?" : "¿Guardar la partida?") + "\nZ: Confirmar    X: Volver";
        Vector2 size = _retroFont.MeasureString(text);
        float scale = Math.Min(1f, (box.Width - 28) / Math.Max(1f, size.X));
        SpriteBatch.DrawString(_retroFont, text, new Vector2(box.X + 14, box.Y + 18), Color.White,
            0f, Vector2.Zero, scale, SpriteEffects.None, 0f);
    }

    private void ReiniciarPartida(bool volverAlMenu)
    {
        _battleMusic.Stop();
        _endingIntroSeen = false;
        _gameOver = null;
        _saveDialogOpen = false;
        _overworldDialogue.Close();
        _party.Clear();
        _party.AddRange(BattleState.CreateParty());
        _zones = new Dictionary<string, ZoneProgress> { ["mapa2"] = new ZoneProgress() };
        if (_mapId != "mapa2") { _mapId = "mapa2"; CargarMapa(); }
        _pendingExit = null;
        _openedGifts.Clear();
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
        DibujarRegalosAbiertos();

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

        _yoko.CurrentAnimation?.Draw(
            SpriteBatch,
            PixelPosition(_yoko.Position));

        _kyoko.CurrentAnimation?.Draw(
            SpriteBatch,
            PixelPosition(_kyoko.Position));

        SpriteBatch.End();

        SpriteBatch.Begin(
            samplerState: SamplerState.PointClamp);

        if (_mapId == "mapa2")
            _caveLighting.Draw(SpriteBatch, GraphicsDevice, _pixel,
                _yoko.Position - _camera.Position + new Vector2(32, 32));

        _choices.DrawMenu(
            SpriteBatch,
            _pixel,
            _retroFont,
            TamanoVista());

        _overworldUI.Draw(SpriteBatch, _pixel, _earthboundHudTexture, _retroFont, _party, TamanoVista());
        if (!_choices.IsChoosing && _saveDialogOpen) DrawSavePanel();
        _overworldDialogue.Draw(SpriteBatch, _earthboundHudTexture, _pixel, _retroFont, TamanoVista());
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






