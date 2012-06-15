using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Interactivity;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace Explosions
{
    public class ExplodeBehavior : Behavior<FrameworkElement>
    {

        public static readonly DependencyProperty PowerProperty = DependencyProperty.Register("Power", typeof (double),
                                                                                              typeof (ExplodeBehavior),
                                                                                              new PropertyMetadata(50.0d));

        public static readonly DependencyProperty RadiusProperty = DependencyProperty.Register("Radius", typeof (double),
                                                                                               typeof (ExplodeBehavior),
                                                                                               new PropertyMetadata(
                                                                                                   20.0d));

        public static readonly DependencyProperty DepthProperty = DependencyProperty.Register("Depth", typeof (double),
                                                                                              typeof (ExplodeBehavior),
                                                                                              new PropertyMetadata(
                                                                                                  -20.0d));

        public static readonly DependencyProperty ShrapnelAmountProperty = DependencyProperty.Register(
            "ShrapnelAmount", typeof (double), typeof (ExplodeBehavior), new PropertyMetadata(500.0d));

        public static readonly DependencyProperty EpicenterProperty = DependencyProperty.Register("Epicenter",
                                                                                                  typeof (Point),
                                                                                                  typeof (
                                                                                                      ExplodeBehavior),
                                                                                                  new PropertyMetadata(
                                                                                                      new Point(.5, .5)));

        public static readonly DependencyProperty ReverseDurationProperty =
            DependencyProperty.Register("ReverseDuration", typeof (TimeSpan), typeof (ExplodeBehavior),
                                        new PropertyMetadata(TimeSpan.FromSeconds(1)));

        private readonly List<Shard> shards = new List<Shard>();
        private Point? lastMousePoint;
        private Popup popup;

        public ExplodeBehavior()
        {
        }

        protected override void OnAttached()
        {
            base.OnAttached();

            this.AssociatedObject.MouseMove += this.HandleMouseMove;
        }

        protected override void OnDetaching()
        {
            base.OnDetaching();

            this.AssociatedObject.MouseMove -= this.HandleMouseMove;
        }

        public Point Epicenter
        {
            get { return (Point) this.GetValue(ExplodeBehavior.EpicenterProperty); }
            set { this.SetValue(ExplodeBehavior.EpicenterProperty, value); }
        }


        public double ShrapnelAmount
        {
            get { return (double) this.GetValue(ExplodeBehavior.ShrapnelAmountProperty); }
            set { this.SetValue(ExplodeBehavior.ShrapnelAmountProperty, value); }
        }


        public double Depth
        {
            get { return (double) this.GetValue(ExplodeBehavior.DepthProperty); }
            set { this.SetValue(ExplodeBehavior.DepthProperty, value); }
        }


        public double Radius
        {
            get { return (double) this.GetValue(ExplodeBehavior.RadiusProperty); }
            set { this.SetValue(ExplodeBehavior.RadiusProperty, value); }
        }


        public double Power
        {
            get { return (double) this.GetValue(ExplodeBehavior.PowerProperty); }
            set { this.SetValue(ExplodeBehavior.PowerProperty, value); }
        }

        public TimeSpan ReverseDuration
        {
            get { return (TimeSpan) this.GetValue(ExplodeBehavior.ReverseDurationProperty); }
            set { this.SetValue(ExplodeBehavior.ReverseDurationProperty, value); }
        }

        [DefaultTrigger(typeof (ButtonBase), typeof (System.Windows.Interactivity.EventTrigger), "Click"),
         DefaultTrigger(typeof (UIElement), typeof (System.Windows.Interactivity.EventTrigger), "MouseLeftButtonDown")]
        public ICommand Ignite
        {
            get
            {
                return new DelegateCommand(delegate(object args)
                                               {
                                                   this.StartExplode();
                                               });
            }
        }

        public ICommand Implode
        {
            get
            {
                return new DelegateCommand(delegate(object args)
                                               {
                                                   this.StartImplode();
                                               });
            }
        }


        private void HandleMouseMove(object sender, MouseEventArgs e)
        {
            this.lastMousePoint = e.GetPosition(this.AssociatedObject);
        }

        private void StartExplode()
        {
            if (this.AssociatedObject == null)
                return;

            Vector3D position;
            if (this.ReadLocalValue(ExplodeBehavior.EpicenterProperty) == DependencyProperty.UnsetValue &&
                this.lastMousePoint.HasValue)
                position = new Vector3D(this.lastMousePoint.Value.X, this.lastMousePoint.Value.Y, this.Depth);
            else
                position = new Vector3D(this.AssociatedObject.ActualWidth*this.Epicenter.X,
                                        this.AssociatedObject.ActualHeight*this.Epicenter.Y, this.Depth);

            this.StartExplode(position);
        }

        public void StartExplode(double x, double y, double z)
        {
            this.StartExplode(new Vector3D(x, y, z));
        }


        private void StartExplode(Vector3D point)
        {

            if (this.shards.Count == 0)
                this.PrepareShards(point);

            this.ApplyForce(point);
        }

        private void StartImplode()
        {
            Storyboard sb = new Storyboard();
            sb.Duration = new Duration(this.ReverseDuration);
            foreach (Shard shard in this.shards)
                shard.Reverse(this.ReverseDuration, sb);

            sb.Completed += delegate
                                {
                                    if (this.popup != null)
                                    {
                                        this.shards.Clear();
                                        this.AssociatedObject.Opacity = 1;
                                        this.popup.IsOpen = false;
                                        this.popup = null;
                                    }
                                    ;
                                };

            sb.Begin();
        }

        private void PrepareShards(Vector3D point)
        {

            FrameworkElement content = this.AssociatedObject;
            if (content == null)
                return;

            double width = content.ActualWidth;
            double height = content.ActualHeight;

            Grid container = new Grid();
            this.popup = new Popup();
            popup.Child = container;


            Point popupOffset = content.TransformToVisual(Application.Current.RootVisual).Transform(new Point(0, 0));

            popup.HorizontalOffset = popupOffset.X;
            popup.VerticalOffset = popupOffset.Y;
            popup.IsHitTestVisible = false;
            container.IsHitTestVisible = false;

            container.Width = width;
            container.Height = height;

            double rows = Math.Sqrt(this.ShrapnelAmount*height/width);
            double columns = this.ShrapnelAmount/rows;

            int rowCount = (int) Math.Round(rows);
            int columnCount = (int) Math.Round(columns);

            for (int x = 0; x < columnCount; ++x)
                container.ColumnDefinitions.Add(new ColumnDefinition()
                                                    {
                                                        Width = new GridLength(1, GridUnitType.Star),
                                                    });

            for (int y = 0; y < rowCount; ++y)
                container.RowDefinitions.Add(new RowDefinition()
                                                 {
                                                     Height = new GridLength(1, GridUnitType.Star),
                                                 });

            WriteableBitmap bitmap = new WriteableBitmap(content, null);
            for (int x = 0; x < columnCount; ++x)
            {
                for (int y = 0; y < rowCount; ++y)
                {
                    Rectangle element = new Rectangle();

                    ImageBrush brush = new ImageBrush()
                                           {
                                               ImageSource = bitmap,
                                           };

                    ScaleTransform scale = new ScaleTransform()
                                               {
                                                   ScaleX = columnCount,
                                                   ScaleY = rowCount,
                                               };

                    TranslateTransform translation = new TranslateTransform()
                                                         {
                                                             X = -x/(double) columnCount,
                                                             Y = -y/(double) rowCount,
                                                         };

                    TransformGroup transform = new TransformGroup();
                    transform.Children.Add(translation);
                    transform.Children.Add(scale);


                    brush.RelativeTransform = transform;

                    element.Fill = brush;

                    Grid.SetColumn(element, x);
                    Grid.SetRow(element, y);

                    container.Children.Add(element);

                    Shard shard = new Shard(element,
                                            new Point(width*(x + .5)/columnCount,
                                                      height*(y + .5)/rowCount));

                    this.shards.Add(shard);
                }
            }

            content.Opacity = 0;
            popup.IsOpen = true;
        }

        private void ApplyForce(Vector3D point)
        {
            Storyboard sb = new Storyboard()
                                {
                                    Duration = new Duration(TimeSpan.FromSeconds(100)),
                                };

            foreach (Shard shard in this.shards)
            {
                Vector3D position = shard.Position;

                Vector3D delta = position - point;

                double magnitude = -Math.Pow(delta.Length, 2)/(2*this.Radius*this.Radius);
                double power = this.Power*Math.Pow(Math.E, magnitude);


                delta.Normalize();
                delta = delta*power*10;

                shard.TranslationVelocity += delta;

                Vector3D offset = point - position;

                Vector3D rotation = new Vector3D(-offset.Y, -offset.X, offset.Z);

                rotation.Normalize();

                rotation = rotation*power*10;
                rotation.Z = 0;

                shard.RotationalVelocity += rotation;

                shard.StartAnims(sb);

            }
            sb.Begin();
        }

        private class Shard
        {

            private readonly PlaneProjection projection = new PlaneProjection();
            private Point initialPosition;

            public Shard(FrameworkElement target, Point initialPosition)
            {
                this.Target = target;
                this.initialPosition = initialPosition;
                target.Projection = this.projection;
            }

            public FrameworkElement Target { get; private set; }

            public Vector3D RotationalVelocity { get; set; }
            public Vector3D TranslationVelocity { get; set; }

            public void Update()
            {
                this.projection.RotationX += this.RotationalVelocity.X;
                this.projection.RotationY += this.RotationalVelocity.Y;
                this.projection.RotationZ += this.RotationalVelocity.Z;

                this.projection.GlobalOffsetX += this.TranslationVelocity.X;
                this.projection.GlobalOffsetY += this.TranslationVelocity.Y;
                this.projection.GlobalOffsetZ += this.TranslationVelocity.Z;
            }

            public Vector3D Position
            {
                get
                {
                    return new Vector3D(this.initialPosition.X + this.projection.GlobalOffsetX,
                                        this.initialPosition.Y + this.projection.GlobalOffsetY,
                                        this.projection.GlobalOffsetZ);
                }
            }

            public void StartAnims(Storyboard sb)
            {
                TimeSpan duration = TimeSpan.FromSeconds(100);
                sb.Duration = new Duration(duration);

                DoubleAnimation tx = new DoubleAnimation()
                                         {
                                             To = this.TranslationVelocity.X*duration.TotalSeconds,
                                             Duration = new Duration(duration),
                                         };
                Storyboard.SetTargetProperty(tx, new PropertyPath(PlaneProjection.GlobalOffsetXProperty));
                Storyboard.SetTarget(tx, this.projection);
                sb.Children.Add(tx);


                DoubleAnimation ty = new DoubleAnimation()
                                         {
                                             To = this.TranslationVelocity.Y*duration.TotalSeconds,
                                             Duration = new Duration(duration),
                                         };
                Storyboard.SetTargetProperty(ty, new PropertyPath(PlaneProjection.GlobalOffsetYProperty));
                Storyboard.SetTarget(ty, this.projection);
                sb.Children.Add(ty);

                DoubleAnimation tz = new DoubleAnimation()
                                         {
                                             To = this.TranslationVelocity.Z*duration.TotalSeconds,
                                             Duration = new Duration(duration),
                                         };
                Storyboard.SetTargetProperty(tz, new PropertyPath(PlaneProjection.GlobalOffsetZProperty));
                Storyboard.SetTarget(tz, this.projection);
                sb.Children.Add(tz);

                DoubleAnimation rx = new DoubleAnimation()
                                         {
                                             To = this.RotationalVelocity.X*duration.TotalSeconds,
                                             Duration = new Duration(duration),
                                         };
                Storyboard.SetTargetProperty(rx, new PropertyPath(PlaneProjection.RotationXProperty));
                Storyboard.SetTarget(rx, this.projection);
                sb.Children.Add(rx);

                DoubleAnimation ry = new DoubleAnimation()
                                         {
                                             To = this.RotationalVelocity.Y*duration.TotalSeconds,
                                             Duration = new Duration(duration),
                                         };
                Storyboard.SetTargetProperty(ry, new PropertyPath(PlaneProjection.RotationYProperty));
                Storyboard.SetTarget(ry, this.projection);
                sb.Children.Add(ry);

                DoubleAnimation rz = new DoubleAnimation()
                                         {
                                             To = this.RotationalVelocity.Z*duration.TotalSeconds,
                                             Duration = new Duration(duration),
                                         };
                Storyboard.SetTargetProperty(rz, new PropertyPath(PlaneProjection.RotationZProperty));
                Storyboard.SetTarget(rz, this.projection);
                sb.Children.Add(rz);

                Storyboard.SetTarget(sb, this.Target);
            }

            public void Reverse(TimeSpan duration, Storyboard sb)
            {
                sb.Duration = new Duration(duration);

                DoubleAnimation tx = new DoubleAnimation()
                                         {
                                             To = 0,
                                             Duration = new Duration(duration),
                                         };
                Storyboard.SetTargetProperty(tx, new PropertyPath(PlaneProjection.GlobalOffsetXProperty));
                Storyboard.SetTarget(tx, this.projection);
                sb.Children.Add(tx);


                DoubleAnimation ty = new DoubleAnimation()
                                         {
                                             To = 0,
                                             Duration = new Duration(duration),
                                         };
                Storyboard.SetTargetProperty(ty, new PropertyPath(PlaneProjection.GlobalOffsetYProperty));
                Storyboard.SetTarget(ty, this.projection);
                sb.Children.Add(ty);

                DoubleAnimation tz = new DoubleAnimation()
                                         {
                                             To = 0,
                                             Duration = new Duration(duration),
                                         };
                Storyboard.SetTargetProperty(tz, new PropertyPath(PlaneProjection.GlobalOffsetZProperty));
                Storyboard.SetTarget(tz, this.projection);
                sb.Children.Add(tz);

                DoubleAnimation rx = new DoubleAnimation()
                                         {
                                             To = 0,
                                             Duration = new Duration(duration),
                                         };
                Storyboard.SetTargetProperty(rx, new PropertyPath(PlaneProjection.RotationXProperty));
                Storyboard.SetTarget(rx, this.projection);
                sb.Children.Add(rx);

                DoubleAnimation ry = new DoubleAnimation()
                                         {
                                             To = 0,
                                             Duration = new Duration(duration),
                                         };
                Storyboard.SetTargetProperty(ry, new PropertyPath(PlaneProjection.RotationYProperty));
                Storyboard.SetTarget(ry, this.projection);
                sb.Children.Add(ry);

                DoubleAnimation rz = new DoubleAnimation()
                                         {
                                             To = 0,
                                             Duration = new Duration(duration),
                                         };
                Storyboard.SetTargetProperty(rz, new PropertyPath(PlaneProjection.RotationZProperty));
                Storyboard.SetTarget(rz, this.projection);
                sb.Children.Add(rz);

                Storyboard.SetTarget(sb, this.Target);
            }
        }

        private class DelegateCommand : ICommand
        {

            public delegate void Handler(object args);

            private Handler handler;

            public DelegateCommand(Handler handler)
            {
                this.handler = handler;
            }

            public bool CanExecute(object parameter)
            {
                return true;
            }

            public event EventHandler CanExecuteChanged;

            public void Execute(object parameter)
            {
                this.handler(parameter);
            }

            protected virtual void OnCanExecuteChanged()
            {
                if (this.CanExecuteChanged != null)
                    this.CanExecuteChanged(this, EventArgs.Empty);
            }

        }

        private struct Vector3D
        {

            public double X { get; set; }
            public double Y { get; set; }
            public double Z { get; set; }

            public Vector3D(double x, double y, double z)
                : this()
            {
                this.X = x;
                this.Y = y;
                this.Z = z;
            }

            public static Vector3D operator -(Vector3D a, Vector3D b)
            {
                return new Vector3D(a.X - b.X, a.Y - b.Y, a.Z - b.Z);
            }

            public static Vector3D operator +(Vector3D a, Vector3D b)
            {
                return new Vector3D(a.X + b.X, a.Y + b.Y, a.Z + b.Z);
            }

            public static Vector3D operator *(Vector3D vector, double scalar)
            {
                return new Vector3D(vector.X*scalar, vector.Y*scalar, vector.Z*scalar);
            }

            public static Vector3D operator /(Vector3D vector, double scalar)
            {
                return (Vector3D) (vector*(1.0/scalar));
            }

            public double Length
            {
                get { return Math.Sqrt(this.X*this.X + this.Y*this.Y + this.Z*this.Z); }
            }

            public void Normalize()
            {
                double x = Math.Abs(this.X);
                double y = Math.Abs(this.Y);
                double z = Math.Abs(this.Z);
                if (z > x)
                    x = z;
                if (y > x)
                    x = y;

                this.X /= x;
                this.Y /= x;
                this.Z /= x;
                double length = Math.Sqrt(this.X*this.X + this.Y*this.Y + this.Z*this.Z);
                this = (Vector3D) (this/length);
            }

            public Vector3D Cross(Vector3D other)
            {
                Vector3D result = new Vector3D();

                result.X = this.Y*other.Z - this.Z*other.Y;
                result.Y = this.Z*other.X - this.X*other.Z;
                result.Z = this.X*other.Y - this.Y*other.X;

                return result;
            }

            public override string ToString()
            {
                return this.X + "," + this.Y + "," + this.Z;
            }
        }
    }
}