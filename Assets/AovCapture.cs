using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Experimental.Rendering.HDPipeline;
using UnityEngine.Experimental.Rendering.HDPipeline.Attributes;
using RTHandle = UnityEngine.Experimental.Rendering.RTHandleSystem.RTHandle;

[ExecuteAlways]
[RequireComponent(typeof(HDAdditionalCameraData))]
public class AovCapture : MonoBehaviour
{
    #region Nested classes

    public enum EdgeSource { Color, Depth, Normal }

    #endregion

    #region Editable attributes

    [SerializeField] Color _edgeColor = Color.black;
    [SerializeField] EdgeSource _edgeSource = EdgeSource.Color;
    [SerializeField, Range(0, 1)] float _edgeThreshold = 0.5f;
    [SerializeField, Range(0, 1)] float _edgeContrast = 0.5f;
    [SerializeField] Gradient _fillGradient = null;
    [SerializeField, Range(0, 1)] float _fillOpacity = 0;
    [SerializeField] RenderTexture _targetTexture = null;
    [SerializeField, HideInInspector] Shader _shader = null;

    #endregion

    #region Public properties

    public Color edgeColor {
        get { return _edgeColor; }
        set { _edgeColor = value; }
    }

    public EdgeSource edgeSource {
        get { return _edgeSource; }
        set { _edgeSource = value; }
    }

    public float edgeThreshold {
        get { return _edgeThreshold; }
        set { _edgeThreshold = value; }
    }

    public float edgeContrast {
        get { return _edgeContrast; }
        set { _edgeContrast = value; }
    }

    public Gradient fillGradient {
        get { return _fillGradient; }
        set { _fillGradient = value; }
    }

    public float fillOpacity {
        get { return _fillOpacity; }
        set { _fillOpacity = value; }
    }

    public RenderTexture targetTexture {
        get { return _targetTexture; }
        set { _targetTexture = value; }
    }

    #endregion

    #region Shader property IDs

    static readonly (
        int ColorTexture,
        int NormalTexture,
        int DepthTexture,
        int EdgeColor,
        int EdgeThresholds,
        int FillOpacity
    ) _ID = (
        Shader.PropertyToID("_ColorTexture"),
        Shader.PropertyToID("_NormalTexture"),
        Shader.PropertyToID("_DepthTexture"),
        Shader.PropertyToID("_EdgeColor"),
        Shader.PropertyToID("_EdgeThresholds"),
        Shader.PropertyToID("_FillOpacity")
    );

    #endregion

    #region Private variables and properties

    Material _material;
    MaterialPropertyBlock _sheet;
    GradientColorKey [] _gradientCache;
    (RTHandle color, RTHandle normal, RTHandle depth) _rtHandles;

    Vector2 EdgeThresholdVector {
        get {
            if (_edgeSource == EdgeSource.Depth)
            {
                var thresh = 1 / Mathf.Lerp(1000, 1, _edgeThreshold);
                var scaler = 1 + 2 / (1.01f - _edgeContrast);
                return new Vector2(thresh, thresh * scaler);
            }
            else // Depth & Color
            {
                var thresh = _edgeThreshold;
                return new Vector2(thresh, thresh + 1.01f - _edgeContrast);
            }
        }
    }

    #endregion

    #region AOV request callbacks

    RTHandle RTHandleAllocator(AOVBuffers bufferID)
    {
        if (bufferID == AOVBuffers.Color)
            return _rtHandles.color ??
                (_rtHandles.color = RTHandles.Alloc(
                    _targetTexture.width, _targetTexture.height, 1,
                    DepthBits.None, GraphicsFormat.R8G8B8A8_SRGB));

        if (bufferID == AOVBuffers.Normals)
            return _rtHandles.normal ??
                (_rtHandles.normal = RTHandles.Alloc(
                    _targetTexture.width, _targetTexture.height, 1,
                    DepthBits.None, GraphicsFormat.R8G8B8A8_UNorm));

        // bufferID == AOVBuffers.Depth
        return _rtHandles.depth ??
            (_rtHandles.depth = RTHandles.Alloc(
                _targetTexture.width, _targetTexture.height, 1,
                DepthBits.None, GraphicsFormat.R32_SFloat));
    }

    void AovCallback(
        CommandBuffer cmd,
        List<RTHandle> buffers,
        RenderOutputProperties properties
    )
    {
        // Shader material
        if (_material == null)
        {
            _material = new Material(_shader);
            _material.hideFlags = HideFlags.DontSave;
        }

        if (_sheet == null)
            _sheet = new MaterialPropertyBlock();

        // AOV buffers
        _sheet.SetTexture(_ID. ColorTexture, buffers[0]);
        _sheet.SetTexture(_ID.NormalTexture, buffers[1]);
        _sheet.SetTexture(_ID. DepthTexture, buffers[2]);

        // Shader properties
        _sheet.SetColor(_ID.EdgeColor, _edgeColor);
        _sheet.SetVector(_ID.EdgeThresholds, EdgeThresholdVector);
        _sheet.SetFloat(_ID.FillOpacity, _fillOpacity);
        GradientUtility.SetColorKeys(_sheet, _gradientCache);

        // Shader pass selection
        var pass = (int)_edgeSource;
        if (_fillOpacity > 0 && _gradientCache.Length > 3) pass += 3;
        if (_fillGradient.mode == GradientMode.Blend) pass += 6;

        UnityEngine.Rendering.CoreUtils.DrawFullScreen(
            cmd, _material, _targetTexture, _sheet, pass);
    }

    #endregion

    #region MonoBehaviour implementation

    void OnEnable()
    {
        // Do nothing if no target is given.
        if (_targetTexture == null) return;

        // AOV request
        GetComponent<HDAdditionalCameraData>().SetAOVRequests(
            new AOVRequestBuilder().Add(
                AOVRequest.@default,
                RTHandleAllocator,
                null,
                new [] {
                    AOVBuffers.Color,
                    AOVBuffers.Normals,
                    AOVBuffers.DepthStencil
                },
                AovCallback
            ).Build()
        );
    }

    void OnDisable()
    {
        GetComponent<HDAdditionalCameraData>().SetAOVRequests(null);
    }

    void OnValidate()
    {
        OnDisable();
        OnEnable();
    }

    void OnDestroy()
    {
        if (_material != null)
        {
            if (Application.isPlaying)
                Destroy(_material);
            else
                DestroyImmediate(_material);
            _material = null;
        }
    }

    #if !UNITY_EDITOR

    void Start()
    {
        // At runtime, copy gradient color keys only once on initialization.
        _gradientCache = _fillGradient.colorKeys;
    }

    #endif

    #if UNITY_EDITOR

    void LateUpdate()
    {
        // In editor, copy gradient color keys every frame.
        _gradientCache = _fillGradient.colorKeys;
    }

    #endif

    #endregion
}
