using System.Collections.Generic;

namespace Modernote.Desktop;

/// <summary>
/// Undo/redo history for editor operations.
/// Stores EditorHost snapshots for undo/redo of block-level changes.
/// </summary>
public sealed class CommandHistory
{
    private readonly Stack<Editor.EditorHost> _undo = new();
    private readonly Stack<Editor.EditorHost> _redo = new();

    public bool CanUndo => _undo.Count > 0;
    public bool CanRedo => _redo.Count > 0;

    public void Push(Editor.EditorHost state)
    {
        _undo.Push(state);
        _redo.Clear();
    }

    public Editor.EditorHost? Undo(Editor.EditorHost current)
    {
        if (_undo.Count == 0) return null;
        _redo.Push(current);
        return _undo.Pop();
    }

    public Editor.EditorHost? Redo(Editor.EditorHost current)
    {
        if (_redo.Count == 0) return null;
        _undo.Push(current);
        return _redo.Pop();
    }
}
