
using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Skies_Scattered.Entities;

namespace Skies_Scattered.UI
{
    public class BattleUI
    {
        private SpriteFont _font;
        private Texture2D _uiBordersTexture;
        private Texture2D _earthboundHudTexture;
        private Texture2D _pixelTexture;
        private float _tiempo = 0f;

        private readonly List<RollingCounter> _hpCounters =
            new List<RollingCounter>();

        private readonly List<RollingCounter> _ppCounters =
            new List<RollingCounter>();

        // Velocidad del contador estilo EarthBound.
        private const float SecondsPerNumber = 0.075f;

        private class RollingCounter
        {
            public int Visible;
            public int Target;
            public float Progress;
            public bool Initialized;

            public void Update(float elapsed)
            {
                if (Visible == Target)
                {
                    Progress = 0f;
                    return;
                }

                Progress += elapsed / SecondsPerNumber;

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

            foreach (var counter in _hpCounters)
                counter.Update(elapsed);

            foreach (var counter in _ppCounters)
                counter.Update(elapsed);
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

            if (enemies != null)
            {
                const float enemyScale = 1.5f;

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

                    float spacing = Math.Min(
                        125f,
                        (screenSize.X - 130f) /
                        Math.Max(1, aliveCount - 1));

                    float offsetX =
                        (aliveIndex - (aliveCount - 1) / 2f)
                        * spacing;

                    float x = screenSize.X / 2f + offsetX;
                    float y = screenSize.Y / 2f;

                    aliveIndex++;

                    // Parpadeo del objetivo seleccionado.
                    bool selected =
                        state.IsSelectingTarget &&
                        i == state.SelectedEnemyIndex;

                    if (selected &&
                        (int)(_tiempo * 6f) % 2 != 0)
                    {
                        continue;
                    }

                    // Movimiento suave del enemigo.
                    float wave = (float)Math.Sin(
                        _tiempo * 2.5f + i * 0.7f);

                    float scaleX =
                        enemyScale * (1f - wave * 0.015f);

                    float scaleY =
                        enemyScale * (1f + wave * 0.03f);

                    Vector2 origin = new Vector2(
                        enemy.Texture.Width / 2f,
                        enemy.Texture.Height / 2f);

                    spriteBatch.Draw(
                        enemy.Texture,
                        new Vector2(x, y),
                        null,
                        Color.White,
                        0f,
                        origin,
                        new Vector2(scaleX, scaleY),
                        SpriteEffects.None,
                        0f);
                }
            }

            // ========================================
            // 2. MENÚ PRINCIPAL
            // ========================================

            if (state.IsActionMenuOpen || state.IsSpellMenuOpen)
            {
                Rectangle menuBox = new Rectangle(
                    25,
                    state.IsSpellMenuOpen ? 195 : 25,
                    350,
                    150);

                DrawEarthboundBox(
                    spriteBatch,
                    _earthboundHudTexture,
                    menuBox);

                if (_font != null &&
                    state.PartyMembers.Count > 0)
                {
                    int heroIndex = Math.Clamp(
                        state.CurrentPartyMemberIndex,
                        0,
                        state.PartyMembers.Count - 1);

                    CharacterStats hero =
                        state.PartyMembers[heroIndex];

                    for (int i = 0;
                         i < state.CurrentMenuOptions.Count;
                         i++)
                    {
                        int col = i % 2;
                        int row = i / 2;

                        float x =
                            menuBox.X + 18 + col * 155;

                        float y =
                            menuBox.Y + 28 + row * 30;

                        bool selected =
                            !state.IsSpellMenuOpen &&
                            i == state.SelectedOptionIndex;

                        Color color = selected
                            ? Color.Cyan
                            : Color.White;

                        if (selected)
                        {
                            DrawFittedText(
                                spriteBatch,
                                ">",
                                new Vector2(x, y),
                                14f,
                                24f,
                                Color.Cyan);
                        }

                        DrawFittedText(
                            spriteBatch,
                            state.CurrentMenuOptions[i],
                            new Vector2(x + 16f, y),
                            132f,
                            24f,
                            color);
                    }

                    // ========================================
                    // 3. SUBMENÚ DE HECHIZOS
                    // ========================================

                    if (state.IsSpellMenuOpen)
                    {
                        Rectangle spellBox =
                            new Rectangle(25, 25, 350, 150);

                        DrawEarthboundBox(
                            spriteBatch,
                            _earthboundHudTexture,
                            spellBox);

                        DrawFittedText(
                            spriteBatch,
                            "Z-SPELL",
                            new Vector2(
                                spellBox.X + 22,
                                spellBox.Y + 8),
                            190f,
                            22f,
                            Color.White);

                        DrawFittedText(
                            spriteBatch,
                            $"PP: {hero.PP}/{hero.MaxPP}",
                            new Vector2(
                                spellBox.X + 235,
                                spellBox.Y + 8),
                            90f,
                            22f,
                            Color.White);

                        for (int i = 0;
                             i < hero.Spells.Count;
                             i++)
                        {
                            string spell = hero.Spells[i];
                            int cost = GetSpellCost(spell);

                            float x = spellBox.X + 22;
                            float y =
                                spellBox.Y + 32 + i * 29;

                            bool selected =
                                i == state.SelectedSpellIndex;

                            Color color = selected
                                ? Color.Cyan
                                : Color.White;

                            if (selected)
                            {
                                DrawFittedText(
                                    spriteBatch,
                                    ">",
                                    new Vector2(x, y),
                                    14f,
                                    22f,
                                    Color.Cyan);
                            }

                            DrawFittedText(
                                spriteBatch,
                                spell,
                                new Vector2(x + 18, y),
                                215f,
                                22f,
                                color);

                            DrawFittedText(
                                spriteBatch,
                                $"{cost} PP",
                                new Vector2(
                                    spellBox.X + 265,
                                    y),
                                64f,
                                22f,
                                hero.PP >= cost
                                    ? color
                                    : Color.Gray);
                        }

                        DrawFittedText(
                            spriteBatch,
                            "X: Volver",
                            new Vector2(
                                spellBox.X + 230,
                                spellBox.Y + 122),
                            98f,
                            20f,
                            Color.LightGray);
                    }
                }
            }
            else
            {
                // ========================================
                // 4. MENSAJES DE COMBATE
                // ========================================

                Rectangle fullTextBox = new Rectangle(
                    25,
                    25,
                    (int)screenSize.X - 50,
                    80);

                DrawEarthboundBox(
                    spriteBatch,
                    _earthboundHudTexture,
                    fullTextBox);

                if (_font != null)
                {
                    DrawWrappedText(
                        spriteBatch,
                        state.TopMessage,
                        new Vector2(
                            fullTextBox.X + 25,
                            fullTextBox.Y + 14),
                        fullTextBox.Width - 50f,
                        42f,
                        Color.White);

                    // Selección de objetivos.
                    if (state.IsSelectingTarget &&
                        enemies != null &&
                        enemies.Count > 0)
                    {
                        float x = fullTextBox.X + 25;
                        float y = fullTextBox.Y + 44;

                        const float gap = 40f;

                        for (int i = 0;
                             i < enemies.Count;
                             i++)
                        {
                            bool selected =
                                i == state.SelectedEnemyIndex;

                            string text = selected
                                ? "> " + enemies[i].Name
                                : "  " + enemies[i].Name;

                            float remaining =
                                fullTextBox.Right - 25f - x;

                            if (remaining < 24f)
                                break;

                            DrawFittedText(
                                spriteBatch,
                                text,
                                new Vector2(x, y),
                                remaining,
                                23f,
                                selected
                                    ? Color.Cyan
                                    : Color.White);

                            x += _font.MeasureString(text).X + gap;
                        }
                    }
                }
            }

            // ========================================
            // 5. CUADROS DE YOKO Y KYOKO
            // ========================================

            EnsureCounters(state);

            float totalWidth =
                state.PartyMembers.Count * 130f;

            float startX =
                (screenSize.X - totalWidth) / 2f;

            float statusPosY =
                screenSize.Y - 145f;

            for (int i = 0;
                 i < state.PartyMembers.Count;
                 i++)
            {
                CharacterStats p =
                    state.PartyMembers[i];

                Vector2 statusPos = new Vector2(
                    startX + i * 130f,
                    statusPosY);

                // Cuadro original de EarthBound.
                if (_earthboundHudTexture != null)
                {
                    Rectangle sourceRect =
                        new Rectangle(917, 143, 56, 64);

                    spriteBatch.Draw(
                        _earthboundHudTexture,
                        statusPos,
                        sourceRect,
                        Color.White,
                        0f,
                        Vector2.Zero,
                        2f,
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
                    p.HP <= 0);

                DrawCounter(
                    spriteBatch,
                    _ppCounters[i],
                    statusPos,
                    41,
                    false);
            }
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
            Color color)
        {
            if (_font == null ||
                string.IsNullOrEmpty(message))
            {
                return;
            }

            string[] words = message
                .Replace("\r", "")
                .Replace("\n", " ")
                .Split(
                    new[] { ' ' },
                    StringSplitOptions.RemoveEmptyEntries);

            string line = "";
            int lineNumber = 0;

            float lineHeight = height / 2f;

            foreach (string word in words)
            {
                string candidate = line.Length == 0
                    ? word
                    : line + " " + word;

                if (line.Length > 0 &&
                    _font.MeasureString(candidate).X > width)
                {
                    DrawFittedText(
                        batch,
                        line,
                        position + new Vector2(
                            0,
                            lineNumber * lineHeight),
                        width,
                        lineHeight,
                        color);

                    lineNumber++;
                    line = word;

                    if (lineNumber >= 2)
                        return;
                }
                else
                {
                    line = candidate;
                }
            }

            if (lineNumber < 2)
            {
                DrawFittedText(
                    batch,
                    line,
                    position + new Vector2(
                        0,
                        lineNumber * lineHeight),
                    width,
                    lineHeight,
                    color);
            }
        }

        // ========================================
        // 10. SPRITES DE LOS DÍGITOS
        // ========================================

        private static Rectangle DigitSource(
            int digit,
            int transitionFrame)
        {
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
            bool defeated)
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
                        (int)panelPosition.X + 25 * 2,
                        (int)panelPosition.Y + rowOffset * 2,
                        23 * 2,
                        12 * 2),
                    new Rectangle(
                        865,
                        161,
                        23,
                        12),
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

                spriteBatch.Draw(
                    _earthboundHudTexture,
                    new Rectangle(
                        (int)panelPosition.X +
                            (25 + place * 8) * 2,
                        (int)panelPosition.Y +
                            rowOffset * 2,
                        7 * 2,
                        12 * 2),
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
