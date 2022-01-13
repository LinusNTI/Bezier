using System;
using System.Collections.Generic;
using System.Drawing;

namespace Bezier
{
    public struct Vector2D {
        public Vector2D(double x, double y) {
            this.x = x;
            this.y = y;
        }

        public static Vector2D zero = new Vector2D(0, 0);

        public double x { get; }
        public double y { get; }

        public static Vector2D operator -(Vector2D a, Vector2D b) => new Vector2D(a.x - b.x, a.y - b.y);
        public static Vector2D operator +(Vector2D a, Vector2D b) => new Vector2D(a.x + b.x, a.y + b.y);
        public static Vector2D operator *(Vector2D a, Vector2D b) => new Vector2D(a.x * b.x, a.y * b.y);

        public static Vector2D operator *(Vector2D a, double b) => new Vector2D(a.x * b, a.y * b);

        public static bool operator ==(Vector2D a, Vector2D b) => a.x == b.x && a.y == b.y;
        public static bool operator !=(Vector2D a, Vector2D b) => a.x != b.x || a.y != b.y;

        public static implicit operator PointF(Vector2D d) => new PointF((float)d.x, (float)d.y);

        public override string ToString() => $"({x}, {y})";
    }

    public struct VectorPair {
        public Vector2D p1, p2;

        public VectorPair(Vector2D p1, Vector2D p2) {
            this.p1 = p1;
            this.p2 = p2;
        }
    }

    public class Curve {
        public Vector2D p1, p2;
        public List<Vector2D> points = new List<Vector2D>();

        private Vector2D Interpolate(Vector2D p1, Vector2D p2, double t) => p1 + (p2 - p1) * t;

        public void AddPoint(Vector2D p) => points.Add(p);

        public Vector2D GetPoint(double t)
        {
            if (points.Count < 1) throw new InvalidPointsException();

            List<Vector2D> p = new List<Vector2D>();
            p.Add(p1);
            for (int i = 0; i < points.Count; i++)
                p.Add(points[(points.Count - 1) - i]);
            p.Add(p2);

            List<VectorPair> lines = new List<VectorPair>();
            for (int i = 1; i < p.Count; i++) {
                var curPoint = p[i];
                var oldPoint = p[i - 1];
                lines.Add(new VectorPair(oldPoint, curPoint));
            }

            return ComputeLines(lines, t);
        }

        private Vector2D ComputeLines(List<VectorPair> lines, double t) {
            if (lines.Count > 2) {
                List<VectorPair> newLines = new List<VectorPair>();
                for (int i = 1; i < lines.Count; i++) {
                    var curLine = lines[i];
                    var oldLine = lines[i - 1];
                    newLines.Add(new VectorPair(Interpolate(oldLine.p1, oldLine.p2, t), Interpolate(curLine.p1, curLine.p2, t)));
                }
                return ComputeLines(newLines, t);
            } else {
                var point1Diff = Interpolate(lines[0].p1, lines[0].p2, t);
                var point2Diff = Interpolate(lines[1].p1, lines[1].p2, t);
                return point1Diff + (point2Diff - point1Diff) * t;
            }
        }
    }


    [Serializable]
    public class InvalidPointsException : Exception
    {
        public InvalidPointsException() { }
        public InvalidPointsException(string message) : base(message) { }
        public InvalidPointsException(string message, Exception inner) : base(message, inner) { }
        protected InvalidPointsException(
          System.Runtime.Serialization.SerializationInfo info,
          System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
    }
}
