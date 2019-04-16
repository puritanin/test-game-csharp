using System.Drawing;
using System.Windows.Forms;

namespace TestGame.Scenes
{
    public delegate void NotificationDelegate(object sender);

    public class Scene
    {
        /// <summary>
        /// notify scenes manager about something
        /// in our case - before exit (time to change scene)
        /// </summary>
        public NotificationDelegate Notification;

        protected Form Form;


        public Scene(Form form)
        {
            Form = form;
        }

        public virtual void Show()
        {
        }

        public virtual void Exit()
        {
        }

        public virtual void Update(float time)
        {
        }

        public virtual void Draw(Graphics graphics)
        {
        }

        public virtual void MouseClick(float x, float y)
        {
        }
    }
}
