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
    private Player _yoko;
    private Player _kyoko;
    private readonly PartyTrail _partyTrail = new PartyTrail();
    private readonly CaveLighting _caveLighting = new CaveLighting();
    private AnimatedSprite _yokoAtras;
    private AnimatedSprite _yokoFrente;
    private AnimatedSprite _yokoLado;
    private AnimatedSprite _yokoIzquierda;
    private AnimatedSprite _yokoDerechaArriba;
    private AnimatedSprite _yokoIzquierdaArriba;
    private AnimatedSprite _yokoDerechaAbajo;
    private AnimatedSprite _yokoIzquierdaAbajo;
    private AnimatedSprite _kyokoAtras;
    private AnimatedSprite _kyokoFrente;
    private AnimatedSprite _kyokoLado;
    private AnimatedSprite _kyokoIzquierda;
    private AnimatedSprite _kyokoDerechaArriba;
    private AnimatedSprite _kyokoIzquierdaArriba;
    private AnimatedSprite _kyokoDerechaAbajo;
    private AnimatedSprite _kyokoIzquierdaAbajo;
    private BattleBackground _battleBackground;
    private SpellEffectLibrary _spellEffects;
    private RenderTarget2D _battleFrame;
    private RenderTarget2D _sceneFrame;
    private Effect _crtFilter;
    private SpriteFont _retroFont;
    private Texture2D _earthboundHudTexture;
    private Texture2D _mapaTextura;
    private readonly Encounter _encuentros = new Encounter();
    private readonly Camera2D _camera = new Camera2D();
    private Texture2D _pixel;
    private Vector2 _worldSize = new Vector2(2048, 2560);
    private TileCollision _colisiones;
    private bool _verColisiones;
    private List<BattleState.EnemyTemplate> _enemyTemplates;
    private readonly List<CharacterStats> _party = BattleState.CreateParty();
    private readonly OverworldUI _overworldUI = new OverworldUI();
    private IGameState _currentState;
    private GameOverState _gameOver;
    private Dictionary<string, ZoneProgress> _zones = new()
    {
        ["mapa2"] = new ZoneProgress()
    };
    private string _mapId = "mapa2";
    private HashSet<string> _openedGifts = new();
    private MapExit _pendingExit;
    private ZoneProgress CurrentZone
    {
        get
        {
            if (!_zones.TryGetValue(_mapId, out var zone))
                _zones.Add(_mapId, zone = new ZoneProgress());
            return zone;
        }
    }

    private readonly SettingsMenu _settings = new SettingsMenu();
    private readonly OverworldDialogue _overworldDialogue = new OverworldDialogue();
    private BattleMusic _battleMusic;
    private SceneMusic _sceneMusic;
    private EncounterTransition _encounterTransition;
    private bool _encounterMusicStarted;
    private const float BattleExitDarkenSeconds = .35f;
    private const float BattleExitRevealSeconds = .45f;
    private float _battleExitElapsed = -1f;
    private bool _battleExitChangedScene;
    private bool _kyokoMovedByContact;
    private const float PartyMinimumSeparation = 48f;
    private bool _endingIntroSeen;
    private bool _endingComplete;
    private readonly Rectangle _endingShore = new Rectangle(1152, 708, 72, 112);
    private MainMenuState _mainMenu;
    private bool _enMenu = true;
    private static readonly Color ColorCielo = new Color(135, 206, 235);
    public Game1() : base("Skies Scattered", 640, 480, false)
    {
    }

    protected override void LoadContent()
    {
        base.LoadContent();
        CargarRecursos();
        CargarEnemigos();
        CargarMapa();
        TextureAtlas atlas = TextureAtlas.FromFile(Content, "atlas-definition.xml");
        CargarAnimaciones(atlas);
        CrearPersonajes();
        CrearFondoDeBatalla();
        CrearMenu();
    }

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
        _encounterTransition?.Reset();
        _encounterMusicStarted = false;
        _overworldDialogue.Close();
        _ = CurrentZone;
        _camera.Follow(_yoko.Position, TamanoVista(), _worldSize);
    }

    private void CargarRecursos()
    {
        SoundEffects.Load(Content);
        _encounterTransition = new EncounterTransition(GraphicsDevice);
        _battleMusic = new BattleMusic(Content.Load<SoundEffect>("Audio/OurWill"));
        _sceneMusic = new SceneMusic();
        _crtFilter = Content.Load<Effect>("CrtFilter");
        _spellEffects = new SpellEffectLibrary(GraphicsDevice);
        _earthboundHudTexture = Content.Load<Texture2D>("Image/Boxes_UI/SNES - EarthBound - Miscellaneous - Battle HUD");
        _retroFont = Content.Load<SpriteFont>("Fonts/RetroFont");
        _pixel = new Texture2D(GraphicsDevice, 1, 1);
        _pixel.SetData(new[] { Color.White });
    }

    private void CargarEnemigos()
    {
        _enemyTemplates = new List<BattleState.EnemyTemplate>
        {
            new BattleState.EnemyTemplate
            {
                Name = "Black Eye",
                Floats = false,
                Texture = Content.Load<Texture2D>("Image/Enemies/black_eye"),
                HP = 40,
                Attack = 9,
                Defense = 0
            },
            new BattleState.EnemyTemplate
            {
                Name = "Caged",
                Texture = Content.Load<Texture2D>("Image/Enemies/caged"),
                HP = 55,
                Attack = 7,
                Defense = 2
            },
            new BattleState.EnemyTemplate
            {
                Name = "Crypted",
                Texture = Content.Load<Texture2D>("Image/Enemies/crypted"),
                HP = 90,
                Attack = 12,
                Defense = 1
            },
            new BattleState.EnemyTemplate
            {
                Name = "Mantle",
                Floats = true,
                Texture = Content.Load<Texture2D>("Image/Enemies/mantle"),
                HP = 70,
                Attack = 8,
                Defense = 3
            },
            new BattleState.EnemyTemplate
            {
                Name = "Mothshum",
                Floats = true,
                Texture = Content.Load<Texture2D>("Image/Enemies/mothshum"),
                HP = 35,
                Attack = 7,
                Defense = 0
            },
            new BattleState.EnemyTemplate
            {
                Name = "Reaper Blossom",
                Texture = Content.Load<Texture2D>("Image/Enemies/reaper_blossom"),
                HP = 60,
                Attack = 11,
                Defense = 1
            },
            new BattleState.EnemyTemplate
            {
                Name = "Snow Twins",
                Floats = true,
                Texture = Content.Load<Texture2D>("Image/Enemies/snow_twins"),
                HP = 45,
                Attack = 10,
                Defense = 0
            }
        };
    }

    private AnimatedSprite CrearAnimacion(TextureAtlas atlas, string nombre)
    {
        AnimatedSprite animacion = atlas.CreateAnimatedSprite(nombre);
        animacion.Scale = new Vector2(2f, 2f);
        return animacion;
    }

    private void CargarAnimaciones(TextureAtlas atlas)
    {
        _yokoFrente = CrearAnimacion(atlas, "yoko-animation");
        _yokoAtras = CrearAnimacion(atlas, "yoko-atras-animation");
        _yokoLado = CrearAnimacion(atlas, "yoko-lado-animation");
        _yokoIzquierda = CrearAnimacion(atlas, "yoko-izquierda-animation");
        _yokoIzquierdaAbajo = CrearAnimacion(atlas, "yoko-izquierdaFrente-animation");
        _yokoIzquierdaArriba = CrearAnimacion(atlas, "yoko-izquierdaAtras-animation");
        _yokoDerechaAbajo = CrearAnimacion(atlas, "yoko-DerechaFrente-animation");
        _yokoDerechaArriba = CrearAnimacion(atlas, "yoko-DerechaAtras-animation");
        _kyokoFrente = CrearAnimacion(atlas, "kyoko-Frente-animation");
        _kyokoAtras = CrearAnimacion(atlas, "kyoko-atras-animation");
        _kyokoLado = CrearAnimacion(atlas, "kyoko-lado-animation");
        _kyokoIzquierda = CrearAnimacion(atlas, "kyoko-izquierda-animation");
        _kyokoIzquierdaAbajo = CrearAnimacion(atlas, "kyoko-izquierdaFrente-animation");
        _kyokoIzquierdaArriba = CrearAnimacion(atlas, "kyoko-izquierdaAtras-animation");
        _kyokoDerechaAbajo = CrearAnimacion(atlas, "kyoko-DerechaFrente-animation");
        _kyokoDerechaArriba = CrearAnimacion(atlas, "kyoko-DerechaAtras-animation");
    }

    private void CrearPersonajes()
    {
        _yoko = new Player(Keys.W, Keys.S, Keys.A, Keys.D, new Vector2(144, 1744))
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
        _kyoko = new Player(Keys.Up, Keys.Down, Keys.Left, Keys.Right, new Vector2(144, 1792))
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

    private void CrearFondoDeBatalla()
    {
        _battleBackground = new BattleBackground(Content);
        _battleBackground.LoadContent();
    }

    private void CrearMenu()
    {
        _mainMenu = new MainMenuState(_retroFont, _earthboundHudTexture, Content.Load<Texture2D>("UI/TitleBackground"));
        _mainMenu.OnStartGame += () => ReiniciarPartida(false);
        _mainMenu.OnOptions += _settings.Open;
        _overworldUI.OnOptionsRequested += _settings.Open;
        _mainMenu.OnExitGame += Exit;
    }



    private void IniciarJuego()
    {
        _enMenu = false;
        _camera.Follow(_yoko.Position, TamanoVista(), _worldSize, snap: true);
    }

    protected override void Update(GameTime gameTime)
    {
        base.Update(gameTime);
        SoundEffects.Update((float)gameTime.ElapsedGameTime.TotalSeconds);
        _battleMusic?.Update((float)gameTime.ElapsedGameTime.TotalSeconds, CurrentZone.RemainingEnemies);
        ActualizarMusicaDeEscena((float)gameTime.ElapsedGameTime.TotalSeconds);
        if (_battleExitElapsed >= 0)
        {
            ActualizarSalidaBatalla((float)gameTime.ElapsedGameTime.TotalSeconds);
            return;
        }

        if (_endingComplete)
        {
            ActualizarFinal();
            return;
        }

        if (_settings.IsOpen)
        {
            _settings.Update();
            return;
        }

        if (!_overworldDialogue.IsOpen)
        {
            ActualizarPantalla();
            if (Input.IsKeyPressed(Keys.F3))
                _verColisiones = !_verColisiones;
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

    private void ActualizarFinal()
    {
        if (Input.IsKeyPressed(Keys.Z))
        {
            SoundEffects.Play(SoundCue.Select);
            ReiniciarPartida(true);
        }
        else if (Input.IsKeyPressed(Keys.Escape))
            Exit();
    }

    private void ActualizarMusicaDeEscena(float seconds)
    {
        string track = null;
        if (_enMenu)
            track = "Menu";
        else if (_gameOver != null)
            track = "GameOver";
        else if (!_encuentros.Activo)
            track = _mapId == "mapa3" ? "Final" : "Cave";
        _sceneMusic.Select(track);
        _sceneMusic.Update(seconds);
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
        Window.Title = titulo;
    }

    private void ActualizarPantalla()
    {
        if (Input.IsKeyPressed(Keys.F9))
            _settings.CrtEnabled = !_settings.CrtEnabled;
        if (Input.IsKeyPressed(Keys.F11))
        {
            Core.Graphics.IsFullScreen = !Core.Graphics.IsFullScreen;
            Core.Graphics.ApplyChanges();
        }
    }

    private void ActualizarExploracion(GameTime gameTime)
    {
        if (_overworldDialogue.IsOpen)
        {
            bool isFinalDialogue = _mapId == "mapa3" && _endingIntroSeen && _overworldDialogue.IsCinematic;
            _overworldDialogue.Update((float)gameTime.ElapsedGameTime.TotalSeconds, Input.IsKeyPressed(Keys.Z), Input.IsKeyPressed(Keys.X));
            if (isFinalDialogue && !_overworldDialogue.IsOpen)
                _endingComplete = true;
            return;
        }

        if (_mapId == "mapa3" && !_endingIntroSeen && _endingShore.Intersects(HitboxPies(_yoko)))
        {
            IniciarEscenaFinal();
            return;
        }

        if (Input.IsKeyPressed(Keys.C))
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


        if (Input.IsKeyPressed(Keys.Z) && InteractuarRegalo())
            return;
        ActualizarMovimientoExploracion(gameTime);
    }

    private void ActualizarMovimientoExploracion(GameTime gameTime)
    {
        Vector2 yokoAntes = _yoko.Position;
        _kyokoMovedByContact = false;
        _yoko.Update(gameTime);
        ResolverColision(_yoko, yokoAntes);
        if (_overworldDialogue.IsOpen)
            return;
        if (_pendingExit != null)
        {
            CambiarMapa(_pendingExit.Destination);
            return;
        }

        if (!_kyokoMovedByContact)
        {
            _kyoko.AplicarSeguimiento(_partyTrail.Advance((float)gameTime.ElapsedGameTime.TotalSeconds, _kyoko.Speed * 60f, PuedeSeguirKyoko));
        }

        _yoko.Position = LimitarPosicion(_yoko.Position, _yoko.CurrentAnimation);
        _kyoko.Position = LimitarPosicion(_kyoko.Position, _kyoko.CurrentAnimation);
        _camera.Follow(_yoko.Position, TamanoVista(), _worldSize);
        float distanciaRecorrida = Vector2.Distance(yokoAntes, _yoko.Position);
        _encuentros.Update(distanciaRecorrida, _mapId == "mapa2" && CurrentZone.RemainingEnemies > 0);
    }

    private void ResolverColision(Player p, Vector2 anterior)
    {
        Vector2 delta = p.Position - anterior;
        p.Position = anterior;
        int steps = Math.Max(1, (int)Math.Ceiling(Math.Max(Math.Abs(delta.X), Math.Abs(delta.Y)) / 4f));
        Vector2 step = delta / steps;
        for (int i = 0; i < steps; i++)
        {
            Vector2 previous = p.Position;
            p.Position += new Vector2(step.X, 0);
            if (!PuedeMoverse(p) || (p == _yoko && !SepararPersonajes(previous)))
                p.Position = previous;
            Vector2 corner = p.Position;
            if (_pendingExit != null || _overworldDialogue.IsOpen)
                return;
            previous = p.Position;
            p.Position += new Vector2(0, step.Y);
            if (!PuedeMoverse(p) || (p == _yoko && !SepararPersonajes(previous)))
                p.Position = previous;
            if (p == _yoko)
                _partyTrail.RecordStep(corner, p.Position, PuedeOcuparKyoko);
            if (_pendingExit != null || _overworldDialogue.IsOpen)
                return;
        }
    }

    private bool SepararPersonajes(Vector2 previousYoko)
    {
        Vector2 direction = _kyoko.Position - _yoko.Position;
        float distance = direction.Length();
        if (distance >= PartyMinimumSeparation)
            return true;
        if (distance < .001f)
            return false;
        Vector2 destination = _yoko.Position + direction * (PartyMinimumSeparation / distance);
        bool canAdvance = PuedeOcuparKyoko(destination);
        if (!canAdvance)
        {
            Vector2 side = new Vector2(-direction.Y, direction.X) / distance * 4f;
            bool foundSpace = false;
            foreach (float sign in new[]
            {
                1f,
                -1f
            }

            )
            {
                Vector2 candidate = _kyoko.Position + side * sign;
                if (PuedeOcuparKyoko(candidate) && Vector2.Distance(candidate, previousYoko) >= PartyMinimumSeparation - .001f)
                {
                    destination = candidate;
                    foundSpace = true;
                    break;
                }
            }

            if (!foundSpace)
                return false;
            canAdvance = Vector2.Distance(destination, _yoko.Position) >= PartyMinimumSeparation - .001f;
        }

        _kyoko.AplicarSeguimiento(destination);
        _partyTrail.Reset(destination, canAdvance ? _yoko.Position : previousYoko);
        _kyokoMovedByContact = true;
        return canAdvance;
    }

    private bool PuedeSeguirKyoko(Vector2 position)
    {
        return Vector2.Distance(position, _yoko.Position) >= PartyMinimumSeparation - .001f && PuedeOcuparKyoko(position);
    }

    private bool PuedeOcuparKyoko(Vector2 position) => !_colisiones.Choca(HitboxPiesEn(_kyoko, position));
    private void ReiniciarSeguimiento()
    {
        _partyTrail.Reset(_kyoko.Position, _yoko.Position);
    }

    private bool PuedeMoverse(Player p)
    {
        Rectangle feet = HitboxPies(p);
        var exit = _colisiones.ExitAt(feet);
        if (p == _yoko && exit != null)
        {
            if (exit.IsBlocked)
            {
                MostrarDialogo("No hay punto de retorno.");
            }
            else
                _pendingExit = exit;
            return false;
        }

        return !_colisiones.Choca(feet);
    }

    private Rectangle HitboxPies(Player p) => HitboxPiesEn(p, p.Position);
    private Rectangle HitboxPiesEn(Player p, Vector2 position)
    {
        var a = p.CurrentAnimation;
        if (a == null)
            return new Rectangle((int)position.X - 8, (int)position.Y - 8, 16, 16);
        int izq = (int)(position.X - a.Origin.X * a.Scale.X);
        int arr = (int)(position.Y - a.Origin.Y * a.Scale.Y);
        return new Rectangle(izq + (int)a.Width / 2 - 8, arr + (int)a.Height - 8, 16, 6);
    }



    private void ActualizarBatalla(GameTime gameTime)
    {
        if (_mapId != "mapa2")
        {
            _encuentros.Reiniciar();
            _battleMusic.Stop();
            _currentState = null;
            return;
        }

        if (_currentState is not BattleState)
        {
            if (CurrentZone.RemainingEnemies <= 0)
            {
                _encuentros.Reiniciar();
                return;
            }

            if (!_encounterTransition.HasCoveredScreen)
            {
                if (!_encounterTransition.IsActive)
                {
                    SoundEffects.Play(SoundCue.Encounter);
                    _encounterTransition.Start(SoundEffects.Duration(SoundCue.Encounter));
                }

                _encounterTransition.Update((float)gameTime.ElapsedGameTime.TotalSeconds, false);
                if (_encounterTransition.HasStartedSwirl && !_encounterMusicStarted)
                {
                    _battleMusic.Start(CurrentZone.RemainingEnemies, .15f);
                    _encounterMusicStarted = true;
                }

                return;
            }

            var batalla = new BattleState(_retroFont, _earthboundHudTexture, _enemyTemplates, _party, _spellEffects, CurrentZone);
            batalla.OnBattleEnded += FinalizarBatalla;
            batalla.OnBattleResolved += () => _battleMusic.FadeOut();
            _currentState = batalla;
            _battleMusic.FadeIn(EncounterTransition.RevealSeconds);
        }

        _encounterTransition.Update((float)gameTime.ElapsedGameTime.TotalSeconds);
        if (_encounterTransition.IsActive)
            return;
        _currentState.Update(gameTime);
    }

    protected override void UnloadContent()
    {
        SoundEffects.Unload();
        _battleMusic?.Dispose();
        _sceneMusic?.Dispose();
        _encounterTransition?.Dispose();
        _spellEffects?.Dispose();
        _caveLighting.Dispose();
        _battleFrame?.Dispose();
        _sceneFrame?.Dispose();
        base.UnloadContent();
    }

    private void FinalizarBatalla()
    {
        if (_battleExitElapsed >= 0)
            return;
        _battleMusic.FadeOut();
        _battleExitElapsed = 0;
        _battleExitChangedScene = false;
    }

    private void ActualizarSalidaBatalla(float seconds)
    {
        _battleExitElapsed += Math.Max(0, seconds);
        if (!_battleExitChangedScene && _battleExitElapsed >= BattleExitDarkenSeconds)
        {
            VolverDeBatalla();
            _battleExitChangedScene = true;
            _battleExitElapsed = BattleExitDarkenSeconds;
        }

        if (_battleExitElapsed >= BattleExitDarkenSeconds + BattleExitRevealSeconds)
            _battleExitElapsed = -1f;
    }

    private void VolverDeBatalla()
    {
        _encuentros.Reiniciar();
        _encounterTransition?.Reset();
        _encounterMusicStarted = false;
        _currentState = null;
        if (_party.TrueForAll(hero => hero.HP <= 0))
        {
            _gameOver = new GameOverState(_retroFont, _earthboundHudTexture, _pixel, Content.Load<Texture2D>("UI/TitleBackground"));
            _gameOver.OnRetry += () => ReiniciarPartida(false);
            _gameOver.OnMainMenu += () => ReiniciarPartida(true);
        }
        else
        {
            foreach (CharacterStats hero in _party)
            {
                if (hero.HP <= 0) hero.HP = Math.Min(10, hero.MaxHP);
            }
        }
    }

    private bool InteractuarRegalo()
    {
        MapGift nearest = null;
        float distance = 49f;
        var feet = HitboxPies(_yoko).Center.ToVector2();
        foreach (var gift in _colisiones.Gifts)
        {
            float candidate = gift.DistanceTo(feet);
            if (candidate < distance)
            {
                nearest = gift;
                distance = candidate;
            }
        }

        if (nearest == null)
            return false;
        if (_openedGifts.Contains(nearest.Id))
            MostrarDialogo("Está vacío.");
        else if (_party[0].Items.Count >= 200)
            MostrarDialogo("No tienes espacio para más objetos.");
        else
        {
            _party[0].Items.Add(nearest.Item);
            _openedGifts.Add(nearest.Id);
            MostrarDialogo($"Encontraste {GameItem.Description(nearest.Item)}.\nGuardado en el inventario.");
        }

        return true;
    }

    private void DibujarRegalosAbiertos()
    {
        foreach (var gift in _colisiones.Gifts)
        {
            if (!_openedGifts.Contains(gift.Id))
                continue;
            var box = gift.Bounds;
            SpriteBatch.Draw(_mapaTextura, new Vector2(box.X, box.Y - 6), new Rectangle(box.X, box.Y, box.Width, 12), Color.White);
            SpriteBatch.Draw(_pixel, new Rectangle(box.X + 6, box.Y + 8, box.Width - 12, 8), new Color(48, 32, 24));
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
            "Kyo: No por donde avanzar al parecer.",
            "Yoko: No tiene sentido que pase esto, ¿No?",
            "Kyo: Podemos detenernos a descansar y volver.",
            "Yoko: ¿Segura que volveremos?",
            "Kyo: Obvio, solo... relajate un poco",
            "Yoko: Está bien, solo esta vez...");
    }

    private void ReiniciarPartida(bool volverAlMenu)
    {
        _battleExitElapsed = -1f;
        _battleExitChangedScene = false;
        _battleMusic.Stop();
        _endingIntroSeen = false;
        _endingComplete = false;
        _gameOver = null;
        _settings.Close();
        _overworldDialogue.Close();
        _party.Clear();
        _party.AddRange(BattleState.CreateParty());
        _zones = new Dictionary<string, ZoneProgress>
        {
            ["mapa2"] = new ZoneProgress()
        };
        if (_mapId != "mapa2")
        {
            _mapId = "mapa2";
            CargarMapa();
        }

        _pendingExit = null;
        _openedGifts.Clear();
        _encuentros.Reiniciar();
        _encounterTransition?.Reset();
        _encounterMusicStarted = false;
        if (_overworldUI.IsOpen)
            _overworldUI.Toggle();
        CrearPersonajes();
        IniciarJuego();
        _enMenu = volverAlMenu;
    }

    protected override void Draw(GameTime gameTime)
    {
        if (!_settings.CrtEnabled)
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
        if (_endingComplete)
        {
            DibujarFinal();
            return;
        }

        if (_battleExitElapsed >= 0)
        {
            DibujarSalidaBatalla(gameTime);
            return;
        }

        if (_settings.IsOpen)
        {
            GraphicsDevice.Clear(new Color(10, 10, 40));
            SpriteBatch.Begin(samplerState: SamplerState.PointClamp);
            _settings.Draw(SpriteBatch, _pixel, _earthboundHudTexture, _retroFont, TamanoVista());
            SpriteBatch.End();
            return;
        }

        if (_gameOver != null)
        {
            GraphicsDevice.Clear(Color.Black);
            SpriteBatch.Begin(samplerState: SamplerState.PointClamp);
            _gameOver.Draw(SpriteBatch, GraphicsDevice);
            SpriteBatch.End();
            base.Draw(gameTime);
            return;
        }

        bool enBatalla = !_enMenu && _encuentros.Activo && _currentState != null;
        GraphicsDevice.Clear(enBatalla ? Color.Black : ColorCielo);
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

        if (_encounterTransition.IsActive)
        {
            SpriteBatch.Begin(samplerState: SamplerState.PointClamp);
            _encounterTransition.Draw(SpriteBatch, _pixel, GraphicsDevice.Viewport.Bounds);
            SpriteBatch.End();
        }

        base.Draw(gameTime);
    }

    private void DibujarFinal()
    {
        GraphicsDevice.Clear(Color.Black);
        SpriteBatch.Begin(samplerState: SamplerState.PointClamp);
        string[] lines =
        {
            "FIN",
            "Solo es una demo, por ahora.",
            "Z: Volver al menú",
            "Escape: Salir del juego"
        };
        int centerY = GraphicsDevice.Viewport.Height / 2;
        for (int i = 0; i < lines.Length; i++)
        {
            Vector2 size = _retroFont.MeasureString(lines[i]);
            Vector2 position = new Vector2((GraphicsDevice.Viewport.Width - size.X) / 2f, centerY - 48 + i * 48);
            SpriteBatch.DrawString(_retroFont, lines[i], PixelPosition(position), Color.White);
        }

        SpriteBatch.End();
    }

    private void DibujarSalidaBatalla(GameTime gameTime)
    {
        GraphicsDevice.Clear(Color.Black);
        if (!_battleExitChangedScene)
        {
            SpriteBatch.Begin(samplerState: SamplerState.PointClamp);
            if (_battleFrame != null)
                SpriteBatch.Draw(_battleFrame, GraphicsDevice.Viewport.Bounds, Color.White);
            SpriteBatch.End();
        }
        else if (_gameOver != null)
        {
            SpriteBatch.Begin(samplerState: SamplerState.PointClamp);
            _gameOver.Draw(SpriteBatch, GraphicsDevice);
            SpriteBatch.End();
        }
        else
        {
            DibujarExploracion(gameTime);
        }

        float opacity;
        if (!_battleExitChangedScene)
            opacity = _battleExitElapsed / BattleExitDarkenSeconds;
        else
            opacity = 1f - (_battleExitElapsed - BattleExitDarkenSeconds) / BattleExitRevealSeconds;
        SpriteBatch.Begin(samplerState: SamplerState.PointClamp);
        SpriteBatch.Draw(_pixel, GraphicsDevice.Viewport.Bounds, Color.Black * Math.Clamp(opacity, 0f, 1f));
        SpriteBatch.End();
    }

    private void DibujarMenu(GameTime gameTime)
    {
        SpriteBatch.Begin(samplerState: SamplerState.PointClamp);
        _mainMenu.Draw(SpriteBatch, GraphicsDevice);
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
        _battleBackground.Draw(SpriteBatch, GraphicsDevice);
        SpriteBatch.Begin(samplerState: SamplerState.PointClamp);
        _currentState.Draw(SpriteBatch, GraphicsDevice);
        SpriteBatch.End();
        GraphicsDevice.SetRenderTargets(previous);
        GraphicsDevice.Clear(Color.Black);
        Vector2 offset = _currentState is BattleState battle ? battle.ShakeOffset : Vector2.Zero;
        SpriteBatch.Begin(samplerState: SamplerState.PointClamp);
        SpriteBatch.Draw(_battleFrame, offset, Color.White);
        if (_currentState is BattleState activeBattle && activeBattle.ScreenFlashOpacity > 0)
            SpriteBatch.Draw(_pixel, new Rectangle(0, 0, width, height), activeBattle.ScreenFlashColor * activeBattle.ScreenFlashOpacity);
        SpriteBatch.End();
    }

    private void DibujarExploracion(GameTime gameTime)
    {
        SpriteBatch.Begin(samplerState: SamplerState.PointClamp, transformMatrix: _camera.Transform);
        SpriteBatch.Draw(_mapaTextura, Vector2.Zero, Color.White);
        DibujarRegalosAbiertos();
        if (_verColisiones)
        {
            _colisiones.DibujarDebug(SpriteBatch, _pixel, new Rectangle((int)_camera.Position.X, (int)_camera.Position.Y, (int)TamanoVista().X, (int)TamanoVista().Y));
        }

        if (_yoko.Position.Y <= _kyoko.Position.Y)
        {
            DibujarPersonaje(_yoko);
            DibujarPersonaje(_kyoko);
        }
        else
        {
            DibujarPersonaje(_kyoko);
            DibujarPersonaje(_yoko);
        }

        SpriteBatch.End();
        SpriteBatch.Begin(samplerState: SamplerState.PointClamp);
        if (_mapId == "mapa2")
            _caveLighting.Draw(SpriteBatch, GraphicsDevice, _pixel, _yoko.Position - _camera.Position + new Vector2(32, 32));
        _overworldUI.Draw(SpriteBatch, _pixel, _earthboundHudTexture, _retroFont, _party, TamanoVista());
        _overworldDialogue.Draw(SpriteBatch, _earthboundHudTexture, _pixel, _retroFont, TamanoVista());
        SpriteBatch.End();
    }

    private void DibujarPersonaje(Player player)
    {
        player.CurrentAnimation?.Draw(SpriteBatch, PixelPosition(player.Position));
    }

    private static Vector2 PixelPosition(Vector2 position) => new Vector2((float)Math.Round(position.X), (float)Math.Round(position.Y));
    private Vector2 TamanoVista()
    {
        return new Vector2(GraphicsDevice.Viewport.Width, GraphicsDevice.Viewport.Height);
    }

    private Vector2 LimitarPosicion(Vector2 posicion, AnimatedSprite personaje)
    {
        if (personaje == null)
            return posicion;
        float ancho = personaje.Width;
        float alto = personaje.Height;
        float izquierda = personaje.Origin.X * personaje.Scale.X;
        float arriba = personaje.Origin.Y * personaje.Scale.Y;
        posicion.X = MathHelper.Clamp(posicion.X, izquierda, _worldSize.X - ancho + izquierda);
        posicion.Y = MathHelper.Clamp(posicion.Y, arriba, _worldSize.Y - alto + arriba);
        return posicion;
    }
}
