using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Experimental.Rendering.HDPipeline;
using UnityEngine.Experimental.Rendering.HDPipeline.Attributes;

[ExecuteAlways]
[RequireComponent(typeof(HDAdditionalCameraData))]
class AovCapture : MonoBehaviour
{
    [SerializeField] RenderTexture _normalTarget = null;
    [SerializeField] RenderTexture _depthTarget = null;

    RTHandleSystem.RTHandle _normalRT;
    RTHandleSystem.RTHandle _depthRT;

    void OnEnable()
    {
        if (_normalTarget == null || _depthTarget == null) return;

        _normalRT = RTHandles.Alloc(
            _normalTarget.width, _normalTarget.height, 1,
            DepthBits.None, _normalTarget.graphicsFormat
        );

        _depthRT = RTHandles.Alloc(
            _depthTarget.width, _depthTarget.height, 1,
            DepthBits.None, _depthTarget.graphicsFormat
        );

        var request = new AOVRequest(AOVRequest.@default);
        request.SetFullscreenOutput(MaterialSharedProperty.Normal);

        GetComponent<HDAdditionalCameraData>().SetAOVRequests(
            new AOVRequestBuilder().Add(
                request,
                bufferID =>
                    bufferID == AOVBuffers.Color ? _normalRT : _depthRT,
                null,
                new [] { AOVBuffers.Color, AOVBuffers.DepthStencil },
                (cmd, textures, properties) => {
                    cmd.Blit(textures[0], _normalTarget);
                    cmd.Blit(textures[1], _depthTarget);
                }
            ).Build()
        );
    }

    void OnDisable()
    {
        GetComponent<HDAdditionalCameraData>().SetAOVRequests(null);
    }
}
