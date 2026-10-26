using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Skies_Scattered.Entities;
using System.Text;
namespace Skies_Scattered.UI
{
    public class BattleUI
    {
        private SpriteFont _font;
        private Texture2D _uiBordersTexture;
        private Texture2D _earthboundHudTexture;
        private Texture2D _pixelTexture;
        private float _tiempo = 0f;

        public void Update(GameTime gameTime)
        {
            _tiempo += (float)gameTime.ElapsedGameTime.TotalSeconds;
        }
        public BattleUI(SpriteFont font, Texture2D uiBordersTexture, Texture2D earthboundHudTexture)
        {
            _font = font;
            _uiBordersTexture = uiBordersTexture;
            _earthboundHudTexture = earthboundHudTexture;
        }

        public void Draw(SpriteBatch spriteBatch, BattleUIState state, List<Enemy> enemies, Vector2 screenSize)
        {
            if (_pixelTexture == null && spriteBatch.GraphicsDevice != null)
            {
                _pixelTexture = new Texture2D(spriteBatch.GraphicsDevice, 1, 1);
                _pixelTexture.SetData(new[] { Color.White });
            }

            // 1. Dibujar a todos los enemigos activos en el combate
            if (enemies != null)
            {
                foreach (var enemy in enemies)
                {
                    if (enemy.Texture != null)
                    {
                        float fase = enemy.Position.X * 0.05f;
                        float onda = (float)System.Math.Sin(_tiempo * 2.5f + fase);

                        float escalaY = 1f + onda * 0.03f;
                        float escalaX = 1f - onda * 0.015f;

                        Vector2 origen = new Vector2(enemy.Texture.Width / 2f, enemy.Texture.Height);
                        Vector2 pos = enemy.Position + origen;

                        spriteBatch.Draw(enemy.Texture, pos, null, Color.White,
                            0f, origen, new Vector2(escalaX, escalaY),
                            SpriteEffects.None, 0f);
                    }
                }
            }

            // 2. DISPOSICIÓN FIEL A EARTHBOUND (Menú de acciones o caja de texto)
            if (state.IsActionMenuOpen)
            {
                Rectangle menuBox = new Rectangle(25, 25, 260, 105);
                DrawEarthboundBox(spriteBatch, _earthboundHudTexture, menuBox);

                if (_font != null)
                {
                    string activeName = state.PartyMembers.Count > 0 ? state.PartyMembers[0].Name : "Yoko";
                    
                    Vector2 namePos = new Vector2(menuBox.X + 22, menuBox.Y - 12);
                    Vector2 nameSize = _font.MeasureString(activeName) * 0.85f;

                    if (_pixelTexture != null)
                    {
                        Rectangle nameBgRect = new Rectangle((int)namePos.X - 4, (int)namePos.Y - 1, (int)nameSize.X + 8, (int)nameSize.Y + 2);
                        spriteBatch.Draw(_pixelTexture, nameBgRect, Color.Black);
                    }

                    spriteBatch.DrawString(_font, activeName, namePos, Color.White, 0f, Vector2.Zero, 0.85f, SpriteEffects.None, 0f);

                    for (int i = 0; i < state.CurrentMenuOptions.Count; i++)
                    {
                        int col = i % 2;
                        int row = i / 2;
                        
                        float optionX = menuBox.X + 18 + (col * 115);
                        float optionY = menuBox.Y + 28 + (row * 30);

                        Color optionColor = (i == state.SelectedOptionIndex) ? Color.Cyan : Color.White;

                        if (i == state.SelectedOptionIndex)
                        {
                            spriteBatch.DrawString(_font, ">", new Vector2(optionX, optionY), Color.Cyan, 0f, Vector2.Zero, 0.85f, SpriteEffects.None, 0f);
                        }

                        spriteBatch.DrawString(_font, state.CurrentMenuOptions[i], new Vector2(optionX + 16, optionY), optionColor, 0f, Vector2.Zero, 0.85f, SpriteEffects.None, 0f);
                    }
                }
            }
            else
            {
                Rectangle fullTextBox = new Rectangle(25, 25, (int)screenSize.X - 50, 80);
                DrawEarthboundBox(spriteBatch, _earthboundHudTexture, fullTextBox);

                if (_font != null)
                {
                    if (state.IsSelectingTarget && enemies != null && enemies.Count > 0)
                    {
                        // Mensaje arriba
                        spriteBatch.DrawString(_font, state.TopMessage,
                            new Vector2(fullTextBox.X + 25, fullTextBox.Y + 14),
                            Color.White, 0f, Vector2.Zero, 0.85f, SpriteEffects.None, 0f);

                        // Nombres uno al lado del otro
                        float x = fullTextBox.X + 25;
                        float y = fullTextBox.Y + 44;
                        const float gap = 40f;

                        for (int i = 0; i < enemies.Count; i++)
                        {
                            bool selected = i == state.SelectedEnemyIndex;
                            string text = selected ? "> " + enemies[i].Name : "  " + enemies[i].Name;

                            spriteBatch.DrawString(_font, text, new Vector2(x, y),
                                selected ? Color.Cyan : Color.White,
                                0f, Vector2.Zero, 0.85f, SpriteEffects.None, 0f);

                            x += _font.MeasureString(text).X * 0.85f + gap;
                        }
                    }
                    else
                    {
                        spriteBatch.DrawString(_font, state.TopMessage,
                            new Vector2(fullTextBox.X + 25, fullTextBox.Y + 28),
                            Color.White, 0f, Vector2.Zero, 0.85f, SpriteEffects.None, 0f);
                    }
                }
            }

            // 3. CUADROS DE ESTADO DE LOS PERSONAJES (Abajo al centro)
            float totalWidth = state.PartyMembers.Count * 130f;
            float startX = (screenSize.X - totalWidth) / 2f;
            float statusPosY = screenSize.Y - 145f;

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
                    float textOffsetX = statusPos.X + 18f;
                    float textOffsetY = statusPos.Y + 12f;
                    spriteBatch.DrawString(_font, p.Name, new Vector2(textOffsetX, textOffsetY), Color.White, 0f, Vector2.Zero, 0.85f, SpriteEffects.None, 0f);
                }
            }
        }

        private void DrawEarthboundBox(SpriteBatch spriteBatch, Texture2D texture, Rectangle rect)
        {
            if (texture != null)
            {
                int atlasX = 976;
                int atlasY = 143;
                int width = 56;
                int height = 64;
                int cornerSize = 12;

                Rectangle topLeftSrc = new Rectangle(atlasX, atlasY, cornerSize, cornerSize);
                Rectangle topRightSrc = new Rectangle(atlasX + width - cornerSize, atlasY, cornerSize, cornerSize);
                Rectangle bottomLeftSrc = new Rectangle(atlasX, atlasY + height - cornerSize, cornerSize, cornerSize);
                Rectangle bottomRightSrc = new Rectangle(atlasX + width - cornerSize, atlasY + height - cornerSize, cornerSize, cornerSize);

                Rectangle topSrc = new Rectangle(atlasX + cornerSize, atlasY, width - (cornerSize * 2), cornerSize);
                Rectangle bottomSrc = new Rectangle(atlasX + cornerSize, atlasY + height - cornerSize, width - (cornerSize * 2), cornerSize);
                Rectangle leftSrc = new Rectangle(atlasX, atlasY + cornerSize, cornerSize, height - (cornerSize * 2));
                Rectangle rightSrc = new Rectangle(atlasX + width - cornerSize, atlasY + cornerSize, cornerSize, height - (cornerSize * 2));
                
                Rectangle centerSrc = new Rectangle(atlasX + cornerSize, atlasY + cornerSize, width - (cornerSize * 2), height - (cornerSize * 2));

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
                spriteBatch.Draw(texture, new Rectangle(rect.X, rect.Y + cornerSize, cornerSize, rect.Height - (cornerSize * 2)), leftSrc, Color.White);
                spriteBatch.Draw(texture, new Rectangle(rect.X + rect.Width - cornerSize, rect.Y + cornerSize, cornerSize, rect.Height - (cornerSize * 2)), rightSrc, Color.White);
            }
        }
    }
    
}
