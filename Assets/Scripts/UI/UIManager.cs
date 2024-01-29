using System;
using TMPro;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.UI;

namespace WebRTCTutorial.UI
{
    public class UIManager : MonoBehaviour
    {
#if UNITY_EDITOR
        // Called by Unity https://docs.unity3d.com/ScriptReference/MonoBehaviour.OnValidate.html
        protected void OnValidate()
        {
            try
            {
                // Validate that all references are connected
                Assert.IsNotNull(_peerViewA);
                Assert.IsNotNull(_peerViewB);
                Assert.IsNotNull(_cameraDropdown);
                Assert.IsNotNull(_connectButton);
                Assert.IsNotNull(_disconnectButton);
            }
            catch (Exception)
            {
                Debug.LogError(
                    $"Some of the references are NULL, please inspect the {nameof(UIManager)} script on this object",
                    this);
            }
        }
#endif

        protected void Awake()
        {
            // Usually better to avoid FindObjectOfType in real production project due to performance 
            _videoManager = FindObjectOfType<VideoManager>();

            // Check if there's any camera device available
            if (WebCamTexture.devices.Length == 0)
            {
                Debug.LogError(
                    "No Camera devices available! Please make sure a camera device is detected and accessible by Unity. " +
                    "This demo application will not work without a camera device.");
            }

            // Clear default options from the dropdown
            _cameraDropdown.ClearOptions();

            // Populate dropdown with the available camera devices
            foreach (var cameraDevice in WebCamTexture.devices)
            {
                _cameraDropdown.options.Add(new TMP_Dropdown.OptionData(cameraDevice.name));
            }

            // Change the active camera device when new dropdown value is selected
            _cameraDropdown.onValueChanged.AddListener(SetActiveCamera);

            // Enable first camera from the dropdown
            SetActiveCamera(deviceIndex: 0);
            
            // Subscribe to when video from the other peer is received
            _videoManager.RemoteVideoReceived += OnRemoteVideoReceived;
        }

        [SerializeField]
        private PeerView _peerViewA;

        [SerializeField]
        private PeerView _peerViewB;

        [SerializeField]
        private TMP_Dropdown _cameraDropdown;

        [SerializeField]
        private Button _connectButton;

        [SerializeField]
        private Button _disconnectButton;

        private WebCamTexture _activeCamera;

        private VideoManager _videoManager;

        private void SetActiveCamera(int deviceIndex)
        {
            var deviceName = _cameraDropdown.options[deviceIndex].text;

            // Stop previous camera capture
            if (_activeCamera != null && _activeCamera.isPlaying)
            {
                _activeCamera.Stop();
            }

            /* Depending on the platform you're targeting you may need to request permission to access the camera device:
                - IOS or WebGL -> https://docs.unity3d.com/2022.3/Documentation/ScriptReference/Application.RequestUserAuthorization.html
                - Android -> https://docs.unity3d.com/Manual/android-RequestingPermissions.html
             */

            var (width, height) = GetVideoTextureResolution();
            _activeCamera = new WebCamTexture(deviceName, width, height, requestedFPS: 30);
            _activeCamera.Play();
            
            // Set preview of the local peer
            _peerViewA.SetVideoTexture(_activeCamera);

            // Notify Video Manager about new active camera device
            _videoManager.SetActiveCamera(_activeCamera);
        }

        /// <summary>
        /// For this demo we'll just take half of the screen resolution
        /// </summary>
        private static (int width, int height) GetVideoTextureResolution()
        {
            switch (Screen.orientation)
            {
                // For portrait mode (like Mobile phones) we take Screen's width and height in reversed order because the device is tilted to the side
                case ScreenOrientation.Portrait:
                case ScreenOrientation.PortraitUpsideDown:
                    return (Screen.height / 2, Screen.width / 2);
                case ScreenOrientation.LandscapeLeft:
                case ScreenOrientation.LandscapeRight:
                case ScreenOrientation.AutoRotation:
                default:
                    return (Screen.width / 2, Screen.width / 2);
            }
        }
        
        private void OnRemoteVideoReceived(Texture texture)
        {
            _peerViewB.SetVideoTexture(texture);
        }
    }
}