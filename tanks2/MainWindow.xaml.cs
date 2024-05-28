using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace tanks2
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        List<Ground>[] ground;
        List<Tank> tanks;
        Canvas mainCanvas = new();
        Canvas fullCanvas = new(); //should only store other cancves
        Canvas textCanvas = new();
        Canvas tankCanvas = new();

        DispatcherTimer keyCheck = new DispatcherTimer() { Interval = new TimeSpan(0, 0, 0, 0, 1000 / 50) };
        DispatcherTimer tankUpdate = new DispatcherTimer() { Interval = new TimeSpan(0, 0, 0, 0, 1000 / 120) };

        List<Stopwatch> watches;
        const bool useWatchesToDebugTime = true;
        const bool updateText = true;
        const bool drawNormals = false;


        public MainWindow()
        {
            InitializeComponent();
            Content = fullCanvas;
            fullCanvas.Children.Add(mainCanvas);
            fullCanvas.Children.Add(textCanvas);
            fullCanvas.Children.Add(tankCanvas);

            watches = new List<Stopwatch>();
            for (int i = 0; i < 4; i++)
            {
                watches.Add(new Stopwatch());
            }

            for (int i = 0; i < 11; i++)
            {
                var t1 = new TextBlock() { RenderTransform = new TranslateTransform(0, i * 10) };
                textCanvas.Children.Add(t1);
            }

            this.MouseLeftButtonUp += Click;
            this.KeyUp += MainWindow_KeyUp;
            this.KeyDown += MainWindow_KeyDown;

            ground = new List<Ground>[(int)Application.Current.MainWindow.Width];
            tanks = new List<Tank>() { new Tank((int)Application.Current.MainWindow.Width / 2) };

            keyCheck.Tick += CheckKeys;
            keyCheck.Start();

            tankUpdate.Tick += TankUpdate_Tick;
            tankUpdate.Start();


            GenerateGround();
            Draw();

            new GeometryCollection();

            new GeometryDrawing();

        }

        private void TankUpdate_Tick(object? sender, EventArgs e)
        {
            UpdateTanks();
            Draw();
            UpdateText();
        }

        private void CheckKeys(object? sender, EventArgs e)
        {
            if (Keyboard.IsKeyDown(Key.D))
            {
                tanks[0].velocity += new Vector2(1, 0);
            }
            if (Keyboard.IsKeyDown(Key.A))
            {
                tanks[0].velocity += new Vector2(-1, 0);
            }
        }

        private void MainWindow_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.A)
            {
                tanks[0].rotation += 1;
            }
            //if (e.Key == Key.D)
            //{
            //    tanks[0].rotation -= 1;
            //}
            //if (e.Key == Key.R)
            //{
            //    for (int i = 0; i < 10; i ++)
            //    UpdateTanks();
            //    Draw();
            //    UpdateText();
            //}
        }
        private void MainWindow_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Q)
            {
                //UpdateTanks();
                Fall();
            }
        }
        void Fall()
        {
            if (useWatchesToDebugTime) watches[0].Restart();
            int speed = 5;
            for (int a = 0; a < ground.Length; a++)
            {
                var gc = ground[a];
                for (int i = gc.Count - 1; i > 0; i--)
                {
                    var current = gc[i];
                    int nextInt = i - 1;
                    if (nextInt == gc.Count)
                    {

                    }
                    else
                    {
                        var next = gc[nextInt];
                        int dif = next.top - current.bottom;
                        if (dif > speed)
                        {
                            current.bottom += speed;
                            current.top += speed;
                        }
                        else if (dif > 0)
                        {
                            current.bottom += dif;
                            current.top += dif;
                        }
                        else
                        {
                            next.top = current.top;
                            gc.Remove(current);
                        }
                    }
                }
            }
            if (useWatchesToDebugTime) watches[0].Stop();
            UpdateText();
            Draw(true);
        }

        void UpdateText()
        {
            if (updateText)
            {
                (textCanvas.Children[0] as TextBlock).Text = ground.Length.ToString();
                int len = 0;
                foreach (List<Ground> l in ground) foreach (Ground g in l) len++;
                (textCanvas.Children[1] as TextBlock).Text = len.ToString();
                (textCanvas.Children[2] as TextBlock).Text = mainCanvas.Children.Count.ToString();
                (textCanvas.Children[3] as TextBlock).Text = "ang vel " + tanks[0].angular.ToString();
                (textCanvas.Children[4] as TextBlock).Text = "cur rot " + tanks[0].rotation.ToString();
                (textCanvas.Children[5] as TextBlock).Text = "lin vel " + tanks[0].velocity.ToString();
                (textCanvas.Children[6] as TextBlock).Text = "cur pos " + tanks[0].position.ToString();
                (textCanvas.Children[7] as TextBlock).Text = "fallSec " + watches[0].ElapsedMilliseconds.ToString();
                (textCanvas.Children[8] as TextBlock).Text = "def Sec " + watches[1].ElapsedMilliseconds.ToString();
                (textCanvas.Children[9] as TextBlock).Text = "upd Sec " + watches[2].ElapsedMilliseconds.ToString();
                (textCanvas.Children[10] as TextBlock).Text = "drawSec " + watches[3].ElapsedMilliseconds.ToString();
            }
        }
        void Deform(int rad, int px, int py)
        {
            if (useWatchesToDebugTime) watches[1].Restart();
            int start = px - rad;

            for (int i = 0; i < rad * 2; i++)
            {

                var current = start + i;
                if (current < 0) continue;
                if (current > ground.Length - 1) break;

                var gc = ground[current];

                var xDist = Math.Abs(current - px);

                int y = (int)Math.Sqrt((rad * rad) - (xDist * xDist)); //the y value based on the circle at the desired x value (just the paythagoren thoem radius^2 - aSide^2 or c^2 - a^2 = b^2)

                if (xDist == 0) y = rad;

                var newTop = py + y; // used for a few cases
                var newBottom = py - y;
                {

                    for (int a = 0; a < gc.Count; a++) // hopefuly each of the lists is sorted by lowest first
                    {
                        var l = gc[a];

                        var case1 = Math.Abs(py - l.bottom) < y + 1;
                        var case2 = Math.Abs(py - l.top) < y + 1;

                        var case3 = newBottom > l.top && newTop < l.bottom;

                        if (case1 && case2) // if the entirety of the segment is incased withn the rad circle
                        {
                            gc.RemoveAt(a);
                            continue;
                        }
                        if (case1) // if the bottom of the segment is in the rad
                        {
                            l.bottom = newBottom;//move the bottom up (for overhang)
                        }
                        if (case2) //if the top of the segment is in the rad
                        {
                            {  //if the desired deformation is deforming rather than reforming (if the desired top value is below the current top value)
                                l.top = newTop; //move the top down
                            }
                        }
                        if (case3) // if the segment still in the circle but doesnt have the top or bottom in the circle
                        {// prob the most likey case due to caves 
                            int oldTop = l.top; //store the old top
                            if (oldTop < newBottom) //if its not inverted 
                                gc.Insert(a + 1, new Ground(oldTop, newBottom)); // splits the segment into two while keeping the orginal on the bottom
                            l.top = newTop;

                            //if (l.top > l.bottom) gc.RemoveAt(a);

                            //a++;
                            continue;
                        }

                        if (l.top > l.bottom) gc.RemoveAt(a); // if the segment is inside out (meaning the top is below the bottom)

                        gc.Sort(delegate (Ground x, Ground y)
                        {
                            if (x.bottom > y.bottom) return -1; else return 1; //ensures that the lowest is the the first
                        });
                    } // end of multiple segment deformation
                }// end of single segment deformation
            }// end of deforming all of the segments based on x
            if (useWatchesToDebugTime) watches[1].Stop();
            Draw(true);
            UpdateText();
        }
        private void Click(object sender, MouseButtonEventArgs e)
        {
            int rad = 30;
            Point pos = Mouse.GetPosition(this);

            int py = (int)pos.Y;
            int px = (int)pos.X;

            Deform(rad, px, py);
        }
        public void GenerateGround()
        {
            int maxHeight = 700;
            int minHeight = 500;

            int maxDif = 2;
            int startingHeight = 600;

            Random rand = new Random();

            var floor = (int)Application.Current.MainWindow.Height;
            for (int i = 0; i < ground.Length; i++)
            {
                var y = ((rand.NextDouble() * 2) - 1) * maxDif;

                if (i == 0) y += startingHeight; else y += floor - ground[i - 1][0].top;

                if (y > maxHeight) y = maxHeight;
                if (y < minHeight) y = minHeight;

                ground[i] = new List<Ground>() { new Ground(floor - (int)y, floor) };


            }
        }
        public int GetTop(int x)
        {
            return ground[x].Last().top;
        }
        public int GetTop(int x, int y)
        {
            var top = GetTop(x);
            if (top < y)
            {
                for (int i = ground[x].Count - 1; i >= 0; i--)
                {
                    if (ground[x][i].top > y)
                    {
                        return ground[x][i].top;
                    }
                }
                return -1;
            }
            else return top;
        }
        public void UpdateTanks()
        {
            if (useWatchesToDebugTime) watches[2].Restart();
            float downGrav = 1f;
            for (int i = 0; i < tanks.Count; i++)
            {

                var tank = tanks[i];
                int px = (int)tank.position.X;
                int py = (int)tank.position.Y;
                int sc = (int)tank.scale.X;
                int scy = (int)tank.scale.Y;

                tank.velocity += new Vector2(0, downGrav);
                tank.GenerateVectors();
                //Vector2 p0 = tank.worldVectors[2];
                //Vector2 p1 = tank.worldVectors[3];
                //Vector2 line = p1 - p0;
                //Vector2 norm = Vector2.Normalize(new Vector2(line.Y, -line.X));
                //Vector2 mid = new Vector2((p0.X + p1.X) / 2, (p0.Y + p1.Y) / 2);
                //line = Vector2.Normalize(line);

                (var line, var norm, var mid) = tank.GetAuxVars();

                //List<Vector2> tops = new List<Vector2>();
                int start = (int)(px - sc / 2);
                for (int j = 0; j < sc; j++)
                {
                    int top = GetTop(start + j, py + scy/2);
                    var vect = new Vector2(start + j, top);

                    if (top == -1)
                    {
                        if (j > sc / 2) if (tank.velocity.X > 0.5f) tank.velocity *= new Vector2(-1,1);
                        else if (tank.velocity.X < -0.5f) tank.velocity *= new Vector2(-1, 1);
                        continue;
                    }

                    var pos = mid - vect;
                    var dot = Vector2.Dot(norm, pos);
                    if (dot > 0.01f && Math.Abs(dot) < scy)
                    {
                        var secondDot = Vector2.Dot(line, pos);
                        if (Math.Abs(secondDot) < sc)
                        {
                            tank.angular += -secondDot / (sc /*/ 2*/);
                            tank.velocity += new Vector2(0, -dot/3f);
                        }
                    }
                }

                tank.rotation += tank.angular;
                tank.angular *= 0.5f;

                tank.position += tank.velocity;
                tank.velocity *= 0.5f;
            }
            if (useWatchesToDebugTime) watches[2].Stop();
        }
        public void Draw(bool updateGround = false)
        {
            if (useWatchesToDebugTime) watches[3].Restart();
            if (updateGround)
            {
                mainCanvas.IsEnabled = false;
                int index = 0;
                for (int i = 0; i < ground.Length; i++)
                {
                    var l = ground[i];
                    for (int b = 0; b < l.Count; b++)
                    {
                        var g = l[b];
                        if (mainCanvas.Children.Count <= index)
                        {
                            mainCanvas.Children.Add(Polyline(i, g.top, i, g.bottom));
                        }
                        else
                        {
                            var line = (mainCanvas.Children[index] as Polyline);
                            line.Points = PC(i, g.top, i, g.bottom);
                            //if (g.changed)
                            //    line.Stroke = Brushes.Red;
                            //else line.Stroke = Brushes.Green;
                            //g.changed = false;

                        }
                        index++;

                    }
                }
                mainCanvas.Children.RemoveRange(index - 1, mainCanvas.Children.Count - index);
                mainCanvas.IsEnabled = true;
            }
            for (int i = 0; i < tanks.Count; i++)
            {
                var tank = tanks[i];
                tank.GeneratePoints();
                if (tankCanvas.Children.Count <= i)
                {
                    tankCanvas.Children.Add(new Polygon()
                    {
                        Stroke = Brushes.DarkBlue,
                        Points = new PointCollection(tank.worldPoints),
                    });
                }
                else
                {
                    (tankCanvas.Children[i] as Polygon).Points = new PointCollection(tank.worldPoints);
                }
                if (drawNormals)
                {
                    var line = Point.Subtract(tank.worldPoints[2], tank.worldPoints[3]);
                    var norm = new Point(line.Y / line.Length * 10f, -line.X / line.Length * 10f);
                    var midd = new Point((tank.worldPoints[2].X + tank.worldPoints[3].X) / 2, (tank.worldPoints[2].Y + tank.worldPoints[3].Y) / 2);
                    mainCanvas.Children.Add(new Polyline()
                    {
                        Stroke = Brushes.Red,
                        Points = PC((int)midd.X, (int)midd.Y, (int)(norm.X + midd.X), (int)(norm.Y + midd.Y))
                    });
                    mainCanvas.Children.Add(new Polyline()
                    {
                        Stroke = Brushes.MediumPurple,
                        Points = PC((int)midd.X - 5, (int)midd.Y - 5, (int)(midd.X + 5), (int)(midd.Y + 5))
                    });
                }
            }
            if (useWatchesToDebugTime) watches[3].Stop();
        }
        public static PointCollection PC(int x, int y, int x1, int y1)
        {
            return new PointCollection(new Point[] { new Point(x, y), new Point(x1, y1) });
        }
        public static Polyline Polyline(int x, int y, int x1, int y1)
        {
            var p = new Polyline()
            {
                Stroke = Brushes.Green,
                StrokeThickness = 1f,
                Points = new PointCollection(new Point[] { new Point(x, y), new Point(x1, y1) }),
            };
            return p;
        }
    }
    public class Tank
    {
        public Vector2 position { get; set; } = new Vector2(0f, 0f);
        public float rotation { get; set; } = 0f;
        public Vector2 scale { get; set; } = new Vector2(10, 10);

        public Vector2 velocity { get; set; } = Vector2.Zero;
        public float angular { get; set; } = 0f;
        public float decay { get; set; } = 0.985f;

        public int floorHeight { get; set; } = 10;
        public Vector2[] basePoints { get; set; } = new Vector2[] { new Vector2(-2, -1), new Vector2(2, -1), new Vector2(2, 1), new Vector2(-2, 1) };
        public Point[] worldPoints { get; set; } = Array.Empty<Point>();
        public Vector2[] worldVectors { get; set; } = Array.Empty<Vector2>();

        public Tank(int x)
        {
            position = new Vector2(x, 0);
            GenerateVectors();
            GeneratePoints();
        }
        public void GeneratePoints()
        {
            if (worldPoints.Length != basePoints.Length)
            {
                worldPoints = new Point[basePoints.Length];
            }
                for (int i = 0; i < basePoints.Length; i++)
            {
                worldPoints[i] = new Point(worldVectors[i].X, worldVectors[i].Y);
            }
        }
        public void GenerateVectors()
        {
            var rad = DegToRad(rotation);
            if (worldVectors.Length != basePoints.Length)
            worldVectors = new Vector2[basePoints.Length];
            var m = Matrix3x2.CreateRotation(rad) * Matrix3x2.CreateScale(scale);
            for (int i = 0; i < basePoints.Length; i++)
            {
                var v = Vector2.Transform(basePoints[i], m) + position;
               // worldPoints[i] = new Point(v.X, v.Y);
                worldVectors[i] = v;
            }
        }
        public (Vector2,Vector2,Vector2) GetAuxVars()
        {
            Vector2 p0 = worldVectors[2];
            Vector2 p1 = worldVectors[3];
            Vector2 line = p1 - p0;
            Vector2 norm = Vector2.Normalize(new Vector2(line.Y, -line.X));
            Vector2 mid = new((p0.X + p1.X) / 2, (p0.Y + p1.Y) / 2);
            line = Vector2.Normalize(line);
            return (line, norm, mid);
        }
        public static float DegToRad(float deg)
        {
            return (float)(Math.PI / 180f) * deg;
        }

    }
    //public struct IntVector
    //{
    //    public int X;
    //    public int Y;

    //    public IntVector(int x, int y)
    //    {
    //        X = x; Y = y;
    //    }

    
    //}

    public class Ground
    {
        public int top { get; set; }
        public int bottom { get; set; }
        public bool changed { get; set; } = false;
        public Ground(int top, int bottom)
        {
            this.top = top;
            this.bottom = bottom;
            //changed = true;
        }
    }
}
