using System;
using System.IO;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Skies_Scattered.Entities;
using Skies_Scattered.Mechanics;

namespace Skies_Scattered.UI
{
    public class BattleUI : IDisposable
    {
        private SpriteFont _font;
        private Texture2D _earthboundHudTexture;
        private Texture2D _pixelTexture;
        private float _tiempo = 0f;
        private int _lastTarget = -1;
        private float _targetFlash;
        private readonly Dictionary<Texture2D, Texture2D> _whiteMasks = new();
        private readonly Dictionary<Texture2D, Rectangle> _visibleBounds = new();
        private Texture2D _shadow;
        private Texture2D _enemyBarTexture;
        private readonly Dictionary<Enemy, RollingCounter> _enemyBars = new();
        private static readonly Rectangle EnemyBarFrame = new Rectangle(1, 61, 105, 17);
        private static readonly Vector2[] RimOffsets =
        {
            new Vector2(-1, 0),
            new Vector2(1, 0),
            new Vector2(0, -1),
            new Vector2(0, 1)
        };
        internal static Vector2 GetIdleScale(Rectangle visible, float baseScale, bool floats, float time, int index)
        {
            if (floats)
                return new Vector2(baseScale);
            float pulse = (float)Math.Sin(time * 2.5f + index * 0.7f) * 0.018f;
            float width = Math.Max(1, (float)Math.Round(visible.Width * baseScale * (1f - pulse * 0.5f)));
            float height = Math.Max(1, (float)Math.Round(visible.Height * baseScale * (1f + pulse)));
            return new Vector2(width / visible.Width, height / visible.Height);
        }

        private Rectangle VisibleBounds(Texture2D texture)
        {
            if (_visibleBounds.TryGetValue(texture, out var result))
                return result;
            var pixels = new Color[texture.Width * texture.Height];
            texture.GetData(pixels);
            int left = texture.Width, top = texture.Height, right = -1, bottom = -1;
            for (int y = 0; y < texture.Height; y++)
                for (int x = 0; x < texture.Width; x++)
                    if (pixels[y * texture.Width + x].A > 0)
                    {
                        left = Math.Min(left, x);
                        top = Math.Min(top, y);
                        right = Math.Max(right, x);
                        bottom = Math.Max(bottom, y);
                    }

            result = right < left ? new Rectangle(0, 0, texture.Width, texture.Height) : new Rectangle(left, top, right - left + 1, bottom - top + 1);
            _visibleBounds.Add(texture, result);
            return result;
        }

        private void EnsureShadow(GraphicsDevice graphics)
        {
            if (_shadow != null)
                return;
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
            if (_whiteMasks.TryGetValue(source, out var mask))
                return mask;
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
            _enemyBarTexture?.Dispose();
            _enemyBars.Clear();
            foreach (var mask in _whiteMasks.Values)
                mask.Dispose();
            _whiteMasks.Clear();
            _pixelTexture?.Dispose();
            _shadow?.Dispose();
        }

        private readonly List<RollingCounter> _hpCounters = new List<RollingCounter>();
        private readonly List<RollingCounter> _ppCounters = new List<RollingCounter>();
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

        public bool HideLeadingZeros { get; set; } = false;

        private class RollingCounter
        {
            public int Visible;
            public int Target;
            public float Progress;
            public bool Initialized;
            public float ShowSeconds;
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

        public BattleUI(SpriteFont font, Texture2D earthboundHudTexture)
        {
            _font = font;
            _earthboundHudTexture = earthboundHudTexture;
        }

        public void Update(GameTime gameTime)
        {
            float elapsed = (float)gameTime.ElapsedGameTime.TotalSeconds;
            _tiempo += elapsed;
            _targetFlash += elapsed;
            foreach (var counter in _hpCounters)
                counter.Update(elapsed, SecondsPerNumber);
            foreach (var counter in _ppCounters)
                counter.Update(elapsed, SecondsPerNumber);
            foreach (var pair in _enemyBars)
            {
                int hp = Math.Max(0, pair.Key.HP);
                if (hp < pair.Value.Target)
                    pair.Value.ShowSeconds = 1.5f;
                else
                    pair.Value.ShowSeconds = Math.Max(0, pair.Value.ShowSeconds - elapsed);
                SetTarget(pair.Value, hp);
                pair.Value.Update(elapsed, .6f / Math.Max(1, pair.Key.MaxHP));
            }
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
                SetTarget(_hpCounters[i], state.PartyMembers[i].HP);
                SetTarget(_ppCounters[i], state.PartyMembers[i].PP);
            }
        }

        private static void SetTarget(RollingCounter counter, int target)
        {
            target = Math.Clamp(target, 0, 999);
            if (!counter.Initialized)
            {
                counter.Visible = target;
                counter.Initialized = true;
            }

            counter.Target = target;
        }

        public void Draw(SpriteBatch spriteBatch, BattleUIState state, List<Enemy> enemies, Vector2 screenSize)
        {
            if (_pixelTexture == null)
            {
                _pixelTexture = new Texture2D(spriteBatch.GraphicsDevice, 1, 1);
                _pixelTexture.SetData(new[] { Color.White });
            }

            spriteBatch.Draw(_pixelTexture, new Rectangle(0, TopBandHeight, (int)screenSize.X, (int)screenSize.Y - TopBandHeight - BottomBandHeight), Color.Black * 0.28f);
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
                    float areaWidth = Math.Min(540f, screenSize.X - 64f);
                    float slotWidth = areaWidth / Math.Max(1, aliveCount);
                    float areaHeight = screenSize.Y - TopBandHeight - BottomBandHeight;
                    float x = (screenSize.X - areaWidth) / 2f + slotWidth * (aliveIndex + 0.5f);
                    float y = TopBandHeight + areaHeight / 2f;
                    Rectangle visible = VisibleBounds(enemy.Texture);
                    float enemyScale = Math.Min(1.5f, Math.Min((slotWidth - 24f) / visible.Width, (areaHeight - 24f) / visible.Height));
                    float groundY = TopBandHeight + areaHeight * 0.72f;
                    y = groundY - visible.Height * enemyScale / 2f;
                    if (enemy.Floats)
                        y -= 18f;
                    aliveIndex++;
                    bool selected = state.IsSelectingTarget && i == state.SelectedEnemyIndex;
                    if (!state.IsSelectingTarget)
                        _lastTarget = -1;
                    if (selected && _lastTarget != i)
                    {
                        _lastTarget = i;
                        _targetFlash = 0f;
                    }

                    float wave = (float)Math.Sin(_tiempo * 2.5f + i * 0.7f);
                    x = (float)Math.Round(x);
                    y = (float)Math.Round(y + (enemy.Floats ? wave * 3f : 0f));
                    Vector2 origin = new Vector2(visible.X + visible.Width / 2f, visible.Y + visible.Height / 2f);
                    Vector2 renderScale = GetIdleScale(visible, enemyScale, enemy.Floats, _tiempo, i);
                    Vector2 drawPosition = new Vector2((float)Math.Round(x - origin.X * renderScale.X), enemy.Floats ? (float)Math.Round(y - origin.Y * renderScale.Y) : (float)Math.Round(groundY - visible.Bottom * renderScale.Y));
                    drawPosition += enemy.ImpactOffset;
                    enemy.Position = drawPosition + origin * renderScale;
                    int shadowWidth = Math.Max(24, (int)(visible.Width * enemyScale * 0.75f));
                    int shadowY = (int)groundY - 3;
                    spriteBatch.Draw(_shadow, new Rectangle((int)x - shadowWidth / 2, shadowY, shadowWidth, 12), Color.White);
                    Texture2D silhouette = WhiteMask(enemy.Texture);
                    Color rim = new Color(190, 205, 230) * 0.18f;
                    foreach (var offset in RimOffsets)
                        spriteBatch.Draw(silhouette, drawPosition + offset, null, rim, 0f, Vector2.Zero, renderScale, SpriteEffects.None, 0f);
                    spriteBatch.Draw(enemy.Texture, drawPosition, null, Color.White, 0f, Vector2.Zero, renderScale, SpriteEffects.None, 0f);
                    if (enemy.ImpactFlash > 0)
                        spriteBatch.Draw(silhouette, drawPosition, null, enemy.ImpactTint * enemy.ImpactFlash, 0f, Vector2.Zero, renderScale, SpriteEffects.None, 0f);
                    DrawEnemyHealthBar(spriteBatch, enemy, (int)x, (int)groundY + 16);
                    bool targetLightOn = _targetFlash % .5f < .18f;
                    if (selected && targetLightOn)
                        spriteBatch.Draw(silhouette, drawPosition, null, Color.White, 0f, Vector2.Zero, renderScale, SpriteEffects.None, 0f);
                }
            }

            Rectangle messageBox = new Rectangle(12, 8, (int)screenSize.X - 24, 64);
            DrawPanel(spriteBatch, messageBox);
            DrawWrappedText(spriteBatch, state.TopMessage, new Vector2(messageBox.X + 14, messageBox.Y + 10), messageBox.Width - 28, 44, Color.White, state.Message.VisibleCharacters);
            int bottom = (int)screenSize.Y - BottomBandHeight + 8;
            int statusWidth = 130;
            int statusStart = (int)screenSize.X - 12 - state.PartyMembers.Count * statusWidth;
            Rectangle menuBox = new Rectangle(12, bottom, Math.Min(300, Math.Max(100, statusStart - 24)), 104);
            DrawPanel(spriteBatch, menuBox);
            if (state.PartyMembers.Count > 0)
            {
                CharacterStats hero = state.PartyMembers[Math.Clamp(state.CurrentPartyMemberIndex, 0, state.PartyMembers.Count - 1)];
                if (state.IsActMenuOpen)
                {
                    DrawFittedText(spriteBatch, "> PERDONAR", new Vector2(26, bottom + 16), menuBox.Width - 28, 24, Color.White);
                    DrawFittedText(spriteBatch, "Enemigo con 25% HP o menos", new Vector2(26, bottom + 48), menuBox.Width - 28, 20, Color.White);
                }
                else if (state.IsItemMenuOpen)
                {
                    DrawFittedText(spriteBatch, "ITEMS", new Vector2(26, bottom + 10), menuBox.Width - 28, 18, Color.White);
                    if (hero.Items.Count == 0)
                        DrawFittedText(spriteBatch, "No tienes objetos", new Vector2(26, bottom + 38), menuBox.Width - 28, 22, Color.White);
                    else
                    {
                        int first = Math.Clamp(state.SelectedItemIndex - 1, 0, Math.Max(0, hero.Items.Count - 3));
                        for (int row = 0; row < Math.Min(3, hero.Items.Count); row++)
                        {
                            int i = first + row;
                            DrawFittedText(spriteBatch, (i == state.SelectedItemIndex ? "> " : "  ") + GameItem.Description(hero.Items[i]), new Vector2(26, bottom + 30 + row * 20), menuBox.Width - 28, 18, Color.White);
                        }
                    }
                }
                else if (state.IsSpellMenuOpen)
                {
                    DrawFittedText(spriteBatch, "Z-SPELL", new Vector2(26, bottom + 8), menuBox.Width - 28, 16, Color.LightGray);
                    for (int i = 0; i < hero.Spells.Count; i++)
                    {
                        string spell = hero.Spells[i];
                        Color color = Color.White;
                        string text = (i == state.SelectedSpellIndex ? "> " : "  ") + SpellNames.Display(spell);
                        DrawFittedText(spriteBatch, text, new Vector2(26, bottom + 30 + i * 22), menuBox.Width - 100, 20, color);
                        DrawFittedText(spriteBatch, $"{SpellRules.GetCost(spell)} PP", new Vector2(menuBox.Right - 70, bottom + 30 + i * 22), 58, 20, hero.PP >= SpellRules.GetCost(spell) ? color : Color.Gray);
                    }
                }
                else if (state.IsActionMenuOpen)
                {
                    for (int i = 0; i < state.CurrentMenuOptions.Count; i++)
                    {
                        float cellWidth = (menuBox.Width - 24) / 2f;
                        bool selected = i == state.SelectedOptionIndex;
                        bool cooling = state.CurrentMenuOptions[i] == "DEFEND" && state.DefendCooldown > 0;
                        string label = state.CurrentMenuOptions[i] + (cooling ? $" ({state.DefendCooldown})" : "");
                        DrawFittedText(spriteBatch, (selected ? "> " : "  ") + label, new Vector2(menuBox.X + 12 + (i % 2) * cellWidth, bottom + 12 + (i / 2) * 28), cellWidth - 8, 24, cooling ? Color.Gray : Color.White);
                    }
                }
                else if (state.IsSelectingTarget)
                {
                    DrawFittedText(spriteBatch, "Elige un objetivo", new Vector2(26, bottom + 22), menuBox.Width - 28, 22, Color.LightGray);
                }
            }

            EnsureCounters(state);
            for (int i = 0; i < state.PartyMembers.Count; i++)
            {
                CharacterStats p = state.PartyMembers[i];
                Vector2 statusPos = new Vector2(statusStart + i * statusWidth + 9, bottom + 4);
                if (i == state.ImpactedPartyIndex)
                    statusPos += state.PartyImpactOffset;
                if (_earthboundHudTexture != null)
                {
                    Rectangle sourceRect = StatBoxSource;
                    spriteBatch.Draw(_earthboundHudTexture, statusPos, sourceRect, Color.White, 0f, Vector2.Zero, StatScale, SpriteEffects.None, 0f);
                }

                if (_font != null)
                {
                    Vector2 measured = _font.MeasureString(p.Name);
                    float nameScale = Math.Min(1f, 94f / Math.Max(1f, measured.X));
                    int nameWidth = (int)Math.Ceiling(measured.X * nameScale);
                    int nameHeight = (int)Math.Ceiling(measured.Y * nameScale);
                    int nameX = (int)statusPos.X + (112 - nameWidth) / 2;
                    int nameY = (int)statusPos.Y - nameHeight + 8;
                    if (_pixelTexture != null)
                    {
                        spriteBatch.Draw(_pixelTexture, new Rectangle(nameX - 4, nameY - 2, nameWidth + 8, nameHeight + 4), Color.Black);
                    }

                    spriteBatch.DrawString(_font, p.Name, new Vector2(nameX, nameY), Color.White, 0f, Vector2.Zero, nameScale, SpriteEffects.None, 0f);
                }

                DrawCounter(spriteBatch, _hpCounters[i], statusPos, 25, p.HP <= 0, StatScale);
                DrawCounter(spriteBatch, _ppCounters[i], statusPos, 41, false, StatScale);
            }
        }

        private void DrawEnemyHealthBar(SpriteBatch batch, Enemy enemy, int centerX, int y)
        {
            if (!_enemyBars.TryGetValue(enemy, out var counter))
            {
                counter = new RollingCounter
                {
                    Visible = enemy.MaxHP,
                    Target = enemy.MaxHP,
                    Initialized = true
                };
                _enemyBars.Add(enemy, counter);
            }

            if (counter.ShowSeconds <= 0)
                return;
            if (_enemyBarTexture == null)
            {
                string path = Path.Combine(AppContext.BaseDirectory, "Content", "UI", "EnemyHealthBar.png");
                using var stream = File.OpenRead(path);
                _enemyBarTexture = Texture2D.FromStream(batch.GraphicsDevice, stream);
            }

            var box = new Rectangle(centerX - EnemyBarFrame.Width / 2, y, EnemyBarFrame.Width, EnemyBarFrame.Height);
            batch.Draw(_enemyBarTexture, box, EnemyBarFrame, Color.Black);
            batch.Draw(_enemyBarTexture, new Rectangle(box.X + 2, box.Y + 2, 101, 13), new Rectangle(3, 123, 1, 1), Color.White);
            float ratio = Math.Clamp(counter.Visible / (float)Math.Max(1, enemy.MaxHP), 0f, 1f);
            int width = (int)Math.Round(101 * ratio);
            if (width <= 0)
                return;
            Rectangle colorSource;
            if (ratio > .5f)
                colorSource = new Rectangle(8, 30, 1, 1);
            else if (ratio > .25f)
                colorSource = new Rectangle(40, 10, 1, 1);
            else
                colorSource = new Rectangle(110, 30, 1, 1);
            batch.Draw(_enemyBarTexture, new Rectangle(box.X + 2, box.Y + 2, width, 13), colorSource, Color.White);
        }

        private void DrawPanel(SpriteBatch batch, Rectangle box)
        {
            if (_earthboundHudTexture != null)
                EarthboundBox.Draw(batch, _earthboundHudTexture, _pixelTexture, box);
        }

        private void DrawFittedText(SpriteBatch batch, string value, Vector2 position, float maxWidth, float maxHeight, Color color)
        {
            if (_font == null || string.IsNullOrEmpty(value) || maxWidth <= 0f || maxHeight <= 0f)
            {
                return;
            }

            Vector2 size = _font.MeasureString(value);
            if (size.X <= 0f || size.Y <= 0f)
                return;
            float scale = Math.Min(1f, Math.Min(maxWidth / size.X, maxHeight / size.Y));
            batch.DrawString(_font, value, new Vector2((int)position.X, (int)position.Y), color, 0f, Vector2.Zero, scale, SpriteEffects.None, 0f);
        }

        private void DrawWrappedText(SpriteBatch batch, string message, Vector2 position, float width, float height, Color color, int visibleCharacters = int.MaxValue)
        {
            if (_font == null || string.IsNullOrEmpty(message))
            {
                return;
            }

            if (width <= 0 || height <= 0)
                return;
            float preferredScale = Math.Min(1f, height / _font.LineSpacing);
            var lines = new List<string>();
            string line = "";
            foreach (string word in message.Replace("\r", "").Replace("\n", " ").Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries))
            {
                string candidate = line.Length == 0 ? word : line + " " + word;
                if (line.Length > 0 && _font.MeasureString(candidate).X * preferredScale > width)
                {
                    lines.Add(line);
                    line = word;
                }
                else
                    line = candidate;
            }

            if (line.Length > 0)
                lines.Add(line);
            if (lines.Count == 0)
                return;
            float widest = 1f;
            foreach (string text in lines)
                widest = Math.Max(widest, _font.MeasureString(text).X);
            float scale = Math.Min(preferredScale, Math.Min(width / widest, height / (lines.Count * _font.LineSpacing)));
            float y = position.Y + (height - lines.Count * _font.LineSpacing * scale) / 2f;
            foreach (string text in lines)
            {
                string visibleText = text.Substring(0, Math.Min(text.Length, Math.Max(0, visibleCharacters)));
                batch.DrawString(_font, visibleText, new Vector2((int)position.X, (int)y), color, 0f, Vector2.Zero, scale, SpriteEffects.None, 0f);
                visibleCharacters -= text.Length + 1;
                y += _font.LineSpacing * scale;
            }
        }

        private static Rectangle DigitSource(int digit, int transitionFrame)
        {
            if (digit < 0 || digit > 9)
                throw new ArgumentOutOfRangeException(nameof(digit));
            if (transitionFrame < 0 || transitionFrame > 3)
                throw new ArgumentOutOfRangeException(nameof(transitionFrame));
            int row = digit / 4;
            int column = digit % 4;
            return new Rectangle(769 + column * 32 + transitionFrame * 8, 129 + row * 16, 7, 12);
        }

        private void DrawCounter(SpriteBatch spriteBatch, RollingCounter counter, Vector2 panelPosition, int rowOffset, bool defeated, float scale = 2f)
        {
            if (_earthboundHudTexture == null)
                return;
            if (defeated && counter.Visible == 0 && counter.Target == 0)
            {
                spriteBatch.Draw(_earthboundHudTexture, new Rectangle((int)panelPosition.X + (int)(25 * scale), (int)panelPosition.Y + (int)(rowOffset * scale), (int)(23 * scale), (int)(12 * scale)), CrossSource, Color.White);
                return;
            }

            int next = counter.Visible;
            if (counter.Visible != counter.Target)
            {
                next += counter.Target > counter.Visible ? 1 : -1;
            }

            for (int place = 0; place < 3; place++)
            {
                int divisor = place == 0 ? 100 : place == 1 ? 10 : 1;
                int currentDigit = (counter.Visible / divisor) % 10;
                int nextDigit = (next / divisor) % 10;
                Rectangle source = DigitSource(currentDigit, 0);
                if (currentDigit != nextDigit)
                {
                    int phase = Math.Min(3, (int)(counter.Progress * 4f));
                    if (next > counter.Visible)
                    {
                        source = DigitSource(currentDigit, phase);
                    }
                    else
                    {
                        if (phase > 0)
                        {
                            source = DigitSource(nextDigit, 4 - phase);
                        }
                    }
                }

                if (HideLeadingZeros && place < 2 && counter.Visible < divisor && next < divisor)
                    source = NullSource;
                spriteBatch.Draw(_earthboundHudTexture, new Rectangle((int)panelPosition.X + (int)((25 + place * 8) * scale), (int)panelPosition.Y + (int)(rowOffset * scale), (int)(7 * scale), (int)(12 * scale)), source, Color.White);
            }
        }
    }
}
