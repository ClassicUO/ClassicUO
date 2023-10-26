using System;
using System.Runtime.CompilerServices;
using ClassicUO.Assets;
using Microsoft.Xna.Framework.Graphics;

namespace ClassicUO.Renderer.Animations
{
    public sealed class Animations
    {
        const int MAX_ANIMATIONS_DATA_INDEX_COUNT = 2048;

        private readonly TextureAtlas _atlas;
        private readonly PixelPicker _picker = new PixelPicker();
        private IndexAnimation[] _dataIndex = new IndexAnimation[MAX_ANIMATIONS_DATA_INDEX_COUNT];

        public Animations(GraphicsDevice device)
        {
            _atlas = new TextureAtlas(device, 4096, 4096, SurfaceFormat.Color);
        }

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

            uint packed32 = (uint)((group | (direction << 8) | ((uop ? 0x01 : 0x00) << 16)));
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

            var original = id;

            ref var index = ref _dataIndex[id];
            if (index == null)
            {
                index = new IndexAnimation();
                var indices = AnimationsLoader.Instance.GetIndices(ref id, ref hue, ref index.Flags, out index.FileIndex, out index.Type);
                var replaced = isCorpse ? AnimationsLoader.Instance.ReplaceCorpse(ref id, ref hue) : AnimationsLoader.Instance.ReplaceBody(ref id, ref hue);

                if (isCorpse)
                    index.CorpseGraphic = id;
                else
                    index.Graphic = id;

                if (!indices.IsEmpty)
                {
                    if (index.Flags.HasFlag(ANIMATION_FLAGS.AF_USE_UOP_ANIMATION))
                    {
                        index.UopGroups = new AnimationGroupUop[indices.Length];
                        for (int i = 0; i < index.UopGroups.Length; i++)
                        {
                            index.UopGroups[i] = new AnimationGroupUop();
                            index.UopGroups[i].FileIndex = index.FileIndex;
                            index.UopGroups[i].DecompressedLength = indices[i].Unknown;
                            index.UopGroups[i].CompressedLength = indices[i].Size;
                            index.UopGroups[i].Offset = indices[i].Position;
                        }
                    }
                    else
                    {
                        index.Groups = new AnimationGroup[indices.Length / 5];
                        for (int i = 0; i < index.Groups.Length; i++)
                        {
                            index.Groups[i] = new AnimationGroup();

                            for (int d = 0; d < 5; d++)
                            {
                                ref readonly var animIdx = ref indices[i * 5 + d];
                                index.Groups[i].Direction[d].Address = animIdx.Position;
                                index.Groups[i].Direction[d].Size = animIdx.Size;
                            }
                        }
                    }  
                }

                _dataIndex[original] = index;
                _dataIndex[id] = index;
            }

            useUOP = index.Flags.HasFlag(ANIMATION_FLAGS.AF_USE_UOP_ANIMATION);

            // NOTE:
            // for UOP: we don't call the method index.GetUopGroup(ref x) because the action has been already changed by the method ReplaceAnimationValues
            AnimationGroup groupObj = null;
            if (useUOP)
            {
                if (index.UopGroups == null || action >= index.UopGroups.Length)
                    return Span<SpriteInfo>.Empty;
                groupObj = index.UopGroups[action];
            }
            else if (index.Groups != null && action < index.Groups.Length)
            {
                groupObj = index.Groups[action];
            }

            if (groupObj == null)
            {
                return Span<SpriteInfo>.Empty;
            }

            ref var animDir = ref groupObj.Direction[dir];

            if (animDir.Address == uint.MaxValue)
            {
                return Span<SpriteInfo>.Empty;
            }

            Span<AnimationsLoader.FrameInfo> frames;

            if (animDir.FrameCount <= 0 || animDir.SpriteInfos == null)
            {
                if (useUOP
                //animDir.IsUOP ||
                ///* If it's not flagged as UOP, but there is no mul data, try to load
                //* it as a UOP anyway. */
                //(animDir.Address == 0 && animDir.Size == 0)
                )
                {
                    var uopGroupObj = (AnimationGroupUop)groupObj;
                    var ff = new AnimationsLoader.AnimIdxBlock()
                    {
                        Position = uopGroupObj.Offset,
                        Size = uopGroupObj.CompressedLength,
                        Unknown = uopGroupObj.DecompressedLength
                    };

                    frames = AnimationsLoader.Instance.ReadUOPAnimationFrames(
                        id,
                        action,
                        dir,
                        index.Type,
                        index.FileIndex,
                        ff
                    );
                }
                else
                {
                    var ff = new AnimationsLoader.AnimIdxBlock()
                    {
                        Position = groupObj.Direction[dir].Address,
                        Size = groupObj.Direction[dir].Size,
                    };

                    frames = AnimationsLoader.Instance.ReadMULAnimationFrames(index.FileIndex, ff);
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

                    uint keyUpper = (uint)((action | (dir << 8) | ((useUOP ? 1 : 0) << 16)));
                    uint keyLower = (uint)((original | (frame.Num << 16)));
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
            AnimationsLoader.Instance.ProcessBodyConvDef(flags);
            //if (flags != _lastFlags)
            //{
            //    if (_lastFlags != (BodyConvFlags)(-1))
            //    {
            //        /* This happens when you log out of an account then into another
            //         * one with different expansions activated. Just reload the anim
            //         * files from scratch. */
            //        Array.Clear(_dataIndex, 0, _dataIndex.Length);
            //        LoadInternal();
            //    }

            //    ProcessBodyConvDef(flags);
            //}

            //_lastFlags = flags;
        }

        public void ConvertBodyIfNeeded(
            ref ushort graphic,
            bool isParent = false,
            bool forceUOP = false
        )
        {
            //if (graphic >= _dataIndex.Length)
            //{
            //    return;
            //}

            //IndexAnimation dataIndex = _dataIndex[graphic];

            //if (dataIndex == null)
            //{
            //    return;
            //}

            //if ((dataIndex.IsUOP && (isParent || !dataIndex.IsValidMUL)) || forceUOP)
            //{
            //    // do nothing ?
            //}
            //else
            //{
            //    if (
            //        dataIndex.FileIndex == 0 /*|| !dataIndex.IsValidMUL*/
            //    )
            //    {
            //        graphic = dataIndex.Graphic;
            //    }
            //}
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
            public int FileIndex;
            public ANIMATION_FLAGS Flags;
            public AnimationGroup[] Groups;
            public AnimationGroupUop[] UopGroups;
            public bool IsValidMUL;
            public sbyte MountedHeightOffset;
            public ANIMATION_GROUPS_TYPE Type = ANIMATION_GROUPS_TYPE.UNKNOWN;

            public AnimationGroupUop GetUopGroup(ref byte group)
            {
                if (_uopReplaceGroupIndex != null && group < AnimationsLoader.MAX_ACTIONS && UopGroups != null)
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
