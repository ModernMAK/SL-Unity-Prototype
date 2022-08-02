using System;
using Unity.Objects;

namespace Unity.Managers
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