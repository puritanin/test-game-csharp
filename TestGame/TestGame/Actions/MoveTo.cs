namespace TestGame.Actions
{
    public class MoveTo : Action
    {
        private float _startX;
        private float _startY;
        private float _x;
        private float _y;

        public MoveTo(SpriteNode target, float duration, float x, float y)
            : base(target, duration)
        {
            _x = x;
            _y = y;
        }

        override protected void Init()
        {
            _startX = Target.X;
            _startY = Target.Y;
        }

        override protected void Update(float t)
        {
            Target.X = _startX + (_x - _startX) * t;
            Target.Y = _startY + (_y - _startY) * t;

            if (IsDone)
            {
                Target.X = _x;
                Target.Y = _y;
            }
        }
    }
}