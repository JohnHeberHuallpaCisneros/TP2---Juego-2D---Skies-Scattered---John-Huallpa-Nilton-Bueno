using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Skies_Scattered.UI
{
    public class BattleUI
    {
        private Texture2D _pixel;
        private SpriteFont _font;

        public BattleUI(SpriteFont font)
        {
            _font = font;
            // ¡IMPORTANTE! No pongas ningún "new Texture2D" aquí en el constructor.
        }

        public void Draw(SpriteBatch spriteBatch, BattleUIState state, Texture2D enemyTexture, Vector2 screenSize)
        {
            // Creación diferida del píxel para evitar errores con el GraphicsDevice
            if (_pixel == null)
            {
                _pixel = new Texture2D(spriteBatch.GraphicsDevice, 1, 1);
                _pixel.SetData(new[] { Color.White });
            }

            // 1. Dibujar el Enemigo centrado (Considerando el tamaño de 96x96)
            if (enemyTexture != null)
            {
                Vector2 enemyPos = new Vector2(
                    (screenSize.X - 96) / 2f,
                    (screenSize.Y - 96) / 2f - 40f
                );
                spriteBatch.Draw(enemyTexture, enemyPos, Color.White);
            }

            // 2. Caja de Texto Superior
            Rectangle topBox = new Rectangle(20, 15, (int)screenSize.X - 40, 60);
            DrawBox(spriteBatch, topBox, Color.Black * 0.85f, Color.White);
            if (_font != null)
            {
                spriteBatch.DrawString(_font, state.TopMessage, new Vector2(35, 30), Color.White);
            }

            // 3. Menú de Comandos (Izquierda)
            Rectangle menuBox = new Rectangle(20, (int)(screenSize.Y - 220), 120, 200);
            DrawBox(spriteBatch, menuBox, Color.Black * 0.9f, Color.White);
            
            if (_font != null)
            {
                string activeName = state.PartyMembers.Count > 0 ? state.PartyMembers[0].Name : "Player";
                spriteBatch.DrawString(_font, activeName, new Vector2(menuBox.X + 15, menuBox.Y + 10), Color.Yellow);

                for (int i = 0; i < state.CurrentMenuOptions.Count; i++)
                {
                    Color optionColor = (i == state.SelectedOptionIndex) ? Color.Cyan : Color.White;
                    spriteBatch.DrawString(_font, state.CurrentMenuOptions[i], new Vector2(menuBox.X + 15, menuBox.Y + 35 + (i * 24)), optionColor);
                }
            }

            // 4. Caja Compartida de PP (Abajo al Centro)
            Rectangle ppBox = new Rectangle((int)(screenSize.X / 2f - 50f), (int)(screenSize.Y - 60f), 100, 45);
            DrawBox(spriteBatch, ppBox, Color.DarkSlateGray * 0.9f, Color.White);
            if (_font != null)
            {
                spriteBatch.DrawString(_font, $"PP: {state.SharedPP}", new Vector2(ppBox.X + 12, ppBox.Y + 12), Color.LightGreen);
            }

            // 5. Cuadros de Estado de los Personajes (Derecha)
            float partyX = screenSize.X - 150f;
            for (int i = 0; i < state.PartyMembers.Count; i++)
            {
                CharacterStats p = state.PartyMembers[i];
                Rectangle statusBox = new Rectangle((int)partyX, (int)(screenSize.Y - 170f + (i * 75f)), 135, 68);
                DrawBox(spriteBatch, statusBox, Color.DarkBlue * 0.85f, Color.White);

                if (_font != null)
                {
                    spriteBatch.DrawString(_font, p.Name, new Vector2(statusBox.X + 10, statusBox.Y + 5), Color.White);
                    spriteBatch.DrawString(_font, $"AP: {p.AP}", new Vector2(statusBox.X + 10, statusBox.Y + 20), Color.Orange);
                    spriteBatch.DrawString(_font, $"HP: {p.HP}", new Vector2(statusBox.X + 10, statusBox.Y + 35), Color.Red);
                    spriteBatch.DrawString(_font, $"CT: {p.CT}", new Vector2(statusBox.X + 10, statusBox.Y + 50), Color.Magenta);
                }
            }
        }

        private void DrawBox(SpriteBatch spriteBatch, Rectangle rect, Color background, Color border)
        {
            int b = 2;
            spriteBatch.Draw(_pixel, rect, background);
            spriteBatch.Draw(_pixel, new Rectangle(rect.X, rect.Y, rect.Width, b), border);
            spriteBatch.Draw(_pixel, new Rectangle(rect.X, rect.Y + rect.Height - b, rect.Width, b), border);
            spriteBatch.Draw(_pixel, new Rectangle(rect.X, rect.Y, b, rect.Height), border);
            spriteBatch.Draw(_pixel, new Rectangle(rect.X + rect.Width - b, rect.Y, b, rect.Height), border);
        }
    }
}