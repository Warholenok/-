using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace SnakeStudio
{
    public partial class MainWindow : Window
    {
        private readonly DispatcherTimer _timer;
        private GameEngine _game;
        private int _bestScore;

        private const double CellSize = 24;

        public MainWindow()
        {
            InitializeComponent();

            _timer = new DispatcherTimer();

            GameSettings settings = new GameSettings(1);

            _game = new GameEngine(settings);
            _game.Changed += Refresh;

            _timer.Interval =
                TimeSpan.FromMilliseconds(settings.Interval);

            _timer.Tick += Timer_Tick;
            Closed += Window_Closed;

            Refresh();
        }

        private void Window_Closed(object sender, EventArgs e)
        {
            _timer.Stop();
            _timer.Tick -= Timer_Tick;
            _game.Changed -= Refresh;
        }

        private void Timer_Tick(object sender, EventArgs e)
        {
            _game.Tick();
        }

        private void StartGame()
        {
            _timer.Stop();
            _game.Changed -= Refresh;

            GameSettings settings =
                new GameSettings(SpeedBox.SelectedIndex);

            _game = new GameEngine(settings);
            _game.Changed += Refresh;

            _timer.Interval =
                TimeSpan.FromMilliseconds(settings.Interval);

            _game.Start();
            Keyboard.Focus(this);
        }

        private void Play_Click(object sender, RoutedEventArgs e)
        {
            if (_game.State == GameState.Paused)
                _game.TogglePause();
            else
                StartGame();
        }

        private void NewGame_Click(object sender, RoutedEventArgs e)
        {
            StartGame();
        }

        private void Pause_Click(object sender, RoutedEventArgs e)
        {
            _game.TogglePause();
        }

        private void Window_Deactivated(object sender, EventArgs e)
        {
            if (_game != null &&
                _game.State == GameState.Running)
            {
                _game.TogglePause();
            }
        }

        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            if (_game == null)
                return;

            Direction? direction = null;

            switch (e.Key)
            {
                case Key.Up:
                case Key.W:
                    direction = Direction.Up;
                    break;

                case Key.Right:
                case Key.D:
                    direction = Direction.Right;
                    break;

                case Key.Down:
                case Key.S:
                    direction = Direction.Down;
                    break;

                case Key.Left:
                case Key.A:
                    direction = Direction.Left;
                    break;
            }

            if (direction.HasValue)
            {
                _game.Turn(direction.Value);
                e.Handled = true;
                return;
            }

            if (e.Key != Key.Space &&
                e.Key != Key.Enter &&
                e.Key != Key.Escape)
            {
                return;
            }

            e.Handled = true;

            // Удерживание клавиши не должно переключать
            // паузу несколько раз подряд.
            if (e.IsRepeat)
                return;

            switch (e.Key)
            {
                case Key.Space:
                    if (_game.State == GameState.Ready)
                        StartGame();
                    else
                        _game.TogglePause();
                    break;

                case Key.Enter:
                    if (_game.State == GameState.Paused)
                        _game.TogglePause();
                    else if (_game.State != GameState.Running)
                        StartGame();
                    break;

                case Key.Escape:
                    if (_game.State == GameState.Running)
                        _game.TogglePause();
                    break;
            }
        }

        private void Refresh()
        {
            _bestScore = Math.Max(_bestScore, _game.Score);

            ScoreText.Text = _game.Score.ToString();
            BestText.Text = _bestScore.ToString();
            LengthText.Text = _game.Snake.Length.ToString();

            DrawBoard();

            bool running = _game.State == GameState.Running;
            bool paused = _game.State == GameState.Paused;

            Overlay.Visibility = running
                ? Visibility.Collapsed
                : Visibility.Visible;

            PauseButton.IsEnabled = running || paused;
            PauseButton.Content = paused ? "Продолжить" : "Пауза";

            switch (_game.State)
            {
                case GameState.Ready:
                    StateText.Text = "Ожидание";
                    OverlayTitle.Text = "Готовы к игре?";
                    OverlayText.Text =
                        "Собирайте яблоки. Избегайте стен и собственного тела.";
                    PlayButton.Content = "Начать игру";
                    break;

                case GameState.Running:
                    StateText.Text = "В игре";
                    break;

                case GameState.Paused:
                    StateText.Text = "Пауза";
                    OverlayTitle.Text = "Можно выдохнуть";
                    OverlayText.Text =
                        "Нажмите пробел или кнопку, чтобы продолжить.";
                    PlayButton.Content = "Продолжить";
                    break;

                case GameState.GameOver:
                    StateText.Text = "Игра окончена";
                    OverlayTitle.Text = "Ещё одну попытку?";
                    OverlayText.Text = string.Format(
                        "Счёт: {0}. Длина: {1}.",
                        _game.Score,
                        _game.Snake.Length);
                    PlayButton.Content = "Играть снова";
                    break;

                case GameState.Won:
                    StateText.Text = "Победа";
                    OverlayTitle.Text = "Поле ваше!";
                    OverlayText.Text = string.Format(
                        "Вы заполнили всё поле! Счёт: {0}.",
                        _game.Score);
                    PlayButton.Content = "Играть снова";
                    break;
            }

            if (running)
                _timer.Start();
            else
                _timer.Stop();
        }

        private void DrawBoard()
        {
            GameCanvas.Children.Clear();

            SolidColorBrush gridBrush = MakeBrush("#1B293B");

            // Вертикальные линии сетки.
            for (int x = 0; x <= _game.Board.Columns; x++)
            {
                GameCanvas.Children.Add(new Line
                {
                    X1 = x * CellSize,
                    X2 = x * CellSize,
                    Y1 = 0,
                    Y2 = _game.Board.Rows * CellSize,
                    Stroke = gridBrush,
                    StrokeThickness = 0.6
                });
            }

            // Горизонтальные линии сетки.
            for (int y = 0; y <= _game.Board.Rows; y++)
            {
                GameCanvas.Children.Add(new Line
                {
                    X1 = 0,
                    X2 = _game.Board.Columns * CellSize,
                    Y1 = y * CellSize,
                    Y2 = y * CellSize,
                    Stroke = gridBrush,
                    StrokeThickness = 0.6
                });
            }

            Food food = _game.CurrentFood;

            if (food != null)
            {
                double x = food.Position.X * CellSize;
                double y = food.Position.Y * CellSize;

                // Полиморфизм: цвет определяется наследником Food.
                SolidColorBrush foodBrush =
                    MakeBrush(food.ColorHex);

                AddShape(new Ellipse
                {
                    Width = 23,
                    Height = 23,
                    Fill = foodBrush,
                    Opacity = 0.16
                }, x + 0.5, y + 0.5);

                AddShape(new Ellipse
                {
                    Width = 15,
                    Height = 15,
                    Fill = foodBrush
                }, x + 4.5, y + 5.5);

                AddShape(new Ellipse
                {
                    Width = 4,
                    Height = 4,
                    Fill = Brushes.White,
                    Opacity = 0.65
                }, x + 7, y + 8);

                AddShape(new Rectangle
                {
                    Width = 3,
                    Height = 5,
                    RadiusX = 1,
                    RadiusY = 1,
                    Fill = MakeBrush("#A7F3D0")
                }, x + 12, y + 2);
            }

            SolidColorBrush headBrush = MakeBrush("#A7F3D0");
            SolidColorBrush bodyBrush = MakeBrush("#34D399");

            // Голова рисуется последней.
            for (int i = _game.Snake.Length - 1; i >= 0; i--)
            {
                Cell cell = _game.Snake.Segments[i];

                AddShape(new Rectangle
                {
                    Width = CellSize - 3,
                    Height = CellSize - 3,
                    RadiusX = i == 0 ? 7 : 5,
                    RadiusY = i == 0 ? 7 : 5,
                    Fill = i == 0 ? headBrush : bodyBrush,
                    Opacity = i == 0 ? 1 : 0.88
                },
                cell.X * CellSize + 1.5,
                cell.Y * CellSize + 1.5);
            }

            DrawEyes();
        }

        private void DrawEyes()
        {
            double x1;
            double y1;
            double x2;
            double y2;

            switch (_game.Heading)
            {
                case Direction.Up:
                    x1 = 6;
                    y1 = 5;
                    x2 = 15;
                    y2 = 5;
                    break;

                case Direction.Down:
                    x1 = 6;
                    y1 = 15;
                    x2 = 15;
                    y2 = 15;
                    break;

                case Direction.Left:
                    x1 = 5;
                    y1 = 6;
                    x2 = 5;
                    y2 = 15;
                    break;

                default:
                    x1 = 15;
                    y1 = 6;
                    x2 = 15;
                    y2 = 15;
                    break;
            }

            Cell head = _game.Snake.Head;
            SolidColorBrush brush = MakeBrush("#08382D");

            AddShape(new Ellipse
            {
                Width = 3.5,
                Height = 3.5,
                Fill = brush
            },
            head.X * CellSize + x1,
            head.Y * CellSize + y1);

            AddShape(new Ellipse
            {
                Width = 3.5,
                Height = 3.5,
                Fill = brush
            },
            head.X * CellSize + x2,
            head.Y * CellSize + y2);
        }

        private void AddShape(Shape shape, double x, double y)
        {
            Canvas.SetLeft(shape, x);
            Canvas.SetTop(shape, y);
            GameCanvas.Children.Add(shape);
        }

        private static SolidColorBrush MakeBrush(string hex)
        {
            SolidColorBrush brush = new SolidColorBrush(
                (Color)ColorConverter.ConvertFromString(hex));

            brush.Freeze();
            return brush;
        }
    }

    // Вспомогательные типы.
    // Перечисления и структура не входят в число семи классов.

    public enum Direction
    {
        Up,
        Right,
        Down,
        Left
    }

    public enum GameState
    {
        Ready,
        Running,
        Paused,
        GameOver,
        Won
    }

    public struct Cell : IEquatable<Cell>
    {
        private readonly int _x;
        private readonly int _y;

        public int X
        {
            get { return _x; }
        }

        public int Y
        {
            get { return _y; }
        }

        public Cell(int x, int y)
        {
            _x = x;
            _y = y;
        }

        public Cell Next(Direction direction)
        {
            switch (direction)
            {
                case Direction.Up:
                    return new Cell(X, Y - 1);

                case Direction.Right:
                    return new Cell(X + 1, Y);

                case Direction.Down:
                    return new Cell(X, Y + 1);

                case Direction.Left:
                    return new Cell(X - 1, Y);

                default:
                    return this;
            }
        }

        public bool Equals(Cell other)
        {
            return X == other.X && Y == other.Y;
        }

        public override bool Equals(object obj)
        {
            if (!(obj is Cell))
                return false;

            return Equals((Cell)obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return (X * 397) ^ Y;
            }
        }

        public static bool operator ==(Cell left, Cell right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(Cell left, Cell right)
        {
            return !left.Equals(right);
        }
    }

    // КЛАСС 1. Настройки игры.
    public sealed class GameSettings
    {
        private readonly int _interval;

        public int Columns
        {
            get { return 28; }
        }

        public int Rows
        {
            get { return 22; }
        }

        public int Interval
        {
            get { return _interval; }
        }

        public GameSettings(int difficulty)
        {
            switch (difficulty)
            {
                case 0:
                    _interval = 170;
                    break;

                case 1:
                    _interval = 115;
                    break;

                case 2:
                    _interval = 75;
                    break;

                default:
                    throw new ArgumentOutOfRangeException(
                        "difficulty");
            }
        }
    }

    // КЛАСС 2. Игровое поле.
    public sealed class Board
    {
        private readonly int _columns;
        private readonly int _rows;

        public int Columns
        {
            get { return _columns; }
        }

        public int Rows
        {
            get { return _rows; }
        }

        public int Capacity
        {
            get { return Columns * Rows; }
        }

        public Board(int columns, int rows)
        {
            if (columns < 8 || rows < 8)
            {
                throw new ArgumentException(
                    "Размер поля должен быть не менее 8 × 8.");
            }

            _columns = columns;
            _rows = rows;
        }

        public bool Contains(Cell cell)
        {
            return cell.X >= 0 &&
                   cell.X < Columns &&
                   cell.Y >= 0 &&
                   cell.Y < Rows;
        }

        public List<Cell> GetFreeCells(IEnumerable<Cell> occupied)
        {
            HashSet<Cell> used = new HashSet<Cell>(occupied);
            List<Cell> result = new List<Cell>();

            for (int y = 0; y < Rows; y++)
            {
                for (int x = 0; x < Columns; x++)
                {
                    Cell cell = new Cell(x, y);

                    if (!used.Contains(cell))
                        result.Add(cell);
                }
            }

            return result;
        }
    }

    // КЛАСС 3. Змейка.
    public sealed class Snake
    {
        // Инкапсуляция: изменяемый список скрыт.
        private readonly List<Cell> _segments;
        private readonly ReadOnlyCollection<Cell> _view;

        public ReadOnlyCollection<Cell> Segments
        {
            get { return _view; }
        }

        public Cell Head
        {
            get { return _segments[0]; }
        }

        public int Length
        {
            get { return _segments.Count; }
        }

        public Snake(Cell head)
        {
            _segments = new List<Cell>();

            for (int i = 0; i < 4; i++)
                _segments.Add(new Cell(head.X - i, head.Y));

            _view = _segments.AsReadOnly();
        }

        public bool WouldCollide(Cell next, bool grows)
        {
            // При движении без роста клетка хвоста освобождается.
            int count = grows ? Length : Length - 1;

            for (int i = 0; i < count; i++)
            {
                if (_segments[i] == next)
                    return true;
            }

            return false;
        }

        public void Move(Cell next, bool grows)
        {
            _segments.Insert(0, next);

            if (!grows)
                _segments.RemoveAt(_segments.Count - 1);
        }
    }

    // КЛАСС 4. Абстрактный базовый класс еды.
    public abstract class Food
    {
        private readonly Cell _position;

        public Cell Position
        {
            get { return _position; }
        }

        public abstract int Points { get; }
        public abstract string ColorHex { get; }

        protected Food(Cell position)
        {
            _position = position;
        }
    }

    // КЛАСС 5. Обычное яблоко.
    public sealed class Apple : Food
    {
        public override int Points
        {
            get { return 10; }
        }

        public override string ColorHex
        {
            get { return "#FB7185"; }
        }

        public Apple(Cell position)
            : base(position)
        {
        }
    }

    // КЛАСС 6. Золотое яблоко.
    public sealed class GoldenApple : Food
    {
        public override int Points
        {
            get { return 30; }
        }

        public override string ColorHex
        {
            get { return "#FBBF24"; }
        }

        public GoldenApple(Cell position)
            : base(position)
        {
        }
    }

    // КЛАСС 7. Игровой движок.
    public sealed class GameEngine
    {
        private readonly Random _random;
        private readonly Board _board;

        private Direction _direction;
        private Direction _nextDirection;

        private bool _turnQueued;
        private int _foodsEaten;

        public Board Board
        {
            get { return _board; }
        }

        public Snake Snake { get; private set; }
        public Food CurrentFood { get; private set; }

        public GameState State { get; private set; }
        public int Score { get; private set; }

        public Direction Heading
        {
            get { return _direction; }
        }

        public event Action Changed;

        public GameEngine(GameSettings settings)
        {
            if (settings == null)
                throw new ArgumentNullException("settings");

            _random = new Random();

            _board = new Board(
                settings.Columns,
                settings.Rows);

            _direction = Direction.Right;
            _nextDirection = Direction.Right;

            State = GameState.Ready;
            Snake = CreateSnake();

            SpawnFood();
        }

        private Snake CreateSnake()
        {
            return new Snake(
                new Cell(
                    Board.Columns / 2,
                    Board.Rows / 2));
        }

        public void Start()
        {
            Snake = CreateSnake();

            Score = 0;
            _foodsEaten = 0;
            _turnQueued = false;

            _direction = Direction.Right;
            _nextDirection = Direction.Right;

            State = GameState.Running;

            SpawnFood();
            NotifyChanged();
        }

        public void Turn(Direction direction)
        {
            if (State != GameState.Running || _turnQueued)
                return;

            // Направления перечислены по часовой стрелке.
            bool opposite =
                ((int)direction + 2) % 4 == (int)_direction;

            if (opposite || direction == _direction)
                return;

            _nextDirection = direction;
            _turnQueued = true;
        }

        public void TogglePause()
        {
            if (State == GameState.Running)
            {
                State = GameState.Paused;
            }
            else if (State == GameState.Paused)
            {
                State = GameState.Running;
            }
            else
            {
                return;
            }

            NotifyChanged();
        }

        public void Tick()
        {
            if (State != GameState.Running)
                return;

            _direction = _nextDirection;
            _turnQueued = false;

            Cell next = Snake.Head.Next(_direction);

            bool grows =
                CurrentFood != null &&
                CurrentFood.Position == next;

            if (!Board.Contains(next) ||
                Snake.WouldCollide(next, grows))
            {
                State = GameState.GameOver;
                NotifyChanged();
                return;
            }

            Snake.Move(next, grows);

            if (grows)
            {
                // Полиморфизм:
                // Apple.Points = 10, GoldenApple.Points = 30.
                Score += CurrentFood.Points;
                _foodsEaten++;

                if (Snake.Length == Board.Capacity)
                {
                    CurrentFood = null;
                    State = GameState.Won;
                }
                else
                {
                    SpawnFood();
                }
            }

            NotifyChanged();
        }

        private void SpawnFood()
        {
            List<Cell> free =
                Board.GetFreeCells(Snake.Segments);

            if (free.Count == 0)
            {
                CurrentFood = null;
                State = GameState.Won;
                return;
            }

            Cell position = free[_random.Next(free.Count)];

            if ((_foodsEaten + 1) % 5 == 0)
                CurrentFood = new GoldenApple(position);
            else
                CurrentFood = new Apple(position);
        }

        private void NotifyChanged()
        {
            Action handler = Changed;

            if (handler != null)
                handler();
        }
    }
}