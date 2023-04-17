using System;

namespace Recursia;
public partial class Chunk
{
    public sealed class StickyReference : IDisposable
    {
        public readonly Chunk Chunk;
        private bool disposed;
        private readonly string stacktrace;
        private int stickyIndex;
        private StickyReference(Chunk c)
        {
            Chunk = c;
            stacktrace = Environment.StackTrace;
        }
        public void Dispose()
        {
            if (disposed) {
                Godot.GD.PushWarning("attempted double dispose");
                return;
            }
            lock (Chunk._stickyLock)
            {
                Chunk.stickyCount--;
                Chunk.AddEvent($"unsticky {Chunk.stickyCount}");
                if (Chunk.stickyCount < 0) Godot.GD.PushError($"negative sticky count {Chunk.stickyCount}");
                if (Chunk.stickyCount <= 0 && Chunk.State == ChunkState.Sticky) Chunk.State = ChunkState.Loaded;
            }
            disposed = true;
            GC.SuppressFinalize(this);
        }
#if DEBUG
        ~StickyReference()
        {
            if (!disposed) Godot.GD.PushError($"NOT DISPOSED {stickyIndex}: " + stacktrace + "\nLOG: " + Chunk.GetEventHistory());
        }
#endif

        public static StickyReference Stick(Chunk c)
        {
            lock (c._stickyLock)
            {
                c.State = ChunkState.Sticky;
                c.stickyCount++;
                c.AddEvent($"sticky {c.stickyCount}");
                return new StickyReference(c){
                    stickyIndex = c.stickyCount
                };
            }
        }
    }
}