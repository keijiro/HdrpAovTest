using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Experimental.Rendering.HDPipeline;

class AovTest : MonoBehaviour
{
    [SerializeField] RenderTexture _output = null;

    RTHandleSystem.RTHandle _rtHandle;

    void Start()
    {
        _rtHandle = RTHandles.Alloc(_output.width, _output.height);

        var request = new AOVRequest(AOVRequest.@default);
        request.SetFullscreenOutput(DebugFullScreen.Depth);

        GetComponent<HDAdditionalCameraData>().SetAOVRequests(
            new AOVRequestBuilder().Add(
                request,
                bufferID => _rtHandle,
                null,
                new [] { AOVBuffers.DepthStencil },
                (cmd, textures, properties) => { cmd.Blit(textures[0], _output); }
            ).Build()
        );
    }
}
