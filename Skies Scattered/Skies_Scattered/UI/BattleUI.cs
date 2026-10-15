using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGameLibrary.Graphics;

namespace Skies_Scattered.UI
{
    public class BattleUI
    {
        private SpriteFont _font;
        private Texture2D _uiBordersTexture;
        private Texture2D _earthboundHudTexture;

        public BattleUI(SpriteFont font, Texture2D uiBordersTexture, Texture2D earthboundHudTexture)
        {
            _font = font;
            _uiBordersTexture = uiBordersTexture;
            _earthboundHudTexture = earthboundHudTexture;
        }

        public void Draw(SpriteBatch spriteBatch, BattleUIState state, Texture2D enemyTexture, Vector2 screenSize)
        {
            // 1. Dibujar el Enemigo centrado
            if (enemyTexture != null)
            {
                Vector2 enemyPos = new Vector2(
                    (screenSize.X - 96) / 2f,
                    (screenSize.Y - 96) / 2f - 20f
                );
                spriteBatch.Draw(enemyTexture, enemyPos, Color.White);
            }

            // 2. DISPOSICIÓN ESTILO EARTHBOUND

            if (state.IsActionMenuOpen)
            {
                // A) MENÚ DE ACCIONES (Arriba a la Izquierda)
                Rectangle menuBox = new Rectangle(20, 15, 270, 95);
                DrawNinePatchBox(spriteBatch, _uiBordersTexture, menuBox);

                if (_font != null)
                {
                    string activeName = state.PartyMembers.Count > 0 ? state.PartyMembers[0].Name : "Yoko";
                    
                    // Nombre del personaje en el menú
                    spriteBatch.DrawString(_font, activeName, new Vector2(menuBox.X + 18, menuBox.Y + 12), Color.White, 0f, Vector2.Zero, 1.0f, SpriteEffects.None, 0f);

                    // Opciones en 2 columnas estilo EarthBound
                    for (int i = 0; i < state.CurrentMenuOptions.Count; i++)
                    {
                        int col = i % 2;
                        int row = i / 2;
                        float optionX = menuBox.X + 18 + (col * 120);
                        float optionY = menuBox.Y + 36 + (row * 24);

                        Color optionColor = (i == state.SelectedOptionIndex) ? Color.Cyan : Color.White;
                        spriteBatch.DrawString(_font, state.CurrentMenuOptions[i], new Vector2(optionX, optionY), optionColor, 0f, Vector2.Zero, 1.0f, SpriteEffects.None, 0f);
                    }
                }

                // B) CUADRO DE TEXTO (A la derecha del menú de acciones)
                Rectangle textBox = new Rectangle(300, 15, (int)screenSize.X - 320, 95);
                DrawNinePatchBox(spriteBatch, _uiBordersTexture, textBox);

                if (_font != null)
                {
                    spriteBatch.DrawString(_font, state.TopMessage, new Vector2(textBox.X + 20, textBox.Y + 20), Color.White, 0f, Vector2.Zero, 1.0f, SpriteEffects.None, 0f);
                }
            }
            else
            {
                // C) CUADRO DE TEXTO INICIAL (Centrado arriba cuando NO se presiona Z)
                Rectangle fullTextBox = new Rectangle(20, 15, (int)screenSize.X - 40, 70);
                DrawNinePatchBox(spriteBatch, _uiBordersTexture, fullTextBox);

                if (_font != null)
                {
                    spriteBatch.DrawString(_font, state.TopMessage, new Vector2(fullTextBox.X + 25, fullTextBox.Y + 22), Color.White, 0f, Vector2.Zero, 1.0f, SpriteEffects.None, 0f);
                }
            }

            // 3. CUADROS DE ESTADO DE LOS PERSONAJES (Abajo al centro)
            float totalWidth = state.PartyMembers.Count * 130f;
            float startX = (screenSize.X - totalWidth) / 2f;
            float statusPosY = screenSize.Y - 145f; // Cambiado a statusPosY para evitar conflicto de nombres

            for (int i = 0; i < state.PartyMembers.Count; i++)
            {
                CharacterStats p = state.PartyMembers[i];
                Vector2 statusPos = new Vector2(startX + (i * 130f), statusPosY);
                
                if (_earthboundHudTexture != null)
                {
                    Rectangle sourceRect = new Rectangle(917, 143, 56, 64);
                    spriteBatch.Draw(_earthboundHudTexture, statusPos, sourceRect, Color.White, 0f, Vector2.Zero, 2.0f, SpriteEffects.None, 0f);
                }

                if (_font != null)
                {
                    float textOffsetX = statusPos.X + 16f;
                    float textOffsetY = statusPos.Y + 8f;
                    spriteBatch.DrawString(_font, p.Name, new Vector2(textOffsetX, textOffsetY), Color.White, 0f, Vector2.Zero, 0.95f, SpriteEffects.None, 0f);
                }
            }
        }

        private void DrawNinePatchBox(SpriteBatch spriteBatch, Texture2D texture, Rectangle rect)
        {
            if (texture != null)
            {
                int atlasX = 16;
                int atlasY = 16;
                int size = 96;
                int cornerSize = 16;

                Rectangle topLeftSrc = new Rectangle(atlasX, atlasY, cornerSize, cornerSize);
                Rectangle topRightSrc = new Rectangle(atlasX + size - cornerSize, atlasY, cornerSize, cornerSize);
                Rectangle bottomLeftSrc = new Rectangle(atlasX, atlasY + size - cornerSize, cornerSize, cornerSize);
                Rectangle bottomRightSrc = new Rectangle(atlasX + size - cornerSize, atlasY + size - cornerSize, cornerSize, cornerSize);

                Rectangle topSrc = new Rectangle(atlasX + cornerSize, atlasY, size - (cornerSize * 2), cornerSize);
                Rectangle bottomSrc = new Rectangle(atlasX + cornerSize, atlasY + size - cornerSize, size - (cornerSize * 2), cornerSize);
                
                Rectangle leftSource = new Rectangle(atlasX, atlasY + cornerSize, cornerSize, size - (cornerSize * 2));
                Rectangle rightSource = new Rectangle(atlasX + size - cornerSize, atlasY + cornerSize, cornerSize, size - (cornerSize * 2));
                Rectangle centerSrc = new Rectangle(atlasX + cornerSize, atlasY + cornerSize, size - (cornerSize * 2), size - (cornerSize * 2));

                Rectangle centerDest = new Rectangle(
                    rect.X + cornerSize, 
                    rect.Y + cornerSize, 
                    rect.Width - (cornerSize * 2), 
                    rect.Height - (cornerSize * 2)
                );
                spriteBatch.Draw(texture, centerDest, centerSrc, Color.White);

                spriteBatch.Draw(texture, new Rectangle(rect.X, rect.Y, cornerSize, cornerSize), topLeftSrc, Color.White);
                spriteBatch.Draw(texture, new Rectangle(rect.X + rect.Width - cornerSize, rect.Y, cornerSize, cornerSize), topRightSrc, Color.White);
                spriteBatch.Draw(texture, new Rectangle(rect.X, rect.Y + rect.Height - cornerSize, cornerSize, cornerSize), bottomLeftSrc, Color.White);
                spriteBatch.Draw(texture, new Rectangle(rect.X + rect.Width - cornerSize, rect.Y + rect.Height - cornerSize, cornerSize, cornerSize), bottomRightSrc, Color.White);

                spriteBatch.Draw(texture, new Rectangle(rect.X + cornerSize, rect.Y, rect.Width - (cornerSize * 2), cornerSize), topSrc, Color.White);
                spriteBatch.Draw(texture, new Rectangle(rect.X + cornerSize, rect.Y + rect.Height - cornerSize, rect.Width - (cornerSize * 2), cornerSize), bottomSrc, Color.White);
                spriteBatch.Draw(texture, new Rectangle(rect.X, rect.Y + cornerSize, cornerSize, rect.Height - (cornerSize * 2)), leftSource, Color.White);
                spriteBatch.Draw(texture, new Rectangle(rect.X + rect.Width - cornerSize, rect.Y + cornerSize, cornerSize, rect.Height - (cornerSize * 2)), rightSource, Color.White);
            }
        }
    }
}