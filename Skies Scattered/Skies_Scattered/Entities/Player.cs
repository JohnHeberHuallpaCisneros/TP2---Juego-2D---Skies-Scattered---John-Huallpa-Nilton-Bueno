using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using MonoGameLibrary;
using MonoGameLibrary.Graphics;

namespace Skies_Scattered.Entities;
public class Player
{
    public Vector2 Position { get; set; }
    public float Speed { get; set; } = 2f;
    public bool AllowArrowKeys { get; set; }

    private float _walkingPixelsPerFrame = 32f;
    private float _walkingDistance;
    private int _walkingFrame;
    public float WalkingPixelsPerFrame
    {
        get => _walkingPixelsPerFrame;
        set
        {
            if (!float.IsFinite(value) || value <= 0f)
                throw new ArgumentOutOfRangeException(nameof(value));
            _walkingPixelsPerFrame = value;
        }
    }

    public AnimatedSprite CurrentAnimation { get; set; }
    public AnimatedSprite AnimFrente { get; set; }
    public AnimatedSprite AnimAtras { get; set; }
    public AnimatedSprite AnimIzquierda { get; set; }
    public AnimatedSprite AnimDerecha { get; set; }
    public AnimatedSprite AnimArribaDerecha { get; set; }
    public AnimatedSprite AnimArribaIzquierda { get; set; }
    public AnimatedSprite AnimAbajoDerecha { get; set; }
    public AnimatedSprite AnimAbajoIzquierda { get; set; }

    private readonly Keys _keyUp;
    private readonly Keys _keyDown;
    private readonly Keys _keyLeft;
    private readonly Keys _keyRight;
    private bool _estabaMoviendose;
    public Player(Keys up, Keys down, Keys left, Keys right, Vector2 startPosition)
    {
        _keyUp = up;
        _keyDown = down;
        _keyLeft = left;
        _keyRight = right;
        Position = startPosition;
    }

    public void Update(GameTime gameTime)
    {
        Vector2 direccion = Vector2.Zero;
        if (Input.IsKeyDown(_keyUp) || AllowArrowKeys && Input.IsKeyDown(Keys.Up))
            direccion.Y -= 1;
        if (Input.IsKeyDown(_keyDown) || AllowArrowKeys && Input.IsKeyDown(Keys.Down))
            direccion.Y += 1;
        if (Input.IsKeyDown(_keyLeft) || AllowArrowKeys && Input.IsKeyDown(Keys.Left))
            direccion.X -= 1;
        if (Input.IsKeyDown(_keyRight) || AllowArrowKeys && Input.IsKeyDown(Keys.Right))
            direccion.X += 1;
        if (direccion == Vector2.Zero)
        {
            DetenerAnimacion();
            return;
        }

        direccion.Normalize();
        SeleccionarAnimacion(direccion);
        float distance = Speed * 60f * (float)gameTime.ElapsedGameTime.TotalSeconds;
        Position += direccion * distance;
        ActualizarAnimacion(distance);
    }

    public void AplicarSeguimiento(Vector2 destino)
    {
        Vector2 delta = destino - Position;
        float distance = delta.Length();
        if (distance <= .001f)
        {
            DetenerAnimacion();
            return;
        }

        SeleccionarAnimacion(delta / distance);
        Position = destino;
        ActualizarAnimacion(distance);
    }

    private void SeleccionarAnimacion(Vector2 direccion)
    {
        bool diagonalX = Math.Abs(direccion.X) > 0.1f;
        bool diagonalY = Math.Abs(direccion.Y) > 0.1f;
        if (diagonalX && diagonalY)
        {
            if (direccion.Y < 0)
            {
                CurrentAnimation = direccion.X > 0 ? AnimArribaDerecha : AnimArribaIzquierda;
            }
            else
            {
                CurrentAnimation = direccion.X > 0 ? AnimAbajoDerecha : AnimAbajoIzquierda;
            }
        }
        else if (diagonalX)
        {
            CurrentAnimation = direccion.X > 0 ? AnimDerecha : AnimIzquierda;
        }
        else
        {
            CurrentAnimation = direccion.Y < 0 ? AnimAtras : AnimFrente;
        }
    }

    private void ActualizarAnimacion(float distance)
    {
        if (CurrentAnimation == null)
            return;
        if (!_estabaMoviendose)
        {
            _walkingDistance = 0f;
            _walkingFrame = 0;
        }

        _walkingDistance += Math.Max(0, distance);
        var frames = CurrentAnimation.Animation.Frames;
        while (_walkingDistance >= WalkingPixelsPerFrame)
        {
            _walkingDistance -= WalkingPixelsPerFrame;
            _walkingFrame = (_walkingFrame + 1) % frames.Count;
        }

        CurrentAnimation.Region = frames[_walkingFrame % frames.Count];
        _estabaMoviendose = true;
    }

    private void DetenerAnimacion()
    {
        if (_estabaMoviendose)
            CurrentAnimation?.Reset();
        _walkingDistance = 0f;
        _walkingFrame = 0;
        _estabaMoviendose = false;
    }
}
