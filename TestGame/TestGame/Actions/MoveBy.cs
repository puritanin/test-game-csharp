namespace TestGame.Actions
{
    public class MoveBy : Action
    {
        private float _startX;
        private float _startY;
        private float _x;
        private float _y;

        public MoveBy(SpriteNode target, float duration, float x, float y)
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
            Target.X = _startX + _x * t;
            Target.Y = _startY + _y * t;

            if (IsDone)
            {
                Target.X = _startX + _x;
                Target.Y = _startY + _y;
            }
        }
    }
}