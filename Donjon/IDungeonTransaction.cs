
using Donjon.Original;

using SixLabors.ImageSharp;

namespace Donjon;

public interface ITransaction<TArg>
{
    public void Execute(TArg d);
}
public interface IReversibleTransaction<TArg> : ITransaction<TArg>
{
    /// <summary>
    /// revert the transaction 
    /// </summary>
    /// <param name="d"></param>
    public void Undo(TArg d);
    /// <summary>
    /// revert the transaction, requiring that the condition before the Undo is 
    /// exactly the state that resulted from the Execute.
    /// </summary>
    /// <param name="d"></param>
    /// <exception cref="InexactUndoException"/>
    public void UndoExactly(TArg d)
    {
        if (!IsInPostcondition(d))
            throw new IReversibleTransaction<TArg>.InexactUndoException();
        this.Undo(d);
    }
    /// <summary>
    /// Test that the condition before the Undo is 
    /// exactly the state that resulted from the Execute.
    /// </summary>
    /// <param name="context"></param>
    /// <returns></returns>
    public bool IsInPostcondition(TArg context);

    /// <summary>
    /// Thrown when an Exact undo is not starting from the command's After state
    /// </summary>
    public class InexactUndoException : InvalidOperationException { }
}

public class SingleCellChangeCommand : ITransaction<IDungeon>
{
    public (int, int) Coordinate { get; init; }
    protected Cellbits After { get; init; }
    public SingleCellChangeCommand(int row, int col, Cellbits endState)
    {
        Coordinate = (row, col);
        After = endState;
    }

    public void Execute(IDungeon d)
    {
        d.cell[Coordinate.Item1, Coordinate.Item2] = After;
    }

}
public class ReversibleSingleCellChangeCommand : SingleCellChangeCommand, IReversibleTransaction<IDungeon>
{
    public ReversibleSingleCellChangeCommand(int row, int col, Cellbits startState, Cellbits endState)
    : base(row, col, endState)
    {
        Before = startState;
    }

    public ReversibleSingleCellChangeCommand(int row, int col, IDungeon startCondition, Cellbits endState)
    : this(row, col, startCondition.cell[row, col], endState)
    {
    }

    protected Cellbits Before { get; init; }

    public bool IsInPostcondition(IDungeon context)
    => After != context.cell[Coordinate.Item1, Coordinate.Item2];

    public void Undo(IDungeon d)
    {
        d.cell[Coordinate.Item1, Coordinate.Item2] = Before;
    }

    public void UndoExactly(IDungeon d)
    {
        if (!IsInPostcondition(d))
            throw new IReversibleTransaction<IDungeon>.InexactUndoException();
        this.Undo(d);
    }
}

public class MultipleCellTransaction : IReversibleTransaction<IDungeon>
{
    public MultipleCellTransaction Add(ReversibleSingleCellChangeCommand c)
    {
        commands.Add(c.Coordinate, c);
        return this;
    }
    Dictionary<(int, int), ReversibleSingleCellChangeCommand> commands = [];
    public void Execute(IDungeon d)
    {
        foreach (var c in commands.Values)
        {
            c.Execute(d);
        }
    }

    public bool IsInPostcondition(IDungeon context) =>
        commands.Values.All(c => c.IsInPostcondition(context));

    public void Undo(IDungeon d)
    {
        foreach (var c in commands.Values)
        {
            c.Undo(d);
        }
    }

    public void UndoExactly(IDungeon d)
    {
        if (!IsInPostcondition(d))
            throw new IReversibleTransaction<IDungeon>.InexactUndoException();

        foreach (var c in commands.Values)
        {
            c.UndoExactly(d);
        }
    }
}

public class CellCallbackTransaction(Point coord, Func<Cellbits, Cellbits> func) : IReversibleTransaction<IDungeon>
{
    public Cellbits? Before { get; private set; } = null;

    public void Execute(IDungeon d)
    {
        Before = d.cell[coord.Row(), coord.Col()];
        d.cell[coord.Row(), coord.Col()] = func(d.cell[coord.Row(), coord.Col()]);
    }

    public bool IsInPostcondition(IDungeon context) => Before.HasValue;

    public void Undo(IDungeon d)
    {
        if (Before is Cellbits b)
        {
            d.cell[coord.Row(), coord.Col()] = b;
            Before = null;
        }
    }
}

class CellTransactionBuilder
{
    public CellTransactionBuilder SetCoords(Point p) { return this; }
    public CellTransactionBuilder Transform(Func<Cellbits, Cellbits> fn)
    {
        return this;
    }
    public ReversibleSingleCellChangeCommand Build()
    {
        // return new ReversibleSingleCellChangeCommand();
        throw new NotImplementedException();
    }
}