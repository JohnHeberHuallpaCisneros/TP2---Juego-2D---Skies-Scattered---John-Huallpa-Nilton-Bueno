using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;

namespace Skies_Scattered.Mechanics;
public sealed class PartyTrail
{
    private readonly Queue<Vector2> _points = new();
    private Vector2 _leader;
    private float _length;
    public Vector2 Position { get; private set; }

    public const float Spacing = 64f;
    public void Reset(Vector2 follower, Vector2 leader)
    {
        _points.Clear();
        Position = follower;
        _leader = leader;
        _length = Vector2.Distance(follower, leader);
        if (_length > .001f)
            _points.Enqueue(leader);
    }

    private static bool SegmentClear(Vector2 from, Vector2 to, Func<Vector2, bool> clear)
    {
        int count = Math.Max(1, (int)Math.Ceiling(Vector2.Distance(from, to) / 4));
        for (int i = 1; i <= count; i++)
        {
            Vector2 point = Vector2.Lerp(from, to, i / (float)count);
            if (!clear(point))
                return false;
        }

        return true;
    }

    public void RecordStep(Vector2 corner, Vector2 leader, Func<Vector2, bool> clear)
    {
        if (!SegmentClear(_leader, leader, clear))
            Record(corner);
        Record(leader);
    }

    public void Record(Vector2 leader)
    {
        float distance = Vector2.Distance(_leader, leader);
        if (distance < .001f)
            return;
        _points.Enqueue(leader);
        _length += distance;
        _leader = leader;
    }

    public Vector2 Advance(float seconds, float speed, Func<Vector2, bool> clear)
    {
        float excess = Math.Max(0, _length - Spacing);
        float catchUp = 1f + MathHelper.Clamp(excess / 96f, 0f, .75f);
        float budget = Math.Min(excess, Math.Max(0, seconds) * speed * catchUp);
        while (budget > .001f && _points.Count > 0)
        {
            Vector2 target = _points.Peek();
            float distance = Vector2.Distance(Position, target);
            if (distance < .001f)
            {
                _points.Dequeue();
                continue;
            }

            float amount = Math.Min(budget, distance);
            Vector2 next = Vector2.Lerp(Position, target, amount / distance);
            if (!SegmentClear(Position, next, clear))
                break;
            Position = next;
            _length = Math.Max(0, _length - amount);
            budget -= amount;
            if (amount >= distance - .001f)
                _points.Dequeue();
        }

        return Position;
    }
}
