using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using MonoGameLibrary;

namespace Skies_Scattered.Mechanics;

public class ChoiceSystem
{
    // =========================================
    // CONFIGURACIÓN: las 3 posiciones EXACTAS (coordenadas del mundo)
    // Rectangle(x, y, ancho, alto). Cambiá estos números a tu gusto.
    // =========================================

    private readonly Rectangle[] _posiciones =
    {
        new Rectangle(600, 250, 40, 40),   // cuadrado 1
        new Rectangle(750, 350, 40, 40),   // cuadrado 2
        new Rectangle(600, 450, 40, 40)    // cuadrado 3
    };

    // Posición exacta del cuadrado final (aparece tras la 3ª elección)
    private readonly Rectangle _posicionFinal = new Rectangle(750, 250, 40, 40);

    // =========================================
    // ESTADO
    // =========================================

    private readonly Random _rng = new Random();
    private readonly Color[] _colores = new Color[3]; // color random de cada cuadrado

    private int _ronda = 0;           // 0, 1, 2 = cuadrado activo | 3 = ya eligió las tres
    private bool _menuAbierto = false;
    private bool _opcionB = false;    // false = resaltada A, true = resaltada B

    // Qué eligió en cada ronda (false = A, true = B)
    private bool _eligio1B = false;
    private bool _eligio2B = false;
    private bool _eligio3B = false;

    private Color _colorFinal;        // color calculado con las 3 elecciones

    public bool IsChoosing => _menuAbierto;
    public bool IsFinished { get; private set; } = false;

    // Se dispara una sola vez, al tocar el cuadrado que cambió de color.
    public event Action<Color> OnFinalSquareTouched;

    public ChoiceSystem()
    {
        GenerarColores();
    }

    private void GenerarColores()
    {
        for (int i = 0; i < _colores.Length; i++)
            _colores[i] = new Color(_rng.Next(60, 256), _rng.Next(60, 256), _rng.Next(60, 256));
    }

    // =========================================
    // UPDATE
    // (el parámetro "mundo" ya no se usa; queda para no tocar Game1)
    // =========================================

    public void Update(Rectangle jugador, Vector2 mundo)
    {
        if (IsFinished) return;

        // 1. Menú abierto: solo se elige
        if (_menuAbierto)
        {
            ActualizarMenu();
            return;
        }

        // 2. Todavía faltan elecciones: solo cuenta el cuadrado de la ronda actual
        if (_ronda < 3)
        {
            if (_posiciones[_ronda].Intersects(jugador))
            {
                _menuAbierto = true;
                _opcionB = false;
            }
        }
        // 3. Ya eligió las tres: solo queda tocar el cuadrado que cambió de color
        else if (_posicionFinal.Intersects(jugador))
        {
            IsFinished = true;
            OnFinalSquareTouched?.Invoke(_colorFinal);
        }
    }

    private void ActualizarMenu()
    {
        if (Input.IsKeyPressed(Keys.Left) || Input.IsKeyPressed(Keys.A))
            _opcionB = false;

        if (Input.IsKeyPressed(Keys.Right) || Input.IsKeyPressed(Keys.D))
            _opcionB = true;

        if (Input.IsKeyPressed(Keys.Z) || Input.IsKeyPressed(Keys.Enter) || Input.IsKeyPressed(Keys.Space))
        {
            // Guardar la elección según la ronda actual
            if (_ronda == 0) _eligio1B = _opcionB;
            else if (_ronda == 1) _eligio2B = _opcionB;
            else if (_ronda == 2) _eligio3B = _opcionB;

            _ronda++;
            _menuAbierto = false;

            // Al subir _ronda, el cuadrado elegido deja de dibujarse (desaparece)
            if (_ronda == 3)
                CalcularColorFinal();
        }
    }

    // =========================================
    // CAMBIO DE COLOR (solo ocurre acá, una vez)
    // =========================================

    private void CalcularColorFinal()
    {
        // Cada elección controla un canal: A = 70 (bajo), B = 255 (alto)
        int rojo = 70;
        int verde = 70;
        int azul = 70;

        if (_eligio1B) rojo = 255;
        if (_eligio2B) verde = 255;
        if (_eligio3B) azul = 255;

        _colorFinal = new Color(rojo, verde, azul);
    }

    public void Reiniciar()
    {
        _ronda = 0;
        _menuAbierto = false;
        _opcionB = false;
        _eligio1B = false;
        _eligio2B = false;
        _eligio3B = false;
        IsFinished = false;
        GenerarColores();
    }

    // =========================================
    // DIBUJADO
    // =========================================

    // Con cámara, ANTES de los personajes.
    public void DrawSquares(SpriteBatch sb, Texture2D pixel)
    {
        // Nunca hay más de un cuadrado a la vez:
        // rondas 0-2 = el cuadrado activo | ronda 3 = el cuadrado final
        if (_ronda < 3)
            sb.Draw(pixel, _posiciones[_ronda], _colores[_ronda]);
        else
            sb.Draw(pixel, _posicionFinal, _colorFinal);
    }

    // Sin cámara (coordenadas de pantalla), DESPUÉS de todo.
    public void DrawMenu(SpriteBatch sb, Texture2D pixel, SpriteFont font, Vector2 pantalla)
    {
        if (!_menuAbierto || font == null) return;

        // Textos según la ronda
        string pregunta = "";
        string opcionA = "";
        string opcionB = "";

        if (_ronda == 0) { pregunta = "Una voz te habla..."; opcionA = "Escuchar"; opcionB = "Ignorar"; }
        else if (_ronda == 1) { pregunta = "Hay dos caminos"; opcionA = "Luz"; opcionB = "Sombra"; }
        else if (_ronda == 2) { pregunta = "Un objeto brilla"; opcionA = "Dejarlo"; opcionB = "Tomarlo"; }

        Rectangle caja = new Rectangle((int)pantalla.X / 2 - 160, (int)pantalla.Y / 2 - 60, 320, 120);
        Rectangle borde = caja;
        borde.Inflate(3, 3);

        sb.Draw(pixel, borde, Color.White);
        sb.Draw(pixel, caja, Color.Black);

        sb.DrawString(font, pregunta + "  (" + (_ronda + 1) + "/3)",
            new Vector2(caja.X + 16, caja.Y + 14), Color.White,
            0f, Vector2.Zero, 0.85f, SpriteEffects.None, 0f);

        Color colorA = Color.White;
        Color colorB = Color.White;
        string textoA = "  " + opcionA;
        string textoB = "  " + opcionB;

        if (_opcionB) { colorB = Color.Cyan; textoB = "> " + opcionB; }
        else { colorA = Color.Cyan; textoA = "> " + opcionA; }

        sb.DrawString(font, textoA, new Vector2(caja.X + 24, caja.Y + 70), colorA,
            0f, Vector2.Zero, 0.85f, SpriteEffects.None, 0f);
        sb.DrawString(font, textoB, new Vector2(caja.X + 180, caja.Y + 70), colorB,
            0f, Vector2.Zero, 0.85f, SpriteEffects.None, 0f);
    }
}