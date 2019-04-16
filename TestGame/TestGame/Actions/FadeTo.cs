namespace TestGame.Actions
{
    public class FadeTo : Action
    {
        private float _start;
        private float _opacity;

        public FadeTo(SpriteNode target, float duration, float opacity)
            : base(target, duration)
        {
            _opacity = opacity;
        }

        override protected void Init()
        {
            _start = Target.Opacity;
        }

        override protected void Update(float t)
        {
            Target.Opacity = _start + (_opacity - _start) * t;

            if (IsDone)
            {
                Target.Opacity = _opacity;
            }
        }
    }
}