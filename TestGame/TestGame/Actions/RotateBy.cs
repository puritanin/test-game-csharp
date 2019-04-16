namespace TestGame.Actions
{
    public class RotateBy : Action
    {
        private float _startAngle;
        private float _angle;

        public RotateBy(SpriteNode target, float duration, float angle)
            : base(target, duration)
        {
            _angle = angle;
        }

        override protected void Init()
        {
            _startAngle = Target.Rotate;
        }

        override protected void Update(float t)
        {
            Target.Rotate = _startAngle + _angle * t;
            
            if (IsDone) Target.Rotate = _startAngle + _angle;
        }
    }
}