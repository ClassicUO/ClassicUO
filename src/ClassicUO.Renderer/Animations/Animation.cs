using System;
using System.Runtime.CompilerServices;
using ClassicUO.Assets;

namespace ClassicUO.Renderer.Animations
{
    public sealed class Animation
    {
        const int MAX_ANIMATIONS_DATA_INDEX_COUNT = 2048;

        private readonly TextureAtlas _atlas;
        private readonly PixelPicker _picker = new PixelPicker();
        private IndexAnimation[] _dataIndex = new IndexAnimation[MAX_ANIMATIONS_DATA_INDEX_COUNT];
        public int MaxAnimationCount => _dataIndex.Length;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ANIMATION_GROUPS_TYPE GetAnimType(ushort graphic) => _dataIndex[graphic]?.Type ?? 0;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ANIMATION_FLAGS GetAnimFlags(ushort graphic) => _dataIndex[graphic]?.Flags ?? 0;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public sbyte GetMountedHeightOffset(ushort graphic) =>
            _dataIndex[graphic]?.MountedHeightOffset ?? 0;

        public bool PixelCheck(
            ushort animID,
            byte group,
            byte direction,
            bool uop,
            int frame,
            int x,
            int y
        )
        {
            ushort hue = 0;
            ReplaceAnimationValues(ref animID, ref group, ref hue, out var isUOP, forceUOP: uop);

            uint packed32 = (uint)((group | (direction << 8) | ((isUOP ? 0x01 : 0x00) << 16)));
            uint packed32_2 = (uint)((animID | (frame << 16)));
            ulong packed = (packed32_2 | ((ulong)packed32 << 32));

            return _picker.Get(packed, x, y);
        }

        public void GetAnimationDimensions(
            byte animIndex,
            ushort graphic,
            byte dir,
            byte animGroup,
            bool ismounted,
            byte frameIndex,
            out int centerX,
            out int centerY,
            out int width,
            out int height
        )
        {
            dir &= 0x7F;
            bool mirror = false;
            AnimationsLoader.Instance.GetAnimDirection(ref dir, ref mirror);

            if (frameIndex == 0xFF)
            {
                frameIndex = (byte)animIndex;
            }

            var frames = GetAnimationFrames(graphic, animGroup, dir, out _, out _, true);

            if (!frames.IsEmpty && frames[frameIndex].Texture != null)
            {
                centerX = frames[frameIndex].Center.X;
                centerY = frames[frameIndex].Center.Y;
                width = frames[frameIndex].UV.Width;
                height = frames[frameIndex].UV.Height;
                return;
            }

            centerX = 0;
            centerY = 0;
            width = 0;
            height = ismounted ? 100 : 60;
        }

        public Span<SpriteInfo> GetAnimationFrames(
            ushort id,
            byte action,
            byte dir,
            out ushort hue,
            out bool useUOP,
            bool isEquip = false,
            bool isCorpse = false,
            bool forceUOP = false
        )
        {
            hue = 0;
            useUOP = false;

            if (
                id >= _dataIndex.Length
                || action >= AnimationsLoader.MAX_ACTIONS
                || dir >= AnimationsLoader.MAX_DIRECTIONS
            )
            {
                return Span<SpriteInfo>.Empty;
            }

            ReplaceAnimationValues(
                ref id,
                ref action,
                ref hue,
                out useUOP,
                isEquip,
                isCorpse,
                forceUOP
            );

            IndexAnimation index = _dataIndex[id];

            if (index == null)
            {
                return Span<SpriteInfo>.Empty;
            }

            // NOTE:
            // for UOP: we don't call the method index.GetUopGroup(ref x) because the action has been already changed by the method ReplaceAnimationValues
            AnimationGroup groupObj = useUOP ? index.UopGroups?[action] : index.Groups?[action];

            if (groupObj == null)
            {
                return Span<SpriteInfo>.Empty;
            }

            ref var animDir = ref groupObj.Direction[dir];

            if (animDir.Address == -1)
            {
                return Span<SpriteInfo>.Empty;
            }

            Span<AnimationsLoader.FrameInfo> frames;

            if (animDir.FrameCount <= 0 || animDir.SpriteInfos == null)
            {
                int uopFlag = 0;

                if (useUOP
                //animDir.IsUOP ||
                ///* If it's not flagged as UOP, but there is no mul data, try to load
                //* it as a UOP anyway. */
                //(animDir.Address == 0 && animDir.Size == 0)
                )
                {
                    frames = AnimationsLoader.Instance.ReadUOPAnimationFrames(
                        id,
                        action,
                        dir,
                        index.Type,
                        index.FileIndex
                    );
                    uopFlag = 1;
                }
                else
                {
                    frames = AnimationsLoader.Instance.ReadMULAnimationFrames(id, index.FileIndex);
                }

                if (frames.IsEmpty)
                {
                    return Span<SpriteInfo>.Empty;
                }

                animDir.FrameCount = (byte)frames.Length;
                animDir.SpriteInfos = new SpriteInfo[frames.Length];

                for (int i = 0; i < frames.Length; i++)
                {
                    ref var frame = ref frames[i];
                    ref var spriteInfo = ref animDir.SpriteInfos[frame.Num];

                    if (frame.Width <= 0 || frame.Height <= 0)
                    {
                        spriteInfo = SpriteInfo.Empty;

                        /* Missing frame. */
                        continue;
                    }

                    uint keyUpper = (uint)((action | (dir << 8) | (uopFlag << 16)));
                    uint keyLower = (uint)((id | (frame.Num << 16)));
                    ulong key = (keyLower | ((ulong)keyUpper << 32));

                    _picker.Set(key, frame.Width, frame.Height, frame.Pixels);

                    spriteInfo.Center.X = frame.CenterX;
                    spriteInfo.Center.Y = frame.CenterY;
                    spriteInfo.Texture = _atlas.AddSprite(
                        frame.Pixels.AsSpan(),
                        frame.Width,
                        frame.Height,
                        out spriteInfo.UV
                    );
                }
            }

            return animDir.SpriteInfos.AsSpan(0, animDir.FrameCount);
        }

        public void UpdateAnimationTable(BodyConvFlags flags)
        {
            // if (flags != _lastFlags)
            // {
            //     if (_lastFlags != (BodyConvFlags)(-1))
            //     {
            //         /* This happens when you log out of an account then into another
            //          * one with different expansions activated. Just reload the anim
            //          * files from scratch. */
            //         Array.Clear(_dataIndex, 0, _dataIndex.Length);
            //         LoadInternal();
            //     }

            //     ProcessBodyConvDef(flags);
            // }

            //_lastFlags = flags;
        }

        public void ConvertBodyIfNeeded(
            ref ushort graphic,
            bool isParent = false,
            bool forceUOP = false
        )
        {
            if (graphic >= _dataIndex.Length)
            {
                return;
            }

            IndexAnimation dataIndex = _dataIndex[graphic];

            if (dataIndex == null)
            {
                return;
            }

            if ((dataIndex.IsUOP && (isParent || !dataIndex.IsValidMUL)) || forceUOP)
            {
                // do nothing ?
            }
            else
            {
                if (
                    dataIndex.FileIndex == 0 /*|| !dataIndex.IsValidMUL*/
                )
                {
                    graphic = dataIndex.Graphic;
                }
            }
        }

        public void ReplaceAnimationValues(
            ref ushort graphic,
            ref byte action,
            ref ushort hue,
            out bool useUOP,
            bool isEquip = false,
            bool isCorpse = false,
            bool forceUOP = false
        )
        {
            useUOP = false;

            if (graphic < _dataIndex.Length && action < AnimationsLoader.MAX_ACTIONS)
            {
                IndexAnimation index = _dataIndex[graphic];

                if (index == null)
                {
                    return;
                }

                if (forceUOP)
                {
                    index.GetUopGroup(ref action);
                    useUOP = true;
                    return;
                }

                if (index.IsUOP)
                {
                    if (!index.IsValidMUL)
                    {
                        /* Regardless of flags, there is only a UOP version so use that. */
                        index.GetUopGroup(ref action);
                        useUOP = true;
                        return;
                    }

                    /* For equipment, prefer the mul version. */
                    if (!isEquip)
                    {
                        index.GetUopGroup(ref action);
                        useUOP = true;
                        return;
                    }
                }

                // Body.def replaces animations always at fileindex == 0.
                // Bodyconv.def instead uses always fileindex >= 1 when replacing animations. So we don't need to replace the animations here. The values have been already replaced.
                // If the animation has been replaced by Body.def means it doesn't exist
                if (
                    index.FileIndex == 0 /*|| !index.IsValidMUL*/
                )
                {
                    hue = isCorpse ? index.CorpseColor : index.Color;
                    graphic = isCorpse ? index.CorpseGraphic : index.Graphic;
                }
            }
        }

        public void ApplyConversions()
        {
            var isCorpse = false;

            if (isCorpse)
                ApplyCorpse();
            else
                ApplyBody();

            ApplyBodyConv();
        }

        private void ApplyBody() { }

        private void ApplyCorpse() { }

        private void ApplyBodyConv() { }

        public bool IsAnimationExists(ushort graphic, byte group, bool isCorpse = false)
        {
            if (graphic < _dataIndex.Length && group < AnimationsLoader.MAX_ACTIONS)
            {
                var frames = GetAnimationFrames(
                    graphic,
                    group,
                    0,
                    out var _,
                    out _,
                    false,
                    isCorpse
                );

                return !frames.IsEmpty && frames[0].Texture != null;
            }

            return false;
        }

        public class IndexAnimation
        {
            private byte[] _uopReplaceGroupIndex;

            public bool IsUOP => (Flags & ANIMATION_FLAGS.AF_USE_UOP_ANIMATION) != 0;

            public ushort Graphic;
            public ushort CorpseGraphic;
            public ushort Color;
            public ushort CorpseColor;
            public byte FileIndex;
            public ANIMATION_FLAGS Flags;
            public AnimationGroup[] Groups;
            public AnimationGroupUop[] UopGroups;
            public bool IsValidMUL;
            public sbyte MountedHeightOffset;
            public ANIMATION_GROUPS_TYPE Type = ANIMATION_GROUPS_TYPE.UNKNOWN;

            public AnimationGroupUop GetUopGroup(ref byte group)
            {
                if (group < AnimationsLoader.MAX_ACTIONS && UopGroups != null)
                {
                    group = _uopReplaceGroupIndex[group];

                    return UopGroups[group];
                }

                return null;
            }

            public void InitializeUOP()
            {
                if (_uopReplaceGroupIndex == null)
                {
                    _uopReplaceGroupIndex = new byte[AnimationsLoader.MAX_ACTIONS];

                    for (byte i = 0; i < AnimationsLoader.MAX_ACTIONS; i++)
                    {
                        _uopReplaceGroupIndex[i] = i;
                    }
                }
            }

            public void ReplaceUopGroup(byte old, byte newG)
            {
                if (old < AnimationsLoader.MAX_ACTIONS && newG < AnimationsLoader.MAX_ACTIONS)
                {
                    _uopReplaceGroupIndex[old] = newG;
                }
            }
        }
    }
}
