using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Experimental.Rendering.HDPipeline;
using UnityEngine.Experimental.Rendering.HDPipeline.Attributes;

[ExecuteAlways]
[RequireComponent(typeof(HDAdditionalCameraData))]
class AovCapture : MonoBehaviour
{
    [SerializeField] RenderTexture _targetRT = null;
    [SerializeField, HideInInspector] Shader _shader = null;

    Material _material;

    RTHandleSystem.RTHandle _colorRT;
    RTHandleSystem.RTHandle _normalRT;
    RTHandleSystem.RTHandle _depthRT;

    void OnEnable()
    {
        if (_targetRT == null) return;

        var dims = (x:_targetRT.width, y:_targetRT.height);

        _colorRT = RTHandles.Alloc(
            dims.x, dims.y, 1, DepthBits.None,
            GraphicsFormat.R8G8B8A8_SRGB
        );

        _normalRT = RTHandles.Alloc(
            dims.x, dims.y, 1, DepthBits.None,
            GraphicsFormat.R16G16B16A16_SFloat
        );

        _depthRT = RTHandles.Alloc(
            dims.x, dims.y, 1, DepthBits.None,
            GraphicsFormat.R32_SFloat
        );

        GetComponent<HDAdditionalCameraData>().SetAOVRequests(
            new AOVRequestBuilder().Add(
                AOVRequest.@default,
                bufferID =>
                    bufferID == AOVBuffers.Color ? _colorRT :
                    (bufferID == AOVBuffers.Normals ? _normalRT : _depthRT),
                null,
                new [] {
                    AOVBuffers.Color,
                    AOVBuffers.Normals,
                    AOVBuffers.DepthStencil
                },
                (cmd, textures, properties) => {
                    if (_material == null)
                    {
                        _material = new Material(_shader);
                        _material.hideFlags = HideFlags.DontSave;
                    }
                    cmd.SetGlobalTexture("_NormalTex", textures[1]);
                    cmd.SetGlobalTexture("_DepthTex", textures[2]);
                    cmd.Blit(textures[0], _targetRT, _material);
                }
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
}
