﻿using System;

namespace SS14.Shared.ContentPack
{
    /// <summary>
    ///     Common entry point for Content assemblies.
    /// </summary>
    public abstract class GameShared : IDisposable
    {
        public virtual void PreInit()
        {
        }

        public virtual void PostInit()
        {

        }

        public virtual void Update(AssemblyLoader.UpdateLevel level, float frameTime)
        {
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(true);
        }

        protected virtual void Dispose(bool disposing)
        {
        }

        ~GameShared()
        {
            Dispose(false);
        }
    }
}
