using System;
using System.Runtime.CompilerServices;
using ClassicUO.Assets;
using Microsoft.Xna.Framework.Graphics;
using static System.Collections.Specialized.BitVector32;

namespace ClassicUO.Renderer.Animations
{
    public sealed class Animations
    {
        const int MAX_ANIMATIONS_DATA_INDEX_COUNT = 2048;

        private readonly TextureAtlas _atlas;
        private readonly PixelPicker _picker = new PixelPicker();
        private readonly AnimationsLoader _animationLoader;
        private IndexAnimation[] _dataIndex = new IndexAnimation[MAX_ANIMATIONS_DATA_INDEX_COUNT];

        private AnimationDirection[][][] _cache;

        public Animations(AnimationsLoader animationLoader, GraphicsDevice device)
        {
            _animationLoader = animationLoader;
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
        public AnimationGroupsType GetAnimType(ushort graphic) => graphic < _dataIndex.Length ? _dataIndex[graphic]?.Type ?? 0 : 0;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public AnimationFlags GetAnimFlags(ushort graphic) => graphic < _dataIndex.Length ? _dataIndex[graphic]?.Flags ?? 0 : 0;

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
                _animationLoader.ReplaceUopGroup(animID, ref group);
            }

            uint packed32 = (uint)((group | (direction << 8) | ((uop ? 0x01 : 0x00) << 16)));
            uint packed32_2 = (uint)((animID | (frame << 16)));
            ulong packed = (packed32_2 | ((ulong)packed32 << 32));

            return _picker.Get(packed, x, y);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void GetAnimDirection(ref byte dir, ref bool mirror)
        {
            switch (dir)
            {
                case 2:
                case 4:
                    mirror = dir == 2;
                    dir = 1;

                    break;

                case 1:
                case 5:
                    mirror = dir == 1;
                    dir = 2;

                    break;

                case 0:
                case 6:
                    mirror = dir == 0;
                    dir = 3;

                    break;

                case 3:
                    dir = 0;

                    break;

                case 7:
                    dir = 4;

                    break;
            }
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
            GetAnimDirection(ref dir, ref mirror);

            if (frameIndex == 0xFF)
            {
                frameIndex = (byte)animIndex;
            }

            var frames = GetAnimationFrames(graphic, animGroup, dir, out _, out _, false);

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
            bool isCorpse = false
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
                    var indices = _animationLoader.GetIndices
                    (
                        _animationLoader.FileManager.Version,
                        id,
                        ref hue,
                        ref index.Flags,
                        out index.FileIndex,
                        out index.Type
                    );

                    if (!indices.IsEmpty)
                    {
                        if ((index.Flags & AnimationFlags.UseUopAnimation) != 0)
                        {
                            index.UopGroups = new AnimationGroupUop[indices.Length];
                            for (int i = 0; i < index.UopGroups.Length; i++)
                            {
                                index.UopGroups[i] = new AnimationGroupUop();
                                index.UopGroups[i].FileIndex = index.FileIndex;
                                index.UopGroups[i].DecompressedLength = indices[i].UncompressedSize;
                                index.UopGroups[i].CompressedLength = indices[i].Size;
                                index.UopGroups[i].Offset = indices[i].Position;
                                index.UopGroups[i].CompressionType = indices[i].CompressionType;
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
                    var replaced = isCorpse ? _animationLoader.ReplaceCorpse(ref id, ref hue) : _animationLoader.ReplaceBody(ref id, ref hue);
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

            useUOP = (index.Flags & AnimationFlags.UseUopAnimation) != 0;
            index.Hue = hue;

            if (useUOP)
            {
                _animationLoader.ReplaceUopGroup(id, ref action);
            }

            // When we are searching for an equipment item we must ignore any other animation which is not equipment
            var currentAnimType = GetAnimType(id);
            if (isEquip && currentAnimType != AnimationGroupsType.Equipment && currentAnimType != AnimationGroupsType.Human)
            {
                return Span<SpriteInfo>.Empty;
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

            if (animDir.FrameCount <= 0 && animDir.SpriteInfos == null)
            {
                if (useUOP
                //animDir.IsUOP ||
                ///* If it's not flagged as UOP, but there is no mul data, try to load
                //* it as a UOP anyway. */
                //(animDir.Address == 0 && animDir.Size == 0)
                )
                {
                    var uopGroupObj = (AnimationGroupUop)groupObj;
                    var ff = new AnimationsLoader.AnimationDirection()
                    {
                        Position = uopGroupObj.Offset,
                        Size = uopGroupObj.CompressedLength,
                        UncompressedSize = uopGroupObj.DecompressedLength,
                        CompressionType = uopGroupObj.CompressionType
                    };

                    frames = _animationLoader.ReadUOPAnimationFrames(
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
                    var ff = new AnimationsLoader.AnimationDirection()
                    {
                        Position = groupObj.Direction[dir].Address,
                        Size = groupObj.Direction[dir].Size,
                    };

                    frames = _animationLoader.ReadMULAnimationFrames(index.FileIndex, ff);
                }

                if (frames.IsEmpty)
                {
                    animDir.FrameCount = 0;
                    animDir.SpriteInfos = Array.Empty<SpriteInfo>();
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
            _animationLoader.ProcessBodyConvDef(flags);
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

            if (_dataIndex[graphic] != null && _dataIndex[graphic].FileIndex == 0 && (_dataIndex[graphic].Flags & AnimationFlags.UseUopAnimation) == 0)
                _ = isCorpse ? _animationLoader.ReplaceCorpse(ref graphic, ref hue) : _animationLoader.ReplaceBody(ref graphic, ref hue);
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
            public AnimationGroupsType Type = AnimationGroupsType.Unknown;
        }
    }
}
