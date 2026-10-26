
using System.Collections.Generic;
using Microsoft.Xna.Framework;

namespace Skies_Scattered.Entities;

public class Follower
{
    private readonly Player _personaje;
    private readonly Queue<Vector2> _recorrido = new();

    private readonly float _distanciaSeguimiento;
    private readonly float _velocidad;

    private Vector2 _ultimaPosicionRegistrada;

    public Follower(
        Player personaje,
        float distanciaSeguimiento = 48f,
        float velocidad = 3.5f)
    {
        _personaje = personaje;
        _distanciaSeguimiento = distanciaSeguimiento;
        _velocidad = velocidad;

        _ultimaPosicionRegistrada = personaje.Position;
        _recorrido.Enqueue(personaje.Position);
    }

    public void RegistrarPosicion(Vector2 posicion)
    {
        float distancia = Vector2.Distance(
            _ultimaPosicionRegistrada,
            posicion);

        // Guardar puntos del recorrido a intervalos regulares.
        if (distancia < 6f)
            return;

        _recorrido.Enqueue(posicion);
        _ultimaPosicionRegistrada = posicion;

        // Limitar el tamaño de la cola.
        while (_recorrido.Count > 500)
            _recorrido.Dequeue();
    }

    public void Update(GameTime gameTime)
    {
        if (_recorrido.Count == 0)
            return;

        Vector2 destino = _recorrido.Peek();
        Vector2 diferencia = destino - _personaje.Position;
        float distancia = diferencia.Length();

        // Llegó al punto actual.
        if (distancia <= _velocidad)
        {
            _personaje.Position = destino;
            _recorrido.Dequeue();
            return;
        }

        // Elegir animación según la dirección dominante.
        if (System.Math.Abs(diferencia.X) >
            System.Math.Abs(diferencia.Y))
        {
            _personaje.CurrentAnimation =
                diferencia.X < 0
                    ? _personaje.AnimIzquierda
                    : _personaje.AnimDerecha;
        }
        else
        {
            _personaje.CurrentAnimation =
                diferencia.Y < 0
                    ? _personaje.AnimAtras
                    : _personaje.AnimFrente;
        }

        diferencia.Normalize();

        _personaje.Position += diferencia * _velocidad;

        _personaje.CurrentAnimation?.Update(gameTime);
    }
}
