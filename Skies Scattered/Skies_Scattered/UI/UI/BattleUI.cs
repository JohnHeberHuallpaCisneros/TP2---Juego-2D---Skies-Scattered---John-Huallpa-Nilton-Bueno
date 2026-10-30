
using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Skies_Scattered.Entities;

namespace Skies_Scattered.UI
{
    public class BattleUI : IDisposable
    {
        private SpriteFont _font;
        private Texture2D _uiBordersTexture;
        private Texture2D _earthboundHudTexture;
        private Texture2D _pixelTexture;
        private float _tiempo = 0f;
        private int _lastTarget = -1;
        private float _targetFlash;
        private readonly Dictionary<Texture2D, Texture2D> _whiteMasks = new();
        private readonly Dictionary<Texture2D, Rectangle> _visibleBounds = new();
        private Texture2D _shadow;
        private static readonly Vector2[] RimOffsets = {
            new Vector2(-1, 0), new Vector2(1, 0), new Vector2(0, -1), new Vector2(0, 1)
        };

        private Rectangle VisibleBounds(Texture2D texture)
        {
            if (_visibleBounds.TryGetValue(texture, out var result)) return result;
            var pixels = new Color[texture.Width * texture.Height];
            texture.GetData(pixels);
            int left = texture.Width, top = texture.Height, right = -1, bottom = -1;
            for (int y = 0; y < texture.Height; y++)
                for (int x = 0; x < texture.Width; x++)
                    if (pixels[y * texture.Width + x].A > 0)
                    {
                        left = Math.Min(left, x); top = Math.Min(top, y);
                        right = Math.Max(right, x); bottom = Math.Max(bottom, y);
                    }
            result = right < left ? new Rectangle(0, 0, texture.Width, texture.Height)
                : new Rectangle(left, top, right - left + 1, bottom - top + 1);
            _visibleBounds.Add(texture, result);
            return result;
        }

        private void EnsureShadow(GraphicsDevice graphics)
        {
            if (_shadow != null) return;
            _shadow = new Texture2D(graphics, 32, 8);
            var pixels = new Color[32 * 8];
            for (int y = 0; y < 8; y++)
                for (int x = 0; x < 32; x++)
                {
                    float dx = (x - 15.5f) / 16f, dy = (y - 3.5f) / 4f;
                    pixels[y * 32 + x] = Color.Black * MathHelper.Clamp((1f - dx * dx - dy * dy) * 0.45f, 0f, 0.45f);
                }
            _shadow.SetData(pixels);
        }

        private Texture2D WhiteMask(Texture2D source)
        {
            if (_whiteMasks.TryGetValue(source, out var mask)) return mask;
            Color[] pixels = new Color[source.Width * source.Height];
            source.GetData(pixels);
            for (int i = 0; i < pixels.Length; i++)
                pixels[i] = new Color(pixels[i].A, pixels[i].A, pixels[i].A, pixels[i].A);
            mask = new Texture2D(source.GraphicsDevice, source.Width, source.Height);
            mask.SetData(pixels);
            _whiteMasks.Add(source, mask);
            return mask;
        }

        public void Dispose()
        {
            foreach (var mask in _whiteMasks.Values) mask.Dispose();
            _whiteMasks.Clear();
            _pixelTexture?.Dispose();
            _shadow?.Dispose();
        }

        private readonly List<RollingCounter> _hpCounters =
            new List<RollingCounter>();

        private readonly List<RollingCounter> _ppCounters =
            new List<RollingCounter>();

        internal const int TopBandHeight = 80;
        internal const int BottomBandHeight = 144;
        private const float StatScale = 2f;
        private static readonly Rectangle StatBoxSource = new Rectangle(917, 143, 56, 64);
        private static readonly Rectangle NullSource = new Rectangle(833, 161, 7, 12);
        private static readonly Rectangle CrossSource = new Rectangle(865, 161, 23, 12);

        private float _secondsPerNumber = 0.075f;
        public float SecondsPerNumber
        {
            get => _secondsPerNumber;
            set
            {
                if (!float.IsFinite(value) || value <= 0f)
                    throw new ArgumentOutOfRangeException(nameof(value));
                _secondsPerNumber = value;
            }
        }

        // Opcional: mantiene el diseño 000 por defecto; Null nunca se trata como cero.
        public bool HideLeadingZeros { get; set; } = false;

        private class RollingCounter
        {
            public int Visible;
            public int Target;
            public float Progress;
            public bool Initialized;

            public void Update(float elapsed, float secondsPerNumber)
            {
                if (Visible == Target)
                {
                    Progress = 0f;
                    return;
                }

                Progress += elapsed / secondsPerNumber;

                while (Progress >= 1f && Visible != Target)
                {
                    Visible += Target > Visible ? 1 : -1;
                    Progress -= 1f;
                }

                if (Visible == Target)
                    Progress = 0f;
            }
        }

        public BattleUI(
            SpriteFont font,
            Texture2D uiBordersTexture,
            Texture2D earthboundHudTexture)
        {
            _font = font;
            _uiBordersTexture = uiBordersTexture;
            _earthboundHudTexture = earthboundHudTexture;
        }

        public void Update(GameTime gameTime)
        {
            float elapsed =
                (float)gameTime.ElapsedGameTime.TotalSeconds;

            _tiempo += elapsed;
            _targetFlash += elapsed;

            foreach (var counter in _hpCounters)
                counter.Update(elapsed, SecondsPerNumber);

            foreach (var counter in _ppCounters)
                counter.Update(elapsed, SecondsPerNumber);
        }

        private void EnsureCounters(BattleUIState state)
        {
            while (_hpCounters.Count < state.PartyMembers.Count)
            {
                _hpCounters.Add(new RollingCounter());
                _ppCounters.Add(new RollingCounter());
            }

            for (int i = 0; i < state.PartyMembers.Count; i++)
            {
                SetTarget(
                    _hpCounters[i],
                    state.PartyMembers[i].HP);

                SetTarget(
                    _ppCounters[i],
                    state.PartyMembers[i].PP);
            }
        }

        private static void SetTarget(
            RollingCounter counter,
            int target)
        {
            target = Math.Clamp(target, 0, 999);

            if (!counter.Initialized)
            {
                counter.Visible = target;
                counter.Initialized = true;
            }

            counter.Target = target;
        }

        public void Draw(
            SpriteBatch spriteBatch,
            BattleUIState state,
            List<Enemy> enemies,
            Vector2 screenSize)
        {
            if (_pixelTexture == null)
            {
                _pixelTexture = new Texture2D(
                    spriteBatch.GraphicsDevice,
                    1,
                    1);

                _pixelTexture.SetData(
                    new[] { Color.White });
            }

            // ========================================
            // 1. ENEMIGOS
            // ========================================

            // Un fondo más tranquilo ayuda a separar las siluetas del escenario.
            spriteBatch.Draw(_pixelTexture, new Rectangle(0, TopBandHeight, (int)screenSize.X,
                (int)screenSize.Y - TopBandHeight - BottomBandHeight), Color.Black * 0.28f);
            EnsureShadow(spriteBatch.GraphicsDevice);

            if (enemies != null)
            {
                int aliveCount = 0;

                foreach (Enemy enemy in enemies)
                {
                    if (enemy.Texture != null && enemy.HP > 0)
                        aliveCount++;
                }

                int aliveIndex = 0;

                for (int i = 0; i < enemies.Count; i++)
                {
                    Enemy enemy = enemies[i];

                    if (enemy.Texture == null || enemy.HP <= 0)
                        continue;

                    // Un espacio por enemigo vivo: 1 centrado, 2 o 3 repartidos.
                    float areaWidth = Math.Min(540f, screenSize.X - 64f);
                    float slotWidth = areaWidth / Math.Max(1, aliveCount);
                    float areaHeight = screenSize.Y - TopBandHeight - BottomBandHeight;
                    float x = (screenSize.X - areaWidth) / 2f + slotWidth * (aliveIndex + 0.5f);
                    float y = TopBandHeight + areaHeight / 2f;
                    Rectangle visible = VisibleBounds(enemy.Texture);
                    // Usar la silueta visible, sin contar márgenes transparentes.
                    float enemyScale = Math.Min(1.5f, Math.Min(
                        (slotWidth - 24f) / visible.Width,
                        (areaHeight - 24f) / visible.Height));
                    float groundY = TopBandHeight + areaHeight * 0.72f;
                    y = groundY - visible.Height * enemyScale / 2f;
                    if (enemy.Floats) y -= 18f;

                    aliveIndex++;

                    bool selected = state.IsSelectingTarget && i == state.SelectedEnemyIndex;
                    if (!state.IsSelectingTarget) _lastTarget = -1;
                    if (selected && _lastTarget != i)
                    {
                        _lastTarget = i;
                        _targetFlash = 0f;
                    }

                    // Movimiento suave del enemigo.
                    float wave = (float)Math.Sin(
                        _tiempo * 2.5f + i * 0.7f);

                    // Flotación alineada a píxeles, conservando el filtrado por puntos.
                    x = (float)Math.Round(x);
                    y = (float)Math.Round(y + (enemy.Floats ? wave * 3f : 0f));
                    Vector2 origin = new Vector2(
                        visible.X + visible.Width / 2f,
                        visible.Y + visible.Height / 2f);
                    Vector2 drawPosition = new Vector2(
                        (float)Math.Round(x - origin.X * enemyScale),
                        (float)Math.Round(y - origin.Y * enemyScale));
                    enemy.Position = drawPosition + origin * enemyScale;

                    int shadowWidth = Math.Max(24, (int)(visible.Width * enemyScale * 0.75f));
                    int shadowY = (int)groundY - 3;
                    spriteBatch.Draw(_shadow, new Rectangle((int)x - shadowWidth / 2, shadowY, shadowWidth, 12), Color.White);
                    Texture2D silhouette = WhiteMask(enemy.Texture);
                    Color rim = new Color(190, 205, 230) * 0.18f;
                    foreach (var offset in RimOffsets)
                        spriteBatch.Draw(silhouette, drawPosition + offset, null, rim,
                            0f, Vector2.Zero, enemyScale, SpriteEffects.None, 0f);

                    spriteBatch.Draw(
                        enemy.Texture,
                        drawPosition,
                        null,
                        Color.White,
                        0f,
                        Vector2.Zero,
                        new Vector2(enemyScale),
                        SpriteEffects.None,
                        0f);
                    // Destello breve sobre la silueta; el enemigo nunca desaparece.
                    float pulse = _targetFlash % 0.8f;
                    if (selected && pulse < 0.12f)
                        spriteBatch.Draw(silhouette, drawPosition, null,
                            Color.White * (0.85f * (1f - pulse / 0.12f)), 0f, Vector2.Zero,
                            new Vector2(enemyScale), SpriteEffects.None, 0f);
                }
            }

            // Franjas reservadas: mensaje arriba, controles y grupo abajo.
            Rectangle messageBox = new Rectangle(12, 8, (int)screenSize.X - 24, 64);
            DrawPanel(spriteBatch, messageBox, Color.Cyan);
            DrawWrappedText(spriteBatch, state.TopMessage,
                new Vector2(messageBox.X + 14, messageBox.Y + 10),
                messageBox.Width - 28, 44, Color.White, state.Message.VisibleCharacters);

            int bottom = (int)screenSize.Y - BottomBandHeight + 8;
            int statusWidth = 130;
            int statusStart = (int)screenSize.X - 12 - state.PartyMembers.Count * statusWidth;
            Rectangle menuBox = new Rectangle(12, bottom, Math.Min(300, Math.Max(100, statusStart - 24)), 104);
            DrawPanel(spriteBatch, menuBox, Color.Cyan);

            if (state.PartyMembers.Count > 0)
            {
                CharacterStats hero = state.PartyMembers[Math.Clamp(
                    state.CurrentPartyMemberIndex, 0, state.PartyMembers.Count - 1)];

                if (state.IsItemMenuOpen)
                {
                    DrawFittedText(spriteBatch, "ITEMS", new Vector2(26, bottom + 10),
                        menuBox.Width - 28, 18, Color.Cyan);
                    DrawFittedText(spriteBatch, "No tienes objetos", new Vector2(26, bottom + 38),
                        menuBox.Width - 28, 22, Color.White);
                    DrawFittedText(spriteBatch, "X / Z: Volver", new Vector2(26, bottom + 76),
                        menuBox.Width - 28, 16, Color.LightGray);
                }
                else if (state.IsSpellMenuOpen)
                {
                    DrawFittedText(spriteBatch, "Z-SPELL   /   X: Volver", new Vector2(26, bottom + 8),
                        menuBox.Width - 28, 16, Color.LightGray);
                    for (int i = 0; i < hero.Spells.Count; i++)
                    {
                        string spell = hero.Spells[i];
                        Color color = i == state.SelectedSpellIndex ? Color.Cyan : Color.White;
                        string text = (i == state.SelectedSpellIndex ? "> " : "  ") + spell;
                        DrawFittedText(spriteBatch, text, new Vector2(26, bottom + 30 + i * 22),
                            menuBox.Width - 100, 20, color);
                        DrawFittedText(spriteBatch, $"{GetSpellCost(spell)} PP",
                            new Vector2(menuBox.Right - 70, bottom + 30 + i * 22),
                            58, 20, hero.PP >= GetSpellCost(spell) ? color : Color.Gray);
                    }
                }
                else if (state.IsActionMenuOpen)
                {
                    for (int i = 0; i < state.CurrentMenuOptions.Count; i++)
                    {
                        float cellWidth = (menuBox.Width - 24) / 2f;
                        bool selected = i == state.SelectedOptionIndex;
                        DrawFittedText(spriteBatch,
                            (selected ? "> " : "  ") + state.CurrentMenuOptions[i],
                            new Vector2(menuBox.X + 12 + (i % 2) * cellWidth,
                                bottom + 12 + (i / 2) * 28),
                            cellWidth - 8, 24, selected ? Color.Cyan : Color.White);
                    }
                }
                else
                {
                    DrawFittedText(spriteBatch,
                        state.IsSelectingTarget ? "A / D: Elegir objetivo" :
                            state.IsAwaitingIntro ? "Z: Continuar" : "Combate en curso",
                        new Vector2(26, bottom + 22), menuBox.Width - 28, 22, Color.LightGray);
                    if (state.IsSelectingTarget)
                        DrawFittedText(spriteBatch, "Z: Confirmar   X: Volver",
                            new Vector2(26, bottom + 58), menuBox.Width - 28, 20, Color.White);
                }
            }

            EnsureCounters(state);
            for (int i = 0; i < state.PartyMembers.Count; i++)
            {
                CharacterStats p = state.PartyMembers[i];
                // El panel original se adapta a la franja inferior sin cubrir enemigos.
                Vector2 statusPos = new Vector2(statusStart + i * statusWidth + 9, bottom + 4);
                // Cuadro original de EarthBound.
                if (_earthboundHudTexture != null)
                {
                    Rectangle sourceRect = StatBoxSource;

                    spriteBatch.Draw(
                        _earthboundHudTexture,
                        statusPos,
                        sourceRect,
                        Color.White,
                        0f,
                        Vector2.Zero,
                        StatScale,
                        SpriteEffects.None,
                        0f);
                }

                // ========================================
                // 6. NOMBRE DEL PERSONAJE
                // ========================================

                if (_font != null)
                {
                    Vector2 measured =
                        _font.MeasureString(p.Name);

                    float nameScale = Math.Min(
                        1f,
                        94f / Math.Max(1f, measured.X));

                    int nameWidth = (int)Math.Ceiling(
                        measured.X * nameScale);

                    int nameHeight = (int)Math.Ceiling(
                        measured.Y * nameScale);

                    int nameX =
                        (int)statusPos.X +
                        (112 - nameWidth) / 2;

                    int nameY =
                        (int)statusPos.Y -
                        nameHeight + 8;

                    // Fondo negro del nombre.
                    if (_pixelTexture != null)
                    {
                        spriteBatch.Draw(
                            _pixelTexture,
                            new Rectangle(
                                nameX - 4,
                                nameY - 2,
                                nameWidth + 8,
                                nameHeight + 4),
                            Color.Black);
                    }

                    spriteBatch.DrawString(
                        _font,
                        p.Name,
                        new Vector2(nameX, nameY),
                        Color.White,
                        0f,
                        Vector2.Zero,
                        nameScale,
                        SpriteEffects.None,
                        0f);
                }

                // ========================================
                // 7. CONTADORES ANIMADOS
                // ========================================

                DrawCounter(
                    spriteBatch,
                    _hpCounters[i],
                    statusPos,
                    25,
                    p.HP <= 0,
                    StatScale);

                DrawCounter(
                    spriteBatch,
                    _ppCounters[i],
                    statusPos,
                    41,
                    false,
                    StatScale);
            }
        }

        private void DrawPanel(SpriteBatch batch, Rectangle box, Color accent)
        {
            DrawEarthboundBox(batch, _earthboundHudTexture, box);
        }

        // ========================================
        // 8. TEXTO AJUSTADO AL CUADRO
        // ========================================

        private void DrawFittedText(
            SpriteBatch batch,
            string value,
            Vector2 position,
            float maxWidth,
            float maxHeight,
            Color color)
        {
            if (_font == null ||
                string.IsNullOrEmpty(value) ||
                maxWidth <= 0f ||
                maxHeight <= 0f)
            {
                return;
            }

            Vector2 size = _font.MeasureString(value);

            if (size.X <= 0f || size.Y <= 0f)
                return;

            float scale = Math.Min(
                1f,
                Math.Min(
                    maxWidth / size.X,
                    maxHeight / size.Y));

            batch.DrawString(
                _font,
                value,
                new Vector2(
                    (int)position.X,
                    (int)position.Y),
                color,
                0f,
                Vector2.Zero,
                scale,
                SpriteEffects.None,
                0f);
        }

        // ========================================
        // 9. SALTOS DE LÍNEA AUTOMÁTICOS
        // ========================================

        private void DrawWrappedText(
            SpriteBatch batch,
            string message,
            Vector2 position,
            float width,
            float height,
            Color color,
            int visibleCharacters = int.MaxValue)
        {
            if (_font == null ||
                string.IsNullOrEmpty(message))
            {
                return;
            }

            if (width <= 0 || height <= 0) return;
            float preferredScale = Math.Min(1f, height / _font.LineSpacing);
            var lines = new List<string>();
            string line = "";
            foreach (string word in message.Replace("\r", "").Replace("\n", " ")
                .Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries))
            {
                string candidate = line.Length == 0 ? word : line + " " + word;
                if (line.Length > 0 && _font.MeasureString(candidate).X * preferredScale > width)
                {
                    lines.Add(line);
                    line = word;
                }
                else line = candidate;
            }
            if (line.Length > 0) lines.Add(line);
            if (lines.Count == 0) return;
            float widest = 1f;
            foreach (string text in lines) widest = Math.Max(widest, _font.MeasureString(text).X);
            float scale = Math.Min(preferredScale, Math.Min(width / widest,
                height / (lines.Count * _font.LineSpacing)));
            float y = position.Y + (height - lines.Count * _font.LineSpacing * scale) / 2f;
            foreach (string text in lines)
            {
                string visibleText = text.Substring(0, Math.Min(text.Length, Math.Max(0, visibleCharacters)));
                batch.DrawString(_font, visibleText, new Vector2((int)position.X, (int)y),
                    color, 0f, Vector2.Zero, scale, SpriteEffects.None, 0f);
                visibleCharacters -= text.Length + 1;
                y += _font.LineSpacing * scale;
            }
        }

        // ========================================
        // 10. SPRITES DE LOS DÍGITOS
        // ========================================

        private static Rectangle DigitSource(
            int digit,
            int transitionFrame)
        {
            if (digit < 0 || digit > 9)
                throw new ArgumentOutOfRangeException(nameof(digit));
            if (transitionFrame < 0 || transitionFrame > 3)
                throw new ArgumentOutOfRangeException(nameof(transitionFrame));
            int row = digit / 4;
            int column = digit % 4;

            return new Rectangle(
                769 + column * 32 + transitionFrame * 8,
                129 + row * 16,
                7,
                12);
        }

        // ========================================
        // 11. CONTADOR PROGRESIVO EARTHBOUND
        // ========================================

        private void DrawCounter(
            SpriteBatch spriteBatch,
            RollingCounter counter,
            Vector2 panelPosition,
            int rowOffset,
            bool defeated,
            float scale = 2f)
        {
            if (_earthboundHudTexture == null)
                return;

            // Símbolo Cross cuando el HP llega a cero.
            if (defeated &&
                counter.Visible == 0 &&
                counter.Target == 0)
            {
                spriteBatch.Draw(
                    _earthboundHudTexture,
                    new Rectangle(
                        (int)panelPosition.X + (int)(25 * scale),
                        (int)panelPosition.Y + (int)(rowOffset * scale),
                        (int)(23 * scale),
                        (int)(12 * scale)),
                    CrossSource,
                    Color.White);

                return;
            }

            int next = counter.Visible;

            if (counter.Visible != counter.Target)
            {
                next += counter.Target > counter.Visible
                    ? 1
                    : -1;
            }

            // Centenas, decenas y unidades.
            for (int place = 0; place < 3; place++)
            {
                int divisor = place == 0
                    ? 100
                    : place == 1 ? 10 : 1;

                int currentDigit =
                    (counter.Visible / divisor) % 10;

                int nextDigit =
                    (next / divisor) % 10;

                Rectangle source =
                    DigitSource(currentDigit, 0);

                if (currentDigit != nextDigit)
                {
                    int phase = Math.Min(
                        3,
                        (int)(counter.Progress * 4f));

                    if (next > counter.Visible)
                    {
                        // Animación ascendente.
                        source = DigitSource(
                            currentDigit,
                            phase);
                    }
                    else
                    {
                        // Animación descendente.
                        if (phase > 0)
                        {
                            source = DigitSource(
                                nextDigit,
                                4 - phase);
                        }
                    }
                }

                // No ocultar una cifra que participa en un acarreo o préstamo.
                if (HideLeadingZeros && place < 2 &&
                    counter.Visible < divisor && next < divisor)
                    source = NullSource;

                spriteBatch.Draw(
                    _earthboundHudTexture,
                    new Rectangle(
                        (int)panelPosition.X +
                            (int)((25 + place * 8) * scale),
                        (int)panelPosition.Y +
                            (int)(rowOffset * scale),
                        (int)(7 * scale),
                        (int)(12 * scale)),
                    source,
                    Color.White);
            }
        }

        // ========================================
        // 12. COSTOS DE LOS HECHIZOS
        // ========================================

        private int GetSpellCost(string spell)
        {
            switch (spell)
            {
                case "FireBeta":
                    return 5;

                case "FireGamma":
                    return 10;

                case "FreezeAlpha":
                    return 5;

                case "FreezeOmega":
                    return 10;

                case "DefenseDownOmega":
                    return 8;

                case "ShieldBeta":
                    return 8;

                default:
                    return 0;
            }
        }

        // ========================================
        // 13. CUADROS ESTILO EARTHBOUND
        // ========================================

        private void DrawEarthboundBox(
            SpriteBatch spriteBatch,
            Texture2D texture,
            Rectangle rect)
        {
            if (texture == null)
                return;

            int atlasX = 976;
            int atlasY = 143;
            int width = 56;
            int height = 64;
            int cornerSize = 12;

            Rectangle topLeftSrc = new Rectangle(
                atlasX,
                atlasY,
                cornerSize,
                cornerSize);

            Rectangle topRightSrc = new Rectangle(
                atlasX + width - cornerSize,
                atlasY,
                cornerSize,
                cornerSize);

            Rectangle bottomLeftSrc = new Rectangle(
                atlasX,
                atlasY + height - cornerSize,
                cornerSize,
                cornerSize);

            Rectangle bottomRightSrc = new Rectangle(
                atlasX + width - cornerSize,
                atlasY + height - cornerSize,
                cornerSize,
                cornerSize);

            Rectangle topSrc = new Rectangle(
                atlasX + cornerSize,
                atlasY,
                width - cornerSize * 2,
                cornerSize);

            Rectangle bottomSrc = new Rectangle(
                atlasX + cornerSize,
                atlasY + height - cornerSize,
                width - cornerSize * 2,
                cornerSize);

            Rectangle leftSrc = new Rectangle(
                atlasX,
                atlasY + cornerSize,
                cornerSize,
                height - cornerSize * 2);

            Rectangle rightSrc = new Rectangle(
                atlasX + width - cornerSize,
                atlasY + cornerSize,
                cornerSize,
                height - cornerSize * 2);

            Rectangle centerSrc = new Rectangle(
                atlasX + cornerSize,
                atlasY + cornerSize,
                width - cornerSize * 2,
                height - cornerSize * 2);

            Rectangle centerDest = new Rectangle(
                rect.X + cornerSize,
                rect.Y + cornerSize,
                rect.Width - cornerSize * 2,
                rect.Height - cornerSize * 2);

            // Centro.
            spriteBatch.Draw(
                texture,
                centerDest,
                centerSrc,
                Color.White);

            // Esquinas.
            spriteBatch.Draw(
                texture,
                new Rectangle(
                    rect.X,
                    rect.Y,
                    cornerSize,
                    cornerSize),
                topLeftSrc,
                Color.White);

            spriteBatch.Draw(
                texture,
                new Rectangle(
                    rect.X + rect.Width - cornerSize,
                    rect.Y,
                    cornerSize,
                    cornerSize),
                topRightSrc,
                Color.White);

            spriteBatch.Draw(
                texture,
                new Rectangle(
                    rect.X,
                    rect.Y + rect.Height - cornerSize,
                    cornerSize,
                    cornerSize),
                bottomLeftSrc,
                Color.White);

            spriteBatch.Draw(
                texture,
                new Rectangle(
                    rect.X + rect.Width - cornerSize,
                    rect.Y + rect.Height - cornerSize,
                    cornerSize,
                    cornerSize),
                bottomRightSrc,
                Color.White);

            // Bordes.
            spriteBatch.Draw(
                texture,
                new Rectangle(
                    rect.X + cornerSize,
                    rect.Y,
                    rect.Width - cornerSize * 2,
                    cornerSize),
                topSrc,
                Color.White);

            spriteBatch.Draw(
                texture,
                new Rectangle(
                    rect.X + cornerSize,
                    rect.Y + rect.Height - cornerSize,
                    rect.Width - cornerSize * 2,
                    cornerSize),
                bottomSrc,
                Color.White);

            spriteBatch.Draw(
                texture,
                new Rectangle(
                    rect.X,
                    rect.Y + cornerSize,
                    cornerSize,
                    rect.Height - cornerSize * 2),
                leftSrc,
                Color.White);

            spriteBatch.Draw(
                texture,
                new Rectangle(
                    rect.X + rect.Width - cornerSize,
                    rect.Y + cornerSize,
                    cornerSize,
                    rect.Height - cornerSize * 2),
                rightSrc,
                Color.White);
        }
    }
}

