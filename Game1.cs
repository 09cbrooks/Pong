using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Audio;
using System;

namespace test2
{
    public class Ball
    {
        public Rectangle Box
        {
            get; private set; 
        }

        public Point Velocity
        {
            get; private set;
        }

        public Ball(Random rand, bool direction)
        {
            Box = new Rectangle(640 / 2, 480 / 2, 8, 8);
            Velocity = new Point(direction ? rand.Next(3, 7) : -rand.Next(3, 7) ,
                rand.Next() > int.MaxValue / 2 ? rand.Next(3, 7) : -rand.Next(3, 7));
        }
        public void SetPosition(Point point)
        {
            Box = new Rectangle(point, Box.Size);
        }

        const int MaxVelocity = 64;
        public void IncreaseVelocity(int? x = null, int? y = null)
        {
            Point vel = Velocity;
            if (x != null)
            {
                vel.X += (int)x;
            }

            if (y != null)
            {
                vel.Y += (int)y;
            }

            //cap ball speed
            if (Math.Abs(Velocity.X) > MaxVelocity)
            {
                vel.X = Math.Sign(vel.X) * MaxVelocity;
            }

            if(Math.Abs(Velocity.Y) > MaxVelocity)
            {
                vel.Y = Math.Sign(vel.Y) * MaxVelocity;
            }

            Velocity = vel;
        }
        public void ReverseVelocity(bool x = false, bool y = false)
        {
            var vel = Velocity;

            if (x)
            {
                vel.X = -vel.X;
            }

            if (y)
            {
                vel.Y = -vel.Y ;
            }

            Velocity = vel;
        }
        public (int, bool) Move(bool bounceOffSides)
        {
            bool bounced = false;

            var pos = Box.Location;

            pos.X += Velocity.X;
            pos.Y += Velocity.Y;

            if (pos.Y < 0)
            {
                bounced = true;
                pos.Y = -pos.Y;
                ReverseVelocity(y: true);
            }

            if (pos.Y + Box.Height > 480)
            {
                bounced = true;
                pos.Y = 480 - (pos.Y + Box.Height - 480);
                ReverseVelocity(y: true);
            }

            int score = 0;

            if (pos.X < 0)
            {
                if (bounceOffSides)
                {
                    bounced = true;
                    pos.X = 0;
                    ReverseVelocity(x: true);
                }
                else
                {
                    score = -1;
                }
            }

            if (pos.X + Box.Width > 640)
            {
                if (bounceOffSides)
                {
                    bounced = true;
                    pos.X = 640 - Box.Width;
                    ReverseVelocity(x: true);
                }
                else
                {
                    score = 1;
                }
            }

            SetPosition(pos);

            return (score, bounced);
        }
    }

    public class Paddle
    {
        public Rectangle Box
        {
            get; private set;
        }

        private readonly bool _side;

        public Paddle(bool side)
        {
            _side = side;
            var x = side ? 600 : 32;
            Box = new Rectangle(new Point(x, 224), new Point(8, 32));
        }

        public bool Ball_Is_Able_To_Be_Hit(Ball ball)
        {
            bool directionCheck;
            bool distanceCheck;

            if (_side)
            {
                directionCheck = ball.Velocity.X > 0;
                distanceCheck = ball.Box.X + ball.Box.Width > Box.X;
                return directionCheck & distanceCheck;
            }
            directionCheck = ball.Velocity.X < 0;
            distanceCheck = ball.Box.X < Box.Width + Box.X;
            return directionCheck & distanceCheck;
        }

        public (float, bool) Find_Delta_In_Ball_Movement(Ball ball)
        {
            float delta;
            bool wayPastPaddle;

            if (_side)
            {
                delta = ball.Box.X + ball.Box.Width - Box.X;
                wayPastPaddle = delta > ball.Velocity.X + ball.Box.Width;
                return (delta, wayPastPaddle);
            }
            delta = ball.Box.X - (Box.Width + Box.X);
            wayPastPaddle = delta < ball.Velocity.X;
            return (delta, wayPastPaddle);
        }

        private bool PaddleCheck(int x, int y)
        {
            return x <= Box.X + Box.Width &&
                x + 8 >= Box.X &&
                y <= Box.Y + Box.Height &&
                y + 8 >= Box.Y;
        }

        public bool CollsionCheck(Ball ball)
        {
            if (!Ball_Is_Able_To_Be_Hit(ball))
            {
                return false;
            }

            (float delta, bool wayPastPaddle) = Find_Delta_In_Ball_Movement(ball);

            if (wayPastPaddle)
            {
                return false;
            }

            float deltaTime = delta / ball.Velocity.X; //how much time hast past for movement
            int colY = (int)(ball.Box.Y - ball.Velocity.Y * deltaTime); //what y position was at time
            int colX = (int)(ball.Box.X - ball.Velocity.X * deltaTime); //what x position was at time

            if (PaddleCheck(colX, colY))
            {

                //make ball linger on hit
                ball.SetPosition(new Point(colX, colY));

                var diffY = (colY + ball.Box.Height / 2) - (Box.Y + Box.Height / 2);
                diffY /= Box.Height / 8;
                diffY -= Math.Sign(diffY);

                ball.IncreaseVelocity(Math.Sign(ball.Velocity.X), diffY);
                ball.ReverseVelocity(true);
                return true;
            }
            return false;
        }

        private void FixBounds(Point pos)
        {
            if (pos.Y < Box.Height)
            {
                pos.Y = Box.Height;
            }

            if (pos.Y + Box.Height > 480)
            {
                pos.Y = 480 - Box.Height;
            }

            Box = new Rectangle(pos, Box.Size);
        }

        public static int AIPaddleSpeed = 4;
        public void AIMove(Ball ball)
        {
            var delta = ball.Box.Y + ball.Box.Height / 2 - (Box.Y + Box.Height / 2);
            var pos = Box.Location;
            
            if (Math.Abs(delta) > AIPaddleSpeed)
            {
                delta = Math.Sign(delta) * AIPaddleSpeed;
            }
            pos.Y += delta;

            FixBounds(pos);
        }

        public void PlayerMove(int diff)
        {
            var pos = Box.Location;
            pos.Y += diff;
            FixBounds(pos);
        }
    }
    public class Game1 : Game
    {
        private GraphicsDeviceManager _graphics;
        private SpriteBatch _spriteBatch;
        private RenderTarget2D _doubleBuffer;
        private Rectangle _renderRectangle;
        private Texture2D _texture;

        private bool _lastPointSide = true;
        private readonly Random _rand;

        private Ball _ball; 
        private Paddle[] _paddles;

        private int[] _scores;
        private SpriteFont _font;

        private SoundEffect _bounceSound;
        private SoundEffect _hitSound;
        private SoundEffect _scoreSound;

        public enum GameState { Idle, Start, Play, CheckEnd }
        private GameState _gameState;

        private int _prevY = 0;

        public Game1()
        {
            _graphics = new GraphicsDeviceManager(this);
            Content.RootDirectory = "Content";
            IsMouseVisible = true;
            this.TargetElapsedTime = new TimeSpan(333333);
            Window.AllowUserResizing = true;

            _gameState = GameState.Idle;

            _rand = new Random();

            _paddles = new Paddle[2];
        }

        protected override void Initialize()
        {
            // TODO: Add your initialization logic here

            _doubleBuffer = new RenderTarget2D(GraphicsDevice, 640, 480);

            _graphics.PreferredBackBufferWidth = 720;
            _graphics.PreferredBackBufferHeight = 540;

            _graphics.IsFullScreen = false;

            _graphics.ApplyChanges();

            Window.ClientSizeChanged += OnWindowSizeChange;
            OnWindowSizeChange(null, null);

            _ball = new Ball(_rand, _lastPointSide);

            base.Initialize();
        }

        private void OnWindowSizeChange(object sender, EventArgs e)
        {
            var width = Window.ClientBounds.Width;
            var height = Window.ClientBounds.Height;

            if(height < width / (float)_doubleBuffer.Width * _doubleBuffer.Height)
            {
                width = (int)(height / (float)_doubleBuffer.Width * _doubleBuffer.Height);
            }
            else
            {
                height = (int)(width / (float)_doubleBuffer.Width * _doubleBuffer.Height);
            }

            var x = (Window.ClientBounds.Width - width) / 2;
            var y = (Window.ClientBounds.Height - height) / 2;
            _renderRectangle = new Rectangle(x, y, width, height);
        }

        /*private void ResetBall()
        {
            _ball = new Rectangle(_doubleBuffer.Width / 2 - 4 , _doubleBuffer.Height / 2 - 4 , 8 , 8);
            _ballVelocity = new Point(_lastPointSide ? _rand.Next(2, 7) : -_rand.Next(2,7) ,
                _rand.Next() > int.MaxValue/ 2 ? _rand.Next(2, 7) : -_rand.Next(2, 7));
        }*/

        protected override void LoadContent()
        {
            _spriteBatch = new SpriteBatch(GraphicsDevice);

            // TODO: use this.Content to load your game content here

            _texture = new Texture2D(GraphicsDevice, 1, 1);
            Color[] data = new Color[1];
            data[0] = Color.White;
            _texture.SetData(data);

            _font = Content.Load<SpriteFont>("font");

            _bounceSound = Content.Load<SoundEffect>("Click3");
            _hitSound = Content.Load<SoundEffect>("Click7");
            _scoreSound = Content.Load<SoundEffect>("Warning");
        }

        protected override void Update(GameTime gameTime)
        {
            if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed || Keyboard.GetState().IsKeyDown(Keys.Escape))
                Exit();

            var mouseState = Mouse.GetState();
            var deltaY = mouseState.Y - _prevY;
            _prevY = mouseState.Y;



            // TODO: Add your update logic here
            switch (_gameState)
            {
                case GameState.Idle:
                    (_, bool bounce) = _ball.Move(true);
                    
                    if (bounce)
                    {
                        _bounceSound.Play();
                    }
                    
                    if (mouseState.LeftButton == ButtonState.Pressed)
                    {
                        _gameState = GameState.Start;
                    }

                    break;
                case GameState.Start:
                    _ball = new Ball(_rand, _lastPointSide);
                    _paddles[0] = new Paddle(false);
                    _paddles[1] = new Paddle(true);
                    _scores = new int[2];
                    _gameState = GameState.Play;
                    break;
                case GameState.Play:
                    (int scored , bool bounced) = _ball.Move(false);
                    
                    if (bounced)
                    {
                        _bounceSound.Play();
                    }

                    _paddles[0].PlayerMove(deltaY);
                    _paddles[1].AIMove(_ball);

                    var hit = _paddles[0].CollsionCheck(_ball);
                    hit |= _paddles[1].CollsionCheck(_ball);

                    if (hit)
                    {
                        _hitSound.Play();
                        return;
                    }

                    if (scored == 0) return;

                    _gameState = GameState.CheckEnd;

                    _lastPointSide = scored == 1;
                    int index = _lastPointSide ? 0 : 1;
                    _scores[index]++;
                    _scoreSound.Play();

                    break;
                case GameState.CheckEnd:
                    _ball = new Ball(_rand, _lastPointSide);

                    if (_scores[0] > 9 || _scores[1] > 9)
                    {
                        _gameState = GameState.Idle;
                        break;
                    }

                    _gameState = GameState.Play;
                    break;
                default:
                    _gameState = GameState.Idle;
                    break;
            }

            base.Update(gameTime);
        }

        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.SetRenderTarget(_doubleBuffer);
            GraphicsDevice.Clear(Color.Black);

            // TODO: Add your drawing code here
            _spriteBatch.Begin();
            for (int i = 0; i < 31; i++)
            _spriteBatch.Draw(_texture, 
                new Rectangle(_doubleBuffer.Width / 2, i * _doubleBuffer.Height / 31 ,
                2 , _doubleBuffer.Height / 62), Color.White);

            switch (_gameState)
            {
                case GameState.Idle:
                    _spriteBatch.Draw(_texture, _ball.Box, Color.White);
                    break;
                case GameState.Start:
                    break;
                case GameState.Play:
                case GameState.CheckEnd:
                    _spriteBatch.Draw(_texture, _ball.Box, Color.White);

                    _spriteBatch.Draw(_texture, _paddles[0].Box, Color.White);
                    _spriteBatch.Draw(_texture, _paddles[1].Box, Color.White);

                    _spriteBatch.DrawString(_font, _scores[0].ToString(), new Vector2(64 , 0) , Color.White);
                    _spriteBatch.DrawString(_font, _scores[1].ToString(), 
                        new Vector2(_doubleBuffer.Width - 102, 0), Color.White);
                    break;
            }

            _spriteBatch.End();

            GraphicsDevice.SetRenderTarget(null);
            GraphicsDevice.Clear(Color.CornflowerBlue);

            // TODO: Add your drawing code here
            _spriteBatch.Begin();
            _spriteBatch.Draw(_doubleBuffer, _renderRectangle, Color.White);
            _spriteBatch.End();


            base.Draw(gameTime);
        }
    }
}
