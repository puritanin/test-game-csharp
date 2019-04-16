using System;
using System.Drawing;
using System.Drawing.Imaging;

namespace TestGame
{
    public enum BonusTypes
    {
        None,
        DestroyerRow,
        DestroyerColumn,
        Bomb,
    }

    public class SpriteNode
    {
        /// <summary>
        /// integer number of stone [1; 5]
        /// </summary>
        public int Code { get; set; }

        /// <summary>
        /// order of priority in global count of stones movement
        /// </summary>
        public int MovementPriority { get; set; }

        /// <summary>
        /// indicates using on field in game
        /// "false" mean marked for destroy after animation
        /// </summary>
        public bool IsAlive { get; set; }

        public Bitmap Sprite { get; set; }


        private BonusTypes _bonusType;
        public BonusTypes BonusType
        {
            get { return _bonusType; }
            set
            {
                _bonusType = value;
                if (_bonusType == BonusTypes.Bomb) BonusSprite = new Bitmap("bomb.png");
                else if (_bonusType == BonusTypes.DestroyerRow) BonusSprite = new Bitmap("destroyer_h.png");
                else if (_bonusType == BonusTypes.DestroyerColumn) BonusSprite = new Bitmap("destroyer_v.png");
                else BonusSprite = null;
            }
        }
        public Bitmap BonusSprite { get; set; }


        public float X { get; set; }
        public float Y { get; set; }

        /// <summary>
        /// in degrees
        /// </summary>
        public float Rotate { get; set; }

        public float ScaleX { get; set; }
        public float ScaleY { get; set; }

        /// <summary>
        /// 1.0 is fully opaque
        /// 0.0 is fully transparent
        /// </summary>
        public float Opacity
        {
            get { return _opacity; }
            set
            {
                _opacity = value;
                if (_opacity > 1f) _opacity = 1f;
                if (_opacity < 0) _opacity = 0;
                _colorMatrix[3, 3] = _opacity;
                _imageAttributes.SetColorMatrix(_colorMatrix);
            }
        }


        private float _opacity;
        private readonly ColorMatrix _colorMatrix = new ColorMatrix(new[]
                        {
                            new []{1f, 0, 0, 0, 0},
                            new []{0, 1f, 0, 0, 0},
                            new []{0, 0, 1f, 0, 0},
                            new []{0, 0, 0, 1f, 0},
                            new []{0, 0, 0, 0, 1f}
                        });
        private readonly ImageAttributes _imageAttributes = new ImageAttributes();


        
        public SpriteNode(int code, float x, float y)
        {
            Code = code;
            Sprite = new Bitmap(String.Format("stone{0}.png", Code));
            X = x;
            Y = y;
            ScaleX = ScaleY = 1f;
            Opacity = 1f;
            BonusType = BonusTypes.None;
        }

        public void Draw(Graphics graphics)
        {
            if (ScaleX <= 0 || ScaleY <= 0) return;

            // Save
            var matrix = graphics.Transform;

            graphics.TranslateTransform(X, Y);
            graphics.RotateTransform(Rotate);
            graphics.ScaleTransform(ScaleX, ScaleY);

            graphics.DrawImage(Sprite, new Rectangle(-Sprite.Width / 2, -Sprite.Height / 2, Sprite.Width, Sprite.Height), 0, 0, Sprite.Width, Sprite.Height, GraphicsUnit.Pixel, _imageAttributes);

            if (BonusType != BonusTypes.None && BonusSprite != null)
                graphics.DrawImage(BonusSprite, new Rectangle(-Sprite.Width / 2, -Sprite.Height / 2, Sprite.Width, Sprite.Height), 0, 0, Sprite.Width, Sprite.Height, GraphicsUnit.Pixel, _imageAttributes);

            // Restore
            graphics.Transform = matrix;
        }
    }
}
