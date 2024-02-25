using ClassicUO.Assets;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Runtime.CompilerServices;

namespace ClassicUO.Renderer.Animations
{
    public sealed class Animations
    {
        const int MAX_ANIMATIONS_DATA_INDEX_COUNT = 2048;

        private readonly TextureAtlas _atlas;
        private readonly PixelPicker _picker = new PixelPicker();
        private IndexAnimation[] _dataIndex = new IndexAnimation[MAX_ANIMATIONS_DATA_INDEX_COUNT];

        private AnimationDirection[][][] _cache;

        public Animations(GraphicsDevice device)
        {
            _atlas = new TextureAtlas(device, 4096, 4096, SurfaceFormat.Color);
        }


        private ref AnimationDirection GetSprite(int body, int action, int dir)
        {
            if (_cache == null)
                _cache = new AnimationDirection[Math.Max(body, MAX_ANIMATIONS_DATA_INDEX_COUNT)][][];

            if (body >= _cache.Length)
                Array.Resize(ref _cache, body);

            if (_cache[body] == null)
                _cache[body] = new AnimationDirection[AnimationsLoader.MAX_ACTIONS][];

            if (_cache[body][action] == null)
                _cache[body][action] = new AnimationDirection[AnimationsLoader.MAX_DIRECTIONS];

            return ref _cache[body][action][dir];
        }

        public int MaxAnimationCount => _dataIndex.Length;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public AnimationGroupsType GetAnimType(ushort graphic) => _dataIndex[graphic]?.Type ?? 0;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public AnimationFlags GetAnimFlags(ushort graphic) => _dataIndex[graphic]?.Flags ?? 0;

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
            ConvertBodyIfNeeded(ref animID);

            if (uop)
            {
                AnimationsLoader.Instance.ReplaceUopGroup(animID, ref group);
            }

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

            if (action >= AnimationsLoader.MAX_ACTIONS || dir >= AnimationsLoader.MAX_DIRECTIONS)
            {
                return Span<SpriteInfo>.Empty;
            }

            if (id >= ushort.MaxValue)
                return Span<SpriteInfo>.Empty;

            if (id >= _dataIndex.Length)
            {
                Array.Resize(ref _dataIndex, id + 1);
            }

            ref var index = ref _dataIndex[id];

            do
            {
                if (index == null)
                {
                    index = new IndexAnimation();
                    var indices = AnimationsLoader.Instance.GetIndices
                    (
                        UOFileManager.Version,
                        id,
                        ref hue,
                        ref index.Flags,
                        out index.FileIndex,
                        out index.Type,
                        out index.MountedHeightOffset
                    );

                    if (!indices.IsEmpty)
                    {
                        if (index.Flags.HasFlag(AnimationFlags.UseUopAnimation))
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
                            index.Groups = new AnimationGroup[indices.Length / AnimationsLoader.MAX_DIRECTIONS];
                            for (int i = 0; i < index.Groups.Length; i++)
                            {
                                index.Groups[i] = new AnimationGroup();

                                for (int d = 0; d < AnimationsLoader.MAX_DIRECTIONS; d++)
                                {
                                    ref readonly var animIdx = ref indices[i * AnimationsLoader.MAX_DIRECTIONS + d];
                                    index.Groups[i].Direction[d].Address = animIdx.Position;
                                    index.Groups[i].Direction[d].Size = /*index.FileIndex > 0 ? Math.Max(1, animIdx.Size) :*/ animIdx.Size;
                                }
                            }
                        }
                    }
                }

                if (index.FileIndex == 0)
                {
                    var replaced = isCorpse ? AnimationsLoader.Instance.ReplaceCorpse(ref id, ref hue) : AnimationsLoader.Instance.ReplaceBody(ref id, ref hue);
                    if (replaced)
                    {
                        if (id >= _dataIndex.Length)
                        {
                            Array.Resize(ref _dataIndex, id + 1);
                        }

                        index = ref _dataIndex[id];
                    }
                }
            } while (index == null);

            useUOP = index.Flags.HasFlag(AnimationFlags.UseUopAnimation);
            index.Hue = hue;

            if (useUOP)
            {
                AnimationsLoader.Instance.ReplaceUopGroup(id, ref action);
            }

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
            bool forceUOP = false,
            bool isCorpse = false
        )
        {
            if (graphic >= _dataIndex.Length)
                return;

            ushort hue = 0;

            if (_dataIndex[graphic] != null && _dataIndex[graphic].FileIndex == 0 && !_dataIndex[graphic].Flags.HasFlag(AnimationFlags.UseUopAnimation))
            {
                _ = isCorpse ? AnimationsLoader.Instance.ReplaceCorpse(ref graphic, ref hue) : AnimationsLoader.Instance.ReplaceBody(ref graphic, ref hue);
            }
        }

        public bool AnimationExists(ushort graphic, byte group, bool isCorpse = false)
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

        private sealed class IndexAnimation
        {
            public int FileIndex;
            public ushort Hue;
            public AnimationFlags Flags;
            public AnimationGroup[] Groups;
            public AnimationGroupUop[] UopGroups;
            public sbyte MountedHeightOffset;
            public AnimationGroupsType Type = AnimationGroupsType.Unknown;
        }
    }
}
