using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using System.Diagnostics;
using TestGame.Scenes;

namespace TestGame
{
    public partial class MainForm : Form
    {
        private readonly Stopwatch _frameRateTimer = new Stopwatch();
        private long _lastFrameTime;

        private Scene _runningScene;
        private readonly Scene _gameScene;
        private readonly Scene _menuScene;


        public MainForm()
        {
            InitializeComponent();

            _menuScene = new MenuScene(this);
            _menuScene.Notification += NotificationFromScene;

            _gameScene = new GameScene(this);
            _gameScene.Notification += NotificationFromScene;

            _runningScene = _menuScene;
            _runningScene.Show();

            _frameRateTimer.Start();
            Application.Idle += HandleApplicationIdle;
        }

        static bool IsApplicationIdle()
        {
            NativeMessage result;
            return PeekMessage(out result, IntPtr.Zero, 0, 0, 0) == 0;
        }

        void HandleApplicationIdle(object sender, EventArgs e)
        {
            while (IsApplicationIdle())
            {
                var timeNow = _frameRateTimer.ElapsedMilliseconds;
                var frameTime = timeNow - _lastFrameTime;
                if (frameTime >= 16) // 33ms ~ 30fps; 16ms ~ 60fps
                {
                    _lastFrameTime = timeNow;

                    _runningScene.Update(frameTime);

                    Invalidate();
                }
            }
        }

        private void MainForm_Paint(object sender, PaintEventArgs e)
        {
            _runningScene.Draw(e.Graphics);
        }

        private void MainForm_MouseClick(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                _runningScene.MouseClick(e.X, e.Y);
                //Debug.WriteLine(String.Format("{0}, {1}", e.X, e.Y));
            }
        }

        private void NotificationFromScene(object sender)
        {
            _runningScene.Exit();
            
            if (sender == _gameScene)
                _runningScene = _menuScene;
            else if (sender == _menuScene)
                _runningScene = _gameScene;
            
            _runningScene.Show();
        }


        [StructLayout(LayoutKind.Sequential)]
        public struct NativeMessage
        {
            public IntPtr Handle;
            public uint Message;
            public IntPtr WParameter;
            public IntPtr LParameter;
            public uint Time;
            public Point Location;
        }

        [DllImport("user32.dll")]
        public static extern int PeekMessage(out NativeMessage message, IntPtr window, uint filterMin, uint filterMax, uint remove);
    }
}
