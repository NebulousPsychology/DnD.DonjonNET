
using Donjon.Original;

using SixLabors.ImageSharp;

namespace Donjon;

public interface ITransaction<TReceiver>
{
    public void Execute(TReceiver d);
}
public interface IReversibleTransaction<TReceiver> : ITransaction<TReceiver>
{
    /// <summary>
    /// revert the transaction 
    /// </summary>
    /// <param name="d"></param>
    public void Undo(TReceiver d);
    /// <summary>
    /// revert the transaction, requiring that the condition before the Undo is 
    /// exactly the state that resulted from the Execute.
    /// </summary>
    /// <param name="d"></param>
    /// <exception cref="InexactUndoException"/>
    public void UndoExactly(TReceiver d)
    {
        if (!IsInPostcondition(d))
            throw new IReversibleTransaction<TReceiver>.InexactUndoException();
        this.Undo(d);
    }
    /// <summary>
    /// Test that the condition before the Undo is 
    /// exactly the state that resulted from the Execute.
    /// </summary>
    /// <param name="context"></param>
    /// <returns></returns>
    public bool IsInPostcondition(TReceiver context);

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

public abstract class MultipleTransactionBase<TReceiver> : IReversibleTransaction<TReceiver>
{
    public MultipleTransactionBase<TReceiver> Add(IReversibleTransaction<TReceiver> c)
    {
        commands.Add(c);
        return this;
    }
    List<IReversibleTransaction<TReceiver>> commands = [];
    public void Execute(TReceiver d)
    {
        foreach (var c in commands)
        {
            c.Execute(d);
        }
    }

    public bool IsInPostcondition(TReceiver context) =>
        commands.All(c => c.IsInPostcondition(context));

    public void Undo(TReceiver d)
    {
        foreach (var c in commands)
        {
            c.Undo(d);
        }
    }

    public void UndoExactly(TReceiver d)
    {
        if (!IsInPostcondition(d))
            throw new IReversibleTransaction<IDungeon>.InexactUndoException();

        foreach (var c in commands)
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