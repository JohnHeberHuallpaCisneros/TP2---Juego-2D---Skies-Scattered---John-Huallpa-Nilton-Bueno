using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using MonoGameLibrary.Graphics;

namespace MonoGameLibrary.Mecanicas;

public class Teclado
{
    private readonly Keys _arriba, _abajo, _izquierda, _derecha;

    public Vector2 Posicion;
    public float Velocidad = 5f;
    public bool SeMovio { get; private set; }

    // Animación que se está mostrando ahora
    public AnimatedSprite Actual { get; set; }

    // Animaciones por tecla
    public AnimatedSprite AnimArriba { get; set; }
    public AnimatedSprite AnimAbajo { get; set; }
    public AnimatedSprite AnimIzquierda { get; set; }
    public AnimatedSprite AnimDerecha { get; set; }

    // Diagonales (opcionales: si es null, se ignoran)
    public AnimatedSprite AnimArribaDerecha { get; set; }
    public AnimatedSprite AnimArribaIzquierda { get; set; }
    public AnimatedSprite AnimAbajoDerecha { get; set; }
    public AnimatedSprite AnimAbajoIzquierda { get; set; }

    public Teclado(Keys arriba, Keys abajo, Keys izquierda, Keys derecha)
    {
        _arriba = arriba;
        _abajo = abajo;
        _izquierda = izquierda;
        _derecha = derecha;
    }

    public void Update(GameTime gameTime)
    {
        Actual.Update(gameTime);
        SeMovio = false;

        bool arriba = Input.IsKeyDown(_arriba);
        bool abajo = Input.IsKeyDown(_abajo);
        bool izquierda = Input.IsKeyDown(_izquierda);
        bool derecha = Input.IsKeyDown(_derecha);

        if (arriba)
        {
            SeMovio = true;
            Posicion.Y -= Velocidad;
            Actual = AnimArriba;
        }

        if (abajo)
        {
            SeMovio = true;
            Posicion.Y += Velocidad;
            Actual = AnimAbajo;
        }

        if (izquierda)
        {
            SeMovio = true;
            Posicion.X -= Velocidad;
            Actual = AnimIzquierda;
        }

        if (derecha)
        {
            SeMovio = true;
            Posicion.X += Velocidad;
            Actual = AnimDerecha;
        }

        // Diagonales (solo si el personaje tiene esa animación)
        if (arriba && derecha && AnimArribaDerecha != null)
        {
            SeMovio = true;
            Posicion.X += 1;
            Posicion.Y -= 1;
            Actual = AnimArribaDerecha;
        }

        if (arriba && izquierda && AnimArribaIzquierda != null)
        {
            SeMovio = true;
            Posicion.X -= 1;
            Posicion.Y -= 1;
            Actual = AnimArribaIzquierda;
        }

        if (abajo && derecha && AnimAbajoDerecha != null)
        {
            SeMovio = true;
            Posicion.X += 1;
            Posicion.Y += 1;
            Actual = AnimAbajoDerecha;
        }

        if (abajo && izquierda && AnimAbajoIzquierda != null)
        {
            SeMovio = true;
            Posicion.X -= 1;
            Posicion.Y += 1;
            Actual = AnimAbajoIzquierda;
        }

        if (!SeMovio)
            Actual.Reset();
    }
}