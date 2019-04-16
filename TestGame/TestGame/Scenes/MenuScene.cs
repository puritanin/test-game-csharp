using System.Drawing;
using System.Windows.Forms;

namespace TestGame.Scenes
{
    public class MenuScene: Scene
    {
        private readonly Button _playButton;

        public MenuScene(Form form)
            : base(form)
        {
            _playButton = new Button { Size = new Size(100, 40), Font = new Font(@"Courier New", 18), Location = new Point(340, 280), Text = @"PLAY" };
            _playButton.Click += delegate { if (Notification != null) Notification(this); };
        }

        override public void Show()
        {
            Form.Controls.Add(_playButton);
        }

        override public void Exit()
        {
            Form.Controls.Clear();
        }
    }
}
