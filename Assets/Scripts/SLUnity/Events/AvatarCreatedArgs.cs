using System;
using SLUnity.Objects;

namespace SLUnity.Events
{
    public class AvatarCreatedArgs : EventArgs
    {
        public AvatarCreatedArgs(SLAvatar Avatar)
        {
            Avatar = Avatar;
        }

        public SLAvatar Avatar { get; }
    }
}