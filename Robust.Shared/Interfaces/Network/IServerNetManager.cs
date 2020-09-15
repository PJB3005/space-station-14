﻿
using Robust.Shared.Network;

namespace Robust.Shared.Interfaces.Network
{
    /// <summary>
    /// The server version of the INetManager.
    /// </summary>
    public interface IServerNetManager : INetManager
    {
        byte[]? RsaPublicKey { get; }
        AuthMode Auth { get; }

        /// <summary>
        ///     Disconnects this channel from the remote peer.
        /// </summary>
        /// <param name="channel">NetChannel to disconnect.</param>
        /// <param name="reason">Reason why it was disconnected.</param>
        void DisconnectChannel(INetChannel channel, string reason);
    }
}
