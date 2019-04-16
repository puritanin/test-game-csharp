namespace TestGame.Actions
{
    public class ScaleTo : Action
    {
        private float _startX;
        private float _startY;
        private float _scaleX;
        private float _scaleY;

        public ScaleTo(SpriteNode target, float duration, float scaleX, float scaleY)
            : base(target, duration)
        {
            _scaleX = scaleX;
            _scaleY = scaleY;
        }

        override protected void Init()
        {
            _startX = Target.ScaleX;
            _startY = Target.ScaleY;
        }

        override protected void Update(float t)
        {
            Target.ScaleX = _startX + (_scaleX - _startX) * t;
            Target.ScaleY = _startY + (_scaleY - _startY) * t;

            if (IsDone)
            {
                Target.ScaleX = _scaleX;
                Target.ScaleY = _scaleY;
            }
        }
    }
}