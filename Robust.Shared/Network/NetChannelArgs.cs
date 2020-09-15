﻿using System;
using System.Net;
using Robust.Shared.Interfaces.Network;

namespace Robust.Shared.Network
{
    /// <summary>
    /// Arguments for NetChannel events.
    /// </summary>
    public class NetChannelArgs : EventArgs
    {
        /// <summary>
        ///     The channel causing the event.
        /// </summary>
        public readonly INetChannel Channel;

        /// <summary>
        /// Constructs a new instance.
        /// </summary>
        /// <param name="channel">The channel causing the event.</param>
        public NetChannelArgs(INetChannel channel)
        {
            Channel = channel;
        }
    }

    /// <summary>
    /// Arguments for incoming connection event.
    /// </summary>
    public class NetConnectingArgs : EventArgs
    {
        /// <summary>
        /// If this is set to true, deny the incoming connection.
        /// </summary>
        public bool Deny { get; set; } = false;

        /// <summary>
        /// The IP of the incoming connection.
        /// </summary>
        public readonly NetUserId UserId;

        public readonly IPEndPoint IP;
        public readonly string UserName;

        /// <summary>
        /// Constructs a new instance.
        /// </summary>
        /// <param name="userId">The session ID of the incoming connection.</param>
        public NetConnectingArgs(NetUserId userId, IPEndPoint ip, string userName)
        {
            UserId = userId;
            IP = ip;
            UserName = userName;
        }
    }

    /// <summary>
    /// Arguments for a failed connection attempt.
    /// </summary>
    public class NetConnectFailArgs : EventArgs
    {
        public NetConnectFailArgs(string reason)
        {
            Reason = reason;
        }

        public string Reason { get; }
    }

    public class NetDisconnectedArgs : NetChannelArgs
    {
        public NetDisconnectedArgs(INetChannel channel, string reason) : base(channel)
        {
            Reason = reason;
        }

        public string Reason { get; }
    }
}
