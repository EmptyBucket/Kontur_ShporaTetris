using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;

namespace ShporaKonturTetris
{
    class EnterData
    {
        public int Width;
        public int Height;
        public Figure[] Pieces;
        public string Commands;

        public class Figure
        {
            public Point[] Cells;

            public class Point
            {
                public int X;
                public int Y;
            }
        }
    }

    static class EnterDataParser
    {
        public static Commander CommanderParse(EnterData enterData)
        {
            return new Commander(enterData.Commands);
        }

        public static Figure[] FigursParse(EnterData enterData)
        {
            EnterData.Figure[] figurs = enterData.Pieces;
            List<Figure> parseFigure = new List<Figure>();
            foreach(var figure in figurs)
            {
                parseFigure.Add(new Figure(CellParse(figure.Cells)));
            }

            return parseFigure.ToArray();
        }

        private static ProjectionPoint[] CellParse(EnterData.Figure.Point[] cells)
        {
            var axisPoint = cells.First(cell => cell.X == 0 && cell.Y == 0);
            var axisProjectionPoint = new ProjectionPoint(axisPoint.X, axisPoint.Y);
            var listCells = cells
                .Where(cell => !(cell.X == 0 && cell.Y == 0))
                .Select(cell => new ProjectionPoint(cell.X, cell.Y, axisProjectionPoint)).ToList();
            listCells.Add(axisProjectionPoint);
            return listCells.ToArray();
        }

        public static PlayingField PlayingFieldParser(EnterData enterData)
        {
            return new PlayingField(enterData.Width, enterData.Height);
        }
    }


    class Commander
    {
        public enum CommandType { A = 'A', D = 'D', S = 'S', Q = 'Q', E = 'E', P = 'P' }

        private readonly ImmutableArray<CommandType> _commands;

        public Commander(string commands)
        {
            List<CommandType> listCommand = new List<CommandType>();
            Array.ForEach(commands.ToCharArray(), command => listCommand.Add((CommandType)command));
            _commands = listCommand.ToImmutableArray();
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
        public bool IsAxis { get; }

        public ProjectionPoint(int x, int y) : base(x, y)
        {
            ProjectionX = x;
            ProjectionY = y;
            IsAxis = true;
            Axis = this;
        }

        public ProjectionPoint(int x, int y, ProjectionPoint axis) : base(x, y)
        {
            ProjectionX = x;
            ProjectionY = y;
            IsAxis = false;
            Axis = axis;
        }

        public ProjectionPoint(int x, int y, int projectionX,
            int projectionY, ProjectionPoint axis) : base(x, y)
        {
            ProjectionX = projectionX;
            ProjectionY = projectionY;
            IsAxis = false;
            Axis = axis;
        }

        public ProjectionPoint(int x, int y, int projectionX,
            int projectionY) : base(x, y)
        {
            ProjectionX = projectionX;
            ProjectionY = projectionY;
            IsAxis = true;
            Axis = this;
        }
    }

    class Figure
    {
        public ImmutableArray<ProjectionPoint> Cells { get; }

        public Figure(ProjectionPoint[] cells)
        {
            Cells = cells.ToImmutableArray();
        }

        public Figure(IEnumerable<ProjectionPoint> cells)
        {
            Cells = cells.ToImmutableArray();
        }

        public Figure RotateClockwise()
        {
            Func<ProjectionPoint, Point> rotateClockwise = projectionPoint =>
            {
                int newProjectionX = projectionPoint.Axis.ProjectionX - projectionPoint.ProjectionY +
                projectionPoint.Axis.ProjectionY;
                int newProjectionY = projectionPoint.Axis.ProjectionY + projectionPoint.ProjectionX -
                projectionPoint.Axis.ProjectionX;
                return new Point(newProjectionX, newProjectionY);
            };

            return PerformOperation(rotateClockwise);
        }

        public Figure RotateCounterClockwise()
        {
            Func<ProjectionPoint, Point> rotateCounterClockwise = projectionPoint =>
            {
                int newProjectionX = projectionPoint.Axis.ProjectionX + projectionPoint.ProjectionY -
                projectionPoint.Axis.ProjectionY;
                int newProjectionY = projectionPoint.Axis.ProjectionY - projectionPoint.ProjectionX +
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
            var axis = Cells.First(cell => cell.IsAxis);
            Point newPointAxis = calculateProjectionCoord(axis);
            var newAxis = new ProjectionPoint(axis.X, axis.Y, newPointAxis.X, newPointAxis.Y);

            var cells = Cells
                .Where(projectionPoint => !projectionPoint.IsAxis)
                .Select(projectionPoint =>
                {
                    Point newPoint = calculateProjectionCoord(projectionPoint);

                    return new ProjectionPoint(
                    projectionPoint.X, projectionPoint.Y,
                    newPoint.X, newPoint.Y, newAxis);
                }).ToList();
            cells.Add(newAxis);

            return new Figure(cells);
        }
    }

    class PlayingField
    {
        public enum StateCoord { Busy = 0, Free, CurrentFigure }

        public ImmutableArray<ImmutableArray<StateCoord>> Field { get; }
        public int CountX { get; }
        public int CountY { get; }

        public PlayingField(int countX, int countY, StateCoord state = StateCoord.Free)
        {
            CountX = countX;
            CountY = countY;

            Field = Enumerable.Repeat
                (
                    Enumerable.Repeat(state, CountX).ToImmutableArray(),
                    CountY
                ).ToImmutableArray();
        }

        public PlayingField(int countX, int countY,
            ImmutableArray<ImmutableArray<StateCoord>> field)
        {
            CountX = countX;
            CountY = countY;

            Field = field;
        }
    }

    abstract class Engine
    {
        public abstract void Start();

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
                    field[cell.ProjectionY][cell.ProjectionX] = PlayingField.StateCoord.Busy;
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
                    var fieldOut = playingField.Field.Select(row => row.ToArray()).ToArray();
                    foreach (var cell in figure.Cells)
                        fieldOut[cell.ProjectionY][cell.ProjectionX] = PlayingField.StateCoord.CurrentFigure;

                    foreach (var row in fieldOut)
                    {
                        foreach (var cell in row)
                            Console.Write
                            (
                                cell == PlayingField.StateCoord.Free ? '.' :
                                cell == PlayingField.StateCoord.Busy ? '#' : '*'
                            );
                        Console.Write(Environment.NewLine);
                    }
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

        protected Figure ProjectNewFigure(Figure figure, PlayingField playingField)
        {
            int rightEndFigure = figure.Cells[0].X;
            foreach (var cell in figure.Cells.Skip(1))
                if (rightEndFigure < cell.X)
                    rightEndFigure = cell.X;

            int leftEndFigure = figure.Cells[0].X;
            foreach (var cell in figure.Cells.Skip(1))
                if (leftEndFigure > cell.X)
                    leftEndFigure = cell.X;

            int lenFigure = rightEndFigure - leftEndFigure + 1;

            int positionOffsetLeft = (playingField.CountX - lenFigure) / 2;

            int upEndFigure = figure.Cells[0].Y;
            foreach (var cell in figure.Cells.Skip(1))
                if (cell.Y < upEndFigure)
                    upEndFigure = cell.Y;

            var newFigure = figure.ShiftXRight(Math.Abs(leftEndFigure) + positionOffsetLeft).ShiftYDown(Math.Abs(upEndFigure));

            if (СheckCollision(newFigure, playingField))
                throw new CollisionException();

            return newFigure;
        }

        protected PlayingField RemAllFullRow(PlayingField playingField, ref int bonus)
        {
            var field = playingField.Field.ToList();
            var remRow = new List<ImmutableArray<PlayingField.StateCoord>>();
            foreach (var row in field)
                if (СheckFullRow(row))
                {
                    remRow.Add(row);
                    bonus++;
                }
            remRow.ForEach(row => field.Remove(row));            

            var valueEmptyRow = playingField.CountY - field.Count;
            var newFreeRow = Enumerable.Repeat
                (
                    Enumerable.Repeat(PlayingField.StateCoord.Free, playingField.CountX).ToImmutableArray(),
                    valueEmptyRow
                );

            var newPlayingField = new PlayingField(
                playingField.CountX, playingField.CountY, newFreeRow.Concat(field).ToImmutableArray());

            return newPlayingField;
        }


        private bool СheckCollision(Figure figure, PlayingField playingField)
        {
            try
            {
                foreach (var cell in figure.Cells)
                    if (playingField.Field[cell.ProjectionY][cell.ProjectionX] != PlayingField.StateCoord.Free)
                        return true;
            }
            catch (IndexOutOfRangeException)
            {
                return true;
            }
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

            var newField = modField.Select(row => row.ToImmutableArray()).ToImmutableArray();
            var newPlayingField = new PlayingField(playingField.CountX, playingField.CountY, newField);

            return newPlayingField;
        }
    }

    class PlayingEngine : Engine
    {
        private readonly Commander _commander;
        private readonly PlayingField _playingField;
        private readonly ImmutableArray<Figure> _figurs;

        public PlayingEngine(PlayingField playingField, Figure[] figurs, Commander commander)
        {
            _playingField = playingField;
            _commander = commander;
            _figurs = figurs.ToImmutableArray();
        }

        public override void Start()
        {
            int bonus = 0;
            int numFigure = 0;
            int numCommand = 0;

            var playingField = _playingField;
            var figure = ProjectNewFigure(_figurs[numFigure++], playingField);

            foreach(var command in _commander.Commands())
            {
                try
                {
                    figure = ExecuteCommand(command, figure, playingField);
                }
                catch (CollisionException)
                {
                    if (numCommand == 23)
                    {

                    }
                    playingField = FixedFigureOnField(figure, playingField);
                    playingField = RemAllFullRow(playingField, ref bonus);

                    try
                    {
                        if (numFigure == _figurs.Count())
                            numFigure = 0;
                        figure = ProjectNewFigure(_figurs[numFigure], playingField);
                        numFigure++;
                    }
                    catch (CollisionException)
                    {
                        playingField = GameOver(playingField, ref bonus);
                        figure = ProjectNewFigure(_figurs[numFigure++], playingField);
                    }
                    Console.WriteLine("{0} {1}", numCommand, bonus);
                }
                numCommand++;
            }
        }
    }

    class Program
    {
        static void Main(string[] args)
        {
            string json = File.ReadAllText(args[0]);
            var enterData = JsonConvert.DeserializeObject<EnterData>(json);

            Commander commander = EnterDataParser.CommanderParse(enterData);
            Figure[] figurs = EnterDataParser.FigursParse(enterData);
            PlayingField playingField = EnterDataParser.PlayingFieldParser(enterData);

            PlayingEngine playingEngine = new PlayingEngine(playingField, figurs, commander);
            playingEngine.Start();
        }
    }
}
