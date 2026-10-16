using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using MonoGameLibrary;
using MonoGameLibrary.Graphics;

namespace Skies_Scattered.Entities;

public class Player
{
    public Vector2 Position { get; set; }
    public float Speed { get; set; } = 4f;
    public AnimatedSprite CurrentAnimation { get; set; }

    // Animaciones principales en 4 direcciones
    public AnimatedSprite AnimFrente { get; set; }
    public AnimatedSprite AnimAtras { get; set; }
    public AnimatedSprite AnimIzquierda { get; set; }
    public AnimatedSprite AnimDerecha { get; set; }

    // Animaciones diagonales (opcionales)
    public AnimatedSprite AnimArribaDerecha { get; set; }
    public AnimatedSprite AnimArribaIzquierda { get; set; }
    public AnimatedSprite AnimAbajoDerecha { get; set; }
    public AnimatedSprite AnimAbajoIzquierda { get; set; }

    private Keys _keyUp, _keyDown, _keyLeft, _keyRight;

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
        bool arriba = Input.IsKeyDown(_keyUp);
        bool abajo = Input.IsKeyDown(_keyDown);
        bool izquierda = Input.IsKeyDown(_keyLeft);
        bool derecha = Input.IsKeyDown(_keyRight);

        Vector2 direction = Vector2.Zero;
        bool moved = false;

        // Comprobación de diagonales y selección de animación correspondiente
        if (arriba && derecha)
        {
            direction = new Vector2(1, -1);
            if (AnimArribaDerecha != null) CurrentAnimation = AnimArribaDerecha;
            else if (AnimAtras != null) CurrentAnimation = AnimAtras;
            moved = true;
        }
        else if (arriba && izquierda)
        {
            direction = new Vector2(-1, -1);
            if (AnimArribaIzquierda != null) CurrentAnimation = AnimArribaIzquierda;
            else if (AnimAtras != null) CurrentAnimation = AnimAtras;
            moved = true;
        }
        else if (abajo && derecha)
        {
            direction = new Vector2(1, 1);
            if (AnimAbajoDerecha != null) CurrentAnimation = AnimAbajoDerecha;
            else if (AnimFrente != null) CurrentAnimation = AnimFrente;
            moved = true;
        }
        else if (abajo && izquierda)
        {
            direction = new Vector2(-1, 1);
            if (AnimAbajoIzquierda != null) CurrentAnimation = AnimAbajoIzquierda;
            else if (AnimFrente != null) CurrentAnimation = AnimFrente;
            moved = true;
        }
        // Direcciones cardinales simples
        else if (arriba)
        {
            direction = new Vector2(0, -1);
            if (AnimAtras != null) CurrentAnimation = AnimAtras;
            moved = true;
        }
        else if (abajo)
        {
            direction = new Vector2(0, 1);
            if (AnimFrente != null) CurrentAnimation = AnimFrente;
            moved = true;
        }
        else if (izquierda)
        {
            direction = new Vector2(-1, 0);
            if (AnimIzquierda != null) CurrentAnimation = AnimIzquierda;
            moved = true;
        }
        else if (derecha)
        {
            direction = new Vector2(1, 0);
            if (AnimDerecha != null) CurrentAnimation = AnimDerecha;
            moved = true;
        }

        if (moved)
        {
            // Normalizamos el vector para que en diagonal no se mueva más rápido
            if (direction != Vector2.Zero)
            {
                direction.Normalize();
                Position += direction * Speed;
            }

            if (CurrentAnimation != null)
                CurrentAnimation.Update(gameTime);
        }
        else
        {
            if (CurrentAnimation != null)
                CurrentAnimation.Reset();
        }
    }
}