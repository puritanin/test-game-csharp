using System;

namespace TestGame.Actions
{
    public class Action
    {
        public bool IsDone { get { return Elapsed >= Duration; } }
        
        public SpriteNode Target { get; set; }
        
        public float Duration { get; set; }

        public bool Repeat { get; set; }
        
        public int RepeatTimes { get; set; }

        protected float Elapsed { get; set; }
        
        protected bool FirstTick { get; set; }


        public Action(SpriteNode target, float duration)
        {
            Target = target;
            Duration = duration;
            if (Duration == 0) Duration = 0.001f;
            Elapsed = 0;
            FirstTick = true;
            RepeatTimes = -1;
        }

        public void Reset()
        {
            Elapsed = 0;
            FirstTick = true;
        }

        /// <summary>
        /// called every frame
        /// </summary>
        /// <param name="time">in ms</param>
        public void Step(float time)
        {
            if (!IsDone)
            {
                if (FirstTick)
                {
                    Init();
                    FirstTick = false;
                    Elapsed = 0;
                }
                else
                {
                    Elapsed += time;
                }

                Update(Math.Min(1f, Elapsed/Duration));

                if (IsDone && Repeat)
                {
                    Reset();
                    if (RepeatTimes != -1)
                    {
                        if (RepeatTimes-- == 0) Repeat = false;
                    }
                }
            }
        }

        protected virtual void Init()
        {
        }

        /// <summary>
        /// 0 means that the action just started
        /// 0.5 means that the action is in the middle
        /// 1 means that the action is over
        /// </summary>
        /// <param name="t">time a value between 0 and 1</param>
        protected virtual void Update(float t)
        {
        }
    }
}
