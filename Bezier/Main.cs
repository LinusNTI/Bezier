using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Drawing.Drawing2D;

namespace Bezier
{
    public partial class Main : Form
    {
        private System.Timers.Timer _updateTimer = new System.Timers.Timer(1000/144);
        private Curve _bezier = new Curve();
        private Random r = new Random();

        private List<Pen> lineClrs = new List<Pen>();
        private bool computeLines = true;

        public Main()
        {
            InitializeComponent();
        }

        private new void KeyDown(object sender, PreviewKeyDownEventArgs e) {
            if (e.KeyCode == Keys.Z) // Reset bezier points
                _bezier = new Curve();
            if (e.KeyCode == Keys.X) // Toggle animated lines
                computeLines = !computeLines;
        }

        private new void Click(object sender, MouseEventArgs e) {
            if (e.Button == MouseButtons.Right)
                _bezier.p1 = new Vector2D(e.X, e.Y);
            if (e.Button == MouseButtons.Left)
                _bezier.p2 = new Vector2D(e.X, e.Y);
            if (e.Button == MouseButtons.Middle)
                _bezier.AddPoint(new Vector2D(e.X, e.Y));
        }

        private void Main_Load(object sender, EventArgs e)
        {
            _updateTimer.Elapsed += Update;
            _updateTimer.Start();

            // Generate colors for animation
            for (int i = 0; i < 100; i++) {
                lineClrs.Add(new Pen(Color.FromArgb(r.Next(0,255),r.Next(0,255),r.Next(0,255))));
            }
        }

        private void Update(object sender, System.Timers.ElapsedEventArgs e)
        {
            // Update functions, runs every frame

            // Invalidate form, force re-rendering and force Draw to run
            this.Invalidate();
        }

        private void Draw(object sender, PaintEventArgs e)
        {
            var g = e.Graphics;

            #region Draw point path
            List<Vector2D> points = new List<Vector2D>();
            points.Add(_bezier.p1);
            for (int i = 0; i < _bezier.points.Count; i++)
                points.Add(_bezier.points[(_bezier.points.Count-1) - i]);
            points.Add(_bezier.p2);

            for (int i = 0; i < points.Count; i++) {
                var point = points[i];
                g.DrawEllipse(Pens.Blue, (float)point.x-5, (float)point.y-5, 10, 10);

                if (i == 0) continue;

                var oldPoint = points[i - 1];
                g.DrawLine(Pens.Red, oldPoint, point);
            }
            #endregion

            #region Draw vertical alignment line
            var definedPoint = (_bezier.p1 != Vector2D.zero) ? _bezier.p1 : (_bezier.p2 != Vector2D.zero) ? _bezier.p2 : Vector2D.zero;
            var cursorPos = this.PointToClient(Cursor.Position);

            if (Math.Abs(cursorPos.Y - definedPoint.y) < 15)
                g.DrawLine(Pens.Black, 0, (float)definedPoint.y, this.Width, (float)definedPoint.y);
            #endregion

            #region Draw height line
            if (_bezier.p1 != Vector2D.zero && _bezier.p2 != Vector2D.zero)
                if (cursorPos.X - _bezier.p1.x <= 0 && cursorPos.X - _bezier.p2.x >= 0) {
                    var bezierLength = _bezier.p2.x - _bezier.p1.x;
                    var cursorRel = cursorPos.X - _bezier.p1.x;

                    var t = ((100 / bezierLength) * cursorRel) / 100;

                    var point = _bezier.p1 + (_bezier.p2 - _bezier.p1)*t;

                    g.DrawEllipse(Pens.Black, (float)point.x - 5, (float)point.y - 5, 10, 10);
                    g.DrawEllipse(Pens.Black, (float)cursorPos.X - 5, (float)cursorPos.Y - 5, 10, 10);
                    g.DrawLine(Pens.Black, point, cursorPos);
                    g.DrawString("Height: " + Math.Abs(Math.Floor(point.y - cursorPos.Y)), DefaultFont, Brushes.Black, new PointF(cursorPos.X, (float)(point.y + (cursorPos.Y - point.y)/2f)));
                }
            #endregion

            #region Draw Bezier
            if (_bezier.points.Count < 1) return;

            var path = new GraphicsPath();
            var resolution = 100f;
            for (int i = 1; i < resolution; i++) {
                var t = i / resolution;
                var tOld = (i - 1) / resolution;

                var curPoint = _bezier.GetPoint(t);
                var oldPoint = _bezier.GetPoint(tOld);
                path.AddLine(curPoint, oldPoint);
            }
            g.DrawPath(Pens.Black, path);
            #endregion

            #region Animate
            float animationTime = 2f;
            float time = ((animationTime - (Environment.TickCount / 1000f) % animationTime)) / animationTime;

            var p = _bezier.GetPoint(time);

            List<VectorPair> lines = new List<VectorPair>();
            for (int i = 1; i < points.Count; i++) {
                var curPoint = points[i];
                var oldPoint = points[i - 1];
                lines.Add(new VectorPair(oldPoint, curPoint));
            }
            if (computeLines)
                DrawComputedLines(lines, time, g);
            g.DrawEllipse(Pens.Blue, (float)p.x - 5, (float)p.y - 5, 10, 10);

            g.DrawEllipse(Pens.Black, (float)p.x - 5, 10, 10, 10);
            g.DrawEllipse(Pens.Black, (float)_bezier.p1.x-5, 10, 10, 10);
            g.DrawEllipse(Pens.Black, (float)_bezier.p2.x-5, 10, 10, 10);
            g.DrawLine(Pens.Black, (float)_bezier.p1.x-5, 15, (float)_bezier.p2.x+5, 15);
            #endregion
        }

        #region Animation methods
        // Copy-pasted from the Curve class, only for animation
        private void DrawComputedLines(List<VectorPair> lines, double t, Graphics g, int depth = 0) {
            if (lines.Count > 2) {
                List<VectorPair> newLines = new List<VectorPair>();
                for (int i = 1; i < lines.Count; i++) {
                    var curLine = lines[i];
                    var oldLine = lines[i - 1];
                    var newLine = new VectorPair(Interpolate(oldLine.p1, oldLine.p2, t), Interpolate(curLine.p1, curLine.p2, t));
                    g.DrawLine(lineClrs[depth], newLine.p1, newLine.p2);
                    newLines.Add(newLine);
                }
                DrawComputedLines(newLines, t, g, depth+1);
            } else {
                var point1Diff = Interpolate(lines[0].p1, lines[0].p2, t);
                var point2Diff = Interpolate(lines[1].p1, lines[1].p2, t);
                g.DrawLine(lineClrs[depth], point1Diff, point2Diff);
            }
        }

        private Vector2D Interpolate(Vector2D p1, Vector2D p2, double t) => p1 + (p2 - p1) * t;
        #endregion
    }
}
