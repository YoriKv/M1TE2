using System.Collections.Generic;

namespace M1TE2
{
    // A snapshot of the editable session state used for undo/redo.
    // Captures the three background maps, all tilesets, and the palette.
    public class UndoSnapshot
    {
        private readonly int[] tile;
        private readonly int[] palette;
        private readonly int[] h_flip;
        private readonly int[] v_flip;
        private readonly int[] priority;
        private readonly int[] tile_arrays;
        private readonly byte[] pal_r;
        private readonly byte[] pal_g;
        private readonly byte[] pal_b;

        private UndoSnapshot()
        {
            tile = (int[])Maps.tile.Clone();
            palette = (int[])Maps.palette.Clone();
            h_flip = (int[])Maps.h_flip.Clone();
            v_flip = (int[])Maps.v_flip.Clone();
            priority = (int[])Maps.priority.Clone();
            tile_arrays = (int[])Tiles.Tile_Arrays.Clone();
            pal_r = (byte[])Palettes.pal_r.Clone();
            pal_g = (byte[])Palettes.pal_g.Clone();
            pal_b = (byte[])Palettes.pal_b.Clone();
        }

        // grab the current state
        public static UndoSnapshot Capture()
        {
            return new UndoSnapshot();
        }

        // write this state back over the live arrays
        public void Restore()
        {
            System.Array.Copy(tile, Maps.tile, tile.Length);
            System.Array.Copy(palette, Maps.palette, palette.Length);
            System.Array.Copy(h_flip, Maps.h_flip, h_flip.Length);
            System.Array.Copy(v_flip, Maps.v_flip, v_flip.Length);
            System.Array.Copy(priority, Maps.priority, priority.Length);
            System.Array.Copy(tile_arrays, Tiles.Tile_Arrays, tile_arrays.Length);
            System.Array.Copy(pal_r, Palettes.pal_r, pal_r.Length);
            System.Array.Copy(pal_g, Palettes.pal_g, pal_g.Length);
            System.Array.Copy(pal_b, Palettes.pal_b, pal_b.Length);
        }

        // true if the live state has changed since this snapshot was taken
        public bool DiffersFromLive()
        {
            return !Same(tile, Maps.tile)
                || !Same(palette, Maps.palette)
                || !Same(h_flip, Maps.h_flip)
                || !Same(v_flip, Maps.v_flip)
                || !Same(priority, Maps.priority)
                || !Same(tile_arrays, Tiles.Tile_Arrays)
                || !Same(pal_r, Palettes.pal_r)
                || !Same(pal_g, Palettes.pal_g)
                || !Same(pal_b, Palettes.pal_b);
        }

        private static bool Same(int[] a, int[] b)
        {
            if (a.Length != b.Length) return false;
            for (int i = 0; i < a.Length; i++)
            {
                if (a[i] != b[i]) return false;
            }
            return true;
        }

        private static bool Same(byte[] a, byte[] b)
        {
            if (a.Length != b.Length) return false;
            for (int i = 0; i < a.Length; i++)
            {
                if (a[i] != b[i]) return false;
            }
            return true;
        }
    }

    // A bounded undo/redo history of session snapshots.
    public static class UndoStack
    {
        // how many undo levels to keep (each snapshot is ~570 KB)
        public const int MAX_LEVELS = 64;

        private static readonly LinkedList<UndoSnapshot> undoStack = new LinkedList<UndoSnapshot>();
        private static readonly Stack<UndoSnapshot> redoStack = new Stack<UndoSnapshot>();

        // pre-edit snapshot for an in-progress interactive edit (slider drag /
        // text entry); committed as a single undo step when the edit finishes.
        private static UndoSnapshot pendingEdit = null;

        public static bool CanUndo { get { return undoStack.Count > 0; } }
        public static bool CanRedo { get { return redoStack.Count > 0; } }

        // call before a change: remember the current state and drop the redo trail
        public static void PushUndo()
        {
            Record(UndoSnapshot.Capture());
        }

        // step back one state. returns false if there was nothing to undo.
        public static bool Undo()
        {
            if (undoStack.Count == 0) return false;
            redoStack.Push(UndoSnapshot.Capture());
            UndoSnapshot snap = undoStack.Last.Value;
            undoStack.RemoveLast();
            snap.Restore();
            return true;
        }

        // step forward one state. returns false if there was nothing to redo.
        public static bool Redo()
        {
            if (redoStack.Count == 0) return false;
            undoStack.AddLast(UndoSnapshot.Capture());
            UndoSnapshot snap = redoStack.Pop();
            snap.Restore();
            return true;
        }

        // Begin an interactive edit: capture the state before it starts.
        // Idempotent, so it can be called on every drag/keystroke and only the
        // first call (per interaction) actually snapshots.
        public static void BeginEdit()
        {
            if (pendingEdit == null)
            {
                pendingEdit = UndoSnapshot.Capture();
            }
        }

        // Finish an interactive edit: push one undo step, but only if the state
        // actually changed (so an empty drag or focus-only visit adds nothing).
        public static void CommitEdit()
        {
            if (pendingEdit == null) return;
            if (pendingEdit.DiffersFromLive())
            {
                Record(pendingEdit);
            }
            pendingEdit = null;
        }

        // Abandon an in-progress interactive edit without recording it.
        public static void CancelEdit()
        {
            pendingEdit = null;
        }

        // forget all history (e.g. when a new session is opened)
        public static void Clear()
        {
            undoStack.Clear();
            redoStack.Clear();
            pendingEdit = null;
        }

        // push a snapshot as a new undo step, cap the depth, and clear redo
        private static void Record(UndoSnapshot snap)
        {
            undoStack.AddLast(snap);
            while (undoStack.Count > MAX_LEVELS)
            {
                undoStack.RemoveFirst(); // discard the oldest states
            }
            redoStack.Clear();
        }
    }
}
