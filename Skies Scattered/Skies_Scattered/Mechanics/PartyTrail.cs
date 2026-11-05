using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;

namespace Skies_Scattered.Mechanics;

// Recuerda los segmentos ya recorridos, sin recortar esquinas hacia el líder.
public sealed class PartyTrail
{
    private readonly Queue<Vector2> _points = new();
    private Vector2 _leader;
    private float _length;
    public Vector2 Position { get; private set; }
    public int PendingPoints => _points.Count;
    public const float Spacing = 48f;

    public bool Reset(Vector2 follower, Vector2 leader, Func<Vector2, bool> clear)
    {
        _points.Clear(); Position = follower; _leader = leader; _length = 0;
        if (!clear(follower) || !clear(leader)) return false;
        if (Vector2.Distance(follower, leader) < .01f) return true;
        // Reconstruye un camino al cargar partidas anteriores o cambiar de mapa.
        var start = Point.Zero;
        var queue = new Queue<Point>(); queue.Enqueue(start);
        var previous = new Dictionary<Point, Point> { [start] = start };
        var moves = new[] { new Point(1,0), new Point(-1,0), new Point(0,1), new Point(0,-1) };
        Vector2 World(Point p) => follower + new Vector2(p.X * 4, p.Y * 4);
        Point? goal = null;
        while (queue.Count > 0 && previous.Count < 150000)
        {
            var node = queue.Dequeue(); var pos = World(node);
            if (Vector2.DistanceSquared(pos, leader) <= 16 && SegmentClear(pos, leader, clear)) { goal = node; break; }
            foreach (var step in moves)
            {
                var next = new Point(node.X + step.X, node.Y + step.Y);
                if (previous.ContainsKey(next) || !SegmentClear(pos, World(next), clear)) continue;
                previous.Add(next, node); queue.Enqueue(next);
            }
        }
        if (!goal.HasValue) return false;
        var path = new List<Vector2>(); var current = goal.Value;
        while (current != start) { path.Add(World(current)); current = previous[current]; }
        path.Reverse(); var last = follower;
        foreach (var point in path) { _points.Enqueue(point); _length += Vector2.Distance(last, point); last = point; }
        if (Vector2.DistanceSquared(last, leader) > .001f) { _points.Enqueue(leader); _length += Vector2.Distance(last, leader); }
        return true;
    }

    private static bool SegmentClear(Vector2 from, Vector2 to, Func<Vector2,bool> clear)
    {
        int count = Math.Max(1, (int)Math.Ceiling(Vector2.Distance(from,to) / 4));
        for (int i = 1; i <= count; i++) if (!clear(Vector2.Lerp(from,to,i/(float)count))) return false;
        return true;
    }

    public void RecordStep(Vector2 corner, Vector2 leader, Func<Vector2,bool> clear)
    {
        // Guardar una diagonal real cuando está libre, sin el zigzag de resolver por ejes.
        if (!SegmentClear(_leader, leader, clear)) Record(corner);
        Record(leader);
    }

    public void Record(Vector2 leader)
    {
        float distance = Vector2.Distance(_leader, leader);
        if (distance < .001f) return;
        _points.Enqueue(leader); _length += distance; _leader = leader;
    }

    public Vector2 Advance(float seconds, float speed, Func<Vector2,bool> clear)
    {
        float excess = Math.Max(0, _length - Spacing);
        float catchUp = 1f + MathHelper.Clamp(excess / 96f, 0f, .75f);
        float budget = Math.Min(excess, Math.Max(0, seconds) * speed * catchUp);
        while (budget > .001f && _points.Count > 0)
        {
            Vector2 target = _points.Peek(); float distance = Vector2.Distance(Position, target);
            if (distance < .001f) { _points.Dequeue(); continue; }
            float amount = Math.Min(budget, distance);
            Vector2 next = Vector2.Lerp(Position, target, amount / distance);
            if (!SegmentClear(Position, next, clear))
            {
                // Recupera el recorrido si un guardado o un cambio de geometría lo invalida.
                Reset(Position, _leader, clear);
                break;
            }
            Position = next; _length = Math.Max(0, _length - amount); budget -= amount;
            if (amount >= distance - .001f) _points.Dequeue();
        }
        return Position;
    }
}

