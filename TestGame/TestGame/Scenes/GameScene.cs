using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Windows.Forms;
using TestGame.Actions;
using System.Linq;
using Action = TestGame.Actions.Action;

namespace TestGame.Scenes
{
    public class GameScene : Scene
    {
        private const int FieldStartX = 100;
        private const int FieldStartY = 50;
        private const int FieldWidth = 8;
        private const int FieldHeight = 8;
        private const int CellSize = 72; // suitable for stones [64x64]
        private const int FieldBorderWidth = 1;

        private const int MinMatchLength = 3;
        private const int BonusDestroyerMatchLength = 4;
        private const int BonusBombMatchLength = 5;

        private enum GameStates
        {
            SelectStone1,
            SelectStone2,
            Animation,
            GameOver,
        }

        private struct Match
        {
            public bool IsRow;
            public int X;
            public int Y;
            public int Length; // from (X,Y) to right in row and to down in column

            public override String ToString()
            {
                return String.Format("X={0}, Y={1}, L={2}, IsRow={3}", X, Y, Length, IsRow);
            }
        }

        private readonly Random _rnd = new Random();

        private readonly Label _timeLabel;
        private readonly Label _scoresLabel;
        private readonly Timer _timer1 = new Timer();

        private int _scores;
        private int _timeLeft; // seconds

        private int TimeLeft {
            get { return _timeLeft; }
            set
            {
                _timeLeft = value;
                _timeLabel.Text = String.Format("TIME: {0}", _timeLeft);
            }
        }


        private readonly List<SpriteNode> _spriteNodes = new List<SpriteNode>();

        private readonly List<Action> _actions = new List<Action>();

        private GameStates _gameState;
        private GameStates _gameStateNext;

        private Point _clickedCell;

        private readonly SpriteNode[,] _field = new SpriteNode[FieldWidth, FieldHeight];

        private Point _selectedCell;

        private int _movementCount;



        public GameScene(Form form) : base(form)
        {
            _timeLabel = new Label { AutoSize = true, Font = new Font(@"Courier New", 16), Location = new Point(100, 10) };
            _scoresLabel = new Label { AutoSize = true, Font = new Font(@"Courier New", 16), Location = new Point(390, 10) };
            
            _timer1.Interval = 1000;
            _timer1.Tick += Timer1Tick;
        }


        #region Scene interaction

        override public void Show()
        {
            Form.Controls.Add(_timeLabel);
            Form.Controls.Add(_scoresLabel);

            FieldRandomFill();

            _clickedCell.X = _clickedCell.Y = -1;
            _gameState = GameStates.SelectStone1;

            _scores = 0;
            UpdateScores();

            _movementCount = 0;

            TimeLeft = 60;
            _timer1.Stop();
            _timer1.Start();
        }

        override public void Exit()
        {
            Form.Controls.Clear();
        }

        private void Timer1Tick(object sender, EventArgs e)
        {
            if (--TimeLeft == 0)
            {
                _timer1.Stop();
                
                // time is up, game over
                _gameState = GameStates.GameOver;

                for (int y = 0; y < FieldHeight; y++)
                    for (int x = 0; x < FieldWidth; x++)
                        _field[x, y] = null;
                _actions.Clear();
                _spriteNodes.Clear();

                Form.Controls.Add(new Label { AutoSize = true, Font = new Font(@"Courier New", 36), Location = new Point(250, 200), Text = @"GAME OVER" });
                var button = new Button
                                 {
                                     Size = new Size(100, 40),
                                     Font = new Font(@"Courier New", 18),
                                     Location = new Point(340, 280),
                                     Text = @"OK"
                                 };
                button.Click += delegate { if (Notification != null) Notification(this); };
                Form.Controls.Add(button);
            }
        }

        private void UpdateScores()
        {
            _scoresLabel.Text = String.Format("SCORES: {0}", _scores);
        }

        #endregion




        #region Game logic

        override public void Update(float time)
        {
            switch (_gameState)
            {
                case GameStates.SelectStone1:
                    if (_clickedCell.X != -1 && _clickedCell.Y != -1)
                    {
                        SelectCell(_clickedCell.X, _clickedCell.Y);

                        _gameState = GameStates.SelectStone2;
                        _clickedCell.X = _clickedCell.Y = -1;
                    }
                    break;


                case GameStates.SelectStone2:
                    if (_clickedCell.X != -1 && _clickedCell.Y != -1 && _selectedCell != _clickedCell)
                    {
                        UnselectNode(_field[_selectedCell.X, _selectedCell.Y]);

                        // is second stone neighbor?
                        if ((Math.Abs(_selectedCell.X - _clickedCell.X) == 1 && _selectedCell.Y == _clickedCell.Y)
                            || (Math.Abs(_selectedCell.Y - _clickedCell.Y) == 1 && _selectedCell.X == _clickedCell.X))
                        {
                            // yes, is neighbor

                            var node1 = _field[_selectedCell.X, _selectedCell.Y];
                            var node2 = _field[_clickedCell.X, _clickedCell.Y];
                            var node1Position = GetPointFromCell(_selectedCell.X, _selectedCell.Y);
                            var node2Position = GetPointFromCell(_clickedCell.X, _clickedCell.Y);

                            // than follows game rules: matches or not?

                            _field[_selectedCell.X, _selectedCell.Y] = node2;
                            _field[_clickedCell.X, _clickedCell.Y] = node1;
                            node1.MovementPriority = ++_movementCount;
                            node2.MovementPriority = ++_movementCount;


                            var m = FindMatches();

                            if (m.Count() > 0)
                            {
                                // we will have matches on field after movement!
                                
                                float timeLine = 0;

                                // swap places two active nodes
                                _actions.Add(new MoveTo(node1, 400, node2Position.X, node2Position.Y));
                                _actions.Add(new MoveTo(node2, 400, node1Position.X, node1Position.Y));
                                timeLine += 400;

                                while (m.Count() > 0)
                                {
                                    // destroy matches
                                    int count;
                                    timeLine = DestroyMatches(m, timeLine, out count);
                                    timeLine += 300;
                                    
                                    _scores += count * 3;

                                    // shift top nodes down
                                    ShiftTopNodesDown(timeLine);
                                    timeLine += 300;
                                    
                                    // fill the holes
                                    FillEmptyCells(timeLine);
                                    timeLine += 200;

                                    // check again
                                    m = FindMatches();
                                }

                                _gameStateNext = GameStates.SelectStone1;
                            }
                            else
                            {
                                // no matches, only animation

                                // return nodes back
                                _field[_selectedCell.X, _selectedCell.Y] = node1;
                                _field[_clickedCell.X, _clickedCell.Y] = node2;

                                // swap and back
                                _actions.Add(new Sequence(node1, 0, new Action[]
                                                                        {
                                                                            new MoveTo(node1, 400, node2Position.X, node2Position.Y),
                                                                            new MoveTo(node1, 400, node1Position.X, node1Position.Y)
                                                                        }));
                                _actions.Add(new Sequence(node2, 0, new Action[]
                                                                        {
                                                                            new MoveTo(node2, 400, node1Position.X, node1Position.Y),
                                                                            new MoveTo(node2, 400, node2Position.X, node2Position.Y)
                                                                        }));

                                _gameStateNext = GameStates.SelectStone1;
                            }
                        }
                        else
                        {
                            // no neighbor, go to begin
                            _gameStateNext = GameStates.SelectStone1;
                        }

                        _clickedCell.X = _clickedCell.Y = -1;
                        _gameState = GameStates.Animation;
                    }
                    break;



                case GameStates.Animation:
                    if (_actions.Count == 0)
                    {
                        UpdateScores();
                        _clickedCell.X = _clickedCell.Y = -1;
                        _gameState = _gameStateNext;
                    }
                    break;


                case GameStates.GameOver:
                    // nothing
                    break;

                default:
                    break;
            }

            // update all actions
            foreach (var action in _actions)
                action.Step(time);
            _actions.RemoveAll(x => x.IsDone);

            // destroy dead nodes after all actions (with no actions)
            _spriteNodes.RemoveAll(x => !x.IsAlive && !_actions.Any(y => y.Target == x));
        }


        private IEnumerable<Match> FindMatches()
        {
            var matches = new List<Match>();

            // in rows
            for (int y = 0; y < FieldHeight; y++)
            {
                int count = 1;
                for (int x = 0; x < FieldWidth; x++)
                {
                    if (x < FieldWidth - 1 && _field[x, y].Code == _field[x + 1, y].Code) count++;
                    else
                    {
                        if (count >= MinMatchLength)
                            // match!!
                            matches.Add(new Match { IsRow = true, X = x - count + 1, Y = y, Length = count });
                        count = 1;
                    }
                }
            }

            // in columns
            for (int x = 0; x < FieldWidth; x++)
            {
                int count = 1;
                for (int y = 0; y < FieldHeight; y++)
                {
                    if (y < FieldHeight - 1 && _field[x, y].Code == _field[x, y + 1].Code) count++;
                    else
                    {
                        if (count >= MinMatchLength)
                            // match!!
                            matches.Add(new Match { IsRow = false, X = x, Y = y - count + 1, Length = count });
                        count = 1;
                    }
                }
            }

            //foreach (var m in matches) Debug.WriteLine(m.ToString());
            return matches.ToArray();
        }


        private float DestroyMatches(IEnumerable<Match> m, float timeLine, out int destroyedCount)
        {
            destroyedCount = 0; // for scores

            bool isAnyBonusFires = false;

            // prepare params for new bonuses
            var bonusParams = new List<int[]>();
            foreach (var match in m)
            {
                if (match.Length < BonusDestroyerMatchLength) continue;

                var lastMovedNode = _field[match.X, match.Y];
                int lastMovedX = match.X;
                int lastMovedY = match.Y;
                for (int i = 1; i < match.Length; i++)
                {
                    int x = match.X + (match.IsRow ? i : 0);
                    int y = match.Y + (match.IsRow ? 0 : i);
                    var node = _field[x, y];
                    if (node.MovementPriority > lastMovedNode.MovementPriority)
                    {
                        lastMovedNode = node;
                        lastMovedX = x;
                        lastMovedY = y;
                    }
                }
                
                if (match.Length >= BonusBombMatchLength)
                {
                    bonusParams.Add(new[] { (int)(BonusTypes.Bomb), lastMovedNode.Code, lastMovedX, lastMovedY });
                }
                else if (match.Length >= BonusDestroyerMatchLength)
                {
                    bonusParams.Add(new[] { (int)(match.IsRow ? BonusTypes.DestroyerRow : BonusTypes.DestroyerColumn), lastMovedNode.Code, lastMovedX, lastMovedY });
                }
            }

            
            // recursive destroy all matches with fires existing bonuses
            foreach (var match in m)
            {
                int dc;
                bool fired;
                timeLine = DestroyOneMatch(match, timeLine, out dc, out fired);
                timeLine += 300;
                destroyedCount += dc;
                isAnyBonusFires |= fired;
            }



            // add new bonuses
            if (!isAnyBonusFires)
            {
                foreach (var param in bonusParams)
                {
                    var p = GetPointFromCell(param[2], param[3]);
                    var node = new SpriteNode(param[1], p.X, p.Y)
                                   {
                                       IsAlive = true,
                                       MovementPriority = ++_movementCount,
                                       BonusType = (BonusTypes) param[0]
                                   };
                    node.ScaleX = node.ScaleY = 0;
                    _spriteNodes.Add(node);
                    _field[param[2], param[3]] = node;
                    _actions.Add(new Sequence(node, 0, new Action[]
                                                           {
                                                               new Action(node, timeLine),
                                                               new ScaleTo(node, 300, 1f, 1f)
                                                           }));
                }
            }

            return timeLine;
        }


        private float DestroyOneMatch(Match match, float timeLine, out int destroyedCount, out bool isAnyBonusFires)
        {
            destroyedCount = 0;
            isAnyBonusFires = false;

            for (int i = 0; i < match.Length; i++)
            {
                int x = match.X + (match.IsRow ? i : 0);
                int y = match.Y + (match.IsRow ? 0 : i);

                var node = _field[x, y];
                if (node == null || !node.IsAlive) continue;

                if (node.BonusType == BonusTypes.None)
                {
                    DestroyNode(x, y, timeLine, match.IsRow);
                }
                else
                {
                    _actions.Add(new Sequence(node, 0, new Action[]
                                                           {
                                                               new Action(node, timeLine),
                                                               new FadeTo(node, 600, 0.1f)
                                                           }));
                    if (node.BonusType == BonusTypes.Bomb)
                    {
                        // add blow effect
                        _actions.Add(new Sequence(node, 0, new Action[]
                                                           {
                                                               new Action(node, timeLine),
                                                               new ScaleTo(node, 600, 3f, 3f)
                                                           }));
                    }

                    node.IsAlive = false;
                    _field[x, y] = null;

                    isAnyBonusFires = true;

                    int dc;
                    bool fired;

                    // fire!!
                    if (node.BonusType == BonusTypes.DestroyerRow && !(match.IsRow && match.Length == FieldWidth))
                    {
                        var bonusMatch = new Match {X = 0, Y = y, Length = FieldWidth, IsRow = true};
                        timeLine = DestroyOneMatch(bonusMatch, timeLine, out dc, out fired);
                        destroyedCount += dc;
                        isAnyBonusFires |= fired;

                        RunDestroyer(timeLine, x, y, 0, y);
                        RunDestroyer(timeLine, x, y, FieldWidth - 1, y);
                        
                        timeLine += 1000;
                    }
                    else if (node.BonusType == BonusTypes.DestroyerColumn && !(!match.IsRow && match.Length == FieldHeight))
                    {
                        var bonusMatch = new Match { X = x, Y = 0, Length = FieldHeight, IsRow = false };
                        timeLine = DestroyOneMatch(bonusMatch, timeLine, out dc, out fired);
                        destroyedCount += dc;
                        isAnyBonusFires |= fired;
                        
                        RunDestroyer(timeLine, x, y, x, 0);
                        RunDestroyer(timeLine, x, y, x, FieldHeight - 1);
                        
                        timeLine += 1000;
                    }
                    else if (node.BonusType == BonusTypes.Bomb)
                    {
                        for (int r = 0; r < 3; r++)
                        {
                            var bonusMatch = new Match {X = x - 1, Y = y - 1 + r, Length = 3, IsRow = true};
                            timeLine = DestroyOneMatch(bonusMatch, timeLine, out dc, out fired);
                            destroyedCount += dc;
                            isAnyBonusFires |= fired;
                        }

                        timeLine += 300;
                    }
                }

                destroyedCount++;
            }

            return timeLine;
        }


        private void ShiftTopNodesDown(float timeLine)
        {
            for (int x = 0; x < FieldWidth; x++)
            {
                for (int y = FieldHeight - 1; y > 0; y--)
                {
                    if (_field[x, y] == null)
                    {
                        int j;
                        for (j = y - 1; j >= 0; j--)
                            if (_field[x, j] != null) break;

                        if (j >= 0)
                        {
                            // here (x,y) = null cell, (x,j) = first not null
                            // shift node
                            var node = _field[x, j];
                            _actions.Add(new Sequence(node, 0, new Action[]
                                                                   {
                                                                       new Action(node, timeLine),
                                                                       new MoveBy(node, 70 * (y - j), 0, CellSize * (y - j))
                                                                   }));
                            node.MovementPriority = ++_movementCount;
                            _field[x, y] = _field[x, j];
                            _field[x, j] = null;
                        }
                    }
                }
            }
        }


        private void FillEmptyCells(float timeLine)
        {
            for (int y = 0; y < FieldHeight; y++)
            {
                for (int x = 0; x < FieldWidth; x++)
                {
                    if (_field[x, y] == null)
                    {
                        var node = AddNewNode(x, y);
                        node.ScaleX = node.ScaleY = 0;
                        _actions.Add(new Sequence(node, 0, new Action[]
                                                               {
                                                                   new Action(node, timeLine),
                                                                   new ScaleTo(node, 300, 1f, 1f)
                                                               }));
                    }
                }
            }
        }
               


        private void FieldRandomFill()
        {
            for (int y = 0; y < FieldHeight; y++)
            {
                for (int x = 0; x < FieldWidth; x++)
                {
                    var node = AddNewNode(x, y);
                    node.MovementPriority = 0;
                }
            }
        }


        private SpriteNode AddNewNode(int x, int y)
        {
            var p = GetPointFromCell(x, y);
            int code = _rnd.Next(1, 6);

            // game rules: check for two sames stones in row and column before our
            int forbidden1 = -1, forbidden2 = -1;
            if (x > 1) forbidden1 = _field[x - 1, y].Code == _field[x - 2, y].Code ? _field[x - 1, y].Code : -1;
            if (y > 1) forbidden2 = _field[x, y - 1].Code == _field[x, y - 2].Code ? _field[x, y - 1].Code : -1;
            while (code == forbidden1 || code == forbidden2)
                code = _rnd.Next(1, 6);

            var node = new SpriteNode(code, p.X, p.Y) { IsAlive = true, MovementPriority = ++_movementCount };
            _spriteNodes.Add(node);
            _field[x, y] = node;

            return node;
        }


        private void DestroyNode(int x, int y, float timeLine, bool isRow)
        {
            var node = _field[x, y];
            if (node == null || !node.IsAlive) return;

            _actions.Add(new Sequence(node, 0, new Action[]
                                               {
                                                   new Action(node, timeLine),
                                                   new MoveBy(node, 600, isRow ? 0 : 3 * CellSize, isRow ? 3 * CellSize : 0)
                                               }));
            _actions.Add(new Sequence(node, 0, new Action[]
                                               {
                                                   new Action(node, timeLine),
                                                   new FadeTo(node, 600, 0.1f)
                                               }));
            node.IsAlive = false;
            _field[x, y] = null;
        }


        private void SelectCell(int x, int y)
        {
            _selectedCell.X = x;
            _selectedCell.Y = y;
            var node = _field[x, y];
            _actions.Add(new Sequence(node, 0, new Action[]
                                                   {
                                                       new ScaleTo(node, 300, 0.1f, 1f),
                                                       new ScaleTo(node, 300, 1f, 1f)
                                                   }) { Repeat = true });
        }


        private void UnselectNode(SpriteNode node)
        {
            var action = _actions.First(a => a.Target == node && a.Repeat);
            action.Repeat = false;
        }


        private void RunDestroyer(float timeLine, int x1, int y1, int x2, int y2)
        {
            var p = GetPointFromCell(x1, y1);
            int cells = Math.Max(Math.Abs(x2 - x1), Math.Abs(y2 - y1));
            var destroyer = new SpriteNode(1, p.X, p.Y) { IsAlive = false, Sprite = new Bitmap("destroyer.png"), Opacity = 0 };
            _spriteNodes.Add(destroyer);
            _actions.Add(new Sequence(destroyer, 0, new Action[]
                                                       {
                                                           new Action(destroyer, timeLine - 100),
                                                           new MoveBy(destroyer, 250 * cells, CellSize * (x2 - x1), CellSize * (y2 - y1))
                                                       }));
            _actions.Add(new Sequence(destroyer, 0, new Action[]
                                                       {
                                                           new Action(destroyer, timeLine - 100),
                                                           new FadeTo(destroyer, 10, 1)
                                                       }));
            _actions.Add(new Sequence(destroyer, 0, new Action[]
                                                       {
                                                           new RotateBy(destroyer, 150, 45),
                                                           new RotateBy(destroyer, 50, -45)
                                                       }) { Repeat = true, RepeatTimes = cells + ((int)timeLine - 100) / (150 + 50) - 1 });
        }


        private Point GetCellFromPoint(float x, float y)
        {
            var cell = new Point
            {
                X =
                    x < FieldStartX || x > FieldStartX + FieldWidth * CellSize
                        ? -1
                        : ((int)x - FieldStartX) / CellSize,
                Y =
                    y < FieldStartY || y > FieldStartY + FieldHeight * CellSize
                        ? -1
                        : ((int)y - FieldStartY) / CellSize
            };
            return cell;
        }

        private static Point GetPointFromCell(int x, int y)
        {
            return new Point(FieldStartX + x * CellSize + CellSize / 2 - FieldBorderWidth, FieldStartY + y * CellSize + CellSize / 2 - FieldBorderWidth);
        }

        #endregion



        override public void Draw(Graphics graphics)
        {
            // Main field
            var pen = new Pen(Color.LightSteelBlue, FieldBorderWidth);
            graphics.DrawRectangle(pen, FieldStartX, FieldStartY, FieldWidth * CellSize, FieldHeight * CellSize);
            for (int x = 1; x < FieldWidth; x++)
                graphics.DrawLine(pen, FieldStartX + x * CellSize, FieldStartY, FieldStartX + x * CellSize, FieldStartY + FieldHeight * CellSize);
            for (int y = 1; y < FieldHeight; y++)
                graphics.DrawLine(pen, FieldStartX, FieldStartY + y * CellSize, FieldStartX + FieldWidth * CellSize, FieldStartY + y * CellSize);

            // Game objects
            foreach (var node in _spriteNodes)
                node.Draw(graphics);
        }

        override public void MouseClick(float x, float y)
        {
            _clickedCell = GetCellFromPoint(x, y);
            //Debug.WriteLine(String.Format("Nodes: {0}, actions: {1}", _spriteNodes.Count, _actions.Count));
        }
    }
}
