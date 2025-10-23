using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using DryIoc;
using Microsoft.Extensions.ObjectPool;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGame.Extended.Graphics;
using Serilog;
using SquidVox.Core.Context;
using SquidVox.Core.GameObjects;
using SquidVox.Core.Interfaces.Services;
using SquidVox.Voxel.Data.Entities;
using SquidVox.Voxel.Primitives;
using SquidVox.Voxel.Types;
using IBlockManagerService = SquidVox.Voxel.Interfaces.Services.IBlockManagerService;

namespace SquidVox.Voxel.GameObjects;

/// <summary>
/// Represents mesh data containing vertices, indices, and texture for rendering.
/// <summary>
/// Renders a single 16x64x16 chunk with greedy meshing and async mesh building.
/// </summary>
/// <summary>
/// Renders a single 16x64x16 chunk with greedy meshing and async mesh building.
/// </summary>
public sealed class ChunkGameObject : Base3dGameObject, IDisposable
{
    private static readonly BlockSide[] AllSides = BlockSideExtensions.AllSides();

    private readonly GraphicsDevice _graphicsDevice;
    private readonly IBlockManagerService _blockManagerService;
    private readonly Effect _blockEffect;
    private readonly Effect _billboardEffect;
    private readonly Effect _fluidEffect;
    private readonly ILogger _logger = Log.ForContext<ChunkGameObject>();
    private BasicEffect? _debugEffect;

    private VertexBuffer? _vertexBuffer;
    private IndexBuffer? _indexBuffer;
    private Texture2D? _texture;
    private bool _geometryInvalidated = true;
    private int _primitiveCount;

    private VertexBuffer? _billboardVertexBuffer;
    private IndexBuffer? _billboardIndexBuffer;
    private int _billboardPrimitiveCount;

    private VertexBuffer? _fluidVertexBuffer;
    private IndexBuffer? _fluidIndexBuffer;
    private int _fluidPrimitiveCount;
    private Texture3D? _lightTexture;
    private Color[]? _lightData;
    private static readonly Vector3 ChunkDimensionsVector = new(ChunkEntity.Size, ChunkEntity.Height, ChunkEntity.Size);
    private const float FluidTextureFrameMultiplier = 1f;

    private ChunkEntity? _chunk;
    private float _rotationY;
    private readonly Vector3 _chunkCenter = new(ChunkEntity.Size / 2f, ChunkEntity.Height / 2f, ChunkEntity.Size / 2f);
    private Vector3? _customCameraTarget;

    private float _opacity = 0f;
    private readonly float _targetOpacity = 1f;
    private bool _isFadingIn;

    private Task<MeshData>? _meshBuildTask;
    private MeshData? _pendingMeshData;


    // Object pools for mesh data to reduce GC pressure
    private static readonly ObjectPool<List<ChunkVertex>> _chunkVertexPool =
        ObjectPool.Create(new ChunkVertexListPolicy());

    private static readonly ObjectPool<List<VertexPositionColorTexture>> _billboardVertexPool =
        ObjectPool.Create(new BillboardVertexListPolicy());

    private static readonly ObjectPool<List<int>> _indexPool =
        ObjectPool.Create(new IndexListPolicy());

    private static readonly ObjectPool<List<VertexPositionColorTextureDirectionTop>> _fluidVertexPool =
        ObjectPool.Create(new FluidVertexListPolicy());

    // GPU upload thread for mesh data
    private static readonly ConcurrentQueue<MeshData> _gpuUploadQueue = new();
    private static Thread? _gpuUploadThread;
    private static readonly AutoResetEvent _gpuUploadSignal = new(false);


    /// <summary>
    /// Gets or sets the delegate for retrieving neighboring chunks for cross-chunk face culling.
    /// </summary>
    /// <summary>
    /// Gets or sets the delegate for retrieving neighboring chunks for cross-chunk face culling.
    /// </summary>
    public Func<Vector3, ChunkEntity?>? GetNeighborChunk { get; set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="ChunkGameObject"/> class.
    /// </summary>
    /// <param name="graphicsDevice">Graphics device used for rendering.</param>
    /// <param name="blockManagerService">Service that resolves block textures and metadata.</param>
    public ChunkGameObject()
    {
        _graphicsDevice = SquidVoxEngineContext.GraphicsDevice;
        _blockManagerService = SquidVoxEngineContext.Container.Resolve<IBlockManagerService>();

        var assetManager = SquidVoxEngineContext.Container.Resolve<IAssetManagerService>();
        _blockEffect = assetManager.GetEffect("Effects/ChunkBlock");
        _billboardEffect = assetManager.GetEffect("Effects/ChunkBillboard");
        _fluidEffect = assetManager.GetEffect("Effects/ChunkFluid");


        _debugEffect = new BasicEffect(_graphicsDevice)
        {
            VertexColorEnabled = true,
            LightingEnabled = false
        };
    }

    /// <summary>
    /// Gets the chunk currently bound to the component.
    /// </summary>
    public ChunkEntity? Chunk => _chunk;

    /// <summary>
    /// Gets or sets the manual rotation applied to the chunk (Yaw, Pitch, Roll).
    /// </summary>
    public Vector3 ManualRotation { get; set; } = Vector3.Zero;

    /// <summary>
    /// Enables a simple idle rotation animation around the Y axis.
    /// </summary>
    public bool AutoRotate { get; set; } = true;

    /// <summary>
    /// Rotation speed in radians per second when <see cref="AutoRotate"/> is enabled.
    /// </summary>
    public float RotationSpeed { get; set; } = MathHelper.ToRadians(10f);

    /// <summary>
    /// Gets or sets the camera position used when drawing the chunk.
    /// </summary>
    public Vector3 CameraPosition { get; set; } = new(35f, 45f, 35f);

    /// <summary>
    /// Gets or sets the camera target for chunk rendering.
    /// </summary>
    public Vector3 CameraTarget
    {
        get => _customCameraTarget ?? DefaultCameraTarget;
        set => _customCameraTarget = value;
    }

    private Vector3 DefaultCameraTarget => Position + _chunkCenter * BlockScale;

    /// <summary>
    /// Gets or sets the uniform block scale applied during rendering.
    /// </summary>
    public float BlockScale { get; set; } = 1f;

    /// <summary>
    /// Gets or sets whether transparent blocks (e.g. water) should be rendered.
    /// </summary>
    public bool RenderTransparentBlocks { get; set; } = false;

    /// <summary>
    /// Gets or sets whether to use greedy meshing for mesh generation.
    /// </summary>
    public bool UseGreedyMeshing { get; set; } = false;

    /// <summary>
    /// Gets or sets whether fog is enabled.
    /// </summary>
    public bool FogEnabled { get; set; } = true;

    /// <summary>
    /// Gets or sets whether textures are enabled.
    /// </summary>
    public bool TextureEnabled { get; set; } = true;

    /// <summary>
    /// Gets or sets the fog color.
    /// </summary>
    public Vector3 FogColor { get; set; } = new Vector3(0.7f, 0.8f, 0.9f);

    /// <summary>
    /// Gets or sets the distance at which fog starts.
    /// </summary>
    public float FogStart { get; set; } = 80f;

    /// <summary>
    /// Gets or sets the distance at which fog is fully opaque.
    /// </summary>
    public float FogEnd { get; set; } = 150f;

    /// <summary>
    /// Gets or sets the ambient light color.
    /// </summary>
    public Vector3 AmbientLight { get; set; } = new Vector3(0.5f, 0.5f, 0.5f);

    /// <summary>
    /// Gets or sets the directional light direction.
    /// </summary>
    public Vector3 LightDirection { get; set; } = new Vector3(0.8f, 1.0f, 0.7f);

    /// <summary>
    /// Gets or sets the speed of the fade-in animation.
    /// </summary>
    /// <summary>
    /// Gets or sets the speed of the fade-in animation.
    /// </summary>
    public float FadeInSpeed { get; set; } = 2f;

    /// <summary>
    /// Gets or sets a value indicating whether fade-in animation is enabled.
    /// </summary>
    /// <summary>
    /// Gets or sets a value indicating whether fade-in animation is enabled.
    /// </summary>
    public bool EnableFadeIn { get; set; } = true;

    /// <summary>
    /// Binds a chunk to the component and schedules a geometry rebuild.
    /// </summary>
    /// <param name="chunk">Chunk to render.</param>
    public void SetChunk(ChunkEntity chunk)
    {
        _chunk = chunk ?? throw new ArgumentNullException(nameof(chunk));
        Position = new Vector3(chunk.Position.X, chunk.Position.Y, chunk.Position.Z);
        _customCameraTarget = null; // Reset to automatic center tracking
        InvalidateGeometry();

        if (EnableFadeIn)
        {
            Opacity = 0f;
            _isFadingIn = true;
        }
        else
        {
            Opacity = 1f;
            _isFadingIn = false;
        }
    }

    /// <summary>
    /// Signals that the underlying chunk data changed and geometry needs to be recreated.
    /// </summary>
    public void InvalidateGeometry() => _geometryInvalidated = true;

    /// <summary>
    /// Gets a value indicating whether the chunk has a built mesh.
    /// </summary>
    /// <summary>
    /// Gets a value indicating whether the chunk has a built mesh.
    /// </summary>
    public bool HasMesh => _vertexBuffer != null;

    /// <summary>
    /// Starts building the mesh data asynchronously if geometry is invalidated.
    /// </summary>
    /// <summary>
    /// Starts building the mesh data asynchronously if geometry is invalidated.
    /// </summary>
    public void BuildMeshImmediate()
    {
        if (!_geometryInvalidated)
        {
            return;
        }

        if (_meshBuildTask != null && !_meshBuildTask.IsCompleted)
        {
            return;
        }

        _meshBuildTask = Task.Run(() => BuildMeshData());
    }

    private void CheckMeshBuildCompletion()
    {
        if (_meshBuildTask != null && _meshBuildTask.IsCompleted)
        {
            if (_meshBuildTask.Exception != null)
            {
                _logger.Error(_meshBuildTask.Exception, "Mesh build failed");
            }
            else
            {
                _pendingMeshData = _meshBuildTask.Result;
            }

            _meshBuildTask = null;
        }

        if (_pendingMeshData != null)
        {
            UploadMeshToGpu(_pendingMeshData);
            _pendingMeshData = null;
            _geometryInvalidated = false;

            if (EnableFadeIn && _opacity < 0.01f)
            {
                _opacity = 0f;
                _isFadingIn = true;
            }
        }
    }

    /// <summary>
    /// Updates the component state (handles optional auto rotation).
    /// </summary>
    /// <param name="gameTime">Elapsed time information.</param>
    public override void Update(GameTime gameTime)
    {
        var elapsedSeconds = (float)gameTime.ElapsedGameTime.TotalSeconds;


        _fluidEffect.Parameters["time"]?.SetValue((float)gameTime.TotalGameTime.TotalSeconds);

        CheckMeshBuildCompletion();

        if (_isFadingIn)
        {
            Opacity += FadeInSpeed * elapsedSeconds;
            if (Opacity >= _targetOpacity)
            {
                Opacity = _targetOpacity;
                _isFadingIn = false;
            }
        }

        if (AutoRotate)
        {
            _rotationY = (_rotationY + RotationSpeed * elapsedSeconds) % MathHelper.TwoPi;
        }


        base.Update(gameTime);
    }

    /// <summary>
    /// Draws the 3D chunk mesh using the component's internal camera.
    /// </summary>
    /// <param name="gameTime">Elapsed time information.</param>
    public override void Draw3d(GameTime gameTime)
    {
        Draw(gameTime);
    }

    /// <summary>
    /// Draws the chunk mesh using the configured camera and textures.
    /// </summary>
    /// <param name="gameTime">Elapsed time information.</param>
    public void Draw(GameTime gameTime)
    {
        var viewport = _graphicsDevice.Viewport;
        var aspectRatio = viewport.AspectRatio <= 0 ? 1f : viewport.AspectRatio;

        var lookTarget = _customCameraTarget ?? DefaultCameraTarget;
        var view = Matrix.CreateLookAt(CameraPosition, lookTarget, Vector3.Up);
        var projection = Matrix.CreatePerspectiveFieldOfView(MathHelper.PiOver4, aspectRatio, 0.1f, 500f);

        DrawWithCamera(gameTime, view, projection);
    }

    /// <summary>
    /// Draws the chunk mesh using external view and projection matrices.
    /// </summary>
    /// <param name="gameTime">Elapsed time information.</param>
    /// <param name="view">View matrix from camera.</param>
    /// <param name="projection">Projection matrix from camera.</param>
    public void DrawWithCamera(GameTime gameTime, Matrix view, Matrix projection)
    {
        if (_vertexBuffer == null || _indexBuffer == null || _texture == null || _primitiveCount == 0)
        {
            return;
        }

        if (Opacity <= 0f)
        {
            return;
        }

        var rotation = Matrix.CreateFromYawPitchRoll(_rotationY + ManualRotation.Y, ManualRotation.X, ManualRotation.Z);
        var world =
            Matrix.CreateTranslation(-_chunkCenter) *
            Matrix.CreateScale(BlockScale) *
            rotation *
            Matrix.CreateTranslation(_chunkCenter + Position);

        var previousBlendState = _graphicsDevice.BlendState;
        var previousDepthStencilState = _graphicsDevice.DepthStencilState;
        var previousRasterizerState = _graphicsDevice.RasterizerState;
        var previousSamplerState = _graphicsDevice.SamplerStates[0];
        var previousSamplerState1 = _graphicsDevice.SamplerStates[1];
        RasterizerState? wireframeState = null;

        var useWireframe = SquidVoxEngineContext.GraphicsDevice.RasterizerState.FillMode == FillMode.WireFrame;
        if (useWireframe)
        {
            wireframeState = new RasterizerState
            {
                FillMode = FillMode.WireFrame,
                CullMode = CullMode.None
            };
        }

        if (_chunk != null && (_lightTexture == null || _chunk.IsLightingDirty))
        {
            UpdateLightTexture();
            _chunk.IsLightingDirty = false;
        }

        _graphicsDevice.SetVertexBuffer(_vertexBuffer);
        _graphicsDevice.Indices = _indexBuffer;

        if (_blockEffect != null && _primitiveCount > 0)
        {
            var needsBlending = RenderTransparentBlocks || _opacity < 1f;

            _graphicsDevice.BlendState = needsBlending ? BlendState.AlphaBlend : BlendState.Opaque;
            _graphicsDevice.DepthStencilState = DepthStencilState.Default;
            _graphicsDevice.RasterizerState = useWireframe ? wireframeState : RasterizerState.CullNone;
            _graphicsDevice.SamplerStates[0] = SamplerState.PointClamp;
            _graphicsDevice.SamplerStates[1] = SamplerState.PointClamp;

            _blockEffect.Parameters["model"]?.SetValue(Position);
            _blockEffect.Parameters["view"]?.SetValue(view);
            _blockEffect.Parameters["projection"]?.SetValue(projection);
            _blockEffect.Parameters["tex"]?.SetValue(TextureEnabled ? _texture : SquidVoxEngineContext.WhitePixel);
            _blockEffect.Parameters["texMultiplier"]?.SetValue(1.0f);
            _blockEffect.Parameters["fogEnabled"]?.SetValue(FogEnabled);
            _blockEffect.Parameters["fogColor"]?.SetValue(FogColor);
            _blockEffect.Parameters["fogStart"]?.SetValue(FogStart);
            _blockEffect.Parameters["fogEnd"]?.SetValue(FogEnd);
            _blockEffect.Parameters["ChunkDimensions"]?.SetValue(ChunkDimensionsVector);
            if (_lightTexture != null)
            {
                _blockEffect.Parameters["BlockLightTexture"]?.SetValue(_lightTexture);
            }

            if (_blockEffect.Parameters["ambient"] != null)
            {
                _blockEffect.Parameters["ambient"].SetValue(AmbientLight);
            }
            else
            {
                _logger.Warning("ambient parameter not found in block effect");
            }

            if (_blockEffect.Parameters["lightDirection"] != null)
            {
                _blockEffect.Parameters["lightDirection"].SetValue(LightDirection);
            }
            else
            {
                _logger.Warning("lightDirection parameter not found in block effect");
            }

            foreach (var pass in _blockEffect.CurrentTechnique.Passes)
            {
                pass.Apply();
                _graphicsDevice.DrawIndexedPrimitives(PrimitiveType.TriangleList, 0, 0, _primitiveCount);
            }
        }

        if (_billboardVertexBuffer != null && _billboardIndexBuffer != null && _billboardPrimitiveCount > 0)
        {
            _graphicsDevice.BlendState = BlendState.Opaque;
            _graphicsDevice.DepthStencilState = DepthStencilState.Default;
            _graphicsDevice.RasterizerState = useWireframe ? wireframeState : new RasterizerState
            {
                CullMode = CullMode.None,
                DepthBias = -0.00001f
            };
            _graphicsDevice.SamplerStates[0] = SamplerState.PointClamp;

            _graphicsDevice.SetVertexBuffer(_billboardVertexBuffer);
            _graphicsDevice.Indices = _billboardIndexBuffer;

            if (_billboardEffect != null)
            {
                _billboardEffect.Parameters["model"]?.SetValue(Position);
                _billboardEffect.Parameters["view"]?.SetValue(view);
                _billboardEffect.Parameters["projection"]?.SetValue(projection);
                _billboardEffect.Parameters["tex"]?.SetValue(_texture);
                _billboardEffect.Parameters["texMultiplier"]?.SetValue(1.0f);
                _billboardEffect.Parameters["ambient"]?.SetValue(AmbientLight);
                _billboardEffect.Parameters["lightDirection"]?.SetValue(LightDirection);

                foreach (var pass in _billboardEffect.CurrentTechnique.Passes)
                {
                    pass.Apply();
                    _graphicsDevice.DrawIndexedPrimitives(PrimitiveType.TriangleList, 0, 0, _billboardPrimitiveCount);
                }
            }
        }

        if (_fluidVertexBuffer != null && _fluidIndexBuffer != null && _fluidPrimitiveCount > 0)
        {
            _graphicsDevice.BlendState = BlendState.AlphaBlend;
            _graphicsDevice.DepthStencilState = DepthStencilState.Default;
            _graphicsDevice.RasterizerState = useWireframe ? wireframeState : RasterizerState.CullNone;
            _graphicsDevice.SamplerStates[0] = SamplerState.PointClamp;

            _graphicsDevice.SetVertexBuffer(_fluidVertexBuffer);
            _graphicsDevice.Indices = _fluidIndexBuffer;

            if (_fluidEffect != null)
            {
                _fluidEffect.Parameters["model"]?.SetValue(Position);
                _fluidEffect.Parameters["view"]?.SetValue(view);
                _fluidEffect.Parameters["projection"]?.SetValue(projection);
                _fluidEffect.Parameters["tex"]?.SetValue(_texture);
            _fluidEffect.Parameters["texMultiplier"]?.SetValue(FluidTextureFrameMultiplier);
                _fluidEffect.Parameters["time"]?.SetValue((float)gameTime.TotalGameTime.TotalSeconds);
                _fluidEffect.Parameters["ambient"]?.SetValue(AmbientLight);
                _fluidEffect.Parameters["lightDirection"]?.SetValue(LightDirection);

                foreach (var pass in _fluidEffect.CurrentTechnique.Passes)
                {
                    pass.Apply();
                    _graphicsDevice.DrawIndexedPrimitives(PrimitiveType.TriangleList, 0, 0, _fluidPrimitiveCount);
                }
            }
        }

        _graphicsDevice.SetVertexBuffer(null);
        _graphicsDevice.Indices = null;

        _graphicsDevice.BlendState = previousBlendState;
        _graphicsDevice.DepthStencilState = previousDepthStencilState;
        _graphicsDevice.RasterizerState = previousRasterizerState;
        _graphicsDevice.SamplerStates[0] = previousSamplerState;
        _graphicsDevice.SamplerStates[1] = previousSamplerState1;
        wireframeState?.Dispose();
    }

    /// <inheritdoc />
    public void Dispose()
    {
        ClearGeometry();
        _debugEffect?.Dispose();
        _lightTexture?.Dispose();
        _lightTexture = null;
        _lightData = null;
    }

    private MeshData BuildMeshData()
    {
        if (_chunk == null)
        {
            return new MeshData();
        }

        var vertices = _chunkVertexPool.Get();
        var indices = _indexPool.Get();
        var billboardVertices = _billboardVertexPool.Get();
        var billboardIndices = _indexPool.Get();
        var fluidVertices = _fluidVertexPool.Get();
        var fluidIndices = _indexPool.Get();
        Texture2D? atlasTexture = null;

        for (int x = 0; x < ChunkEntity.Size; x++)
        {
            for (int y = 0; y < ChunkEntity.Height; y++)
            {
                for (int z = 0; z < ChunkEntity.Size; z++)
                {
                    var block = _chunk.Blocks[ChunkEntity.GetIndex(x, y, z)];

                    if (block == null || block.BlockType == BlockType.Air)
                    {
                        continue;
                    }

                    var definition = _blockManagerService.GetBlockDefinition(block.BlockType);

                    if (definition == null)
                    {
                        continue;
                    }

                    if (definition.IsTransparent && !RenderTransparentBlocks)
                    {
                        continue;
                    }

                    var blockHeight = definition.Height;

                    if (definition.IsBillboard)
                    {
                        var region = _blockManagerService.GetBlockSide(block.BlockType, BlockSide.North);
                        if (region != null)
                        {
                            atlasTexture ??= region.Texture;
                            var uv = ExtractUv(region);
                            var faceColor = CalculateFaceColor(x, y, z, BlockSide.Top);

                            var billboardVerts = GetBillboardVertices(x, y, z, uv, faceColor, blockHeight);
                            var baseIndex = billboardVertices.Count;

                            billboardVertices.AddRange(billboardVerts);

                            billboardIndices.AddRange(
                                new[]
                                {
                                    baseIndex, baseIndex + 1, baseIndex + 2,
                                    baseIndex + 2, baseIndex + 3, baseIndex,

                                    baseIndex + 4, baseIndex + 5, baseIndex + 6,
                                    baseIndex + 6, baseIndex + 7, baseIndex + 4
                                }
                            );
                        }
                    }
                    else if (definition.IsLiquid)
                    {
                        foreach (var side in AllSides)
                        {
                            if (!ShouldRenderFace(x, y, z, side))
                            {
                                continue;
                            }

                            var region = _blockManagerService.GetBlockSide(block.BlockType, side);

                            if (region == null)
                            {
                                continue;
                            }

                            atlasTexture ??= region.Texture;

                            var uv = ExtractUv(region);
                            var faceColor = CalculateFaceColor(x, y, z, side);
                            var faceVertices = GetFluidFaceVertices(side, x, y, z, uv, faceColor, blockHeight);

                            var baseIndex = fluidVertices.Count;

                            var direction = GetDirectionIndex(side);
                            var isTop = side == BlockSide.Top ? 1.0f : 0.0f;

                            foreach (var vertex in faceVertices)
                            {
                                fluidVertices.Add(
                                    new VertexPositionColorTextureDirectionTop(
                                        vertex.Position,
                                        vertex.Color,
                                        vertex.TextureCoordinate,
                                        direction,
                                        isTop
                                    )
                                );
                            }

                            fluidIndices.AddRange(
                                new[]
                                {
                                    baseIndex,
                                    baseIndex + 1,
                                    baseIndex + 2,
                                    baseIndex + 2,
                                    baseIndex + 3,
                                    baseIndex
                                }
                            );
                        }
                    }
                    else
                    {
                        if (!UseGreedyMeshing)
                        {
                            foreach (var side in AllSides)
                            {
                                if (!ShouldRenderFace(x, y, z, side))
                                {
                                    continue;
                                }

                                var region = _blockManagerService.GetBlockSide(block.BlockType, side);

                                if (region == null)
                                {
                                    continue;
                                }

                                atlasTexture ??= region.Texture;

                                var uv = ExtractUv(region);
                                var faceColor = CalculateFaceColor(x, y, z, side);
                                var faceVertices = GetFaceVertices(side, x, y, z, uv, faceColor, blockHeight);

                                var baseIndex = vertices.Count;
                                vertices.AddRange(faceVertices);
                                indices.AddRange(
                                    new[]
                                    {
                                        baseIndex,
                                        baseIndex + 1,
                                        baseIndex + 2,
                                        baseIndex + 2,
                                        baseIndex + 3,
                                        baseIndex
                                    }
                                );
                            }
                        }
                    }
                }
            }
        }

        if (UseGreedyMeshing)
        {
            GenerateGreedySolidFaces(vertices, indices, ref atlasTexture);
        }

        var meshData = new MeshData
        {
            Vertices = vertices.ToArray(),
            Indices = indices.ToArray(),
            BillboardVertices = billboardVertices.ToArray(),
            BillboardIndices = billboardIndices.ToArray(),
            FluidVertices = fluidVertices.ToArray(),
            FluidIndices = fluidIndices.ToArray(),
            Texture = atlasTexture
        };

        _logger.Debug(
            "Mesh built: {SolidVerts} solid, {BillboardVerts} billboard, {FluidVerts} fluid vertices",
            vertices.Count,
            billboardVertices.Count,
            fluidVertices.Count
        );

        _chunkVertexPool.Return(vertices);
        _indexPool.Return(indices);
        _billboardVertexPool.Return(billboardVertices);
        _indexPool.Return(billboardIndices);
        _fluidVertexPool.Return(fluidVertices);
        _indexPool.Return(fluidIndices);

        return meshData;
    }

    private void UploadMeshToGpu(MeshData meshData)
    {
        ClearGeometry();

        if (meshData.Texture == null)
        {
            return;
        }

        if (meshData.Vertices.Length > 0 && meshData.Indices.Length > 0)
        {
            _vertexBuffer = new VertexBuffer(
                _graphicsDevice,
                typeof(ChunkVertex),
                meshData.Vertices.Length,
                BufferUsage.WriteOnly
            );
            _vertexBuffer.SetData(meshData.Vertices);

            _indexBuffer = new IndexBuffer(
                _graphicsDevice,
                IndexElementSize.ThirtyTwoBits,
                meshData.Indices.Length,
                BufferUsage.WriteOnly
            );
            _indexBuffer.SetData(meshData.Indices);

            _primitiveCount = meshData.Indices.Length / 3;
        }

        if (meshData.BillboardVertices.Length > 0 && meshData.BillboardIndices.Length > 0)
        {
            _billboardVertexBuffer = new VertexBuffer(
                _graphicsDevice,
                typeof(VertexPositionColorTexture),
                meshData.BillboardVertices.Length,
                BufferUsage.WriteOnly
            );
            _billboardVertexBuffer.SetData(meshData.BillboardVertices);

            _billboardIndexBuffer = new IndexBuffer(
                _graphicsDevice,
                IndexElementSize.ThirtyTwoBits,
                meshData.BillboardIndices.Length,
                BufferUsage.WriteOnly
            );
            _billboardIndexBuffer.SetData(meshData.BillboardIndices);

            _billboardPrimitiveCount = meshData.BillboardIndices.Length / 3;
        }

        if (meshData.FluidVertices.Length > 0 && meshData.FluidIndices.Length > 0)
        {
            _fluidVertexBuffer = new VertexBuffer(
                _graphicsDevice,
                typeof(VertexPositionColorTextureDirectionTop),
                meshData.FluidVertices.Length,
                BufferUsage.WriteOnly
            );
            _fluidVertexBuffer.SetData(meshData.FluidVertices);

            _fluidIndexBuffer = new IndexBuffer(
                _graphicsDevice,
                IndexElementSize.ThirtyTwoBits,
                meshData.FluidIndices.Length,
                BufferUsage.WriteOnly
            );
            _fluidIndexBuffer.SetData(meshData.FluidIndices);

            _fluidPrimitiveCount = meshData.FluidIndices.Length / 3;
        }

        _texture = meshData.Texture;

        _logger.Information(
            "Chunk mesh uploaded: {Vertices} solid ({Faces} faces), {BillboardVerts} billboard ({BillboardFaces} faces), {FluidVerts} fluid ({FluidFaces} faces)",
            meshData.Vertices.Length,
            meshData.Indices.Length / 3,
            meshData.BillboardVertices.Length,
            meshData.BillboardIndices.Length / 3,
            meshData.FluidVertices.Length,
            meshData.FluidIndices.Length / 3
        );
    }

    private bool ShouldRenderFace(int x, int y, int z, BlockSide side)
    {
        var currentBlock = _chunk!.Blocks[ChunkEntity.GetIndex(x, y, z)];
        if (currentBlock == null)
        {
            return false;
        }

        var (offsetX, offsetY, offsetZ) = BlockSideExtensions.NeighborOffsets[side];
        var neighborX = x + offsetX;
        var neighborY = y + offsetY;
        var neighborZ = z + offsetZ;

        if (!IsWithinChunk(neighborX, neighborY, neighborZ))
        {
            return ShouldRenderCrossChunkFace(x, y, z, side, currentBlock.BlockType);
        }

        var neighbor = _chunk!.Blocks[ChunkEntity.GetIndex(neighborX, neighborY, neighborZ)];

        if (neighbor == null || neighbor.BlockType == BlockType.Air)
        {
            return true;
        }

        var neighborDefinition = _blockManagerService.GetBlockDefinition(neighbor.BlockType);
        if (neighborDefinition != null && neighborDefinition.IsBillboard)
        {
            return true;
        }

        if (currentBlock.BlockType == neighbor.BlockType)
        {
            var currentDef = _blockManagerService.GetBlockDefinition(currentBlock.BlockType);
            if (currentDef != null && currentDef.IsLiquid)
            {
                return false;
            }
        }

        var currentBlockDef = _blockManagerService.GetBlockDefinition(currentBlock.BlockType);
        if (currentBlockDef != null && currentBlockDef.IsLiquid)
        {
            var neighborDef = _blockManagerService.GetBlockDefinition(neighbor.BlockType);
            if (neighborDef != null && !neighborDef.IsTransparent)
            {
                return false;
            }
        }

        return _blockManagerService.IsTransparent(neighbor.BlockType);
    }

    private bool ShouldRenderCrossChunkFace(int x, int y, int z, BlockSide side, BlockType currentBlockType)
    {
        if (GetNeighborChunk == null || _chunk == null)
        {
            return true;
        }

        var (offsetX, offsetY, offsetZ) = BlockSideExtensions.NeighborOffsets[side];
        var worldX = _chunk.Position.X + x + offsetX;
        var worldY = _chunk.Position.Y + y + offsetY;
        var worldZ = _chunk.Position.Z + z + offsetZ;

        var neighborChunkX = MathF.Floor(worldX / ChunkEntity.Size) * ChunkEntity.Size;
        var neighborChunkZ = MathF.Floor(worldZ / ChunkEntity.Size) * ChunkEntity.Size;
        var neighborChunkPos = new Vector3(neighborChunkX, 0f, neighborChunkZ);

        var neighborChunk = GetNeighborChunk(neighborChunkPos);
        if (neighborChunk == null)
        {
            return true;
        }

        var localX = (int)(worldX - neighborChunk.Position.X);
        var localY = (int)(worldY - neighborChunk.Position.Y);
        var localZ = (int)(worldZ - neighborChunk.Position.Z);

        if (localX < 0 || localX >= ChunkEntity.Size ||
            localY < 0 || localY >= ChunkEntity.Height ||
            localZ < 0 || localZ >= ChunkEntity.Size)
        {
            return true;
        }

        var neighborBlock = neighborChunk.GetBlock(localX, localY, localZ);

        if (neighborBlock == null || neighborBlock.BlockType == BlockType.Air)
        {
            return true;
        }

        if (currentBlockType == neighborBlock.BlockType)
        {
            var currentDef = _blockManagerService.GetBlockDefinition(currentBlockType);
            if (currentDef != null && currentDef.IsLiquid)
            {
                return false;
            }
        }

        return _blockManagerService.IsTransparent(neighborBlock.BlockType);
    }

    private static bool IsWithinChunk(int x, int y, int z)
    {
        return x >= 0 && x < ChunkEntity.Size &&
               y >= 0 && y < ChunkEntity.Height &&
               z >= 0 && z < ChunkEntity.Size;
    }

    private static float GetDirectionIndex(BlockSide side)
    {
        return side switch
        {
            BlockSide.South => 0,
            BlockSide.North => 1,
            BlockSide.East => 2,
            BlockSide.West => 3,
            BlockSide.Top => 4,
            BlockSide.Bottom => 5,
            _ => 6
        };
    }

    private Color CalculateFaceColor(int x, int y, int z, BlockSide side)
    {
        var ambientOcclusion = 1.0f;
        var faceNormal = Vector3.Zero;

        switch (side)
        {
            case BlockSide.Top:
                ambientOcclusion = 1.0f;
                faceNormal = Vector3.Up;
                break;
            case BlockSide.Bottom:
                ambientOcclusion = 0.5f;
                faceNormal = Vector3.Down;
                break;
            case BlockSide.North:
                ambientOcclusion = 0.8f;
                faceNormal = Vector3.Backward;
                break;
            case BlockSide.South:
                ambientOcclusion = 0.8f;
                faceNormal = Vector3.Forward;
                break;
            case BlockSide.East:
                ambientOcclusion = 0.75f;
                faceNormal = Vector3.Right;
                break;
            case BlockSide.West:
                ambientOcclusion = 0.75f;
                faceNormal = Vector3.Left;
                break;
        }

        var lightLevel = 0.2f;
        var lightColor = Vector3.One;

        if (_chunk != null && _chunk.IsInBounds(x, y, z))
        {
            var block = _chunk.GetBlock(x, y, z);
            if (block != null)
            {
                var rawLight = block.LightLevel;
                lightLevel = Math.Max(0.2f, rawLight / 15f);
                lightColor = block.LightColor;
            }
        }

        var finalBrightness = ambientOcclusion * lightLevel;
        finalBrightness = Math.Max(0.1f, finalBrightness);

        var colorR = (byte)(finalBrightness * lightColor.X * 255);
        var colorG = (byte)(finalBrightness * lightColor.Y * 255);
        var colorB = (byte)(finalBrightness * lightColor.Z * 255);

        // Apply dynamic shadows based on sun direction
        // var shadowFactor = 1.0f;
        // if (_dayNightCycle != null)
        // {
        //     var sunDirection = _dayNightCycle.GetSunDirection();
        //     // Normalize the sun direction
        //     sunDirection = Vector3.Normalize(sunDirection);

        //     // Dot product between face normal and sun direction
        //     // Positive values mean face is facing toward sun (more light)
        //     // Negative values mean face is facing away from sun (less light)
        //     var dotProduct = Vector3.Dot(faceNormal, sunDirection);

        //     // Map from [-1,1] to [0.2,1.0] for very visible shadows
        //     shadowFactor = Math.Max(0.2f, (dotProduct + 1.0f) * 0.4f);

        //     // Make shadows more pronounced during day
        //     var currentSunIntensity = _dayNightCycle.GetSunIntensity();
        //     shadowFactor = MathHelper.Lerp(0.8f, shadowFactor, currentSunIntensity);
        // }

        // finalBrightness *= shadowFactor;

        // Apply sun color and intensity
        // var sunColor = Color.White;
        // var sunIntensity = 1.0f;

        // if (_dayNightCycle != null)
        // {
        //     sunColor = _dayNightCycle.GetSunColor();
        //     sunIntensity = _dayNightCycle.GetSunIntensity();
        // }

        return new Color(colorR, colorG, colorB, (byte)255);
    }

    private static ChunkVertex[] GetFaceVertices(
        BlockSide side, int blockX, int blockY, int blockZ, (Vector2 Min, Vector2 Max) uv, Color color, float height = 1.0f
    )
    {
        var (min, max) = uv;
        float x = blockX;
        float y = blockY;
        float z = blockZ;
        float x1 = blockX + 1f;
        float y1 = blockY + height;
        float z1 = blockZ + 1f;

        var direction = (byte)GetDirectionIndex(side);
        var colorWithDir = new Color(color.R, color.G, color.B, direction);

        var tileBase = min;
        var tileSize = max - min;

        static ChunkVertex CreateVertex(Vector3 position, Vector2 tileCoord, Vector2 tileBase, Vector2 tileSize, Color color, Vector3 blockCoord)
        {
            return new ChunkVertex(position, color, tileCoord, tileBase, tileSize, blockCoord);
        }

        return side switch
        {
            BlockSide.Top => new[]
            {
                CreateVertex(new Vector3(x, y1, z), new Vector2(0f, 0f), tileBase, tileSize, colorWithDir, new Vector3(blockX, blockY, blockZ)),
                CreateVertex(new Vector3(x, y1, z1), new Vector2(0f, 1f), tileBase, tileSize, colorWithDir, new Vector3(blockX, blockY, blockZ + 1)),
                CreateVertex(new Vector3(x1, y1, z1), new Vector2(1f, 1f), tileBase, tileSize, colorWithDir, new Vector3(blockX + 1, blockY, blockZ + 1)),
                CreateVertex(new Vector3(x1, y1, z), new Vector2(1f, 0f), tileBase, tileSize, colorWithDir, new Vector3(blockX + 1, blockY, blockZ))
            },
            BlockSide.Bottom => new[]
            {
                CreateVertex(new Vector3(x, y, z1), new Vector2(0f, 0f), tileBase, tileSize, colorWithDir, new Vector3(blockX, blockY, blockZ + 1)),
                CreateVertex(new Vector3(x, y, z), new Vector2(0f, 1f), tileBase, tileSize, colorWithDir, new Vector3(blockX, blockY, blockZ)),
                CreateVertex(new Vector3(x1, y, z), new Vector2(1f, 1f), tileBase, tileSize, colorWithDir, new Vector3(blockX + 1, blockY, blockZ)),
                CreateVertex(new Vector3(x1, y, z1), new Vector2(1f, 0f), tileBase, tileSize, colorWithDir, new Vector3(blockX + 1, blockY, blockZ + 1))
            },
            BlockSide.North => new[]
            {
                CreateVertex(new Vector3(x, y1, z), new Vector2(0f, 0f), tileBase, tileSize, colorWithDir, new Vector3(blockX, blockY + height, blockZ)),
                CreateVertex(new Vector3(x, y, z), new Vector2(0f, 1f), tileBase, tileSize, colorWithDir, new Vector3(blockX, blockY, blockZ)),
                CreateVertex(new Vector3(x1, y, z), new Vector2(1f, 1f), tileBase, tileSize, colorWithDir, new Vector3(blockX + 1, blockY, blockZ)),
                CreateVertex(new Vector3(x1, y1, z), new Vector2(1f, 0f), tileBase, tileSize, colorWithDir, new Vector3(blockX + 1, blockY + height, blockZ))
            },
            BlockSide.South => new[]
            {
                CreateVertex(new Vector3(x1, y1, z1), new Vector2(0f, 0f), tileBase, tileSize, colorWithDir, new Vector3(blockX + 1, blockY + height, blockZ + 1)),
                CreateVertex(new Vector3(x1, y, z1), new Vector2(0f, 1f), tileBase, tileSize, colorWithDir, new Vector3(blockX + 1, blockY, blockZ + 1)),
                CreateVertex(new Vector3(x, y, z1), new Vector2(1f, 1f), tileBase, tileSize, colorWithDir, new Vector3(blockX, blockY, blockZ + 1)),
                CreateVertex(new Vector3(x, y1, z1), new Vector2(1f, 0f), tileBase, tileSize, colorWithDir, new Vector3(blockX, blockY + height, blockZ + 1))
            },
            BlockSide.East => new[]
            {
                CreateVertex(new Vector3(x1, y1, z), new Vector2(0f, 0f), tileBase, tileSize, colorWithDir, new Vector3(blockX + 1, blockY + height, blockZ)),
                CreateVertex(new Vector3(x1, y, z), new Vector2(0f, 1f), tileBase, tileSize, colorWithDir, new Vector3(blockX + 1, blockY, blockZ)),
                CreateVertex(new Vector3(x1, y, z1), new Vector2(1f, 1f), tileBase, tileSize, colorWithDir, new Vector3(blockX + 1, blockY, blockZ + 1)),
                CreateVertex(new Vector3(x1, y1, z1), new Vector2(1f, 0f), tileBase, tileSize, colorWithDir, new Vector3(blockX + 1, blockY + height, blockZ + 1))
            },
            BlockSide.West => new[]
            {
                CreateVertex(new Vector3(x, y1, z1), new Vector2(0f, 0f), tileBase, tileSize, colorWithDir, new Vector3(blockX, blockY + height, blockZ + 1)),
                CreateVertex(new Vector3(x, y, z1), new Vector2(0f, 1f), tileBase, tileSize, colorWithDir, new Vector3(blockX, blockY, blockZ + 1)),
                CreateVertex(new Vector3(x, y, z), new Vector2(1f, 1f), tileBase, tileSize, colorWithDir, new Vector3(blockX, blockY, blockZ)),
                CreateVertex(new Vector3(x, y1, z), new Vector2(1f, 0f), tileBase, tileSize, colorWithDir, new Vector3(blockX, blockY + height, blockZ))
            },
            _ => throw new ArgumentOutOfRangeException(nameof(side), side, "Unsupported side type")
        };
    }

    private static VertexPositionColorTexture[] GetFluidFaceVertices(
        BlockSide side, int blockX, int blockY, int blockZ, (Vector2 Min, Vector2 Max) uv, Color color, float height = 1.0f
    )
    {
        var (min, max) = uv;
        float x = blockX;
        float y = blockY;
        float z = blockZ;
        float x1 = blockX + 1f;
        float y1 = blockY + height;
        float z1 = blockZ + 1f;

        return side switch
        {
            BlockSide.Top => new[]
            {
                new VertexPositionColorTexture(new Vector3(x, y1, z), color, new Vector2(min.X, min.Y)),
                new VertexPositionColorTexture(new Vector3(x, y1, z1), color, new Vector2(min.X, max.Y)),
                new VertexPositionColorTexture(new Vector3(x1, y1, z1), color, new Vector2(max.X, max.Y)),
                new VertexPositionColorTexture(new Vector3(x1, y1, z), color, new Vector2(max.X, min.Y))
            },
            BlockSide.Bottom => new[]
            {
                new VertexPositionColorTexture(new Vector3(x, y, z1), color, new Vector2(min.X, min.Y)),
                new VertexPositionColorTexture(new Vector3(x, y, z), color, new Vector2(min.X, max.Y)),
                new VertexPositionColorTexture(new Vector3(x1, y, z), color, new Vector2(max.X, max.Y)),
                new VertexPositionColorTexture(new Vector3(x1, y, z1), color, new Vector2(max.X, min.Y))
            },
            BlockSide.North => new[]
            {
                new VertexPositionColorTexture(new Vector3(x, y1, z), color, new Vector2(min.X, min.Y)),
                new VertexPositionColorTexture(new Vector3(x, y, z), color, new Vector2(min.X, max.Y)),
                new VertexPositionColorTexture(new Vector3(x1, y, z), color, new Vector2(max.X, max.Y)),
                new VertexPositionColorTexture(new Vector3(x1, y1, z), color, new Vector2(max.X, min.Y))
            },
            BlockSide.South => new[]
            {
                new VertexPositionColorTexture(new Vector3(x1, y1, z1), color, new Vector2(min.X, min.Y)),
                new VertexPositionColorTexture(new Vector3(x1, y, z1), color, new Vector2(min.X, max.Y)),
                new VertexPositionColorTexture(new Vector3(x, y, z1), color, new Vector2(max.X, max.Y)),
                new VertexPositionColorTexture(new Vector3(x, y1, z1), color, new Vector2(max.X, min.Y))
            },
            BlockSide.East => new[]
            {
                new VertexPositionColorTexture(new Vector3(x1, y1, z), color, new Vector2(min.X, min.Y)),
                new VertexPositionColorTexture(new Vector3(x1, y, z), color, new Vector2(min.X, max.Y)),
                new VertexPositionColorTexture(new Vector3(x1, y, z1), color, new Vector2(max.X, max.Y)),
                new VertexPositionColorTexture(new Vector3(x1, y1, z1), color, new Vector2(max.X, min.Y))
            },
            BlockSide.West => new[]
            {
                new VertexPositionColorTexture(new Vector3(x, y1, z1), color, new Vector2(min.X, min.Y)),
                new VertexPositionColorTexture(new Vector3(x, y, z1), color, new Vector2(min.X, max.Y)),
                new VertexPositionColorTexture(new Vector3(x, y, z), color, new Vector2(max.X, max.Y)),
                new VertexPositionColorTexture(new Vector3(x, y1, z), color, new Vector2(max.X, min.Y))
            },
            _ => throw new ArgumentOutOfRangeException(nameof(side), side, "Unsupported side type")
        };
    }

    private static VertexPositionColorTexture[] GetBillboardVertices(
        int blockX, int blockY, int blockZ, (Vector2 Min, Vector2 Max) uv, Color color, float height = 1.0f
    )
    {
        var (min, max) = uv;
        float x = blockX + 0.5f;
        float yBottom = blockY;
        float z = blockZ + 0.5f;
        float yTop = blockY + height;

        const float offset = 0.4f;

        return new[]
        {
            new VertexPositionColorTexture(new Vector3(x - offset, yTop, z - offset), color, new Vector2(min.X, min.Y)),
            new VertexPositionColorTexture(new Vector3(x - offset, yBottom, z - offset), color, new Vector2(min.X, max.Y)),
            new VertexPositionColorTexture(new Vector3(x + offset, yBottom, z + offset), color, new Vector2(max.X, max.Y)),
            new VertexPositionColorTexture(new Vector3(x + offset, yTop, z + offset), color, new Vector2(max.X, min.Y)),

            new VertexPositionColorTexture(new Vector3(x + offset, yTop, z - offset), color, new Vector2(min.X, min.Y)),
            new VertexPositionColorTexture(new Vector3(x + offset, yBottom, z - offset), color, new Vector2(min.X, max.Y)),
            new VertexPositionColorTexture(new Vector3(x - offset, yBottom, z + offset), color, new Vector2(max.X, max.Y)),
            new VertexPositionColorTexture(new Vector3(x - offset, yTop, z + offset), color, new Vector2(max.X, min.Y))
        };
    }

    private static (Vector2 Min, Vector2 Max) ExtractUv(Texture2DRegion region)
    {
        var texture = region.Texture;
        var bounds = region.Bounds;

        const float inset = 0.001f;

        var minX = (bounds.X + inset) / texture.Width;
        var minY = (bounds.Y + inset) / texture.Height;
        var maxX = (bounds.X + bounds.Width - inset) / texture.Width;
        var maxY = (bounds.Y + bounds.Height - inset) / texture.Height;

        return (new Vector2(minX, minY), new Vector2(maxX, maxY));
    }


    private void ClearGeometry()
    {
        _vertexBuffer?.Dispose();
        _vertexBuffer = null;

        _indexBuffer?.Dispose();
        _indexBuffer = null;

        _billboardVertexBuffer?.Dispose();
        _billboardVertexBuffer = null;

        _billboardIndexBuffer?.Dispose();
        _billboardIndexBuffer = null;

        _fluidVertexBuffer?.Dispose();
        _fluidVertexBuffer = null;

        _fluidIndexBuffer?.Dispose();
        _fluidIndexBuffer = null;

        _texture = null;
        _primitiveCount = 0;
        _billboardPrimitiveCount = 0;
        _fluidPrimitiveCount = 0;
    }

    private void GenerateGreedySolidFaces(
        List<ChunkVertex> vertices,
        List<int> indices,
        ref Texture2D? atlasTexture)
    {
        if (_chunk == null)
        {
            return;
        }

        GenerateHorizontalGreedyFaces(BlockSide.Top, vertices, indices, ref atlasTexture);
        GenerateHorizontalGreedyFaces(BlockSide.Bottom, vertices, indices, ref atlasTexture);
        GenerateVerticalGreedyFaces(BlockSide.North, vertices, indices, ref atlasTexture);
        GenerateVerticalGreedyFaces(BlockSide.South, vertices, indices, ref atlasTexture);
        GenerateVerticalGreedyFaces(BlockSide.East, vertices, indices, ref atlasTexture);
        GenerateVerticalGreedyFaces(BlockSide.West, vertices, indices, ref atlasTexture);
    }

    private void GenerateHorizontalGreedyFaces(
        BlockSide side,
        List<ChunkVertex> vertices,
        List<int> indices,
        ref Texture2D? atlasTexture)
    {
        var mask = new GreedyCell?[ChunkEntity.Size, ChunkEntity.Size];
        var minHeight = 0;
        var maxHeight = ChunkEntity.Height;

        for (int y = minHeight; y < maxHeight; y++)
        {
            for (int z = 0; z < ChunkEntity.Size; z++)
            {
                for (int x = 0; x < ChunkEntity.Size; x++)
                {
                    mask[x, z] = null;

                    if (!ShouldRenderFace(x, y, z, side))
                    {
                        continue;
                    }

                    var block = _chunk.GetBlock(x, y, z);
                    if (block == null)
                    {
                        continue;
                    }

                    var definition = _blockManagerService.GetBlockDefinition(block.BlockType);
                    if (definition == null || definition.IsBillboard || definition.IsLiquid)
                    {
                        continue;
                    }

                    if (Math.Abs(definition.Height - 1f) > 0.0001f)
                    {
                        AddSolidFace(side, x, y, z, block.BlockType, definition, vertices, indices, ref atlasTexture);
                        continue;
                    }

                    var region = _blockManagerService.GetBlockSide(block.BlockType, side);
                    if (region == null)
                    {
                        continue;
                    }

                    atlasTexture ??= region.Texture;

                    var uv = ExtractUv(region);
                    var color = CalculateFaceColor(x, y, z, side);

                    mask[x, z] = new GreedyCell(block.BlockType, color, uv.Min, uv.Max, definition.Height);
                }
            }

            ProcessHorizontalMask(side, y, mask, vertices, indices);
        }
    }

    private void ProcessHorizontalMask(
        BlockSide side,
        int y,
        GreedyCell?[,] mask,
        List<ChunkVertex> vertices,
        List<int> indices)
    {
        for (int z = 0; z < ChunkEntity.Size; z++)
        {
            int x = 0;
            while (x < ChunkEntity.Size)
            {
                var cell = mask[x, z];
                if (cell == null)
                {
                    x++;
                    continue;
                }

                int width = 1;
                while (x + width < ChunkEntity.Size && mask[x + width, z]?.Equals(cell) == true)
                {
                    width++;
                }

                int height = 1;
                bool done = false;
                while (z + height < ChunkEntity.Size && !done)
                {
                    for (int dx = 0; dx < width; dx++)
                    {
                        if (mask[x + dx, z + height]?.Equals(cell) != true)
                        {
                            done = true;
                            break;
                        }
                    }

                    if (!done)
                    {
                        height++;
                    }
                }

                AddHorizontalQuad(side, y, x, z, width, height, cell.Value, vertices, indices);

                for (int dz = 0; dz < height; dz++)
                {
                    for (int dx = 0; dx < width; dx++)
                    {
                        mask[x + dx, z + dz] = null;
                    }
                }

                x += width;
            }
        }
    }

    private void AddHorizontalQuad(
        BlockSide side,
        int y,
        int startX,
        int startZ,
        int width,
        int depth,
        GreedyCell cell,
        List<ChunkVertex> vertices,
        List<int> indices)
    {
        var baseIndex = vertices.Count;
        var uvMin = cell.UvMin;
        var uvMax = cell.UvMax;
        var uvSpan = uvMax - uvMin;
        var colorWithDir = new Color(cell.Color.R, cell.Color.G, cell.Color.B, (byte)GetDirectionIndex(side));
        var minX = startX;
        var maxX = startX + width;
        var minZ = startZ;
        var maxZ = startZ + depth;

        if (side == BlockSide.Top)
        {
            float yTop = y + cell.Height;
            vertices.Add(new ChunkVertex(new Vector3(minX, yTop, minZ), colorWithDir, new Vector2(0f, 0f), uvMin, uvSpan, new Vector3(minX, y, minZ)));
            vertices.Add(new ChunkVertex(new Vector3(minX, yTop, maxZ), colorWithDir, new Vector2(0f, depth), uvMin, uvSpan, new Vector3(minX, y, maxZ)));
            vertices.Add(new ChunkVertex(new Vector3(maxX, yTop, maxZ), colorWithDir, new Vector2(width, depth), uvMin, uvSpan, new Vector3(maxX, y, maxZ)));
            vertices.Add(new ChunkVertex(new Vector3(maxX, yTop, minZ), colorWithDir, new Vector2(width, 0f), uvMin, uvSpan, new Vector3(maxX, y, minZ)));
        }
        else
        {
            float yBottom = y;
            vertices.Add(new ChunkVertex(new Vector3(minX, yBottom, maxZ), colorWithDir, new Vector2(0f, 0f), uvMin, uvSpan, new Vector3(minX, y, maxZ)));
            vertices.Add(new ChunkVertex(new Vector3(minX, yBottom, minZ), colorWithDir, new Vector2(0f, depth), uvMin, uvSpan, new Vector3(minX, y, minZ)));
            vertices.Add(new ChunkVertex(new Vector3(maxX, yBottom, minZ), colorWithDir, new Vector2(width, depth), uvMin, uvSpan, new Vector3(maxX, y, minZ)));
            vertices.Add(new ChunkVertex(new Vector3(maxX, yBottom, maxZ), colorWithDir, new Vector2(width, 0f), uvMin, uvSpan, new Vector3(maxX, y, maxZ)));
        }

        indices.Add(baseIndex);
        indices.Add(baseIndex + 1);
        indices.Add(baseIndex + 2);
        indices.Add(baseIndex + 2);
        indices.Add(baseIndex + 3);
        indices.Add(baseIndex);
    }

    private void GenerateVerticalGreedyFaces(
        BlockSide side,
        List<ChunkVertex> vertices,
        List<int> indices,
        ref Texture2D? atlasTexture)
    {
        var width = side is BlockSide.North or BlockSide.South ? ChunkEntity.Size : ChunkEntity.Size;
        var height = ChunkEntity.Height;
        var mask = new GreedyCell?[width, height];
        var axisLength = side is BlockSide.North or BlockSide.South ? ChunkEntity.Size : ChunkEntity.Size;

        for (int axis = 0; axis < axisLength; axis++)
        {
            for (int y = 0; y < height; y++)
            {
                for (int u = 0; u < width; u++)
                {
                    mask[u, y] = null;

                    int x = 0, z = 0;

                    switch (side)
                    {
                        case BlockSide.North:
                            x = u;
                            z = axis;
                            break;
                        case BlockSide.South:
                            x = u;
                            z = axis;
                            break;
                        case BlockSide.East:
                            x = axis;
                            z = u;
                            break;
                        case BlockSide.West:
                            x = axis;
                            z = u;
                            break;
                    }

                    if (!ShouldRenderFace(x, y, z, side))
                    {
                        continue;
                    }

                    var block = _chunk.GetBlock(x, y, z);
                    if (block == null)
                    {
                        continue;
                    }

                    var definition = _blockManagerService.GetBlockDefinition(block.BlockType);
                    if (definition == null || definition.IsBillboard || definition.IsLiquid)
                    {
                        continue;
                    }

                    if (Math.Abs(definition.Height - 1f) > 0.0001f)
                    {
                        AddSolidFace(side, x, y, z, block.BlockType, definition, vertices, indices, ref atlasTexture);
                        continue;
                    }

                    var region = _blockManagerService.GetBlockSide(block.BlockType, side);
                    if (region == null)
                    {
                        continue;
                    }

                    atlasTexture ??= region.Texture;

                    var uv = ExtractUv(region);
                    var color = CalculateFaceColor(x, y, z, side);

                    mask[u, y] = new GreedyCell(block.BlockType, color, uv.Min, uv.Max, definition.Height);
                }
            }

            ProcessVerticalMask(side, axis, mask, vertices, indices);
        }
    }

    private void ProcessVerticalMask(
        BlockSide side,
        int axis,
        GreedyCell?[,] mask,
        List<ChunkVertex> vertices,
        List<int> indices)
    {
        int width = mask.GetLength(0);
        int height = mask.GetLength(1);

        for (int y = 0; y < height; y++)
        {
            int u = 0;
            while (u < width)
            {
                var cell = mask[u, y];
                if (cell == null)
                {
                    u++;
                    continue;
                }

                int span = 1;
                while (u + span < width && mask[u + span, y]?.Equals(cell) == true)
                {
                    span++;
                }

                int vertical = 1;
                bool done = false;
                while (y + vertical < height && !done)
                {
                    for (int du = 0; du < span; du++)
                    {
                        if (mask[u + du, y + vertical]?.Equals(cell) != true)
                        {
                            done = true;
                            break;
                        }
                    }

                    if (!done)
                    {
                        vertical++;
                    }
                }

                AddVerticalQuad(side, axis, u, y, span, vertical, cell.Value, vertices, indices);

                for (int dv = 0; dv < vertical; dv++)
                {
                    for (int du = 0; du < span; du++)
                    {
                        mask[u + du, y + dv] = null;
                    }
                }

                u += span;
            }
        }
    }

    private void AddVerticalQuad(
        BlockSide side,
        int axis,
        int start,
        int startY,
        int span,
        int height,
        GreedyCell cell,
        List<ChunkVertex> vertices,
        List<int> indices)
    {
        var baseIndex = vertices.Count;
        var uvMin = cell.UvMin;
        var uvMax = cell.UvMax;
        var uvSpan = uvMax - uvMin;
        var colorWithDir = new Color(cell.Color.R, cell.Color.G, cell.Color.B, (byte)GetDirectionIndex(side));

        switch (side)
        {
            case BlockSide.North:
            {
                float minX = start;
                float maxX = start + span;
                float minY = startY;
                float maxY = startY + height;
                float z = axis;

                vertices.Add(new ChunkVertex(new Vector3(minX, maxY, z), colorWithDir, new Vector2(0f, height), uvMin, uvSpan, new Vector3(minX, maxY, axis)));
                vertices.Add(new ChunkVertex(new Vector3(minX, minY, z), colorWithDir, new Vector2(0f, 0f), uvMin, uvSpan, new Vector3(minX, minY, axis)));
                vertices.Add(new ChunkVertex(new Vector3(maxX, minY, z), colorWithDir, new Vector2(span, 0f), uvMin, uvSpan, new Vector3(maxX, minY, axis)));
                vertices.Add(new ChunkVertex(new Vector3(maxX, maxY, z), colorWithDir, new Vector2(span, height), uvMin, uvSpan, new Vector3(maxX, maxY, axis)));
                break;
            }
            case BlockSide.South:
            {
                float minX = start;
                float maxX = start + span;
                float minY = startY;
                float maxY = startY + height;
                float z = axis + 1f;

                vertices.Add(new ChunkVertex(new Vector3(maxX, maxY, z), colorWithDir, new Vector2(0f, height), uvMin, uvSpan, new Vector3(maxX, maxY, axis + 1)));
                vertices.Add(new ChunkVertex(new Vector3(maxX, minY, z), colorWithDir, new Vector2(0f, 0f), uvMin, uvSpan, new Vector3(maxX, minY, axis + 1)));
                vertices.Add(new ChunkVertex(new Vector3(minX, minY, z), colorWithDir, new Vector2(span, 0f), uvMin, uvSpan, new Vector3(minX, minY, axis + 1)));
                vertices.Add(new ChunkVertex(new Vector3(minX, maxY, z), colorWithDir, new Vector2(span, height), uvMin, uvSpan, new Vector3(minX, maxY, axis + 1)));
                break;
            }
            case BlockSide.East:
            {
                float x = axis + 1f;
                float minZ = start;
                float maxZ = start + span;
                float minY = startY;
                float maxY = startY + height;

                vertices.Add(new ChunkVertex(new Vector3(x, maxY, minZ), colorWithDir, new Vector2(0f, height), uvMin, uvSpan, new Vector3(axis + 1, maxY, minZ)));
                vertices.Add(new ChunkVertex(new Vector3(x, minY, minZ), colorWithDir, new Vector2(0f, 0f), uvMin, uvSpan, new Vector3(axis + 1, minY, minZ)));
                vertices.Add(new ChunkVertex(new Vector3(x, minY, maxZ), colorWithDir, new Vector2(span, 0f), uvMin, uvSpan, new Vector3(axis + 1, minY, maxZ)));
                vertices.Add(new ChunkVertex(new Vector3(x, maxY, maxZ), colorWithDir, new Vector2(span, height), uvMin, uvSpan, new Vector3(axis + 1, maxY, maxZ)));
                break;
            }
            case BlockSide.West:
            {
                float x = axis;
                float minZ = start;
                float maxZ = start + span;
                float minY = startY;
                float maxY = startY + height;

                vertices.Add(new ChunkVertex(new Vector3(x, maxY, maxZ), colorWithDir, new Vector2(0f, height), uvMin, uvSpan, new Vector3(axis, maxY, maxZ)));
                vertices.Add(new ChunkVertex(new Vector3(x, minY, maxZ), colorWithDir, new Vector2(0f, 0f), uvMin, uvSpan, new Vector3(axis, minY, maxZ)));
                vertices.Add(new ChunkVertex(new Vector3(x, minY, minZ), colorWithDir, new Vector2(span, 0f), uvMin, uvSpan, new Vector3(axis, minY, minZ)));
                vertices.Add(new ChunkVertex(new Vector3(x, maxY, minZ), colorWithDir, new Vector2(span, height), uvMin, uvSpan, new Vector3(axis, maxY, minZ)));
                break;
            }
        }

        indices.Add(baseIndex);
        indices.Add(baseIndex + 1);
        indices.Add(baseIndex + 2);
        indices.Add(baseIndex + 2);
        indices.Add(baseIndex + 3);
        indices.Add(baseIndex);
    }

    private void AddSolidFace(
        BlockSide side,
        int x,
        int y,
        int z,
        BlockType blockType,
        BlockDefinitionData? definition,
        List<ChunkVertex> vertices,
        List<int> indices,
        ref Texture2D? atlasTexture)
    {
        if (definition == null)
        {
            return;
        }

        var region = _blockManagerService.GetBlockSide(blockType, side);
        if (region == null)
        {
            return;
        }

        atlasTexture ??= region.Texture;

        var uv = ExtractUv(region);
        var faceColor = CalculateFaceColor(x, y, z, side);
        var faceVertices = GetFaceVertices(side, x, y, z, uv, faceColor, definition.Height);

        var baseIndex = vertices.Count;
        vertices.AddRange(faceVertices);
        indices.AddRange(
            new[]
            {
                baseIndex,
                baseIndex + 1,
                baseIndex + 2,
                baseIndex + 2,
                baseIndex + 3,
                baseIndex
            }
        );
    }

    private readonly record struct GreedyCell(
        BlockType BlockType,
        Color Color,
        Vector2 UvMin,
        Vector2 UvMax,
        float Height);

    private void UpdateLightTexture()
    {
        if (_chunk == null)
        {
            return;
        }

        var expectedCount = ChunkEntity.Size * ChunkEntity.Height * ChunkEntity.Size;

        if (_lightTexture == null ||
            _lightTexture.Width != ChunkEntity.Size ||
            _lightTexture.Height != ChunkEntity.Height ||
            _lightTexture.Depth != ChunkEntity.Size)
        {
            _lightTexture?.Dispose();
            _lightTexture = new Texture3D(
                _graphicsDevice,
                ChunkEntity.Size,
                ChunkEntity.Height,
                ChunkEntity.Size,
                false,
                SurfaceFormat.Color
            );

            _lightData = new Color[expectedCount];
        }

        if (_lightData == null || _lightData.Length != expectedCount)
        {
            _lightData = new Color[expectedCount];
        }

        var data = _lightData;
        if (data == null)
        {
            return;
        }

        var lightLevels = _chunk.LightLevels;
        for (int i = 0; i < lightLevels.Length && i < data.Length; i++)
        {
            float normalized = Math.Clamp(lightLevels[i] / 15f, 0f, 1f);
            byte value = (byte)(normalized * 255f);
            data[i] = new Color(value, value, value, (byte)255);
        }

        _lightTexture!.SetData(data);
    }

    // Object pool policies for mesh data
    private class ChunkVertexListPolicy : IPooledObjectPolicy<List<ChunkVertex>>
    {
        public List<ChunkVertex> Create()
        {
            return new List<ChunkVertex>(16384); // Pre-allocate capacity for typical chunk
        }

        public bool Return(List<ChunkVertex> obj)
        {
            obj.Clear();
            return true;
        }
    }

    private class BillboardVertexListPolicy : IPooledObjectPolicy<List<VertexPositionColorTexture>>
    {
        public List<VertexPositionColorTexture> Create()
        {
            return new List<VertexPositionColorTexture>(4096);
        }

        public bool Return(List<VertexPositionColorTexture> obj)
        {
            obj.Clear();
            return true;
        }
    }

    private class IndexListPolicy : IPooledObjectPolicy<List<int>>
    {
        public List<int> Create()
        {
            return
                new List<int>(
                    24576
                );
        }

        public bool Return(List<int> obj)
        {
            obj.Clear();
            return true;
        }
    }

    private class FluidVertexListPolicy : IPooledObjectPolicy<List<VertexPositionColorTextureDirectionTop>>
    {
        public List<VertexPositionColorTextureDirectionTop> Create()
        {
            return new List<VertexPositionColorTextureDirectionTop>(16384);
        }

        public bool Return(List<VertexPositionColorTextureDirectionTop> obj)
        {
            obj.Clear();
            return true;
        }
    }

    // GPU upload thread management
    private static void StartGpuUploadThread()
    {
        if (_gpuUploadThread != null) return;

        _gpuUploadThread = new Thread(() =>
            {
                while (true)
                {
                    if (_gpuUploadQueue.TryDequeue(out var meshData))
                    {
                        UploadMeshToGpuAsync(meshData);
                    }
                    else
                    {
                        _gpuUploadSignal.WaitOne(1); // Wait for new work or timeout
                    }
                }
            }
        )
        {
            IsBackground = true,
            Name = "GPU Upload Thread"
        };

        _gpuUploadThread.Start();
    }

    // Simple greedy meshing optimization - skip faces between same block types
    private bool ShouldSkipFace(int x, int y, int z, BlockSide side, BlockType currentBlockType)
    {
        if (_chunk == null) return false;

        // Check if the neighboring block is the same type
        var (offsetX, offsetY, offsetZ) = side switch
        {
            BlockSide.Top => (0, 1, 0),
            BlockSide.Bottom => (0, -1, 0),
            BlockSide.North => (0, 0, -1),
            BlockSide.South => (0, 0, 1),
            BlockSide.East => (1, 0, 0),
            BlockSide.West => (-1, 0, 0),
            _ => (0, 0, 0)
        };

        var neighborX = x + offsetX;
        var neighborY = y + offsetY;
        var neighborZ = z + offsetZ;

        // Check bounds
        if (neighborX < 0 || neighborX >= ChunkEntity.Size ||
            neighborY < 0 || neighborY >= ChunkEntity.Height ||
            neighborZ < 0 || neighborZ >= ChunkEntity.Size)
        {
            // Check cross-chunk neighbor if delegate is available
            if (GetNeighborChunk != null)
            {
                var chunkPos = new Vector3(
                    (int)(_chunk.Position.X / ChunkEntity.Size),
                    0,
                    (int)(_chunk.Position.Z / ChunkEntity.Size)
                );

                var neighborChunkPos = chunkPos + new Vector3(
                    neighborX < 0 ? -1 :
                    neighborX >= ChunkEntity.Size ? 1 : 0,
                    0,
                    neighborZ < 0 ? -1 :
                    neighborZ >= ChunkEntity.Size ? 1 : 0
                );

                var neighborChunk = GetNeighborChunk(neighborChunkPos);
                if (neighborChunk != null)
                {
                    var localX = neighborX < 0 ? ChunkEntity.Size - 1 :
                        neighborX >= ChunkEntity.Size ? 0 : neighborX;
                    var localZ = neighborZ < 0 ? ChunkEntity.Size - 1 :
                        neighborZ >= ChunkEntity.Size ? 0 : neighborZ;

                    if (neighborY < 0 || neighborY >= ChunkEntity.Height)
                    {
                        return false;
                    }

                    var crossChunkBlock = neighborChunk.Blocks[ChunkEntity.GetIndex(localX, neighborY, localZ)];
                    return crossChunkBlock?.BlockType == currentBlockType;
                }
            }

            return false; // Edge of world, render face
        }

        var neighborBlock = _chunk.Blocks[ChunkEntity.GetIndex(neighborX, neighborY, neighborZ)];
        return neighborBlock?.BlockType == currentBlockType;
    }

    private static void UploadMeshToGpuAsync(MeshData meshData)
    {
        // This method runs on the GPU upload thread
        // Note: In MonoGame, GPU operations should be done on the main thread
        // This is a placeholder - actual implementation would need to coordinate with main thread

        // For now, we'll just simulate the work
        // In a real implementation, this would:
        // 1. Create vertex and index buffers
        // 2. Upload data to GPU
        // 3. Signal completion back to main thread

        Thread.Sleep(1); // Simulate GPU upload time
    }
}
