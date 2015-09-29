using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace ShporaKonturTetris
{
    class EnterData
    {
        public int Width { get; }
        public int Height { get; }
        public Figure[] Pieces { get; }
        public string Commands { get; }

        public class Figure
        {
            public readonly Point[] Cells;

            public class Point
            {
                public int X { get; }
                public int Y { get; }
            }
        }
    }

    class FigureParser
    {
        public Figure[] Parse(EnterData.Figure figure)
        {

        }
    }


    class Commander
    {
        public enum CommandType { A = 'A', D = 'D', S = 'S', Q = 'Q', E = 'E', P = 'P' }

        private readonly IEnumerable<CommandType> _commands;

        public Commander(string commands)
        {
            List<CommandType> listCommand = new List<CommandType>();
            foreach(var command in commands)
            {
                listCommand.Add((CommandType)command);
            }
        }

        public IEnumerable<CommandType> Commands()
        {
            foreach (var command in _commands)
                yield return command;
        }
    }

    class Point
    {
        public int X { get; }
        public int Y { get; }

        public Point(int x, int y)
        {
            X = x;
            Y = y;
        }
    }

    class ProjectionPoint : Point
    {
        public int ProjectionX { get; }
        public int ProjectionY { get; }

        public ProjectionPoint Axis { get; }

        public ProjectionPoint(int x, int y, ProjectionPoint axis) : base(x, y)
        {
            ProjectionX = x;
            ProjectionY = y;
            Axis = axis;
        }

        public ProjectionPoint(int x, int y, int projectionX,
            int projectionY, ProjectionPoint axis) : base(x, y)
        {
            ProjectionX = projectionX;
            ProjectionY = projectionY;
            Axis = axis;
        }
    }

    class Figure
    {
        public ProjectionPoint[] Cells { get; }

        public Figure(ProjectionPoint[] cells)
        {
            Cells = cells;
        }

        public Figure RotateClockwise()
        {
            Func<ProjectionPoint, Point> rotateClockwise = projectionPoint =>
            {
                int newProjectionX = projectionPoint.Axis.ProjectionX + projectionPoint.ProjectionY -
                projectionPoint.Axis.ProjectionY;
                int newProjectionY = projectionPoint.Axis.ProjectionY - projectionPoint.ProjectionX +
                projectionPoint.Axis.ProjectionX;
                return new Point(newProjectionX, newProjectionY);
            };


            return PerformOperation(rotateClockwise);
        }

        public Figure RotateCounterClockwise()
        {
            Func<ProjectionPoint, Point> rotateCounterClockwise = projectionPoint =>
            {
                int newProjectionX = projectionPoint.Axis.ProjectionX - projectionPoint.ProjectionY +
                projectionPoint.Axis.ProjectionY;
                int newProjectionY = projectionPoint.Axis.ProjectionY + projectionPoint.ProjectionX -
                projectionPoint.Axis.ProjectionX;
                return new Point(newProjectionX, newProjectionY);
            };

            return PerformOperation(rotateCounterClockwise);
        }

        public Figure ShiftXLeft(int value = 1)
        {
            Func<ProjectionPoint, Point> calculateProjectionShiftLeft = projectionPoint =>
            new Point(projectionPoint.ProjectionX - value, projectionPoint.ProjectionY);

            return PerformOperation(calculateProjectionShiftLeft);
        }

        public Figure ShiftXRight(int value = 1)
        {
            Func<ProjectionPoint, Point> calculateProjectionShiftLeft = projectionPoint =>
            new Point(projectionPoint.ProjectionX + value, projectionPoint.ProjectionY);

            return PerformOperation(calculateProjectionShiftLeft);
        }

        public Figure ShiftYDown(int value = 1)
        {
            Func<ProjectionPoint, Point> calculateProjectionShiftLeft = projectionPoint =>
            new Point(projectionPoint.ProjectionX, projectionPoint.ProjectionY + value);

            return PerformOperation(calculateProjectionShiftLeft);
        }


        private Figure PerformOperation(Func<ProjectionPoint, Point> calculateProjectionCoord)
        {
            var cells = Cells.Select(projectionPoint =>
            {
                Point newPoint = calculateProjectionCoord(projectionPoint);

                return new ProjectionPoint(
                projectionPoint.X, projectionPoint.Y,
                newPoint.X, newPoint.Y, projectionPoint.Axis);
            }).ToArray();

            return new Figure(cells);
        }
    }

    class PlayingField
    {
        public enum StateCoord { Busy = 1, Free = 0 }

        public ImmutableArray<ImmutableArray<StateCoord>> Field { get; }
        public int CountX { get; }
        public int CountY { get; }

        public PlayingField(int countX, int countY, StateCoord state = StateCoord.Free)
        {
            CountX = countX;
            CountY = countY;

            Field = ImmutableArray.Create(Enumerable.Repeat(
                ImmutableArray.Create(Enumerable.Repeat(
                    state, CountX).ToArray()), CountY).ToArray());
        }

        public PlayingField(int CountX, int CountY,
            ImmutableArray<ImmutableArray<StateCoord>> field)
        {
            Field = field;
        }
    }

    abstract class Engine
    {
        private readonly ImmutableArray<Figure> _figurs;

        public abstract void Start();

        protected Engine(Figure[] figurs)
        {
            _figurs = figurs.ToImmutableArray();
        }

        protected class CollisionException : Exception
        {
            public CollisionException() { }

            public CollisionException(string message) : base(message) { }
        }

        protected PlayingField FixedFigureOnField(Figure figure, PlayingField playingField)
        {
            Action<PlayingField.StateCoord[][]> modPutFigure = field =>
            {
                foreach (var cell in figure.Cells)
                    field[cell.ProjectionX][cell.ProjectionY] = PlayingField.StateCoord.Busy;
            };

            PlayingField newPlayingField = ModifyField(modPutFigure, playingField);

            return newPlayingField;
        }

        protected Figure ExecuteCommand(Commander.CommandType command, Figure figure, PlayingField playingField)
        {
            switch (command)
            {
                case Commander.CommandType.A:
                    figure = figure.ShiftXLeft();
                    break;
                case Commander.CommandType.D:
                    figure = figure.ShiftXRight();
                    break;
                case Commander.CommandType.S:
                    figure = figure.ShiftYDown();
                    break;
                case Commander.CommandType.Q:
                    figure = figure.RotateCounterClockwise();
                    break;
                case Commander.CommandType.E:
                    figure = figure.RotateClockwise();
                    break;
                case Commander.CommandType.P:
                    //вывести состояние поля
                    break;
            }
            if (СheckCollision(figure, playingField))
                throw new CollisionException();
            return figure;
        }

        protected PlayingField GameOver(PlayingField playingField, ref int bonus)
        {
            bonus -= 10;
            return new PlayingField(playingField.CountX, playingField.CountY);
        }

        protected IEnumerable<Figure> ProjectNewFigure(PlayingField playingField)
        {
            foreach (var figure in _figurs)
            {
                int lenFigure = figure.Cells[0].X;
                foreach (var cell in figure.Cells.Skip(1))
                    if (lenFigure < cell.X)
                        lenFigure = cell.X;

                int positionOffsetLeft = (playingField.CountX - lenFigure) / 2;

                var newFigure = figure.ShiftXRight(positionOffsetLeft);

                if (СheckCollision(newFigure, playingField))
                    throw new CollisionException();

                yield return newFigure;
            }
        }

        protected PlayingField RemAllFullRow(PlayingField playingField, ref int bonus)
        {
            var field = playingField.Field.ToList();
            foreach (var row in field)
                if (СheckFullRow(row))
                {
                    field.Remove(row);
                    bonus++;
                }

            var valueEmptyRow = playingField.CountY - field.Count;
            var newFreeRow = new List<ImmutableArray<PlayingField.StateCoord>>(
                Enumerable.Repeat(ImmutableArray.Create(
                    Enumerable.Repeat(PlayingField.StateCoord.Free, playingField.CountX).ToArray()),
                    valueEmptyRow));

            var newPlayingField = new PlayingField(
                playingField.CountX, playingField.CountY, field.Concat(newFreeRow).ToImmutableArray());

            return newPlayingField;
        }


        private bool СheckCollision(Figure figure, PlayingField playingField)
        {
            foreach (var cell in figure.Cells)
                if (playingField.Field[cell.ProjectionX][cell.ProjectionY] == PlayingField.StateCoord.Busy)
                    return true;
            return false;
        }

        private bool СheckFullRow(ImmutableArray<PlayingField.StateCoord> row)
        {
            foreach (var cell in row)
                if (cell == PlayingField.StateCoord.Free)
                    return false;
            return true;
        }

        private PlayingField ModifyField(Action<PlayingField.StateCoord[][]> modifyFieldFunc, PlayingField playingField)
        {
            var modField = playingField.Field.Select(row => row.ToArray()).ToArray();

            modifyFieldFunc(modField);

            var newField = modField.Select(row => ImmutableArray.Create(row)).ToImmutableArray();
            var newPlayingField = new PlayingField(playingField.CountX, playingField.CountY, newField);

            return newPlayingField;
        }
    }

    class PlayingEngine : Engine
    {
        private readonly Commander _commander;
        private readonly PlayingField _playingField;

        public PlayingEngine(int width, int height, Figure[] figurs, Commander commander) :
            base(figurs)
        {
            _playingField = new PlayingField(width, height);
            _commander = commander;
        }

        public override void Start()
        {
            int bonus = 0;

            var playingField = _playingField;

            var enumeratorFigure = ProjectNewFigure(playingField).GetEnumerator();
            enumeratorFigure.MoveNext();
            var figure = enumeratorFigure.Current;

            foreach(var command in _commander.Commands())
            {
                try
                {
                    figure = ExecuteCommand(command, figure, playingField);
                }
                catch (CollisionException)
                {
                    playingField = FixedFigureOnField(figure, playingField);
                    playingField = RemAllFullRow(playingField, ref bonus);

                    try
                    {
                        enumeratorFigure.MoveNext();
                        figure = enumeratorFigure.Current;
                    }
                    catch (CollisionException)
                    {
                        playingField = GameOver(playingField, ref bonus);
                        figure = enumeratorFigure.Current;
                    }
                }
            }
        }
    }

    class Program
    {
        static void Main(string[] args)
        {
            var enterData = JsonConvert.DeserializeObject<EnterData>(args[0]);

            Commander commander = new Commander(enterData.Commands);
            Figure[] figurs = FigureParser.Parse(enterData.Pieces);
            PlayingEngine playingEngine = new PlayingEngine(enterData.Width, enterData.Height, figurs, commander);
            playingEngine.Start();
        }
    }
}
