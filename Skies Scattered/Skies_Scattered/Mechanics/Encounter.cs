using System;

namespace Skies_Scattered.Mechanics;

public class Encounter
{
    private readonly Random _rng;
    private float _distanciaAcumulada;
    private int _pasosRecorridos;
    private int _tiradasFallidas;
    private float _pixelesPorPaso = 32f;
    private int _pasosSeguros = 15;
    private float _probabilidadInicial = 0.03f;
    private float _incrementoPorPaso = 0.005f;
    private float _probabilidadMaxima = 0.20f;
    
    public float PixelesPorPaso
    {
        get => _pixelesPorPaso;
        set { if (!float.IsFinite(value) || value <= 0) throw new ArgumentOutOfRangeException(nameof(value)); _pixelesPorPaso = value; }
    }
    public int PasosSeguros
    {
        get => _pasosSeguros;
        set { if (value < 0) throw new ArgumentOutOfRangeException(nameof(value)); _pasosSeguros = value; }
    }
    public float ProbabilidadInicial
    {
        get => _probabilidadInicial;
        set { ValidateProbability(value); _probabilidadInicial = value; }
    }
    public float IncrementoPorPaso
    {
        get => _incrementoPorPaso;
        set { ValidateProbability(value); _incrementoPorPaso = value; }
    }
    public float ProbabilidadMaxima
    {
        get => _probabilidadMaxima;
        set { ValidateProbability(value); _probabilidadMaxima = value; }
    }
    public bool Activo { get; private set; } = false;

    public int PasosSegurosRestantes => Math.Max(0, PasosSeguros - _pasosRecorridos);
    // Probabilidad de la próxima tirada; no hay un número fijo de pasos hasta pelear.
    public float ProbabilidadActual => PasosSegurosRestantes > 0 ? 0f :
        Math.Min(ProbabilidadMaxima, ProbabilidadInicial + _tiradasFallidas * IncrementoPorPaso);

    public Encounter(Random random = null) { _rng = random ?? new Random(); }

    public void Update(float distanciaRecorrida, bool encountersAllowed = true)
    {
        if (!encountersAllowed || Activo || !float.IsFinite(distanciaRecorrida) || distanciaRecorrida <= 0f)
            return;

        _distanciaAcumulada += distanciaRecorrida;

        while (_distanciaAcumulada >= PixelesPorPaso)
        {
            _distanciaAcumulada -= PixelesPorPaso;
            if (PasosSegurosRestantes > 0)
            {
                _pasosRecorridos++;
                continue;
            }
            // Una sola tirada por paso real, independientemente de los FPS.
            if (_rng.NextDouble() < ProbabilidadActual)
            {
                Activo = true;
                _distanciaAcumulada = 0f;
                break;
            }
            if (_tiradasFallidas < int.MaxValue) _tiradasFallidas++;
        }
    }

    public void Reiniciar()
    {
        Activo = false;
        _distanciaAcumulada = 0f;
        _pasosRecorridos = 0;
        _tiradasFallidas = 0;
    }

    private static void ValidateProbability(float value)
    {
        if (!float.IsFinite(value) || value < 0 || value > 1) throw new ArgumentOutOfRangeException(nameof(value));
    }
}
