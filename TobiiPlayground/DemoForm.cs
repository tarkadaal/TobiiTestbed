using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using Tobii.Gaze.Core;

namespace TobiiPlayground
{
    public partial class DemoForm : Form
    {
        public DemoForm(EyeTrackingEngine engine)
        {
            InitializeComponent();

            SetStyle(
                ControlStyles.AllPaintingInWmPaint |
                ControlStyles.UserPaint |
                ControlStyles.DoubleBuffer,
                true);

            _eyeTrackingEngine = engine;
            _eyeTrackingEngine.StateChanged += StateChanged;
            _eyeTrackingEngine.GazePoint += GazePoint;
            _eyeTrackingEngine.Initialize();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            e.Graphics.DrawImage(_plainBitmap, 0, 0, Width, Height);
            var cropRect = new Rectangle(
                Math.Max(_gazePoint.X - _radius, 0),
                Math.Max(_gazePoint.Y - _radius, 0),
                _diameter,
                _diameter);

                    if (_gazePoint != null)
                    {
                        e.Graphics.FillEllipse(
                            _tb,
                            cropRect);
                    }

        }

        private void StateChanged(object sender, EyeTrackingStateChangedEventArgs e)
        {
            // Forward state change to UI thread
            if (InvokeRequired)
            {
                Invoke((Action<EyeTrackingStateChangedEventArgs>)UpdateState, new object[] { e });
            }
            else
            {
                UpdateState(e);
            }
        }

        private void UpdateState(EyeTrackingStateChangedEventArgs eyeTrackingStateChangedEventArgs)
        {
            if (!string.IsNullOrEmpty(eyeTrackingStateChangedEventArgs.ErrorMessage))
            {

                return;
            }


            if (eyeTrackingStateChangedEventArgs.EyeTrackingState != EyeTrackingState.Tracking)
            {

            }
            else
            {
            }
        }

        private void GazePoint(object sender, GazePointEventArgs e)
        {
            var p = new Point(e.X, e.Y);
            if (p != _gazePoint) 
            {
                Invalidate(new Rectangle
                {
                    X = _gazePoint.X - _radius,
                    Y = _gazePoint.Y - _radius,
                    Width = _diameter,
                    Height = _diameter
                });

                _gazePoint = p;
                Invalidate(new Rectangle
                {
                    X = _gazePoint.X - _radius,
                    Y = _gazePoint.Y - _radius,
                    Width = _diameter,
                    Height = _diameter
                }
                );
            }
        }


        private EyeTrackingEngine _eyeTrackingEngine;
        private Point _gazePoint;
        private static int _radius = 80;
        private static int _diameter = _radius * 2;
        private static Bitmap _plainBitmap = new Bitmap("03317_angrybird_1440x900.jpg");
        private static Bitmap _hiddenBitmap = new Bitmap("03318_copperbandbutterflyfish_1440x900.jpg");
        private static TextureBrush _tb = new TextureBrush(_hiddenBitmap);
    }
}
