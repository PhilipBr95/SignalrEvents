﻿using System;

namespace Notification.NotifierLibrary
{
    [Flags]
    public enum NotifierPurpose
    {
        Receiver = 1,
        Transmitter = 2
    }
}