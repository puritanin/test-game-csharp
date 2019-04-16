using System.Linq;

namespace TestGame.Actions
{
    public class Sequence : Action
    {
        private readonly Action[] _actions;
        private int _index;
        private float _tPrev;
        
        public Sequence(SpriteNode target, float duration, Action[] actions)
            : base(target, duration)
        {
            _actions = actions;
            Duration = _actions.Sum(x => x.Duration);
            _tPrev = 0;
        }

        override protected void Update(float t)
        {
            var t2 = (t - _tPrev) * Duration;
            _actions[_index].Step(t2);

            _tPrev = t;
            if (!_actions[_index].IsDone) return;

            if (++_index == _actions.Length)
            {
                // last action is done, stop the sequence
                Elapsed = Duration + 1;
                _index = 0;
                _tPrev = 0;
                foreach (var action in _actions)
                    action.Reset();
            }
            else
            {
                // alignment common time line
                _actions[_index].Step(0);
                _actions[_index].Step(t2);
            }
        }
    }
}
