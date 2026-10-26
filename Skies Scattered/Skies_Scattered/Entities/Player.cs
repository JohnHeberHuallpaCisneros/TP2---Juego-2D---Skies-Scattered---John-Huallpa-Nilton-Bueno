
using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using MonoGameLibrary;
using MonoGameLibrary.Graphics;

namespace Skies_Scattered.Entities;

public class Player
{
    public Vector2 Position { get; set; }

    public float Speed { get; set; } = 3f;

    public AnimatedSprite CurrentAnimation { get; set; }

    // Animaciones principales
    public AnimatedSprite AnimFrente { get; set; }
    public AnimatedSprite AnimAtras { get; set; }
    public AnimatedSprite AnimIzquierda { get; set; }
    public AnimatedSprite AnimDerecha { get; set; }

    // Animaciones diagonales
    public AnimatedSprite AnimArribaDerecha { get; set; }
    public AnimatedSprite AnimArribaIzquierda { get; set; }
    public AnimatedSprite AnimAbajoDerecha { get; set; }
    public AnimatedSprite AnimAbajoIzquierda { get; set; }

    private readonly Keys _keyUp;
    private readonly Keys _keyDown;
    private readonly Keys _keyLeft;
    private readonly Keys _keyRight;

    private AnimatedSprite _ultimaAnimacion;
    private bool _estabaMoviendose;

    public Player(
        Keys up,
        Keys down,
        Keys left,
        Keys right,
        Vector2 startPosition)
    {
        _keyUp = up;
        _keyDown = down;
        _keyLeft = left;
        _keyRight = right;

        Position = startPosition;
    }

    // =========================================
    // MOVIMIENTO DEL JUGADOR
    // =========================================

    public void Update(GameTime gameTime)
    {
        Vector2 direccion = Vector2.Zero;

        if (Input.IsKeyDown(_keyUp))
            direccion.Y -= 1;

        if (Input.IsKeyDown(_keyDown))
            direccion.Y += 1;

        if (Input.IsKeyDown(_keyLeft))
            direccion.X -= 1;

        if (Input.IsKeyDown(_keyRight))
            direccion.X += 1;

        if (direccion == Vector2.Zero)
        {
            DetenerAnimacion();
            return;
        }

        direccion.Normalize();

        SeleccionarAnimacion(direccion);

        Position += direccion * Speed;

        ActualizarAnimacion(gameTime);
    }

    // =========================================
    // SEGUIMIENTO DE KYOKO
    // =========================================


public void Seguir(Vector2 objetivo, GameTime gameTime)
{
    Vector2 direccion = objetivo - Position;
    float distancia = direccion.Length();

    // Kyoko se detiene al acercarse a Yoko.
    if (distancia <= 48f)
    {
        DetenerAnimacion();
        return;
    }

    direccion.Normalize();

    SeleccionarAnimacion(direccion);

    // Movimiento basado en segundos.
    float velocidad = Speed * 60f * (float)gameTime.ElapsedGameTime.TotalSeconds;

    Position += direccion * velocidad;

    ActualizarAnimacion(gameTime);
}

    // =========================================
    // SELECCIÓN DE DIRECCIÓN
    // =========================================

    private void SeleccionarAnimacion(Vector2 direccion)
    {
        bool diagonalX = Math.Abs(direccion.X) > 0.1f;
        bool diagonalY = Math.Abs(direccion.Y) > 0.1f;

        if (diagonalX && diagonalY)
        {
            if (direccion.Y < 0)
            {
                CurrentAnimation = direccion.X > 0
                    ? AnimArribaDerecha
                    : AnimArribaIzquierda;
            }
            else
            {
                CurrentAnimation = direccion.X > 0
                    ? AnimAbajoDerecha
                    : AnimAbajoIzquierda;
            }
        }
        else if (diagonalX)
        {
            CurrentAnimation = direccion.X > 0
                ? AnimDerecha
                : AnimIzquierda;
        }
        else
        {
            CurrentAnimation = direccion.Y < 0
                ? AnimAtras
                : AnimFrente;
        }
    }

    // =========================================
    // ACTUALIZACIÓN DE LA ANIMACIÓN
    // =========================================

    private void ActualizarAnimacion(GameTime gameTime)
    {
        if (CurrentAnimation == null)
            return;

        // Reiniciar solamente cuando comienza una animación diferente
        // o cuando el personaje vuelve a caminar.
        if (CurrentAnimation != _ultimaAnimacion ||
            !_estabaMoviendose)
        {
            CurrentAnimation.Reset();
            _ultimaAnimacion = CurrentAnimation;
        }

        CurrentAnimation.Update(gameTime);

        _estabaMoviendose = true;
    }

    private void DetenerAnimacion()
    {
        _estabaMoviendose = false;
    }
}
