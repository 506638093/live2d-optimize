﻿/**
 * Copyright(c) Live2D Inc. All rights reserved.
 *
 * Use of this source code is governed by the Live2D Open Software license
 * that can be found at https://www.live2d.com/eula/live2d-open-software-license-agreement_en.html.
 */


using System.Collections.Generic;
using UnityEngine;
#if OPTIMIZE_LIVE2D
using UnityEngine.AddressableAssets;
#endif
using UnityEngine.Rendering;


namespace Live2D.Cubism.Rendering.Masking
{
    /// <summary>
    /// Texture for rendering masks.
    /// </summary>
    [CreateAssetMenu(menuName = "Live2D Cubism/Mask Texture")]
    public sealed class CubismMaskTexture : ScriptableObject, ICubismMaskCommandSource
    {
        #region Conversion

        /// <summary>
        /// Converts a <see cref="CubismMaskTexture"/> to a <see cref="Texture"/>.
        /// </summary>
        /// <param name="value">Value to convert.</param>
        public static implicit operator Texture(CubismMaskTexture value)
        {
            return value.RenderTexture;
        }

        #endregion

        /// <summary>
        /// The global mask texture.
        /// </summary>
        public static CubismMaskTexture GlobalMaskTexture
        {
            get
            {
                //HuaHua
#if OPTIMIZE_LIVE2D
#if UNITY_EDITOR
                return UnityEditor.AssetDatabase.LoadAssetAtPath<CubismMaskTexture>("Assets/Thirds/Live2D/Cubism/Rendering/Res/Live2D/Cubism/GlobalMaskTexture.asset");
#else
                var handle = Addressables.LoadAssetAsync<CubismMaskTexture>("Live2D/GlobalMaskTexture.asset");
                handle.WaitForCompletion();
                return handle.Result;
#endif
#else
            return Resources.Load<CubismMaskTexture>("Live2D/Cubism/GlobalMaskTexture");
#endif
            }
        }


    /// <summary>
    /// <see cref="Size"/> backing field.
    /// </summary>
    [SerializeField, HideInInspector]

#if OPTIMIZE_LIVE2D
        private int _size = 128;
#else
        private int _size = 1024;
#endif

        /// <summary>
        /// Texture size in pixels.
        /// </summary>
        public int Size
        {
            get { return _size; }
            set
            {
                // Return early if same value given.
                if (value == _size)
                {
                    return;
                }


                // Fail silently if not power-of-two.
                if (!value.IsPowerOfTwo())
                {
                    return;
                }


                // Apply changes.
                _size = value;


                RefreshRenderTexture();
            }
        }


        /// <summary>
        /// Channel count.
        /// </summary>
        public int Channels
        {
            get { return 4; }
        }


        /// <summary>
        /// <see cref="Subdivisions"/> backing field.
        /// </summary>
        [SerializeField, HideInInspector]
        private int _subdivisions = 3;

        /// <summary>
        /// Subdivision level.
        /// </summary>
        public int Subdivisions
        {
            get { return _subdivisions; }
            set
            {
                if (value == _subdivisions)
                {
                    return;
                }


                // Apply changes.
                _subdivisions = value;


                RefreshRenderTexture();
            }
        }


        /// <summary>
        /// Tile pool 'allocator'.
        /// </summary>
        private CubismMaskTilePool TilePool { get; set; }


        /// <summary>
        /// <see cref="RenderTexture"/> backing field.
        /// </summary>
        private RenderTexture _renderTexture;

        /// <summary>
        /// <see cref="RenderTexture"/> to draw on.
        /// </summary>
        private RenderTexture RenderTexture
        {
            get
            {
                if (_renderTexture == null)
                {
                    RefreshRenderTexture();
                }


                return _renderTexture;
            }
            set { _renderTexture = value; }
        }


        /// <summary>
        /// Sources.
        /// </summary>
        private List<SourcesItem> Sources { get; set; }


        /// <summary>
        /// True if instance is revived.
        /// </summary>
        private bool IsRevived
        {
            get { return TilePool != null; }
        }

        /// <summary>
        /// True if instance contains any sources.
        /// </summary>
        private bool ContainsSources
        {
            get { return Sources != null && Sources.Count > 0; }
        }

#region Interface For ICubismMaskSources

        /// <summary>
        /// Add source of masks for drawing.
        /// </summary>
        public void AddSource(ICubismMaskTextureCommandSource source)
        {
            // Make sure instance is valid.
            TryRevive();


            // Initialize container if necessary.
            if (Sources == null)
            {
                Sources = new List<SourcesItem>();
            }


            // Return early if source already exists.
            else if (Sources.FindIndex(i => i.Source == source) != -1)
            {
                return;
            }


            // Register source.
            var item = new SourcesItem
            {
                Source = source,
                Tiles = TilePool.AcquireTiles(source.GetNecessaryTileCount())
            };


            Sources.Add(item);


            // Apply tiles to source.
            source.SetTiles(item.Tiles);
        }

        //HuaHua
#if OPTIMIZE_LIVE2D
        /// <summary>
        ///  check renderTexture no reference, delete it
        /// </summary>
        private void CheckAndReleaseRenderTexture()
        {
            if (Sources.Count > 0)
            {
                return;
            }

            if (_renderTexture != null)
            {
                _renderTexture.Release();
                _renderTexture = null;
            }
        }
#endif

        /// <summary>
        /// Remove source of masks
        /// </summary>
        public void RemoveSource(ICubismMaskTextureCommandSource source)
        {
            // Return early if empty.
            if (!ContainsSources)
            {
                return;
            }


            var itemIndex = Sources.FindIndex(i => i.Source == source);


            // Return if source is invalid.
            if (itemIndex == -1)
            {
                return;
            }


            // Return tiles and deregister source.
            TilePool.ReturnTiles(Sources[itemIndex].Tiles);
            Sources.RemoveAt(itemIndex);

            //HuaHua
#if OPTIMIZE_LIVE2D
            CheckAndReleaseRenderTexture();
#endif

        }

#endregion

        private void TryRevive()
        {
            // Return early if already revived.
            if (IsRevived)
            {
                return;
            }


            RefreshRenderTexture();
        }

        private void ReinitializeSources()
        {
            // Reallocate tiles if sources exist.
            if (ContainsSources)
            {
                for (var i = 0; i < Sources.Count; ++i)
                {
                    var source = Sources[i];


                    source.Tiles = TilePool.AcquireTiles(source.Source.GetNecessaryTileCount());

                    source.Source.SetTiles(source.Tiles);


                    Sources[i] = source;
                }
            }
        }

        private void RefreshRenderTexture()
        {
            // Recreate render texture.
            RenderTexture = new RenderTexture(Size, Size, 0, RenderTextureFormat.ARGB32);


            // Recreate allocator.
            TilePool = new CubismMaskTilePool(Subdivisions, Channels);


            // Reinitialize sources.
            ReinitializeSources();
        }

#region Unity Event Handling

        /// <summary>
        /// Initializes instance.
        /// </summary>
        // ReSharper disable once UnusedMember.Local
        private void OnEnable()
        {
            CubismMaskCommandBuffer.AddSource(this);
        }

        /// <summary>
        /// Finalizes instance.
        /// </summary>
        // ReSharper disable once UnusedMember.Local
        private void OnDestroy()
        {
            CubismMaskCommandBuffer.RemoveSource(this);
        }

#endregion

#region ICubismMaskCommandSource

        /// <summary>
        /// Called to enqueue source.
        /// </summary>
        /// <param name="buffer">Buffer to enqueue in.</param>
        void ICubismMaskCommandSource.AddToCommandBuffer(CommandBuffer buffer)
        {
            // Return early if empty.
            if (!ContainsSources)
            {
                return;
            }


            // Enqueue render target.
            buffer.SetRenderTarget(RenderTexture);
            buffer.ClearRenderTarget(false, true, Color.clear);


            // Enqueue sources.
            for (var i = 0; i < Sources.Count; ++i)
            {
                Sources[i].Source.AddToCommandBuffer(buffer);
            }
        }

#endregion

#region Source Item

        /// <summary>
        /// Source of masks and its tiles
        /// </summary>
        private struct SourcesItem
        {
            /// <summary>
            /// SourcesItem instance.
            /// </summary>
            public ICubismMaskTextureCommandSource Source;

            /// <summary>
            /// Tiles assigned to the instance.
            /// </summary>
            public CubismMaskTile[] Tiles;
        }

#endregion
    }
}
